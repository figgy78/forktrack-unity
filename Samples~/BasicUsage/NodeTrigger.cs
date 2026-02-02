using UnityEngine;
using UnityEngine.Events;
using ForkTrack;
using ForkTrack.Core;

namespace ForkTrack.Samples.BasicUsage
{
    /// <summary>
    /// Simple trigger component to complete ForkTrack nodes.
    /// Attach to interactable objects like NPCs, items, or doors.
    /// </summary>
    public class NodeTrigger : MonoBehaviour
    {
        [Header("Node Configuration")]
        [Tooltip("Complete node by ID")]
        [SerializeField] private string nodeId;

        [Tooltip("Or complete by object name")]
        [SerializeField] private string objectName;

        [Tooltip("And action name")]
        [SerializeField] private string actionName;

        [Header("Trigger Settings")]
        [Tooltip("Complete on trigger enter (requires collider with IsTrigger)")]
        [SerializeField] private bool completeOnTriggerEnter = false;

        [Tooltip("Tag filter for trigger (leave empty for any)")]
        [SerializeField] private string triggerTag = "Player";

        [Tooltip("Only trigger once")]
        [SerializeField] private bool triggerOnce = true;

        [Header("Visual Feedback")]
        [Tooltip("Disable renderer when node is completed")]
        [SerializeField] private bool hideWhenCompleted = false;

        [Header("Events")]
        [SerializeField] private UnityEvent onTriggered;
        [SerializeField] private UnityEvent onAlreadyCompleted;

        private bool _hasTriggered = false;

        private void Start()
        {
            if (hideWhenCompleted)
            {
                ForkTrack.OnNodeCompleted += CheckHideState;
                CheckHideStateImmediate();
            }
        }

        private void OnDestroy()
        {
            if (hideWhenCompleted)
            {
                ForkTrack.OnNodeCompleted -= CheckHideState;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!completeOnTriggerEnter)
                return;

            if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag))
                return;

            TriggerNode();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!completeOnTriggerEnter)
                return;

            if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag))
                return;

            TriggerNode();
        }

        /// <summary>
        /// Call this to trigger the node completion (e.g., from a button click)
        /// </summary>
        public void TriggerNode()
        {
            if (!ForkTrack.IsLoaded)
            {
                Debug.LogWarning($"[NodeTrigger] {name}: No graph loaded");
                return;
            }

            if (triggerOnce && _hasTriggered)
            {
                return;
            }

            var node = GetTargetNode();
            if (node == null)
            {
                Debug.LogWarning($"[NodeTrigger] {name}: Node not found");
                return;
            }

            if (node.IsCompleted)
            {
                onAlreadyCompleted?.Invoke();
                return;
            }

            bool success = ForkTrack.CompleteNode(node.id);
            if (success)
            {
                _hasTriggered = true;
                onTriggered?.Invoke();
                Debug.Log($"[NodeTrigger] {name}: Completed node '{node.GetDisplayName()}'");
            }
            else
            {
                Debug.LogWarning($"[NodeTrigger] {name}: Could not complete node (may be locked)");
            }
        }

        /// <summary>
        /// Returns the current state of the target node
        /// </summary>
        public NodeState GetNodeState()
        {
            var node = GetTargetNode();
            return node?.State ?? NodeState.Locked;
        }

        private ForkTrackNode GetTargetNode()
        {
            if (!string.IsNullOrEmpty(nodeId))
            {
                return ForkTrack.GetNode(nodeId);
            }

            if (!string.IsNullOrEmpty(objectName) && !string.IsNullOrEmpty(actionName))
            {
                return ForkTrack.GetNode(objectName, actionName);
            }

            return null;
        }

        private void CheckHideState(ForkTrackNode completedNode)
        {
            var targetNode = GetTargetNode();
            if (targetNode != null && targetNode.id == completedNode.id)
            {
                SetVisible(false);
            }
        }

        private void CheckHideStateImmediate()
        {
            if (!ForkTrack.IsLoaded)
                return;

            var node = GetTargetNode();
            if (node != null && node.IsCompleted)
            {
                SetVisible(false);
            }
        }

        private void SetVisible(bool visible)
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = visible;
            }

            // Also check children
            foreach (var childRenderer in GetComponentsInChildren<Renderer>())
            {
                childRenderer.enabled = visible;
            }
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure at least one identifier is set
            if (string.IsNullOrEmpty(nodeId) &&
                (string.IsNullOrEmpty(objectName) || string.IsNullOrEmpty(actionName)))
            {
                Debug.LogWarning($"[NodeTrigger] {name}: Set either nodeId OR both objectName and actionName");
            }
        }
        #endif
    }
}
