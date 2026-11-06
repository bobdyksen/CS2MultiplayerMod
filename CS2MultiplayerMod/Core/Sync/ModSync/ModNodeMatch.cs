using System.Collections.Generic;

namespace CS2MultiplayerMod.Core.Sync.ModSync
{
    /// <summary>
    /// Which searched net node is the one a mod-state reference named.
    ///
    /// The reference's key buckets position to half a metre (<see cref="ModEntityRef"/>). A node
    /// inside that radius is the same junction, float noise included. A node merely inside the
    /// wider search box is a different junction, and more than one node inside the identity
    /// radius is a tie. Both of those stay unresolved: picking the nearest would write the edit
    /// onto a road the sender did not name.
    /// </summary>
    public static class ModNodeMatch
    {
        /// <summary>Same spacing as <c>ModEntityRef.Key</c>'s half-metre buckets.</summary>
        public const float IdentityRadiusMeters = 0.5f;

        /// <summary>How far the search tree is asked to look. Acceptance is the identity radius.</summary>
        public const float SearchRadiusMeters = 3f;

        /// <summary>Elevation is re-derived per machine, so height is a loose filter only.</summary>
        public const float VerticalSlackMeters = 12f;

        public static float IdentityRadiusSquared
        {
            get { return IdentityRadiusMeters * IdentityRadiusMeters; }
        }

        /// <summary>
        /// Index of the single node within the identity radius and vertical slack, or -1 when
        /// none qualify or more than one does.
        /// </summary>
        public static int Select(IList<float> horizontalDistanceSquared, IList<float> absoluteVertical,
            float identityRadiusSquared, float verticalSlack)
        {
            if (horizontalDistanceSquared == null) throw new System.ArgumentNullException(nameof(horizontalDistanceSquared));
            if (absoluteVertical == null) throw new System.ArgumentNullException(nameof(absoluteVertical));
            int count = horizontalDistanceSquared.Count;
            if (absoluteVertical.Count < count) count = absoluteVertical.Count;

            int chosen = -1;
            for (int i = 0; i < count; i++)
            {
                if (horizontalDistanceSquared[i] > identityRadiusSquared) continue;
                if (absoluteVertical[i] > verticalSlack) continue;
                if (chosen >= 0) return -1;
                chosen = i;
            }
            return chosen;
        }
    }
}
