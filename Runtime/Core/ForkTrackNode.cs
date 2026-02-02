using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForkTrack.Core
{
    /// <summary>
    /// Represents a single node in the narrative graph with state machine behavior
    /// </summary>
    [Serializable]
    public class ForkTrackNode
    {
        #region Identity Fields

        /// <summary>Unique identifier for this node</summary>
        public string id;

        /// <summary>Reference to the object definition (v1.1.0+)</summary>
        public string objectId;

        /// <summary>Reference to the action definition (v1.1.0+)</summary>
        public string actionId;

        /// <summary>Optional title for the node (nullable in v1.1.0+)</summary>
        public string title;

        /// <summary>Whether the title was manually overridden</summary>
        public bool titleOverridden;

        /// <summary>Detailed description of the node</summary>
        public string description;

        #endregion

        #region Display Names (Resolved from Objects/Actions)

        /// <summary>Resolved object name (set during graph initialization)</summary>
        [NonSerialized]
        public string objectName;

        /// <summary>Resolved action name (set during graph initialization)</summary>
        [NonSerialized]
        public string actionName;

        /// <summary>Resolved category name (set during graph initialization)</summary>
        [NonSerialized]
        public string categoryName;

        /// <summary>Resolved category color (set during graph initialization)</summary>
        [NonSerialized]
        public string categoryColor;

        /// <summary>Resolved group name (set during graph initialization)</summary>
        [NonSerialized]
        public string groupName;

        #endregion

        #region Categorization

        /// <summary>Category this node belongs to (can be null)</summary>
        public string categoryId;

        /// <summary>Group this node belongs to (can be null)</summary>
        public string groupId;

        #endregion

        #region State

        [SerializeField]
        private NodeState _state = NodeState.Locked;

        /// <summary>
        /// Current state of the node (Locked, Unlocked, or Completed)
        /// </summary>
        public NodeState State => _state;

        #endregion

        #region Configuration

        /// <summary>If true, node completes automatically when unlocked</summary>
        public bool autoCompleteOnUnlock;

        /// <summary>Music track to play (can be null)</summary>
        public string music;

        #endregion

        #region Collections

        /// <summary>Notes attached to this node with trigger conditions</summary>
        public List<NodeNote> notes = new List<NodeNote>();

        /// <summary>Custom events to fire when node triggers</summary>
        public List<CustomEvent> customEvents = new List<CustomEvent>();

        /// <summary>Development todos associated with this node</summary>
        public List<NodeTodo> todos = new List<NodeTodo>();

        /// <summary>Variable actions to execute on node triggers (v1.2.0)</summary>
        public List<VariableAction> variableActions = new List<VariableAction>();

        #endregion

        #region Position (Editor only)

        /// <summary>Position in the graph editor</summary>
        public Vector2 position;

        #endregion

        #region Convenience Properties

        /// <summary>Returns true if node is in Locked state</summary>
        public bool IsLocked => _state == NodeState.Locked;

        /// <summary>Returns true if node is in Unlocked state</summary>
        public bool IsUnlocked => _state == NodeState.Unlocked;

        /// <summary>Returns true if node is in Completed state</summary>
        public bool IsCompleted => _state == NodeState.Completed;

        #endregion

        #region State Machine Methods

        /// <summary>
        /// Unlocks this node, transitioning from Locked to Unlocked state.
        /// If autoCompleteOnUnlock is true, transitions directly to Completed.
        /// Idempotent: calling on an already Unlocked or Completed node has no effect.
        /// </summary>
        /// <returns>True if state changed, false otherwise</returns>
        public bool Unlock()
        {
            if (_state == NodeState.Completed)
            {
                // Cannot unlock a completed node
                return false;
            }

            if (_state == NodeState.Unlocked)
            {
                // Already unlocked, idempotent
                return false;
            }

            _state = NodeState.Unlocked;

            // Auto-complete if configured
            if (autoCompleteOnUnlock)
            {
                CompleteInternal();
            }

            return true;
        }

        /// <summary>
        /// Completes this node, transitioning from Unlocked to Completed state.
        /// Throws InvalidOperationException if node is Locked.
        /// Idempotent: calling on an already Completed node has no effect.
        /// </summary>
        /// <returns>True if state changed, false otherwise</returns>
        /// <exception cref="InvalidOperationException">Thrown if node is Locked</exception>
        public bool Complete()
        {
            if (_state == NodeState.Locked)
            {
                throw new InvalidOperationException(
                    $"Cannot complete locked node: {id} ({GetDisplayName()}). Call Unlock() first.");
            }

            if (_state == NodeState.Completed)
            {
                // Already completed, idempotent
                return false;
            }

            return CompleteInternal();
        }

        private bool CompleteInternal()
        {
            _state = NodeState.Completed;
            return true;
        }

        /// <summary>
        /// Resets this node to Locked state from any state.
        /// Idempotent: calling on an already Locked node has no effect.
        /// </summary>
        /// <returns>True if state changed, false otherwise</returns>
        public bool Reset()
        {
            if (_state == NodeState.Locked)
            {
                // Already locked, idempotent
                return false;
            }

            _state = NodeState.Locked;
            return true;
        }

        /// <summary>
        /// Sets the state directly (used for loading saved progress)
        /// </summary>
        internal void SetState(NodeState state)
        {
            _state = state;
        }

        #endregion

        #region Display Methods

        /// <summary>
        /// Gets the display name for this node based on object, action, and title
        /// </summary>
        /// <returns>
        /// "Object - Action: Title" if all present,
        /// "Object - Action" if no title,
        /// "Title" if only title,
        /// node ID if nothing else available
        /// </returns>
        public string GetDisplayName()
        {
            bool hasObject = !string.IsNullOrEmpty(objectName);
            bool hasAction = !string.IsNullOrEmpty(actionName);
            bool hasTitle = !string.IsNullOrEmpty(title);

            if (hasObject && hasAction)
            {
                string baseName = $"{objectName} - {actionName}";
                return hasTitle ? $"{baseName}: {title}" : baseName;
            }

            if (hasTitle)
            {
                return title;
            }

            return id;
        }

        /// <summary>
        /// Gets the variable actions that should execute on the specified trigger
        /// </summary>
        public List<VariableAction> GetVariableActionsForTrigger(TriggerType trigger)
        {
            var result = new List<VariableAction>();
            foreach (var action in variableActions)
            {
                if (action.GetTriggerType() == trigger)
                {
                    result.Add(action);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the notes that should fire on the specified trigger
        /// </summary>
        public List<NodeNote> GetNotesForTrigger(TriggerType trigger)
        {
            var result = new List<NodeNote>();
            foreach (var note in notes)
            {
                if (note.GetTriggerType() == trigger)
                {
                    result.Add(note);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the custom events that should fire on the specified trigger
        /// </summary>
        public List<CustomEvent> GetCustomEventsForTrigger(TriggerType trigger)
        {
            var result = new List<CustomEvent>();
            foreach (var evt in customEvents)
            {
                if (evt.GetTriggerType() == trigger)
                {
                    result.Add(evt);
                }
            }
            return result;
        }

        #endregion
    }
}
