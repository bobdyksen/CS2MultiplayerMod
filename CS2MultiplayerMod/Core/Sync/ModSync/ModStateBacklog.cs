using System.Collections.Generic;

namespace CS2MultiplayerMod.Core.Sync.ModSync
{
    /// <summary>
    /// Distinct mod-state closures waiting to be applied. Keyed by carrier, so a repeated update
    /// replaces that carrier's snapshot instead of occupying another slot. The active set stays
    /// at <see cref="ActiveLimit"/>; a burst may sit at <see cref="HardLimit"/> until the frame's
    /// apply pass has had a chance to retire the ones it can write.
    ///
    /// Shedding is not silent. The newest closure is the one kept, the oldest is the one removed,
    /// and <see cref="ConsumeRecovery"/> is true once per episode so the caller can ask for a
    /// single world recovery. Later sheds in the same episode do not ask again.
    /// </summary>
    public sealed class ModStateBacklog<T>
    {
        public const int ActiveLimit = 256;

        /// <summary>Matches the sync inbox cap, so one drained burst cannot grow without bound.</summary>
        public const int HardLimit = 1024;

        private readonly int _activeLimit;
        private readonly int _hardLimit;
        private readonly Dictionary<string, T> _items = new Dictionary<string, T>(System.StringComparer.Ordinal);
        private readonly List<string> _order = new List<string>();
        private bool _recoveryNeeded;
        private bool _recoverySent;

        public ModStateBacklog() : this(ActiveLimit, HardLimit) { }

        public ModStateBacklog(int activeLimit, int hardLimit)
        {
            if (activeLimit < 1) throw new System.ArgumentOutOfRangeException(nameof(activeLimit));
            if (hardLimit < activeLimit) throw new System.ArgumentOutOfRangeException(nameof(hardLimit));
            _activeLimit = activeLimit;
            _hardLimit = hardLimit;
        }

        public int Count { get { return _order.Count; } }

        public bool TryGet(string key, out T value)
        {
            return _items.TryGetValue(key, out value);
        }

        public void CopyKeys(List<string> into)
        {
            into.Clear();
            into.AddRange(_order);
        }

        /// <summary>
        /// Stores <paramref name="value"/> for <paramref name="key"/>. An existing key is replaced
        /// and moved to the newest end, so a fresh update is not the first thing shed.
        /// </summary>
        public void Admit(string key, T value)
        {
            T ignored;
            if (_items.TryGetValue(key, out ignored))
            {
                _items[key] = value;
                NoteUpdated(key);
                return;
            }

            while (_order.Count >= _hardLimit)
            {
                RemoveOldest();
                _recoveryNeeded = true;
            }
            _items.Add(key, value);
            _order.Add(key);
        }

        /// <summary>A repeated update stays newest without replacing the caller's object.</summary>
        public void NoteUpdated(string key)
        {
            int index = _order.IndexOf(key);
            if (index < 0 || index == _order.Count - 1) return;
            _order.RemoveAt(index);
            _order.Add(key);
        }

        public bool Remove(string key)
        {
            if (!_items.Remove(key)) return false;
            _order.Remove(key);
            return true;
        }

        /// <summary>
        /// After apply, shrink to the active limit. Oldest first, which is the closure that has
        /// been waiting the longest. Returns whether this shed should raise the one recovery.
        /// </summary>
        public bool TrimExcess()
        {
            bool shed = false;
            while (_order.Count > _activeLimit)
            {
                RemoveOldest();
                shed = true;
            }
            if (shed) _recoveryNeeded = true;
            return ConsumeRecovery();
        }

        /// <summary>True once when something unapplied was discarded, then quiet until <see cref="Clear"/>.</summary>
        public bool ConsumeRecovery()
        {
            if (!_recoveryNeeded || _recoverySent) return false;
            _recoverySent = true;
            return true;
        }

        public void Clear()
        {
            _items.Clear();
            _order.Clear();
            _recoveryNeeded = false;
            _recoverySent = false;
        }

        private void RemoveOldest()
        {
            string key = _order[0];
            _order.RemoveAt(0);
            _items.Remove(key);
        }
    }
}
