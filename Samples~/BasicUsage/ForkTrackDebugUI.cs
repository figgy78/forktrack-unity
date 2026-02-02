using System.Collections.Generic;
using UnityEngine;
using ForkTrack;
using ForkTrack.Core;

namespace ForkTrack.Samples.BasicUsage
{
    /// <summary>
    /// Interactive debug UI for testing ForkTrack graphs.
    /// Provides a runtime interface to:
    /// - View all nodes and their states
    /// - Complete nodes interactively
    /// - View event log
    /// - View and modify variables
    /// - Test API connectivity
    /// </summary>
    public class ForkTrackDebugUI : MonoBehaviour
    {
        [Header("Graph Configuration")]
        [SerializeField] private TextAsset graphJson;

        [Header("API Testing")]
        [SerializeField] private string apiToken = "";
        [SerializeField] private string apiUrl = "https://forktrack.pixfork.com";

        // UI State
        private Vector2 _nodeScrollPos;
        private Vector2 _eventScrollPos;
        private Vector2 _variableScrollPos;
        private Vector2 _subscriptionScrollPos;
        private List<string> _eventLog = new List<string>();
        private string _selectedNodeId;
        private bool _showNodes = true;
        private bool _showEvents = true;
        private bool _showVariables = true;
        private bool _showAPI = false;
        private bool _showSubscriptions = false;
        private string _filterState = "All";

        // Subscription test state
        private string _subNodeId = "";
        private string _subObjectName = "";
        private string _subActionName = "";
        private string _subEventPropertyId = "";
        private List<NodeSubscription> _testNodeSubs = new List<NodeSubscription>();
        private List<EventSubscription> _testEventSubs = new List<EventSubscription>();

        // API State
        private bool _apiLoading;
        private string _apiError;
        private API.ForkTrackAPIClient.GraphListResponse _graphList;

        private void OnEnable()
        {
            // Subscribe to all events
            ForkTrack.OnGraphLoaded += OnGraphLoaded;
            ForkTrack.OnGraphLoadError += OnGraphLoadError;
            ForkTrack.OnNodeUnlocked += OnNodeUnlocked;
            ForkTrack.OnNodeCompleted += OnNodeCompleted;
            ForkTrack.OnNodeReset += OnNodeReset;
            ForkTrack.OnNodeStateChanged += OnNodeStateChanged;
            ForkTrack.OnVariableChanged += OnVariableChanged;
            ForkTrack.OnNoteFired += OnNoteFired;
            ForkTrack.OnEventTriggered += OnCustomEventTriggered;
        }

        private void OnDisable()
        {
            ForkTrack.OnGraphLoaded -= OnGraphLoaded;
            ForkTrack.OnGraphLoadError -= OnGraphLoadError;
            ForkTrack.OnNodeUnlocked -= OnNodeUnlocked;
            ForkTrack.OnNodeCompleted -= OnNodeCompleted;
            ForkTrack.OnNodeReset -= OnNodeReset;
            ForkTrack.OnNodeStateChanged -= OnNodeStateChanged;
            ForkTrack.OnVariableChanged -= OnVariableChanged;
            ForkTrack.OnNoteFired -= OnNoteFired;
            ForkTrack.OnEventTriggered -= OnCustomEventTriggered;
        }

        private void Start()
        {
            // Auto-load if graph is assigned
            if (graphJson != null && !ForkTrack.IsLoaded)
            {
                LoadGraph();
            }
        }

        #region Event Handlers

        private void OnGraphLoaded(ForkTrackGraph graph)
        {
            Log($"<color=green>GRAPH LOADED</color>: {graph.NodeCount} nodes, {graph.EdgeCount} edges, {graph.VariableCount} variables");
        }

        private void OnGraphLoadError(string error)
        {
            Log($"<color=red>GRAPH ERROR</color>: {error}");
        }

        private void OnNodeUnlocked(ForkTrackNode node)
        {
            Log($"<color=yellow>NODE UNLOCKED</color>: {node.GetDisplayName()} (id: {node.id})");
        }

        private void OnNodeCompleted(ForkTrackNode node)
        {
            Log($"<color=cyan>NODE COMPLETED</color>: {node.GetDisplayName()} (id: {node.id})");
        }

        private void OnNodeReset(ForkTrackNode node)
        {
            Log($"<color=orange>NODE RESET</color>: {node.GetDisplayName()} (id: {node.id})");
        }

        private void OnNodeStateChanged(ForkTrackNode node, NodeState oldState, NodeState newState)
        {
            Log($"STATE CHANGED: {node.GetDisplayName()} [{oldState} -> {newState}]");
        }

        private void OnVariableChanged(ForkTrackVariable variable, object oldValue, object newValue)
        {
            Log($"<color=magenta>VARIABLE</color>: {variable.name} [{oldValue} -> {newValue}]");
        }

        private void OnNoteFired(ForkTrackNode node, NodeNote note)
        {
            Log($"<color=white>NOTE</color>: [{node.GetDisplayName()}] {note.text}");
        }

        private void OnCustomEventTriggered(CustomEventData eventData)
        {
            // Use ToString() for full event details (node, property, value, weight, delay, amount, text)
            Log($"<color=blue>EVENT</color>: {eventData}");

            // Also show text/note if present (for easier debugging)
            if (!string.IsNullOrEmpty(eventData.Text))
            {
                Log($"  <color=#888888>Note:</color> {eventData.Text}");
            }
        }

        private void Log(string message)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            _eventLog.Add($"[{timestamp}] {message}");

            // Keep last 100 entries
            while (_eventLog.Count > 100)
            {
                _eventLog.RemoveAt(0);
            }

            // Auto-scroll to bottom
            _eventScrollPos.y = float.MaxValue;

            Debug.Log($"[ForkTrackDebug] {message}");
        }

        #endregion

        #region Actions

        private void LoadGraph()
        {
            if (graphJson != null)
            {
                _eventLog.Clear();
                ForkTrack.LoadGraphFromJSON(graphJson.text);
            }
            else
            {
                Log("<color=red>No graph JSON assigned!</color>");
            }
        }

        private void FetchGraphList()
        {
            if (string.IsNullOrEmpty(apiToken))
            {
                Log("<color=red>API token not set!</color>");
                return;
            }

            _apiLoading = true;
            _apiError = null;

            var client = new API.ForkTrackAPIClient(apiToken, apiUrl);
            StartCoroutine(client.ListGraphs(
                onSuccess: response =>
                {
                    _graphList = response;
                    _apiLoading = false;
                    Log($"<color=green>API</color>: Found {response.graphs.Length} graphs");
                },
                onError: error =>
                {
                    _apiError = error;
                    _apiLoading = false;
                    Log($"<color=red>API ERROR</color>: {error}");
                }
            ));
        }

        private void LoadGraphFromAPI(string graphId)
        {
            if (string.IsNullOrEmpty(apiToken)) return;

            _apiLoading = true;
            _apiError = null;

            var client = new API.ForkTrackAPIClient(apiToken, apiUrl);
            StartCoroutine(client.FetchGraph(graphId,
                onSuccess: json =>
                {
                    _apiLoading = false;
                    _eventLog.Clear();
                    ForkTrack.LoadGraphFromJSON(json);
                },
                onError: error =>
                {
                    _apiError = error;
                    _apiLoading = false;
                    Log($"<color=red>API ERROR</color>: {error}");
                }
            ));
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            // Setup styles
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            // Main panel
            GUILayout.BeginArea(new Rect(10, 10, 400, Screen.height - 20));
            GUILayout.BeginVertical(boxStyle);

            // Header
            GUILayout.Label("ForkTrack Debug UI", headerStyle);
            GUILayout.Space(5);

            // Status
            if (ForkTrack.IsLoaded)
            {
                var graph = ForkTrack.CurrentGraph;
                GUILayout.Label($"Status: <color=green>Loaded</color> | Nodes: {graph.NodeCount} | Edges: {graph.EdgeCount} | Variables: {graph.VariableCount}", GUI.skin.label);
            }
            else
            {
                GUILayout.Label($"Status: <color=yellow>Not Loaded</color>", GUI.skin.label);
            }

            GUILayout.Space(10);

            // Control buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Graph"))
            {
                LoadGraph();
            }
            if (GUILayout.Button("Reset All"))
            {
                ForkTrack.ResetAll();
                Log("Reset all nodes and variables");
            }
            if (GUILayout.Button("Clear Log"))
            {
                _eventLog.Clear();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Section toggles
            GUILayout.BeginHorizontal();
            _showNodes = GUILayout.Toggle(_showNodes, "Nodes", GUILayout.Width(60));
            _showVariables = GUILayout.Toggle(_showVariables, "Vars", GUILayout.Width(50));
            _showEvents = GUILayout.Toggle(_showEvents, "Events", GUILayout.Width(60));
            _showSubscriptions = GUILayout.Toggle(_showSubscriptions, "Subs", GUILayout.Width(50));
            _showAPI = GUILayout.Toggle(_showAPI, "API", GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Nodes Section
            if (_showNodes && ForkTrack.IsLoaded)
            {
                DrawNodesSection();
            }

            // Variables Section
            if (_showVariables && ForkTrack.IsLoaded)
            {
                DrawVariablesSection();
            }

            // Events Section
            if (_showEvents)
            {
                DrawEventsSection();
            }

            // Subscriptions Section
            if (_showSubscriptions)
            {
                DrawSubscriptionsSection();
            }

            // API Section
            if (_showAPI)
            {
                DrawAPISection();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawNodesSection()
        {
            GUILayout.Label("--- Nodes ---");

            // Filter
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter:", GUILayout.Width(40));
            if (GUILayout.Button("All", GUILayout.Width(50))) _filterState = "All";
            if (GUILayout.Button("Locked", GUILayout.Width(60))) _filterState = "Locked";
            if (GUILayout.Button("Unlocked", GUILayout.Width(70))) _filterState = "Unlocked";
            if (GUILayout.Button("Completed", GUILayout.Width(80))) _filterState = "Completed";
            GUILayout.EndHorizontal();

            _nodeScrollPos = GUILayout.BeginScrollView(_nodeScrollPos, GUILayout.Height(200));

            var nodes = ForkTrack.GetAllNodes();
            foreach (var node in nodes)
            {
                // Apply filter
                if (_filterState != "All" && node.State.ToString() != _filterState)
                    continue;

                GUILayout.BeginHorizontal();

                // State color indicator
                Color stateColor = GetStateColor(node.State);
                var prevColor = GUI.color;
                GUI.color = stateColor;
                GUILayout.Label($"[{node.State}]", GUILayout.Width(80));
                GUI.color = prevColor;

                // Node name
                string displayName = node.GetDisplayName();
                if (displayName.Length > 25)
                    displayName = displayName.Substring(0, 22) + "...";

                GUILayout.Label(displayName, GUILayout.Width(180));

                // Complete button
                GUI.enabled = node.IsUnlocked;
                if (GUILayout.Button("Complete", GUILayout.Width(70)))
                {
                    ForkTrack.CompleteNode(node.id);
                }
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        private void DrawVariablesSection()
        {
            GUILayout.Label("--- Variables ---");

            _variableScrollPos = GUILayout.BeginScrollView(_variableScrollPos, GUILayout.Height(100));

            var graph = ForkTrack.CurrentGraph;
            if (graph?.variables != null)
            {
                foreach (var variable in graph.variables)
                {
                    GUILayout.BeginHorizontal();

                    GUILayout.Label(variable.name, GUILayout.Width(100));

                    var type = variable.GetVariableType();
                    if (type == VariableType.NUMBER)
                    {
                        float value = ForkTrack.GetVariable<float>(variable.name);
                        GUILayout.Label(value.ToString("F1"), GUILayout.Width(60));

                        if (GUILayout.Button("-10", GUILayout.Width(40)))
                        {
                            ForkTrack.SetVariable(variable.name, value - 10);
                        }
                        if (GUILayout.Button("+10", GUILayout.Width(40)))
                        {
                            ForkTrack.SetVariable(variable.name, value + 10);
                        }
                    }
                    else if (type == VariableType.BOOLEAN)
                    {
                        bool value = ForkTrack.GetVariable<bool>(variable.name);
                        GUILayout.Label(value.ToString(), GUILayout.Width(60));

                        if (GUILayout.Button("Toggle", GUILayout.Width(60)))
                        {
                            ForkTrack.SetVariable(variable.name, !value);
                        }
                    }

                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawEventsSection()
        {
            GUILayout.Label($"--- Event Log ({_eventLog.Count}) ---");

            _eventScrollPos = GUILayout.BeginScrollView(_eventScrollPos, GUILayout.Height(150));

            // Use RichText style
            var style = new GUIStyle(GUI.skin.label) { richText = true };

            foreach (var entry in _eventLog)
            {
                GUILayout.Label(entry, style);
            }

            GUILayout.EndScrollView();
        }

        private void DrawSubscriptionsSection()
        {
            GUILayout.Label("--- Subscriptions ---");

            _subscriptionScrollPos = GUILayout.BeginScrollView(_subscriptionScrollPos, GUILayout.Height(200));

            // Node subscription by ID
            GUILayout.Label("Subscribe to Node by ID:");
            GUILayout.BeginHorizontal();
            _subNodeId = GUILayout.TextField(_subNodeId, GUILayout.Width(200));
            if (GUILayout.Button("Subscribe", GUILayout.Width(80)))
            {
                if (!string.IsNullOrEmpty(_subNodeId))
                {
                    var sub = ForkTrack.SubscribeNode(_subNodeId,
                        node => Log($"<color=lime>SUBSCRIBED NODE COMPLETED</color>: {node.GetDisplayName()}"),
                        node => Log($"<color=yellow>SUBSCRIBED NODE UNLOCKED</color>: {node.GetDisplayName()}")
                    );
                    _testNodeSubs.Add(sub);
                    Log($"<color=white>Subscribed to node ID: {_subNodeId}</color>");
                    _subNodeId = "";
                }
            }
            GUILayout.EndHorizontal();

            // Node subscription by Object+Action
            GUILayout.Label("Subscribe to Node by Object+Action:");
            GUILayout.BeginHorizontal();
            _subObjectName = GUILayout.TextField(_subObjectName, GUILayout.Width(95));
            _subActionName = GUILayout.TextField(_subActionName, GUILayout.Width(95));
            if (GUILayout.Button("Subscribe", GUILayout.Width(80)))
            {
                if (!string.IsNullOrEmpty(_subObjectName) && !string.IsNullOrEmpty(_subActionName))
                {
                    var sub = ForkTrack.SubscribeNode(_subObjectName, _subActionName,
                        node => Log($"<color=lime>SUBSCRIBED NODE COMPLETED</color>: {node.GetDisplayName()}"),
                        node => Log($"<color=yellow>SUBSCRIBED NODE UNLOCKED</color>: {node.GetDisplayName()}")
                    );
                    _testNodeSubs.Add(sub);
                    Log($"<color=white>Subscribed to: {_subObjectName} - {_subActionName}</color>");
                    _subObjectName = "";
                    _subActionName = "";
                }
            }
            GUILayout.EndHorizontal();

            // Event subscription
            GUILayout.Label("Subscribe to Event by PropertyId:");
            GUILayout.BeginHorizontal();
            _subEventPropertyId = GUILayout.TextField(_subEventPropertyId, GUILayout.Width(200));
            if (GUILayout.Button("Subscribe", GUILayout.Width(80)))
            {
                if (!string.IsNullOrEmpty(_subEventPropertyId))
                {
                    var sub = ForkTrack.SubscribeEvent(_subEventPropertyId,
                        evt => Log($"<color=lime>SUBSCRIBED EVENT</color>: {evt}")
                    );
                    _testEventSubs.Add(sub);
                    Log($"<color=white>Subscribed to event: {_subEventPropertyId}</color>");
                    _subEventPropertyId = "";
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Show active subscriptions
            var allNodeSubs = ForkTrack.GetNodeSubscriptions();
            var allEventSubs = ForkTrack.GetEventSubscriptions();

            GUILayout.Label($"Active Node Subs: {allNodeSubs.Count}");
            foreach (var sub in allNodeSubs)
            {
                GUILayout.BeginHorizontal();
                string status = sub.IsValid ? "<color=green>✓</color>" : "<color=red>✗</color>";
                var style = new GUIStyle(GUI.skin.label) { richText = true };
                GUILayout.Label($"{status} {sub.GetTargetDescription()}", style, GUILayout.Width(250));
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    ForkTrack.UnsubscribeNode(sub);
                    _testNodeSubs.Remove(sub);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Label($"Active Event Subs: {allEventSubs.Count}");
            foreach (var sub in allEventSubs)
            {
                GUILayout.BeginHorizontal();
                string status = sub.IsValid ? "<color=green>✓</color>" : "<color=red>✗</color>";
                var style = new GUIStyle(GUI.skin.label) { richText = true };
                GUILayout.Label($"{status} {sub.GetTargetDescription()}", style, GUILayout.Width(250));
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    ForkTrack.UnsubscribeEvent(sub);
                    _testEventSubs.Remove(sub);
                }
                GUILayout.EndHorizontal();
            }

            // Clear all button
            GUILayout.Space(5);
            if (GUILayout.Button("Clear All Subscriptions"))
            {
                foreach (var sub in _testNodeSubs.ToArray())
                {
                    ForkTrack.UnsubscribeNode(sub);
                }
                foreach (var sub in _testEventSubs.ToArray())
                {
                    ForkTrack.UnsubscribeEvent(sub);
                }
                _testNodeSubs.Clear();
                _testEventSubs.Clear();
                Log("<color=white>Cleared all subscriptions</color>");
            }

            GUILayout.EndScrollView();
        }

        private void DrawAPISection()
        {
            GUILayout.Label("--- API Testing ---");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Token:", GUILayout.Width(50));
            apiToken = GUILayout.TextField(apiToken, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.enabled = !_apiLoading;
            if (GUILayout.Button("Validate Token"))
            {
                _apiLoading = true;
                var client = new API.ForkTrackAPIClient(apiToken, apiUrl);
                StartCoroutine(client.ValidateToken(isValid =>
                {
                    _apiLoading = false;
                    Log(isValid ? "<color=green>Token valid!</color>" : "<color=red>Token invalid!</color>");
                }));
            }
            if (GUILayout.Button("List Graphs"))
            {
                FetchGraphList();
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            if (_apiLoading)
            {
                GUILayout.Label("<color=yellow>Loading...</color>", new GUIStyle(GUI.skin.label) { richText = true });
            }

            if (!string.IsNullOrEmpty(_apiError))
            {
                GUILayout.Label($"<color=red>{_apiError}</color>", new GUIStyle(GUI.skin.label) { richText = true });
            }

            // Graph list
            if (_graphList?.graphs != null && _graphList.graphs.Length > 0)
            {
                GUILayout.Label($"Available Graphs ({_graphList.graphs.Length}):");

                foreach (var graph in _graphList.graphs)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(graph.name, GUILayout.Width(200));
                    if (GUILayout.Button("Load", GUILayout.Width(60)))
                    {
                        LoadGraphFromAPI(graph.id);
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }

        private Color GetStateColor(NodeState state)
        {
            switch (state)
            {
                case NodeState.Locked: return Color.gray;
                case NodeState.Unlocked: return Color.yellow;
                case NodeState.Completed: return Color.green;
                default: return Color.white;
            }
        }

        #endregion
    }
}
