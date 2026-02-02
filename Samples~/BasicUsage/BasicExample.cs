using UnityEngine;
using ForkTrack;
using ForkTrack.Core;

namespace ForkTrack.Samples.BasicUsage
{
    /// <summary>
    /// Basic example demonstrating ForkTrack API usage.
    /// Loads a graph, subscribes to events, and handles node completion.
    /// </summary>
    public class BasicExample : MonoBehaviour
    {
        [Header("Graph Configuration")]
        [SerializeField] private TextAsset graphJson;

        [Header("Debug Output")]
        [SerializeField] private bool logEvents = true;

        private void Start()
        {
            // Subscribe to events
            ForkTrack.OnGraphLoaded += HandleGraphLoaded;
            ForkTrack.OnGraphLoadError += HandleGraphLoadError;
            ForkTrack.OnNodeUnlocked += HandleNodeUnlocked;
            ForkTrack.OnNodeCompleted += HandleNodeCompleted;
            ForkTrack.OnVariableChanged += HandleVariableChanged;
            ForkTrack.OnEventTriggered += HandleEventTriggered;
            ForkTrack.OnNoteFired += HandleNoteFired;

            // Load the graph
            if (graphJson != null)
            {
                Log("Loading graph from TextAsset...");
                ForkTrack.LoadGraphFromJSON(graphJson.text);
            }
            else
            {
                Debug.LogWarning("[BasicExample] No graph JSON assigned. Please assign a TextAsset.");
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            ForkTrack.OnGraphLoaded -= HandleGraphLoaded;
            ForkTrack.OnGraphLoadError -= HandleGraphLoadError;
            ForkTrack.OnNodeUnlocked -= HandleNodeUnlocked;
            ForkTrack.OnNodeCompleted -= HandleNodeCompleted;
            ForkTrack.OnVariableChanged -= HandleVariableChanged;
            ForkTrack.OnEventTriggered -= HandleEventTriggered;
            ForkTrack.OnNoteFired -= HandleNoteFired;
        }

        #region Event Handlers

        private void HandleGraphLoaded(ForkTrackGraph graph)
        {
            Log($"Graph loaded! Nodes: {graph.nodes.Length}, Edges: {graph.edges.Length}");

            // Print unlocked nodes
            var unlockedNodes = ForkTrack.GetNodesInState(NodeState.Unlocked);
            Log($"Initially unlocked nodes: {unlockedNodes.Count}");

            foreach (var node in unlockedNodes)
            {
                Log($"  - {node.GetDisplayName()}");
            }
        }

        private void HandleGraphLoadError(string error)
        {
            Debug.LogError($"[BasicExample] Failed to load graph: {error}");
        }

        private void HandleNodeUnlocked(ForkTrackNode node)
        {
            Log($"Node UNLOCKED: {node.GetDisplayName()}");
        }

        private void HandleNodeCompleted(ForkTrackNode node)
        {
            Log($"Node COMPLETED: {node.GetDisplayName()}");
        }

        private void HandleVariableChanged(ForkTrackVariable variable, object oldValue, object newValue)
        {
            Log($"Variable '{variable.name}' changed: {oldValue} -> {newValue}");
        }

        private void HandleEventTriggered(CustomEventData eventData)
        {
            Log($"Custom event triggered from node {eventData.nodeId}: Property={eventData.customEvent.propertyId}");
        }

        private void HandleNoteFired(ForkTrackNode node, NodeNote note)
        {
            Log($"Note fired from '{node.GetDisplayName()}': {note.text}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Completes a node by its object and action names.
        /// Call this from UI buttons or game triggers.
        /// </summary>
        public void CompleteNode(string objectName, string actionName)
        {
            if (!ForkTrack.IsLoaded)
            {
                Debug.LogWarning("[BasicExample] No graph loaded");
                return;
            }

            var node = ForkTrack.GetNode(objectName, actionName);
            if (node == null)
            {
                Debug.LogWarning($"[BasicExample] Node not found: {objectName} - {actionName}");
                return;
            }

            Log($"Attempting to complete: {objectName} - {actionName}");
            ForkTrack.CompleteNode(node.id);
        }

        /// <summary>
        /// Saves current progress
        /// </summary>
        public void SaveGame()
        {
            ForkTrack.SaveProgress("save1");
            Log("Progress saved to slot 'save1'");
        }

        /// <summary>
        /// Loads saved progress
        /// </summary>
        public void LoadGame()
        {
            if (ForkTrack.HasSavedProgress("save1"))
            {
                ForkTrack.LoadProgress("save1");
                Log("Progress loaded from slot 'save1'");
            }
            else
            {
                Log("No saved progress found");
            }
        }

        /// <summary>
        /// Resets all progress
        /// </summary>
        public void ResetGame()
        {
            ForkTrack.ResetAll();
            Log("All progress reset");
        }

        #endregion

        #region Helpers

        private void Log(string message)
        {
            if (logEvents)
            {
                Debug.Log($"[BasicExample] {message}");
            }
        }

        #endregion
    }
}
