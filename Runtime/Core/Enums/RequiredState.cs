namespace ForkTrack.Core
{
    /// <summary>
    /// The required state of a source node for an edge dependency to be satisfied
    /// </summary>
    public enum RequiredState
    {
        /// <summary>Source node must be Unlocked or Completed</summary>
        OnUnlock,
        /// <summary>Source node must be Completed</summary>
        OnComplete
    }
}
