using System;

namespace ForkTrack.Core
{
    /// <summary>
    /// Identifies how a node subscription was created
    /// </summary>
    public enum NodeSubscriptionType
    {
        /// <summary>Subscribed by node ID</summary>
        ById,
        /// <summary>Subscribed by title</summary>
        ByTitle,
        /// <summary>Subscribed by object and action names</summary>
        ByObjectAction
    }

    /// <summary>
    /// Represents a subscription to a specific node's state changes.
    /// Created via ForkTrack.SubscribeNode() methods.
    /// </summary>
    public class NodeSubscription
    {
        /// <summary>Unique subscription ID</summary>
        public string Id { get; }

        /// <summary>How this subscription identifies the target node</summary>
        public NodeSubscriptionType Type { get; }

        /// <summary>Node ID (for ById subscriptions)</summary>
        public string NodeId { get; }

        /// <summary>Node title (for ByTitle subscriptions)</summary>
        public string Title { get; }

        /// <summary>Object name (for ByObjectAction subscriptions)</summary>
        public string ObjectName { get; }

        /// <summary>Action name (for ByObjectAction subscriptions)</summary>
        public string ActionName { get; }

        /// <summary>Callback when node is completed</summary>
        public Action<ForkTrackNode> OnCompleted { get; }

        /// <summary>Callback when node is unlocked (optional)</summary>
        public Action<ForkTrackNode> OnUnlocked { get; }

        /// <summary>Whether this subscription was validated against the loaded graph</summary>
        public bool IsValidated { get; internal set; }

        /// <summary>Whether the target node exists in the current graph</summary>
        public bool IsValid { get; internal set; }

        /// <summary>Warning message if subscription is invalid</summary>
        public string ValidationWarning { get; internal set; }

        /// <summary>
        /// Creates a subscription by node ID
        /// </summary>
        public NodeSubscription(string nodeId, Action<ForkTrackNode> onCompleted, Action<ForkTrackNode> onUnlocked = null)
        {
            Id = Guid.NewGuid().ToString();
            Type = NodeSubscriptionType.ById;
            NodeId = nodeId;
            OnCompleted = onCompleted;
            OnUnlocked = onUnlocked;
        }

        /// <summary>
        /// Creates a subscription by object and action names
        /// </summary>
        public NodeSubscription(string objectName, string actionName, Action<ForkTrackNode> onCompleted, Action<ForkTrackNode> onUnlocked = null)
        {
            Id = Guid.NewGuid().ToString();
            Type = NodeSubscriptionType.ByObjectAction;
            ObjectName = objectName;
            ActionName = actionName;
            OnCompleted = onCompleted;
            OnUnlocked = onUnlocked;
        }

        /// <summary>
        /// Creates a subscription by title (internal constructor with explicit type)
        /// </summary>
        internal NodeSubscription(NodeSubscriptionType type, string title, Action<ForkTrackNode> onCompleted, Action<ForkTrackNode> onUnlocked)
        {
            Id = Guid.NewGuid().ToString();
            Type = type;
            Title = title;
            OnCompleted = onCompleted;
            OnUnlocked = onUnlocked;
        }

        /// <summary>
        /// Factory method to create a subscription by title
        /// </summary>
        public static NodeSubscription ByTitle(string title, Action<ForkTrackNode> onCompleted, Action<ForkTrackNode> onUnlocked = null)
        {
            return new NodeSubscription(NodeSubscriptionType.ByTitle, title, onCompleted, onUnlocked);
        }

        /// <summary>
        /// Returns a description of what this subscription is targeting
        /// </summary>
        public string GetTargetDescription()
        {
            switch (Type)
            {
                case NodeSubscriptionType.ById:
                    return $"node ID \"{NodeId}\"";
                case NodeSubscriptionType.ByTitle:
                    return $"node title \"{Title}\"";
                case NodeSubscriptionType.ByObjectAction:
                    return $"node \"{ObjectName} - {ActionName}\"";
                default:
                    return "unknown target";
            }
        }

        public override string ToString()
        {
            string status = IsValidated ? (IsValid ? "valid" : "INVALID") : "not validated";
            return $"NodeSubscription({GetTargetDescription()}, {status})";
        }
    }

    /// <summary>
    /// Represents a subscription to a specific custom event type.
    /// Created via ForkTrack.SubscribeEvent() method.
    /// </summary>
    public class EventSubscription
    {
        /// <summary>Unique subscription ID</summary>
        public string Id { get; }

        /// <summary>The property ID / event category to match</summary>
        public string PropertyId { get; }

        /// <summary>Optional specific value to match (null = match any value)</summary>
        public string Value { get; }

        /// <summary>Callback when the event is triggered</summary>
        public Action<CustomEventData> OnTriggered { get; }

        /// <summary>Whether this subscription was validated against the loaded graph</summary>
        public bool IsValidated { get; internal set; }

        /// <summary>Whether matching events exist in the current graph</summary>
        public bool IsValid { get; internal set; }

        /// <summary>Warning message if subscription is invalid</summary>
        public string ValidationWarning { get; internal set; }

        /// <summary>
        /// Creates an event subscription
        /// </summary>
        /// <param name="propertyId">The event category/property ID to listen for</param>
        /// <param name="onTriggered">Callback when event fires</param>
        /// <param name="value">Optional specific value to filter (null = any value)</param>
        public EventSubscription(string propertyId, Action<CustomEventData> onTriggered, string value = null)
        {
            Id = Guid.NewGuid().ToString();
            PropertyId = propertyId;
            Value = value;
            OnTriggered = onTriggered;
        }

        /// <summary>
        /// Returns a description of what this subscription is targeting
        /// </summary>
        public string GetTargetDescription()
        {
            if (!string.IsNullOrEmpty(Value))
                return $"event \"{PropertyId}\" with value \"{Value}\"";
            return $"event \"{PropertyId}\"";
        }

        public override string ToString()
        {
            string status = IsValidated ? (IsValid ? "valid" : "INVALID") : "not validated";
            return $"EventSubscription({GetTargetDescription()}, {status})";
        }
    }
}
