namespace CS2MultiplayerMod.Core.Sync
{
    /// <summary>
    /// What the zoned-building sync does on a frame when it cannot both capture and apply.
    ///
    /// <see cref="HoldForSameWorld"/> is the pause while simulation sync stays on: a barrier-only
    /// epoch (another player is joining) and the wait for a save the host is still sending. This
    /// city is either unchanged or has already had its queues purged by the save install, so the
    /// decisions already queued still belong to it. Dropping them, or turning the local spawner
    /// back on, is how a client grows a different set of buildings across that pause.
    ///
    /// <see cref="ReleaseLocalSimulation"/> is every other closed gate: simulation sync is off,
    /// the session has ended, or this machine is the host sitting in its own barrier. The local
    /// city has to be able to grow again, and a queue from a session that is no longer authoritative
    /// must not replay onto a later world.
    /// </summary>
    public enum GrowableGateDecision
    {
        /// <summary>Simulation sync is on and gameplay is open. Capture on the host, apply on clients.</summary>
        Run = 0,

        /// <summary>
        /// Client, still connected, simulation sync still on, gameplay gate closed. Keep zoning
        /// authority held and keep the bounded queue.
        /// </summary>
        HoldForSameWorld = 1,

        /// <summary>Hand zoning back to the local simulation and discard queued host decisions.</summary>
        ReleaseLocalSimulation = 2,
    }

    /// <summary>
    /// The one decision <c>GrowableSyncSystem</c> uses when <c>SimulationSyncReady</c> is false.
    /// Kept free of the game assemblies so the pause behavior can be tested without Cities: Skylines II.
    /// </summary>
    public static class GrowableGate
    {
        public static GrowableGateDecision Decide(
            bool simulationSyncEnabled,
            bool gameplaySyncReady,
            bool sessionConnected,
            bool isClient)
        {
            if (simulationSyncEnabled && gameplaySyncReady)
                return GrowableGateDecision.Run;

            // Same city, host still authoritative. A world that is actually being replaced
            // clears this queue through the save-install drain; this branch must not do it
            // again on every paused frame, or the commands the barrier asked the client to
            // finish applying are gone before the pause ends.
            if (simulationSyncEnabled && sessionConnected && isClient)
                return GrowableGateDecision.HoldForSameWorld;

            return GrowableGateDecision.ReleaseLocalSimulation;
        }
    }
}
