using System;
using System.Collections.Generic;

namespace ForkTrack.Core
{
    /// <summary>
    /// A custom event that fires when a node trigger activates
    /// </summary>
    [Serializable]
    public class CustomEvent
    {
        /// <summary>ID of the custom property this event affects</summary>
        public string propertyId;

        /// <summary>When to fire the event (OnUnlock or OnComplete)</summary>
        public string trigger;

        /// <summary>Possible values for weight-based selection</summary>
        public List<string> values = new List<string>();

        /// <summary>Weight for random selection (higher = more likely)</summary>
        public float weight = 1f;

        /// <summary>Delay in seconds before firing</summary>
        public float delay;

        /// <summary>Amount value for numeric properties</summary>
        public float amount;

        /// <summary>Optional text/note associated with this event</summary>
        public string text;

        public CustomEvent() { }

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

    /// <summary>
    /// Data passed when a custom event is triggered
    /// </summary>
    [Serializable]
    public class CustomEventData
    {
        /// <summary>The node that triggered the event</summary>
        public string nodeId;

        /// <summary>The node's display name (e.g., "Fred - Interact")</summary>
        public string nodeDisplayName;

        /// <summary>The custom event configuration</summary>
        public CustomEvent customEvent;

        /// <summary>The selected value (from weight-based selection)</summary>
        public string selectedValue;

        /// <summary>The trigger type that fired this event</summary>
        public TriggerType triggerType;

        public CustomEventData() { }

        public CustomEventData(string nodeId, string nodeDisplayName, CustomEvent customEvent, string selectedValue, TriggerType triggerType)
        {
            this.nodeId = nodeId;
            this.nodeDisplayName = nodeDisplayName;
            this.customEvent = customEvent;
            this.selectedValue = selectedValue;
            this.triggerType = triggerType;
        }

        /// <summary>
        /// Gets the property ID (category/type) of this event
        /// </summary>
        public string PropertyId => customEvent?.propertyId;

        /// <summary>
        /// Gets the weight value for this event
        /// </summary>
        public float Weight => customEvent?.weight ?? 0f;

        /// <summary>
        /// Gets the delay value for this event
        /// </summary>
        public float Delay => customEvent?.delay ?? 0f;

        /// <summary>
        /// Gets the amount value for this event
        /// </summary>
        public float Amount => customEvent?.amount ?? 0f;

        /// <summary>
        /// Gets all possible values for this event
        /// </summary>
        public List<string> AllValues => customEvent?.values ?? new List<string>();

        /// <summary>
        /// Gets the optional text/note for this event
        /// </summary>
        public string Text => customEvent?.text;

        /// <summary>
        /// Returns a formatted string with all event details
        /// </summary>
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"[{nodeDisplayName ?? nodeId}] ");
            sb.Append($"{PropertyId ?? "unknown"}");

            if (!string.IsNullOrEmpty(selectedValue))
                sb.Append($" = \"{selectedValue}\"");

            if (Weight != 0f && Weight != 1f)
                sb.Append($" (weight: {Weight:F1})");

            if (Delay > 0f)
                sb.Append($" (delay: {Delay:F1}s)");

            if (Amount != 0f)
                sb.Append($" (amount: {Amount:F1})");

            if (!string.IsNullOrEmpty(Text))
                sb.Append($" (text: \"{Text}\")");

            sb.Append($" [{triggerType}]");

            return sb.ToString();
        }
    }
}
