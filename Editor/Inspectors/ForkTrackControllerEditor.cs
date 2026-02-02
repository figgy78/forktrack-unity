using UnityEngine;
using UnityEditor;
using ForkTrack.Components;
using ForkTrack.Core;

namespace ForkTrack.Editor.Inspectors
{
    /// <summary>
    /// Custom Inspector for ForkTrackController.
    /// Shows graph info, node states, and provides quick actions.
    /// </summary>
    [CustomEditor(typeof(ForkTrackController))]
    public class ForkTrackControllerEditor : UnityEditor.Editor
    {
        private ForkTrackController _controller;
        private bool _showRuntimeInfo = true;
        private bool _showNodeStates = false;
        private Vector2 _nodeScrollPosition;

        private SerializedProperty _graphId;
        private SerializedProperty _localGraph;
        private SerializedProperty _loadTiming;
        private SerializedProperty _forceRefresh;
        private SerializedProperty _autoSaveProgress;
        private SerializedProperty _autoLoadProgress;
        private SerializedProperty _saveSlotName;

        private void OnEnable()
        {
            _controller = (ForkTrackController)target;

            _graphId = serializedObject.FindProperty("graphId");
            _localGraph = serializedObject.FindProperty("localGraph");
            _loadTiming = serializedObject.FindProperty("loadTiming");
            _forceRefresh = serializedObject.FindProperty("forceRefresh");
            _autoSaveProgress = serializedObject.FindProperty("autoSaveProgress");
            _autoLoadProgress = serializedObject.FindProperty("autoLoadProgress");
            _saveSlotName = serializedObject.FindProperty("saveSlotName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();
            EditorGUILayout.Space();

            DrawGraphConfiguration();
            EditorGUILayout.Space();

            DrawLoadSettings();
            EditorGUILayout.Space();

            DrawProgressSettings();
            EditorGUILayout.Space();

            if (Application.isPlaying)
            {
                DrawRuntimeInfo();
                EditorGUILayout.Space();

                DrawQuickActions();
                EditorGUILayout.Space();

                if (_controller.IsLoaded)
                {
                    DrawNodeStates();
                }
            }

            DrawEvents();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ForkTrack Controller", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                var statusColor = _controller.IsLoaded ? Color.green : Color.yellow;
                var statusText = _controller.IsLoaded ? "Loaded" : "Not Loaded";

                var style = new GUIStyle(EditorStyles.label);
                style.normal.textColor = statusColor;
                EditorGUILayout.LabelField(statusText, style, GUILayout.Width(80));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGraphConfiguration()
        {
            EditorGUILayout.LabelField("Graph Configuration", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_graphId, new GUIContent("Graph ID", "Graph ID to load from API"));
            EditorGUILayout.PropertyField(_localGraph, new GUIContent("Local Graph", "Local JSON file (fallback or primary)"));

            if (string.IsNullOrEmpty(_graphId.stringValue) && _localGraph.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Configure either a Graph ID for API loading or a Local Graph TextAsset.", MessageType.Warning);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawLoadSettings()
        {
            EditorGUILayout.LabelField("Load Settings", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_loadTiming, new GUIContent("Load Timing", "When to auto-load the graph"));
            EditorGUILayout.PropertyField(_forceRefresh, new GUIContent("Force Refresh", "Skip cache and fetch from API"));

            EditorGUI.indentLevel--;
        }

        private void DrawProgressSettings()
        {
            EditorGUILayout.LabelField("Progress Settings", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_autoSaveProgress, new GUIContent("Auto Save", "Save progress on node changes"));
            EditorGUILayout.PropertyField(_autoLoadProgress, new GUIContent("Auto Load", "Load progress after graph loads"));
            EditorGUILayout.PropertyField(_saveSlotName, new GUIContent("Save Slot", "Name of the save slot"));

            EditorGUI.indentLevel--;
        }

        private void DrawRuntimeInfo()
        {
            _showRuntimeInfo = EditorGUILayout.Foldout(_showRuntimeInfo, "Runtime Info", true);

            if (_showRuntimeInfo && _controller.IsLoaded)
            {
                EditorGUI.indentLevel++;

                var graph = _controller.CurrentGraph;
                if (graph != null)
                {
                    EditorGUILayout.LabelField("Nodes", graph.NodeCount.ToString());
                    EditorGUILayout.LabelField("Edges", graph.EdgeCount.ToString());
                    EditorGUILayout.LabelField("Variables", graph.VariableCount.ToString());

                    // Node state summary
                    int locked = 0, unlocked = 0, completed = 0;
                    if (graph.nodes != null)
                    {
                        foreach (var node in graph.nodes)
                        {
                            switch (node.State)
                            {
                                case NodeState.Locked: locked++; break;
                                case NodeState.Unlocked: unlocked++; break;
                                case NodeState.Completed: completed++; break;
                            }
                        }
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Node States");
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("Locked", locked.ToString());
                    EditorGUILayout.LabelField("Unlocked", unlocked.ToString());
                    EditorGUILayout.LabelField("Completed", completed.ToString());
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }
        }

        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Load Graph"))
            {
                _controller.LoadGraph();
            }

            GUI.enabled = _controller.IsLoaded;

            if (GUILayout.Button("Save Progress"))
            {
                _controller.SaveProgress();
            }

            if (GUILayout.Button("Load Progress"))
            {
                _controller.LoadProgress();
            }

            if (GUILayout.Button("Reset All"))
            {
                _controller.ResetAll();
            }

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNodeStates()
        {
            _showNodeStates = EditorGUILayout.Foldout(_showNodeStates, "Node States", true);

            if (_showNodeStates)
            {
                var graph = _controller.CurrentGraph;
                if (graph?.nodes == null) return;

                _nodeScrollPosition = EditorGUILayout.BeginScrollView(_nodeScrollPosition, GUILayout.MaxHeight(200));

                foreach (var node in graph.nodes)
                {
                    EditorGUILayout.BeginHorizontal();

                    // State indicator
                    var stateColor = GetStateColor(node.State);
                    var style = new GUIStyle(EditorStyles.label);
                    style.normal.textColor = stateColor;

                    EditorGUILayout.LabelField(node.State.ToString(), style, GUILayout.Width(80));
                    EditorGUILayout.LabelField(node.GetDisplayName());

                    GUI.enabled = node.State != NodeState.Completed;
                    if (GUILayout.Button("Complete", GUILayout.Width(70)))
                    {
                        ForkTrack.CompleteNode(node.id);
                    }
                    GUI.enabled = true;

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawEvents()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);

            // Draw remaining serialized properties (events)
            var iterator = serializedObject.GetIterator();
            iterator.NextVisible(true); // Skip script reference

            while (iterator.NextVisible(false))
            {
                // Skip properties we've already drawn
                if (iterator.name == "graphId" ||
                    iterator.name == "localGraph" ||
                    iterator.name == "loadTiming" ||
                    iterator.name == "forceRefresh" ||
                    iterator.name == "autoSaveProgress" ||
                    iterator.name == "autoLoadProgress" ||
                    iterator.name == "saveSlotName")
                {
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
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
    }
}
