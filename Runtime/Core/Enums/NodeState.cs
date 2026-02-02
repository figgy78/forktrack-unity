namespace ForkTrack.Core
{
    /// <summary>
    /// Represents the state of a node in the narrative graph
    /// </summary>
    public enum NodeState
    {
        /// <summary>Node is locked and cannot be completed until dependencies are met</summary>
        Locked,
        /// <summary>Node is unlocked and available for completion</summary>
        Unlocked,
        /// <summary>Node has been completed</summary>
        Completed
    }
}
