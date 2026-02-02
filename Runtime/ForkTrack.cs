using System;
using System.Collections.Generic;
using UnityEngine;
using ForkTrack.Core;
using ForkTrack.Internal;

namespace ForkTrack
{
    /// <summary>
    /// Main static API for ForkTrack integration.
    /// Provides a clean, simple interface for loading graphs, completing nodes, and handling events.
    /// </summary>
    public static class ForkTrack
    {
        #region Events

        /// <summary>
        /// Fired when a graph is successfully loaded
        /// </summary>
        public static event Action<ForkTrackGraph> OnGraphLoaded
        {
            add => ForkTrackRuntime.Instance.OnGraphLoaded += value;
            remove => ForkTrackRuntime.Instance.OnGraphLoaded -= value;
        }

        /// <summary>
        /// Fired when graph loading fails
        /// </summary>
        public static event Action<string> OnGraphLoadError
        {
            add => ForkTrackRuntime.Instance.OnGraphLoadError += value;
            remove => ForkTrackRuntime.Instance.OnGraphLoadError -= value;
        }

        /// <summary>
        /// Fired when a node is unlocked (Locked -> Unlocked)
        /// </summary>
        public static event Action<ForkTrackNode> OnNodeUnlocked
        {
            add => ForkTrackRuntime.Instance.OnNodeUnlocked += value;
            remove => ForkTrackRuntime.Instance.OnNodeUnlocked -= value;
        }

        /// <summary>
        /// Fired when a node is completed (Unlocked -> Completed)
        /// </summary>
        public static event Action<ForkTrackNode> OnNodeCompleted
        {
            add => ForkTrackRuntime.Instance.OnNodeCompleted += value;
            remove => ForkTrackRuntime.Instance.OnNodeCompleted -= value;
        }

        /// <summary>
        /// Fired when a node is reset (any state -> Locked)
        /// </summary>
        public static event Action<ForkTrackNode> OnNodeReset
        {
            add => ForkTrackRuntime.Instance.OnNodeReset += value;
            remove => ForkTrackRuntime.Instance.OnNodeReset -= value;
        }

        /// <summary>
        /// Fired when a node's state changes
        /// Parameters: node, oldState, newState
        /// </summary>
        public static event Action<ForkTrackNode, NodeState, NodeState> OnNodeStateChanged
        {
            add => ForkTrackRuntime.Instance.OnNodeStateChanged += value;
            remove => ForkTrackRuntime.Instance.OnNodeStateChanged -= value;
        }

        /// <summary>
        /// Fired when a variable value changes
        /// Parameters: variable, oldValue, newValue
        /// </summary>
        public static event Action<ForkTrackVariable, object, object> OnVariableChanged
        {
            add => ForkTrackRuntime.Instance.OnVariableChanged += value;
            remove => ForkTrackRuntime.Instance.OnVariableChanged -= value;
        }

        /// <summary>
        /// Fired when a custom event is triggered from a node
        /// </summary>
        public static event Action<CustomEventData> OnEventTriggered
        {
            add => ForkTrackRuntime.Instance.OnEventTriggered += value;
            remove => ForkTrackRuntime.Instance.OnEventTriggered -= value;
        }

        /// <summary>
        /// Fired when a custom event is triggered from a node (alias for OnEventTriggered)
        /// </summary>
        public static event Action<CustomEventData> OnCustomEvent
        {
            add => ForkTrackRuntime.Instance.OnEventTriggered += value;
            remove => ForkTrackRuntime.Instance.OnEventTriggered -= value;
        }

        /// <summary>
        /// Fired when a note is triggered from a node
        /// Parameters: node, note
        /// </summary>
        public static event Action<ForkTrackNode, NodeNote> OnNoteFired
        {
            add => ForkTrackRuntime.Instance.OnNoteFired += value;
            remove => ForkTrackRuntime.Instance.OnNoteFired -= value;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns true if a graph is currently loaded
        /// </summary>
        public static bool IsLoaded => ForkTrackRuntime.Instance.IsLoaded;

        /// <summary>
        /// Gets the currently loaded graph (null if none loaded)
        /// </summary>
        public static ForkTrackGraph CurrentGraph => ForkTrackRuntime.Instance.Graph;

        #endregion

        #region Loading

        /// <summary>
        /// Loads a graph from a JSON string
        /// </summary>
        /// <param name="json">JSON string containing the graph data</param>
        public static void LoadGraphFromJSON(string json)
        {
            ForkTrackRuntime.Instance.LoadGraphFromJSON(json);
        }

        /// <summary>
        /// Loads a graph from a TextAsset
        /// </summary>
        /// <param name="textAsset">TextAsset containing the graph JSON</param>
        public static void LoadGraphFromTextAsset(TextAsset textAsset)
        {
            ForkTrackRuntime.Instance.LoadGraphFromTextAsset(textAsset);
        }

        #endregion

        #region Node Queries

        /// <summary>
        /// Gets a node by its ID
        /// </summary>
        /// <param name="nodeId">The unique node ID</param>
        /// <returns>The node, or null if not found</returns>
        public static ForkTrackNode GetNode(string nodeId)
        {
            return ForkTrackRuntime.Instance.GetNode(nodeId);
        }

        /// <summary>
        /// Gets a node by object and action names
        /// </summary>
        /// <param name="objectName">The object name (e.g., "Fred")</param>
        /// <param name="actionName">The action name (e.g., "Interact")</param>
        /// <returns>The node, or null if not found</returns>
        public static ForkTrackNode GetNode(string objectName, string actionName)
        {
            return ForkTrackRuntime.Instance.GetNode(objectName, actionName);
        }

        /// <summary>
        /// Gets a node by its title (case-insensitive search)
        /// </summary>
        /// <param name="title">The node title</param>
        /// <returns>The first matching node, or null if not found</returns>
        public static ForkTrackNode GetNodeByTitle(string title)
        {
            return ForkTrackRuntime.Instance.GetNodeByTitle(title);
        }

        /// <summary>
        /// Gets all nodes with the specified object name
        /// </summary>
        /// <param name="objectName">The object name to filter by</param>
        /// <returns>List of matching nodes</returns>
        public static List<ForkTrackNode> GetNodesByObject(string objectName)
        {
            return ForkTrackRuntime.Instance.GetNodesByObject(objectName);
        }

        /// <summary>
        /// Gets all nodes with the specified action name
        /// </summary>
        /// <param name="actionName">The action name to filter by</param>
        /// <returns>List of matching nodes</returns>
        public static List<ForkTrackNode> GetNodesByAction(string actionName)
        {
            return ForkTrackRuntime.Instance.GetNodesByAction(actionName);
        }

        /// <summary>
        /// Gets all nodes in the specified state
        /// </summary>
        /// <param name="state">The state to filter by</param>
        /// <returns>List of matching nodes</returns>
        public static List<ForkTrackNode> GetNodesInState(NodeState state)
        {
            return ForkTrackRuntime.Instance.GetNodesInState(state);
        }

        /// <summary>
        /// Gets all nodes in the current graph
        /// </summary>
        /// <returns>List of all nodes</returns>
        public static List<ForkTrackNode> GetAllNodes()
        {
            return ForkTrackRuntime.Instance.GetAllNodes();
        }

        #endregion

        #region Node Actions

        /// <summary>
        /// Unlocks a node by ID (transitions from Locked to Unlocked)
        /// </summary>
        /// <param name="nodeId">The node ID</param>
        /// <returns>True if the node was unlocked, false if already unlocked or dependencies not met</returns>
        public static bool UnlockNode(string nodeId)
        {
            return ForkTrackRuntime.Instance.UnlockNode(nodeId);
        }

        /// <summary>
        /// Unlocks a node by object and action names
        /// </summary>
        /// <param name="objectName">The object name</param>
        /// <param name="actionName">The action name</param>
        /// <returns>True if the node was unlocked</returns>
        public static bool UnlockNode(string objectName, string actionName)
        {
            return ForkTrackRuntime.Instance.UnlockNode(objectName, actionName);
        }

        /// <summary>
        /// Completes a node by ID (transitions from Unlocked to Completed)
        /// </summary>
        /// <param name="nodeId">The node ID</param>
        /// <returns>True if the node was completed, false if locked or already completed</returns>
        public static bool CompleteNode(string nodeId)
        {
            return ForkTrackRuntime.Instance.CompleteNode(nodeId);
        }

        /// <summary>
        /// Completes a node by object and action names
        /// </summary>
        /// <param name="objectName">The object name</param>
        /// <param name="actionName">The action name</param>
        /// <returns>True if the node was completed</returns>
        public static bool CompleteNode(string objectName, string actionName)
        {
            return ForkTrackRuntime.Instance.CompleteNode(objectName, actionName);
        }

        /// <summary>
        /// Resets a node to Locked state
        /// </summary>
        /// <param name="nodeId">The node ID</param>
        /// <returns>True if the node was reset</returns>
        public static bool ResetNode(string nodeId)
        {
            return ForkTrackRuntime.Instance.ResetNode(nodeId);
        }

        /// <summary>
        /// Resets all nodes to Locked state and variables to defaults
        /// </summary>
        public static void ResetAll()
        {
            ForkTrackRuntime.Instance.ResetAll();
        }

        #endregion

        #region Variables

        /// <summary>
        /// Gets a variable value by name
        /// </summary>
        /// <typeparam name="T">The expected type (float for NUMBER, bool for BOOLEAN)</typeparam>
        /// <param name="variableName">The variable name (case-insensitive)</param>
        /// <returns>The variable value, or default if not found</returns>
        public static T GetVariable<T>(string variableName)
        {
            return ForkTrackRuntime.Instance.GetVariable<T>(variableName);
        }

        /// <summary>
        /// Sets a variable value by name
        /// </summary>
        /// <typeparam name="T">The value type</typeparam>
        /// <param name="variableName">The variable name (case-insensitive)</param>
        /// <param name="value">The new value</param>
        public static void SetVariable<T>(string variableName, T value)
        {
            ForkTrackRuntime.Instance.SetVariable(variableName, value);
        }

        /// <summary>
        /// Gets all variable definitions in the current graph
        /// </summary>
        /// <returns>List of variable definitions</returns>
        public static List<ForkTrackVariable> GetAllVariables()
        {
            return ForkTrackRuntime.Instance.GetAllVariables();
        }

        #endregion

        #region Progress Persistence

        /// <summary>
        /// Saves progress for the current graph to the specified slot
        /// </summary>
        /// <param name="slotName">Name of the save slot (default: "default")</param>
        public static void SaveProgress(string slotName = "default")
        {
            ForkTrackRuntime.Instance.SaveProgress(slotName);
        }

        /// <summary>
        /// Loads progress for the current graph from the specified slot
        /// </summary>
        /// <param name="slotName">Name of the save slot (default: "default")</param>
        /// <returns>True if progress was loaded successfully</returns>
        public static bool LoadProgress(string slotName = "default")
        {
            return ForkTrackRuntime.Instance.LoadProgress(slotName);
        }

        /// <summary>
        /// Checks if saved progress exists for the specified slot
        /// </summary>
        /// <param name="slotName">Name of the save slot (default: "default")</param>
        /// <returns>True if saved progress exists</returns>
        public static bool HasSavedProgress(string slotName = "default")
        {
            return ForkTrackRuntime.Instance.HasSavedProgress(slotName);
        }

        /// <summary>
        /// Deletes saved progress for the specified slot
        /// </summary>
        /// <param name="slotName">Name of the save slot (default: "default")</param>
        public static void DeleteProgress(string slotName = "default")
        {
            ForkTrackRuntime.Instance.DeleteProgress(slotName);
        }

        /// <summary>
        /// Exports current progress as a JSON string (for cloud saves, etc.)
        /// </summary>
        /// <returns>JSON string containing progress data</returns>
        public static string ExportProgress()
        {
            return ForkTrackRuntime.Instance.ExportProgress();
        }

        /// <summary>
        /// Imports progress from a JSON string
        /// </summary>
        /// <param name="json">JSON string containing progress data</param>
        /// <returns>True if progress was imported successfully</returns>
        public static bool ImportProgress(string json)
        {
            return ForkTrackRuntime.Instance.ImportProgress(json);
        }

        #endregion

        #region Cache

        /// <summary>
        /// Clears the graph cache (memory and disk)
        /// </summary>
        public static void ClearCache()
        {
            ForkTrackRuntime.Instance.ClearCache();
        }

        #endregion

        #region Subscriptions

        /// <summary>
        /// Subscribes to a specific node's completion by ID.
        /// The subscription will be validated when a graph is loaded.
        /// If the node doesn't exist, a warning will be logged.
        /// </summary>
        /// <param name="nodeId">The node ID to watch</param>
        /// <param name="onCompleted">Callback when the node is completed</param>
        /// <param name="onUnlocked">Optional callback when the node is unlocked</param>
        /// <returns>The subscription (can be used to unsubscribe)</returns>
        public static NodeSubscription SubscribeNode(string nodeId, Action<ForkTrackNode> onCompleted, Action<ForkTrackNode> onUnlocked = null)
        {
            var subscription = new NodeSubscription(nodeId, onCompleted, onUnlocked);
            ForkTrackRuntime.Instance.AddNodeSubscription(subscription);
            return subscription;
        }

        /// <summary>
        /// Subscribes to a specific node's completion by object and action names.
        /// The subscription will be validated when a graph is loaded.
        /// If the node doesn't exist, a warning will be logged.
        /// </summary>
        /// <param name="objectName">The object name (e.g., "Fred")</param>
        /// <param name="actionName">The action name (e.g., "Interact")</param>
        /// <param name="onCompleted">Callback when the node is completed</param>
        /// <param name="onUnlocked">Optional callback when the node is unlocked</param>
        /// <returns>The subscription (can be used to unsubscribe)</returns>
        public static NodeSubscription SubscribeNode(string objectName, string actionName, Action<ForkTrackNode> onCompleted, Action<ForkTrackNode> onUnlocked = null)
        {
            var subscription = new NodeSubscription(objectName, actionName, onCompleted, onUnlocked);
            ForkTrackRuntime.Instance.AddNodeSubscription(subscription);
            return subscription;
        }

        /// <summary>
        /// Subscribes to a specific node's completion by title.
        /// The subscription will be validated when a graph is loaded.
        /// If the node doesn't exist, a warning will be logged.
        /// </summary>
        /// <param name="title">The node title to search for</param>
        /// <param name="onCompleted">Callback when the node is completed</param>
        /// <param name="onUnlocked">Optional callback when the node is unlocked</param>
        /// <returns>The subscription (can be used to unsubscribe)</returns>
        public static NodeSubscription SubscribeNodeByTitle(string title, Action<ForkTrackNode> onCompleted, Action<ForkTrackNode> onUnlocked = null)
        {
            var subscription = NodeSubscription.ByTitle(title, onCompleted, onUnlocked);
            ForkTrackRuntime.Instance.AddNodeSubscription(subscription);
            return subscription;
        }

        /// <summary>
        /// Subscribes to a specific custom event type.
        /// The subscription will be validated when a graph is loaded.
        /// If no events with the property ID exist, a warning will be logged.
        /// </summary>
        /// <param name="propertyId">The event property ID / category to listen for</param>
        /// <param name="onTriggered">Callback when the event is triggered</param>
        /// <param name="value">Optional specific value to filter (null = any value)</param>
        /// <returns>The subscription (can be used to unsubscribe)</returns>
        public static EventSubscription SubscribeEvent(string propertyId, Action<CustomEventData> onTriggered, string value = null)
        {
            var subscription = new EventSubscription(propertyId, onTriggered, value);
            ForkTrackRuntime.Instance.AddEventSubscription(subscription);
            return subscription;
        }

        /// <summary>
        /// Unsubscribes a node subscription
        /// </summary>
        /// <param name="subscription">The subscription to remove</param>
        public static void UnsubscribeNode(NodeSubscription subscription)
        {
            if (subscription != null)
            {
                ForkTrackRuntime.Instance.RemoveNodeSubscription(subscription.Id);
            }
        }

        /// <summary>
        /// Unsubscribes an event subscription
        /// </summary>
        /// <param name="subscription">The subscription to remove</param>
        public static void UnsubscribeEvent(EventSubscription subscription)
        {
            if (subscription != null)
            {
                ForkTrackRuntime.Instance.RemoveEventSubscription(subscription.Id);
            }
        }

        /// <summary>
        /// Gets all active node subscriptions
        /// </summary>
        /// <returns>List of node subscriptions</returns>
        public static List<NodeSubscription> GetNodeSubscriptions()
        {
            return ForkTrackRuntime.Instance.GetNodeSubscriptions();
        }

        /// <summary>
        /// Gets all active event subscriptions
        /// </summary>
        /// <returns>List of event subscriptions</returns>
        public static List<EventSubscription> GetEventSubscriptions()
        {
            return ForkTrackRuntime.Instance.GetEventSubscriptions();
        }

        /// <summary>
        /// Validates all subscriptions against the current graph.
        /// This is called automatically when a graph is loaded.
        /// </summary>
        public static void ValidateSubscriptions()
        {
            ForkTrackRuntime.Instance.ValidateSubscriptions();
        }

        #endregion

        #region Debug / Testing

        /// <summary>
        /// Resets the ForkTrack runtime (for testing/debugging)
        /// Clears all state, events, and subscriptions.
        /// </summary>
        public static void ResetRuntime()
        {
            ForkTrackRuntime.Reset();
        }

        #endregion
    }
}
