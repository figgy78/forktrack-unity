using System;
using System.Collections.Generic;
using UnityEngine;
using ForkTrack.Core;

namespace ForkTrack.Internal
{
    /// <summary>
    /// Internal runtime manager that handles all ForkTrack operations
    /// </summary>
    internal class ForkTrackRuntime
    {
        #region Singleton

        private static ForkTrackRuntime _instance;
        internal static ForkTrackRuntime Instance => _instance ??= new ForkTrackRuntime();

        private ForkTrackRuntime()
        {
            _progressManager = new ProgressManager();
            _graphCache = new GraphCache();
        }

        internal static void Reset()
        {
            _instance = new ForkTrackRuntime();
        }

        #endregion

        #region State

        private ForkTrackGraph _graph;
        private VariableStore _variableStore;
        private DependencyEvaluator _dependencyEvaluator;
        private ProgressManager _progressManager;
        private GraphCache _graphCache;
        private string _currentGraphId;
        private bool _isLoaded;

        // Subscription tracking
        private List<NodeSubscription> _nodeSubscriptions = new List<NodeSubscription>();
        private List<EventSubscription> _eventSubscriptions = new List<EventSubscription>();
        private Dictionary<string, ForkTrackNode> _subscriptionNodeCache = new Dictionary<string, ForkTrackNode>();

        public ForkTrackGraph Graph => _graph;
        public VariableStore VariableStore => _variableStore;
        public bool IsLoaded => _isLoaded;
        public string CurrentGraphId => _currentGraphId;

        #endregion

        #region Events

        // Graph events
        internal event Action<ForkTrackGraph> OnGraphLoaded;
        internal event Action<string> OnGraphLoadError;

        // Node events
        internal event Action<ForkTrackNode> OnNodeUnlocked;
        internal event Action<ForkTrackNode> OnNodeCompleted;
        internal event Action<ForkTrackNode> OnNodeReset;
        internal event Action<ForkTrackNode, NodeState, NodeState> OnNodeStateChanged;

        // Variable events
        internal event Action<ForkTrackVariable, object, object> OnVariableChanged;

        // Custom events
        internal event Action<CustomEventData> OnEventTriggered;
        internal event Action<ForkTrackNode, NodeNote> OnNoteFired;

        #endregion

        #region Loading

        /// <summary>
        /// Loads a graph from JSON string
        /// REQ-001: Load from JSON string
        /// </summary>
        public void LoadGraphFromJSON(string json)
        {
            try
            {
                _graph = JsonGraphLoader.LoadFromJson(json);
                InitializeGraph();
                OnGraphLoaded?.Invoke(_graph);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ForkTrack] Failed to load graph: {ex.Message}");
                OnGraphLoadError?.Invoke(ex.Message);
            }
        }

        /// <summary>
        /// Loads a graph from TextAsset
        /// REQ-002: Load from TextAsset
        /// </summary>
        public void LoadGraphFromTextAsset(TextAsset textAsset)
        {
            if (textAsset == null)
            {
                OnGraphLoadError?.Invoke("TextAsset is null");
                return;
            }

            LoadGraphFromJSON(textAsset.text);
        }

        private void InitializeGraph()
        {
            if (_graph == null)
            {
                _isLoaded = false;
                return;
            }

            // Initialize variable store
            _variableStore = new VariableStore();
            _variableStore.Initialize(_graph.variables);
            _variableStore.OnVariableChanged += HandleVariableChanged;

            // Initialize dependency evaluator
            _dependencyEvaluator = new DependencyEvaluator(_variableStore);

            // Unlock root nodes (nodes with no incoming edges)
            UnlockRootNodes();

            _isLoaded = true;

            // Validate any existing subscriptions against the newly loaded graph
            ValidateSubscriptions();
        }

        private void UnlockRootNodes()
        {
            foreach (var node in _graph.nodes)
            {
                if (_dependencyEvaluator.CanUnlock(node, _graph))
                {
                    PerformUnlock(node);
                }
            }
        }

        #endregion

        #region Node Operations

        /// <summary>
        /// Unlocks a node by ID
        /// REQ-120: UnlockNode(nodeId)
        /// </summary>
        public bool UnlockNode(string nodeId)
        {
            EnsureLoaded();

            var node = _graph.GetNodeById(nodeId);
            if (node == null)
            {
                Debug.LogWarning($"[ForkTrack] Node not found: {nodeId}");
                return false;
            }

            return UnlockNode(node);
        }

        /// <summary>
        /// Unlocks a node by object+action
        /// REQ-121: UnlockNode(objectName, actionName)
        /// </summary>
        public bool UnlockNode(string objectName, string actionName)
        {
            EnsureLoaded();

            var node = _graph.GetNode(objectName, actionName);
            if (node == null)
            {
                Debug.LogWarning($"[ForkTrack] Node not found: {objectName} - {actionName}");
                return false;
            }

            return UnlockNode(node);
        }

        private bool UnlockNode(ForkTrackNode node)
        {
            if (!node.IsLocked)
            {
                return false;
            }

            if (!_dependencyEvaluator.CanUnlock(node, _graph))
            {
                Debug.LogWarning($"[ForkTrack] Dependencies not satisfied for node: {node.id}");
                return false;
            }

            return PerformUnlock(node);
        }

        private bool PerformUnlock(ForkTrackNode node)
        {
            var oldState = node.State;

            // REQ-082: Execute variableActions BEFORE node state changes
            ExecuteVariableActions(node, TriggerType.OnUnlock);

            // Perform unlock
            if (!node.Unlock())
            {
                return false;
            }

            var newState = node.State;

            // Fire events in order: REQ-030 through REQ-035
            // Notes -> State events

            // REQ-033: Fire OnNoteFired for OnUnlock notes
            FireNotes(node, TriggerType.OnUnlock);

            // Fire custom events for OnUnlock trigger
            FireCustomEvents(node, TriggerType.OnUnlock);

            // REQ-026: OnNodeStateChanged
            OnNodeStateChanged?.Invoke(node, oldState, newState);

            // REQ-030: OnNodeUnlocked
            OnNodeUnlocked?.Invoke(node);

            // Notify node subscriptions
            NotifyNodeSubscriptionsUnlocked(node);

            // If autoCompleteOnUnlock caused completion, fire completion events too
            if (node.IsCompleted)
            {
                FireCompletionEvents(node, NodeState.Unlocked);
            }

            // Cascade: dependents connected via OnUnlock edges become satisfied as
            // soon as this node is Unlocked (not just Completed), so re-evaluate.
            // Without this, children of an unlocked-but-not-completed parent stay
            // Locked unless UnlockRootNodes happens to iterate them after the parent.
            PerformCascadeUnlock(node.id);

            return true;
        }

        /// <summary>
        /// Completes a node by ID
        /// REQ-122: CompleteNode(nodeId)
        /// </summary>
        public bool CompleteNode(string nodeId)
        {
            EnsureLoaded();

            var node = _graph.GetNodeById(nodeId);
            if (node == null)
            {
                Debug.LogWarning($"[ForkTrack] Node not found: {nodeId}");
                return false;
            }

            return CompleteNode(node);
        }

        /// <summary>
        /// Completes a node by object+action
        /// REQ-123: CompleteNode(objectName, actionName)
        /// </summary>
        public bool CompleteNode(string objectName, string actionName)
        {
            EnsureLoaded();

            var node = _graph.GetNode(objectName, actionName);
            if (node == null)
            {
                Debug.LogWarning($"[ForkTrack] Node not found: {objectName} - {actionName}");
                return false;
            }

            return CompleteNode(node);
        }

        private bool CompleteNode(ForkTrackNode node)
        {
            // REQ-024: Cannot complete locked node
            if (node.IsLocked)
            {
                Debug.LogWarning($"[ForkTrack] Cannot complete locked node: {node.id}. Unlock first.");
                return false;
            }

            // REQ-023: Idempotent - already completed
            if (node.IsCompleted)
            {
                return false;
            }

            var oldState = node.State;

            // REQ-082: Execute variableActions BEFORE node state changes
            ExecuteVariableActions(node, TriggerType.OnComplete);

            // Perform completion
            if (!node.Complete())
            {
                return false;
            }

            FireCompletionEvents(node, oldState);

            // REQ-050-053: Cascade unlock dependent nodes
            PerformCascadeUnlock(node.id);

            return true;
        }

        private void FireCompletionEvents(ForkTrackNode node, NodeState oldState)
        {
            // REQ-034: Fire OnNoteFired for OnComplete notes
            FireNotes(node, TriggerType.OnComplete);

            // Fire custom events for OnComplete trigger
            FireCustomEvents(node, TriggerType.OnComplete);

            // REQ-026: OnNodeStateChanged
            OnNodeStateChanged?.Invoke(node, oldState, NodeState.Completed);

            // REQ-031: OnNodeCompleted
            OnNodeCompleted?.Invoke(node);

            // Notify node subscriptions
            NotifyNodeSubscriptionsCompleted(node);
        }

        /// <summary>
        /// Resets a node to Locked state
        /// REQ-124: ResetNode(nodeId)
        /// </summary>
        public bool ResetNode(string nodeId)
        {
            EnsureLoaded();

            var node = _graph.GetNodeById(nodeId);
            if (node == null)
            {
                Debug.LogWarning($"[ForkTrack] Node not found: {nodeId}");
                return false;
            }

            var oldState = node.State;

            if (!node.Reset())
            {
                return false;
            }

            // REQ-032: OnNodeReset
            OnNodeReset?.Invoke(node);
            OnNodeStateChanged?.Invoke(node, oldState, NodeState.Locked);

            return true;
        }

        /// <summary>
        /// Resets all nodes and variables
        /// REQ-125: ResetAll()
        /// </summary>
        public void ResetAll()
        {
            EnsureLoaded();

            // Reset all nodes
            foreach (var node in _graph.nodes)
            {
                if (!node.IsLocked)
                {
                    var oldState = node.State;
                    node.Reset();
                    OnNodeReset?.Invoke(node);
                    OnNodeStateChanged?.Invoke(node, oldState, NodeState.Locked);
                }
            }

            // Reset variables
            _variableStore.Reset();

            // Re-unlock root nodes
            UnlockRootNodes();
        }

        #endregion

        #region Cascade Unlock

        /// <summary>
        /// Performs cascade unlock after a node is completed
        /// REQ-050-053
        /// </summary>
        private void PerformCascadeUnlock(string completedNodeId)
        {
            var unlockQueue = new Queue<ForkTrackNode>();
            var unlocked = new HashSet<string>();

            // Get initially unlockable nodes
            var initialUnlockable = _dependencyEvaluator.GetUnlockableNodes(completedNodeId, _graph);
            foreach (var node in initialUnlockable)
            {
                if (!unlocked.Contains(node.id))
                {
                    unlockQueue.Enqueue(node);
                    unlocked.Add(node.id);
                }
            }

            // REQ-052: Process recursively
            while (unlockQueue.Count > 0)
            {
                var node = unlockQueue.Dequeue();

                if (PerformUnlock(node))
                {
                    // If node auto-completed, check its dependents too
                    if (node.IsCompleted)
                    {
                        var moreUnlockable = _dependencyEvaluator.GetUnlockableNodes(node.id, _graph);
                        foreach (var dependent in moreUnlockable)
                        {
                            if (!unlocked.Contains(dependent.id))
                            {
                                unlockQueue.Enqueue(dependent);
                                unlocked.Add(dependent.id);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Variable Actions & Events

        /// <summary>
        /// Executes variable actions for a trigger
        /// REQ-080-083
        /// </summary>
        private void ExecuteVariableActions(ForkTrackNode node, TriggerType trigger)
        {
            var actions = node.GetVariableActionsForTrigger(trigger);
            _variableStore.ExecuteActions(actions);
        }

        private void HandleVariableChanged(ForkTrackVariable variable, object oldValue, object newValue)
        {
            // REQ-063: Forward variable change events
            OnVariableChanged?.Invoke(variable, oldValue, newValue);
        }

        private void FireNotes(ForkTrackNode node, TriggerType trigger)
        {
            var notes = node.GetNotesForTrigger(trigger);
            foreach (var note in notes)
            {
                try
                {
                    OnNoteFired?.Invoke(node, note);
                }
                catch (Exception ex)
                {
                    // REQ-NFR-041: One handler crash doesn't stop others
                    Debug.LogError($"[ForkTrack] Note handler threw exception: {ex.Message}");
                }
            }
        }

        private void FireCustomEvents(ForkTrackNode node, TriggerType trigger)
        {
            var events = node.GetCustomEventsForTrigger(trigger);
            foreach (var evt in events)
            {
                try
                {
                    // Select value if weight-based
                    string selectedValue = null;
                    if (evt.values != null && evt.values.Count > 0)
                    {
                        selectedValue = evt.values[UnityEngine.Random.Range(0, evt.values.Count)];
                    }

                    var eventData = new CustomEventData(
                        node.id,
                        node.GetDisplayName(),
                        evt,
                        selectedValue,
                        trigger
                    );

                    OnEventTriggered?.Invoke(eventData);

                    // Notify event subscriptions
                    NotifyEventSubscriptions(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ForkTrack] Event handler threw exception: {ex.Message}");
                }
            }
        }

        #endregion

        #region Queries

        /// <summary>
        /// Gets a node by ID
        /// REQ-110: GetNode(nodeId)
        /// </summary>
        public ForkTrackNode GetNode(string nodeId)
        {
            EnsureLoaded();
            return _graph.GetNodeById(nodeId);
        }

        /// <summary>
        /// Gets a node by object+action
        /// REQ-111: GetNode(objectName, actionName)
        /// </summary>
        public ForkTrackNode GetNode(string objectName, string actionName)
        {
            EnsureLoaded();
            return _graph.GetNode(objectName, actionName);
        }

        /// <summary>
        /// Gets a node by title
        /// REQ-112: GetNodeByTitle(title)
        /// </summary>
        public ForkTrackNode GetNodeByTitle(string title)
        {
            EnsureLoaded();
            return _graph.GetNodeByTitle(title);
        }

        /// <summary>
        /// Gets all nodes with specified object
        /// REQ-113: GetNodesByObject(objectName)
        /// </summary>
        public List<ForkTrackNode> GetNodesByObject(string objectName)
        {
            EnsureLoaded();
            return _graph.GetNodesByObject(objectName);
        }

        /// <summary>
        /// Gets all nodes with specified action
        /// REQ-114: GetNodesByAction(actionName)
        /// </summary>
        public List<ForkTrackNode> GetNodesByAction(string actionName)
        {
            EnsureLoaded();
            return _graph.GetNodesByAction(actionName);
        }

        /// <summary>
        /// Gets all nodes in specified state
        /// REQ-115: GetNodesInState(state)
        /// </summary>
        public List<ForkTrackNode> GetNodesInState(NodeState state)
        {
            EnsureLoaded();
            return _graph.GetNodesInState(state);
        }

        /// <summary>
        /// Gets all nodes
        /// REQ-116: GetAllNodes()
        /// </summary>
        public List<ForkTrackNode> GetAllNodes()
        {
            EnsureLoaded();
            return new List<ForkTrackNode>(_graph.nodes);
        }

        #endregion

        #region Variable Queries

        /// <summary>
        /// Gets a variable value by name
        /// REQ-100: GetVariable<T>(name)
        /// </summary>
        public T GetVariable<T>(string variableName)
        {
            EnsureLoaded();
            return _variableStore.Get<T>(variableName);
        }

        /// <summary>
        /// Sets a variable value by name
        /// REQ-101: SetVariable<T>(name, value)
        /// </summary>
        public void SetVariable<T>(string variableName, T value)
        {
            EnsureLoaded();
            _variableStore.Set(variableName, value);
        }

        /// <summary>
        /// Gets all variable definitions
        /// REQ-102: GetAllVariables()
        /// </summary>
        public List<ForkTrackVariable> GetAllVariables()
        {
            EnsureLoaded();
            return _variableStore.GetAllVariables();
        }

        #endregion

        #region Helpers

        private void EnsureLoaded()
        {
            if (!_isLoaded || _graph == null)
            {
                throw new InvalidOperationException("No graph loaded. Call LoadGraph first.");
            }
        }

        #endregion

        #region Progress Operations

        /// <summary>
        /// Saves progress for the current graph
        /// REQ-150: SaveProgress(slotName)
        /// </summary>
        public void SaveProgress(string slotName = "default")
        {
            EnsureLoaded();
            var graphId = !string.IsNullOrEmpty(_currentGraphId) ? _currentGraphId : "local";
            var saveKey = $"{graphId}_{slotName}";
            _progressManager.SaveProgress(saveKey, _graph, _variableStore);
        }

        /// <summary>
        /// Loads progress for the current graph
        /// REQ-151: LoadProgress(slotName)
        /// </summary>
        public bool LoadProgress(string slotName = "default")
        {
            EnsureLoaded();
            var graphId = !string.IsNullOrEmpty(_currentGraphId) ? _currentGraphId : "local";
            var saveKey = $"{graphId}_{slotName}";
            return _progressManager.LoadProgress(saveKey, _graph, _variableStore);
        }

        /// <summary>
        /// Checks if saved progress exists
        /// REQ-152: HasSavedProgress(slotName)
        /// </summary>
        public bool HasSavedProgress(string slotName = "default")
        {
            var graphId = !string.IsNullOrEmpty(_currentGraphId) ? _currentGraphId : "local";
            var saveKey = $"{graphId}_{slotName}";
            return _progressManager.HasSavedProgress(saveKey);
        }

        /// <summary>
        /// Deletes saved progress
        /// REQ-153: DeleteProgress(slotName)
        /// </summary>
        public void DeleteProgress(string slotName = "default")
        {
            var graphId = !string.IsNullOrEmpty(_currentGraphId) ? _currentGraphId : "local";
            var saveKey = $"{graphId}_{slotName}";
            _progressManager.DeleteProgress(saveKey);
        }

        /// <summary>
        /// Exports current progress as JSON string
        /// REQ-154: ExportProgress()
        /// </summary>
        public string ExportProgress()
        {
            EnsureLoaded();

            var progress = new ProgressExport
            {
                graphId = _currentGraphId ?? "local",
                nodeStates = new List<NodeStateExport>(),
                variableValues = new List<VariableValueExport>()
            };

            foreach (var node in _graph.nodes)
            {
                progress.nodeStates.Add(new NodeStateExport
                {
                    nodeId = node.id,
                    state = node.State.ToString()
                });
            }

            if (_variableStore != null)
            {
                var exported = _variableStore.Export();
                foreach (var kvp in exported)
                {
                    progress.variableValues.Add(new VariableValueExport
                    {
                        variableId = kvp.Key,
                        value = kvp.Value
                    });
                }
            }

            return JsonUtility.ToJson(progress, true);
        }

        /// <summary>
        /// Imports progress from JSON string
        /// REQ-155: ImportProgress(json)
        /// </summary>
        public bool ImportProgress(string json)
        {
            EnsureLoaded();

            try
            {
                var progress = JsonUtility.FromJson<ProgressExport>(json);
                if (progress == null)
                    return false;

                // Restore node states
                if (progress.nodeStates != null)
                {
                    foreach (var nodeState in progress.nodeStates)
                    {
                        var node = _graph.GetNodeById(nodeState.nodeId);
                        if (node != null && Enum.TryParse<NodeState>(nodeState.state, out var state))
                        {
                            node.SetState(state);
                        }
                    }
                }

                // Restore variable values
                if (_variableStore != null && progress.variableValues != null)
                {
                    var values = new Dictionary<string, object>();
                    foreach (var varData in progress.variableValues)
                    {
                        values[varData.variableId] = varData.value;
                    }
                    _variableStore.Import(values);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ForkTrack] Failed to import progress: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Cache Operations

        /// <summary>
        /// Clears the graph cache
        /// </summary>
        public void ClearCache()
        {
            _graphCache.Clear();
        }

        #endregion

        #region Subscription Management

        /// <summary>
        /// Adds a node subscription
        /// </summary>
        public void AddNodeSubscription(NodeSubscription subscription)
        {
            if (subscription == null) return;

            _nodeSubscriptions.Add(subscription);

            // If a graph is already loaded, validate immediately
            if (_isLoaded)
            {
                ValidateNodeSubscription(subscription);
            }
        }

        /// <summary>
        /// Removes a node subscription by ID
        /// </summary>
        public void RemoveNodeSubscription(string subscriptionId)
        {
            _nodeSubscriptions.RemoveAll(s => s.Id == subscriptionId);
            _subscriptionNodeCache.Remove(subscriptionId);
        }

        /// <summary>
        /// Adds an event subscription
        /// </summary>
        public void AddEventSubscription(EventSubscription subscription)
        {
            if (subscription == null) return;

            _eventSubscriptions.Add(subscription);

            // If a graph is already loaded, validate immediately
            if (_isLoaded)
            {
                ValidateEventSubscription(subscription);
            }
        }

        /// <summary>
        /// Removes an event subscription by ID
        /// </summary>
        public void RemoveEventSubscription(string subscriptionId)
        {
            _eventSubscriptions.RemoveAll(s => s.Id == subscriptionId);
        }

        /// <summary>
        /// Gets all node subscriptions
        /// </summary>
        public List<NodeSubscription> GetNodeSubscriptions()
        {
            return new List<NodeSubscription>(_nodeSubscriptions);
        }

        /// <summary>
        /// Gets all event subscriptions
        /// </summary>
        public List<EventSubscription> GetEventSubscriptions()
        {
            return new List<EventSubscription>(_eventSubscriptions);
        }

        /// <summary>
        /// Validates all subscriptions against the current graph
        /// </summary>
        public void ValidateSubscriptions()
        {
            if (!_isLoaded || _graph == null)
            {
                Debug.LogWarning("[ForkTrack] Cannot validate subscriptions - no graph loaded");
                return;
            }

            // Clear the cache
            _subscriptionNodeCache.Clear();

            // Validate node subscriptions
            foreach (var sub in _nodeSubscriptions)
            {
                ValidateNodeSubscription(sub);
            }

            // Validate event subscriptions
            foreach (var sub in _eventSubscriptions)
            {
                ValidateEventSubscription(sub);
            }
        }

        private void ValidateNodeSubscription(NodeSubscription subscription)
        {
            subscription.IsValidated = true;
            ForkTrackNode node = null;

            switch (subscription.Type)
            {
                case NodeSubscriptionType.ById:
                    node = _graph.GetNodeById(subscription.NodeId);
                    if (node == null)
                    {
                        subscription.IsValid = false;
                        subscription.ValidationWarning = $"Node with ID \"{subscription.NodeId}\" not found in graph";
                        Debug.LogWarning($"[ForkTrack] Subscription warning: {subscription.ValidationWarning}");
                    }
                    break;

                case NodeSubscriptionType.ByTitle:
                    node = _graph.GetNodeByTitle(subscription.Title);
                    if (node == null)
                    {
                        subscription.IsValid = false;
                        subscription.ValidationWarning = $"Node with title \"{subscription.Title}\" not found in graph";
                        Debug.LogWarning($"[ForkTrack] Subscription warning: {subscription.ValidationWarning}");
                    }
                    break;

                case NodeSubscriptionType.ByObjectAction:
                    node = _graph.GetNode(subscription.ObjectName, subscription.ActionName);
                    if (node == null)
                    {
                        subscription.IsValid = false;
                        subscription.ValidationWarning = $"Node \"{subscription.ObjectName} - {subscription.ActionName}\" not found in graph";
                        Debug.LogWarning($"[ForkTrack] Subscription warning: {subscription.ValidationWarning}");
                    }
                    break;
            }

            if (node != null)
            {
                subscription.IsValid = true;
                subscription.ValidationWarning = null;
                _subscriptionNodeCache[subscription.Id] = node;
            }
        }

        private void ValidateEventSubscription(EventSubscription subscription)
        {
            subscription.IsValidated = true;

            // Check if any node has a custom event with this propertyId
            bool found = false;
            foreach (var node in _graph.nodes)
            {
                if (node.customEvents != null)
                {
                    foreach (var evt in node.customEvents)
                    {
                        if (evt.propertyId == subscription.PropertyId)
                        {
                            // If a specific value is requested, check if it's in the values list
                            if (!string.IsNullOrEmpty(subscription.Value))
                            {
                                if (evt.values != null && evt.values.Contains(subscription.Value))
                                {
                                    found = true;
                                    break;
                                }
                            }
                            else
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                }
                if (found) break;
            }

            if (!found)
            {
                subscription.IsValid = false;
                if (!string.IsNullOrEmpty(subscription.Value))
                {
                    subscription.ValidationWarning = $"No custom event with propertyId \"{subscription.PropertyId}\" and value \"{subscription.Value}\" found in any node";
                }
                else
                {
                    subscription.ValidationWarning = $"No custom event with propertyId \"{subscription.PropertyId}\" found in any node";
                }
                Debug.LogWarning($"[ForkTrack] Subscription warning: {subscription.ValidationWarning}");
            }
            else
            {
                subscription.IsValid = true;
                subscription.ValidationWarning = null;
            }
        }

        /// <summary>
        /// Notifies node subscriptions when a node is completed
        /// </summary>
        private void NotifyNodeSubscriptionsCompleted(ForkTrackNode node)
        {
            foreach (var sub in _nodeSubscriptions)
            {
                if (!sub.IsValid || sub.OnCompleted == null) continue;

                // Check if this subscription matches the node
                bool matches = false;

                if (_subscriptionNodeCache.TryGetValue(sub.Id, out var cachedNode))
                {
                    matches = cachedNode.id == node.id;
                }
                else
                {
                    // Fallback to direct matching
                    switch (sub.Type)
                    {
                        case NodeSubscriptionType.ById:
                            matches = node.id == sub.NodeId;
                            break;
                        case NodeSubscriptionType.ByTitle:
                            matches = string.Equals(node.title, sub.Title, StringComparison.OrdinalIgnoreCase);
                            break;
                        case NodeSubscriptionType.ByObjectAction:
                            matches = string.Equals(node.objectName, sub.ObjectName, StringComparison.OrdinalIgnoreCase)
                                   && string.Equals(node.actionName, sub.ActionName, StringComparison.OrdinalIgnoreCase);
                            break;
                    }
                }

                if (matches)
                {
                    try
                    {
                        sub.OnCompleted?.Invoke(node);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ForkTrack] Node subscription callback threw exception: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Notifies node subscriptions when a node is unlocked
        /// </summary>
        private void NotifyNodeSubscriptionsUnlocked(ForkTrackNode node)
        {
            foreach (var sub in _nodeSubscriptions)
            {
                if (!sub.IsValid || sub.OnUnlocked == null) continue;

                // Check if this subscription matches the node
                bool matches = false;

                if (_subscriptionNodeCache.TryGetValue(sub.Id, out var cachedNode))
                {
                    matches = cachedNode.id == node.id;
                }
                else
                {
                    // Fallback to direct matching
                    switch (sub.Type)
                    {
                        case NodeSubscriptionType.ById:
                            matches = node.id == sub.NodeId;
                            break;
                        case NodeSubscriptionType.ByTitle:
                            matches = string.Equals(node.title, sub.Title, StringComparison.OrdinalIgnoreCase);
                            break;
                        case NodeSubscriptionType.ByObjectAction:
                            matches = string.Equals(node.objectName, sub.ObjectName, StringComparison.OrdinalIgnoreCase)
                                   && string.Equals(node.actionName, sub.ActionName, StringComparison.OrdinalIgnoreCase);
                            break;
                    }
                }

                if (matches)
                {
                    try
                    {
                        sub.OnUnlocked?.Invoke(node);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ForkTrack] Node subscription callback threw exception: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Notifies event subscriptions when a custom event is fired
        /// </summary>
        private void NotifyEventSubscriptions(CustomEventData eventData)
        {
            foreach (var sub in _eventSubscriptions)
            {
                if (!sub.IsValid || sub.OnTriggered == null) continue;

                // Check if this subscription matches the event
                if (eventData.PropertyId != sub.PropertyId) continue;

                // If subscription has a specific value filter, check it
                if (!string.IsNullOrEmpty(sub.Value))
                {
                    if (eventData.selectedValue != sub.Value) continue;
                }

                try
                {
                    sub.OnTriggered?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ForkTrack] Event subscription callback threw exception: {ex.Message}");
                }
            }
        }

        #endregion

        #region Progress Data Classes

        [Serializable]
        private class ProgressExport
        {
            public string graphId;
            public List<NodeStateExport> nodeStates;
            public List<VariableValueExport> variableValues;
        }

        [Serializable]
        private class NodeStateExport
        {
            public string nodeId;
            public string state;
        }

        [Serializable]
        private class VariableValueExport
        {
            public string variableId;
            public object value;
        }

        #endregion
    }
}
