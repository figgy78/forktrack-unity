using System;

namespace ForkTrack.Core
{
    /// <summary>
    /// A note attached to a node that fires at a specific trigger
    /// </summary>
    [Serializable]
    public class NodeNote
    {
        /// <summary>The text content of the note</summary>
        public string text;

        /// <summary>When to fire the note (OnUnlock or OnComplete)</summary>
        public string trigger;

        public NodeNote() { }

        public NodeNote(string text, TriggerType trigger)
        {
            this.text = text;
            this.trigger = trigger.ToString();
        }

        /// <summary>
        /// Gets the trigger type as an enum
        /// </summary>
        public TriggerType GetTriggerType()
        {
            if (string.IsNullOrEmpty(trigger))
                return TriggerType.OnComplete;

            return trigger == "OnUnlock" ? TriggerType.OnUnlock : TriggerType.OnComplete;
        }
    }
}
