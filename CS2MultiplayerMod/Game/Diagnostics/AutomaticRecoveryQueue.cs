using System;

namespace CS2MultiplayerMod.Game.Diagnostics
{
    /// <summary>
    /// Coalesces settled recovery reports while automatic world sync is cooling down. A caller may
    /// have discarded its only copy of an edit, so delaying a reload must not discard the report too.
    /// Main thread only.
    /// </summary>
    internal sealed class AutomaticRecoveryQueue
    {
        private readonly long _cooldownMs;
        private long _lastRequestMs = long.MinValue;
        private long _worldGeneration;
        private ResyncReport _pending;

        public AutomaticRecoveryQueue(long cooldownMs)
        {
            if (cooldownMs < 0) throw new ArgumentOutOfRangeException(nameof(cooldownMs));
            _cooldownMs = cooldownMs;
        }

        /// <summary>First report wins: one authoritative snapshot answers the coalesced reports.</summary>
        public bool Enqueue(ResyncReport report, long worldGeneration)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            ObserveWorld(worldGeneration);
            if (_pending != null) return false;
            _pending = report;
            return true;
        }

        /// <summary>
        /// A pause or an aborted transfer does not replace this city. Only a newly installed snapshot
        /// supersedes its pending report; it does not lift the automatic-recovery rate limit.
        /// </summary>
        public bool ObserveWorld(long worldGeneration)
        {
            if (_worldGeneration == worldGeneration) return false;
            _worldGeneration = worldGeneration;
            _pending = null;
            return true;
        }

        public bool TryTake(long nowMs, bool connected, bool recoveryInFlight,
            long worldGeneration, out ResyncReport report)
        {
            report = null;
            ObserveWorld(worldGeneration);
            if (!connected)
            {
                Reset(worldGeneration);
                return false;
            }
            if (_pending == null || recoveryInFlight) return false;
            if (IsCoolingDown(nowMs)) return false;

            report = _pending;
            _pending = null;
            _lastRequestMs = nowMs;
            return true;
        }

        // Subtracting the sentinel would overflow on the first request.
        public bool IsCoolingDown(long nowMs) =>
            _lastRequestMs != long.MinValue && nowMs - _lastRequestMs < _cooldownMs;

        /// <summary>A different session must not inherit either the report or the cooldown.</summary>
        public void Reset(long worldGeneration)
        {
            _pending = null;
            _lastRequestMs = long.MinValue;
            _worldGeneration = worldGeneration;
        }
    }
}
