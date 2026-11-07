using System.Collections.Generic;
using CS2MultiplayerMod.Core.Diagnostics;
using CS2MultiplayerMod.Core.Protocol.Messages;
using CS2MultiplayerMod.Core.Session;
using CS2MultiplayerMod.Core.Sync.ModSync;
using CS2MultiplayerMod.Game.Diagnostics;
using CS2MultiplayerMod.Game.Sync.Commands;
using CS2MultiplayerMod.Game.Sync.Infrastructure;
using CS2MultiplayerMod.Game.Sync.ModSync;
using Unity.Entities;

namespace CS2MultiplayerMod.Game.Sync.Systems.Mods
{
    public partial class ModStateSyncSystem
    {
        private const int ComplaintThrottleMs = 10000;

        /// <summary>One carrier's most recent closure, and how long it has been waiting.</summary>
        private sealed class Held
        {
            public ModStateSnapshot Snapshot;
            public long FirstSeenMs;
            public string LastDetail;
            public int WaitedFrames;
            public bool NotedUnresolved;
        }

        /// <summary>
        /// Closures waiting for something they name. Latest per carrier, bounded, and a shed
        /// asks once for the host world instead of forgetting that carrier forever.
        /// </summary>
        private readonly ModStateBacklog<Held> _held = new ModStateBacklog<Held>();

        private readonly List<string> _finished = new List<string>();
        private readonly List<string> _scanKeys = new List<string>();
        private ModClosureApply _apply;
        private ModTypeBinding _applyBinding;
        private readonly Dictionary<string, ModEntityRef> _awaitingSettle =
            new Dictionary<string, ModEntityRef>(System.StringComparer.Ordinal);
        private readonly Dictionary<string, int> _lastComplaintTick =
            new Dictionary<string, int>(System.StringComparer.Ordinal);

        private void RealizeIncoming(MultiplayerSession session, long now)
        {
            Receive(session, now);
            if (ModSyncFeature.ApplyReceived && _held.Count > 0)
                ApplyHeld(session, now);
            // Shed after apply, so a burst that can be written this frame is not discarded
            // to make room, and a hard-limit shed during receive still asks exactly once.
            if (_held.TrimExcess()) RequestBacklogRecovery();
        }

        private void ApplyHeld(MultiplayerSession session, long now)
        {
            if (_apply == null || _applyBinding != _binding)
            {
                _apply = new ModClosureApply(EntityManager, _identity, _binding, _catalog);
                _applyBinding = _binding;
            }

            _finished.Clear();
            _held.CopyKeys(_scanKeys);
            for (int scan = 0; scan < _scanKeys.Count; scan++)
            {
                string key = _scanKeys[scan];
                Held held;
                if (!_held.TryGet(key, out held)) continue;
                KeyValuePair<string, Held> pair = new KeyValuePair<string, Held>(key, held);
                ModApplyOutcome outcome;
                try
                {
                    outcome = _apply.Apply(held.Snapshot);
                }
                catch (System.Exception ex)
                {
                    // One bad closure must not cost every other carrier its edit this frame.
                    Complain(pair.Key, "Could not apply mod state at " + pair.Key + ".", ex);
                    _finished.Add(pair.Key);
                    continue;
                }

                if (outcome == ModApplyOutcome.Applied)
                {
                    _finished.Add(pair.Key);
                    _appliedTransactions++;
                    SyncLog.Trace(LogTopic.ModSync, "mod state applied " + pair.Key + " (" +
                        held.Snapshot.CarrierValues.Components.Count + " type(s), " +
                        held.Snapshot.Satellites.Count + " satellite(s))" +
                        (held.WaitedFrames > 0 ? " after waiting " + held.WaitedFrames + " frame(s)" : ""));
                    Settle(session, held.Snapshot.Carrier, pair.Key);
                    continue;
                }

                if (outcome == ModApplyOutcome.TypeMissing)
                {
                    // Waiting cannot help: this machine has nothing to write that type into.
                    Complain(pair.Key, "Mod state at " + pair.Key + " names a type this game " +
                        "does not have (" + _apply.Detail + ").", null);
                    _finished.Add(pair.Key);
                    continue;
                }

                if (held.WaitedFrames++ == 0)
                    SyncLog.Trace(LogTopic.ModSync, "mod state waiting at " + pair.Key + ": " +
                        (outcome == ModApplyOutcome.CarrierMissing
                            ? "no such object here yet"
                            : "waiting for " + _apply.Detail));

                held.LastDetail = _apply.Detail;
                if (now - held.FirstSeenMs < ModSyncFeature.UnresolvedHoldMs) continue;

                // Still the latest closure for this carrier, so it stays queued and is retried.
                // Forgetting it here is how a road that appears a moment later never receives the
                // edit: the sender already recorded the send and will not repeat it. If the set
                // has to shrink, TrimExcess sheds the oldest and asks once for the host world.
                if (held.NotedUnresolved) continue;
                held.NotedUnresolved = true;
                _unresolvedGaveUp++;
                Complain(pair.Key, "Mod state still unresolved at " + pair.Key + " after " +
                    (ModSyncFeature.UnresolvedHoldMs / 1000) + "s: " +
                    (outcome == ModApplyOutcome.CarrierMissing
                        ? "this machine has no such object yet"
                        : "it refers to " + held.LastDetail + ", which is not here yet"), null);
            }

            for (int i = 0; i < _finished.Count; i++) _held.Remove(_finished[i]);
        }

        private static void RequestBacklogRecovery()
        {
            SyncInbox.RequestResync(ResyncReport
                .Create("mod state backlog shed an unapplied closure", "modsync",
                    ResyncEvidence.StreamLoss)
                .About("mod state")
                .Tried("kept the newest closures and asked once for the host world"));
        }

        private void Receive(MultiplayerSession session, long now)
        {
            while (_incomingState.TryDequeue(out SimulationCommandMessage message))
            {
                if (message.OriginPlayerId == session.LocalPlayerId) continue;

                ModStateCommand command;
                try
                {
                    command = ModStateCommand.Decode(message.Body, _binding);
                }
                catch (System.Exception ex)
                {
                    SyncLog.Warn(LogTopic.ModSync,
                        "Dropping a malformed mod state command: " + ex.Message);
                    continue;
                }

                string key = command.Snapshot.Carrier.Key();
                SyncLog.Trace(LogTopic.ModSync, "mod state received " + key + " from player " +
                    message.OriginPlayerId + " (" + (message.Body == null ? 0 : message.Body.Length) +
                    " bytes, " + command.Snapshot.CarrierValues.Components.Count + " type(s), " +
                    command.Snapshot.Satellites.Count + " satellite(s))");

                if (_held.TryGet(key, out Held existing))
                {
                    existing.Snapshot = command.Snapshot;
                    existing.FirstSeenMs = now;
                    existing.WaitedFrames = 0;
                    existing.NotedUnresolved = false;
                    _held.NoteUpdated(key);
                    continue;
                }

                _held.Admit(key, new Held { Snapshot = command.Snapshot, FirstSeenMs = now });
            }
        }

        /// <summary>
        /// After applying a closure. The host republishes the result to everyone, the sender included, so
        /// two simultaneous edits converge on the host's answer. A client records it so it is not echoed.
        /// </summary>
        private void Settle(MultiplayerSession session, ModEntityRef carrierRef, string key) =>
            _awaitingSettle[key] = carrierRef;

        private void SettleApplied(MultiplayerSession session)
        {
            foreach (KeyValuePair<string, ModEntityRef> pair in _awaitingSettle)
            {
                if (!_identity.TryResolve(pair.Value, out Entity carrier)) continue;

                // Resolve with the sender's position but record what local capture will read, or it echoes.
                if (!_identity.TryDescribe(carrier, out ModEntityRef localRef)) continue;
                if (!TryEncode(carrier, localRef, out byte[] body, out ulong hash)) continue;

                string localKey = localRef.Key();
                _shadow[localKey] = hash;
                RememberCarrier(localKey, localRef);

                // An empty closure no longer matches the capture query; publish removals explicitly.
                if (session.Role != SessionRole.Host || !ModSyncFeature.SendCaptured) continue;
                session.SendCommand(0, ModStateCommand.Id, body);
                _sentBytes += body.Length;
                SyncLog.Trace(LogTopic.ModSync, "mod state settled " + localKey + " (" +
                    body.Length + " bytes)");
            }
            _awaitingSettle.Clear();
        }

        /// <summary>One complaint per carrier per ten seconds.</summary>
        private void Complain(string key, string message, System.Exception ex)
        {
            int now = System.Environment.TickCount;
            if (_lastComplaintTick.TryGetValue(key, out int last) &&
                unchecked(now - last) < ComplaintThrottleMs) return;
            _lastComplaintTick[key] = now;

            if (ex != null) SyncLog.Error(LogTopic.ModSync, message, ex);
            else SyncLog.Warn(LogTopic.ModSync, message);
        }
    }
}
