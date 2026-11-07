using System;
using CS2MultiplayerMod.Core.Sync.ModSync;
using CS2MultiplayerMod.Game.Diagnostics;
using CS2MultiplayerMod.Game.Sync.Infrastructure;

internal static class AutomaticRecoveryChecks
{
    private const long Cooldown = 90000;

    private static ResyncReport Lost(string subject) =>
        ResyncReport.Create("mod state backlog overflow", "mods", ResyncEvidence.StreamLoss)
            .About(subject);

    public static void Run(Action<bool, string> check)
    {
        var recovery = new AutomaticRecoveryQueue(Cooldown);
        recovery.Enqueue(Lost("previous recovery"), 0);
        check(recovery.TryTake(1000, true, false, 0, out _), "first recovery starts immediately");
        recovery.ObserveWorld(1);

        // Reproduce a one-shot backlog report arriving just after a preceding world sync. Neither
        // the backlog nor SyncInbox will repeat it after the service consumes it during cooldown.
        var backlog = new ModStateBacklog<int>(1, 2);
        backlog.Admit("old junction", 1);
        backlog.Admit("new junction", 2);
        check(backlog.TrimExcess(), "discarded unapplied mod state requests recovery once");
        SyncInbox.RequestResync(Lost("junctions"));
        check(SyncInbox.TryTakeResyncRequest(out ResyncReport loss), "service takes the sole loss report");
        check(ResyncArbiter.Submit(loss, 2000, false) == ResyncVerdict.Settled,
            "stream loss settles without a retry hold");
        check(recovery.Enqueue(loss, 1), "service retains the settled loss report");
        check(!recovery.TryTake(2000, true, false, 1, out _), "cooldown initially refuses recovery");
        check(!backlog.ConsumeRecovery() && !SyncInbox.TryTakeResyncRequest(out _),
            "the original caller will not submit the lost report again");
        check(!recovery.Enqueue(Lost("later loss"), 1), "additional losses share one bounded request");
        check(!recovery.TryTake(90999, true, false, 1, out _), "cooldown never dispatches early");
        check(recovery.TryTake(91000, true, false, 1, out ResyncReport ready) &&
            ReferenceEquals(ready, loss), "original loss recovers when the cooldown expires without a new report");
        check(!recovery.TryTake(181000, true, false, 1, out _), "successful dispatch is not repeated");

        // An unrelated peer's snapshot only pauses this city. Likewise, aborting an incoming
        // transfer leaves the same city, so neither event may consume its deferred recovery.
        var paused = new AutomaticRecoveryQueue(Cooldown);
        ResyncReport pausedLoss = Lost("paused city");
        paused.Enqueue(pausedLoss, 10);
        check(!paused.TryTake(0, true, true, 10, out _), "in-flight sync blocks dispatch");
        check(!paused.TryTake(180000, true, true, 10, out _), "long barrier retains pending recovery");
        check(paused.TryTake(180001, true, false, 10, out ready) && ReferenceEquals(ready, pausedLoss),
            "barrier-only resume or transfer abort retains the same-world report");

        // Installing a snapshot supersedes pending work, but does not permit a reload storm.
        var replaced = new AutomaticRecoveryQueue(Cooldown);
        replaced.Enqueue(Lost("first city"), 0);
        check(replaced.TryTake(1000, true, false, 0, out _), "replacement fixture starts first recovery");
        replaced.Enqueue(Lost("obsolete city"), 0);
        check(!replaced.TryTake(2000, true, true, 1, out _), "installed snapshot supersedes old report");
        check(!replaced.TryTake(2001, true, false, 1, out _), "old city report is no longer queued");
        replaced.Enqueue(Lost("fresh loss after snapshot"), 1);
        check(!replaced.TryTake(3000, true, false, 1, out _), "snapshot does not reset recovery cooldown");
        check(replaced.TryTake(91000, true, false, 1, out _), "new-world loss is still recovered later");

        var disconnected = new AutomaticRecoveryQueue(Cooldown);
        disconnected.Enqueue(Lost("departing session"), 0);
        check(disconnected.TryTake(1000, true, false, 0, out _), "disconnect fixture starts first recovery");
        disconnected.Enqueue(Lost("stale session"), 0);
        check(!disconnected.TryTake(2000, false, false, 0, out _), "disconnect cancels deferred recovery");
        check(!disconnected.TryTake(2001, true, false, 0, out _), "new session cannot inherit stale reports");
        disconnected.Enqueue(Lost("new session"), 0);
        check(disconnected.TryTake(2002, true, false, 0, out _), "new session does not inherit old cooldown");
        disconnected.Enqueue(Lost("shutdown"), 0);
        disconnected.Reset(0);
        check(!disconnected.TryTake(2003, true, false, 0, out _), "explicit shutdown clears deferred recovery");
    }
}
