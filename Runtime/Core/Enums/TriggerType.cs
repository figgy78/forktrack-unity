namespace ForkTrack.Core
{
    /// <summary>
    /// When a note, variable action, or custom event should be triggered
    /// </summary>
    public enum TriggerType
    {
        /// <summary>Trigger when the node is unlocked</summary>
        OnUnlock,
        /// <summary>Trigger when the node is completed</summary>
        OnComplete
    }
}
