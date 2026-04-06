using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using ForkTrack.Core;
using ForkTrack.Config;
using ForkTrack.API;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ForkTrack.Components
{
    /// <summary>
    /// Optional MonoBehaviour for Inspector-based ForkTrack integration.
    /// Bridges static C# events to UnityEvents for easy hookup in the Inspector.
    /// REQ-190-193: ForkTrackController MonoBehaviour
    /// </summary>
    [AddComponentMenu("ForkTrack/ForkTrack Controller")]
    public class ForkTrackController : MonoBehaviour
    {
        #region Configuration

        [Header("Graph Configuration")]
        [Tooltip("Graph ID to load from API (leave empty for local graph only)")]
        [SerializeField] private string graphId;

        [Tooltip("Local graph JSON file (used as fallback or primary if graphId is empty)")]
        [SerializeField] private TextAsset localGraph;

        [Header("Load Settings")]
        [Tooltip("When to automatically load the graph")]
        [SerializeField] private LoadTiming loadTiming = LoadTiming.Start;

        [Tooltip("Force refresh from API even if cached")]
        [SerializeField] private bool forceRefresh = false;

        [Header("Progress Settings")]
        [Tooltip("Automatically save progress when nodes change")]
        [SerializeField] private bool autoSaveProgress = false;

        [Tooltip("Automatically load progress after graph loads")]
        [SerializeField] private bool autoLoadProgress = false;

        [Tooltip("Save slot name for auto save/load")]
        [SerializeField] private string saveSlotName = "default";

        #endregion

        #region Unity Events

        [Header("Graph Events")]
        [SerializeField] private UnityEvent<ForkTrackGraph> onGraphLoaded = new UnityEvent<ForkTrackGraph>();
        [SerializeField] private UnityEvent<string> onGraphLoadError = new UnityEvent<string>();

        [Header("Node Events")]
        [SerializeField] private UnityEvent<ForkTrackNode> onNodeUnlocked = new UnityEvent<ForkTrackNode>();
        [SerializeField] private UnityEvent<ForkTrackNode> onNodeCompleted = new UnityEvent<ForkTrackNode>();
        [SerializeField] private UnityEvent<ForkTrackNode> onNodeReset = new UnityEvent<ForkTrackNode>();

        [Header("Variable Events")]
        [SerializeField] private VariableChangedEvent onVariableChanged = new VariableChangedEvent();

        [Header("Custom Events")]
        [SerializeField] private UnityEvent<CustomEventData> onEventTriggered = new UnityEvent<CustomEventData>();
        [SerializeField] private NoteEvent onNoteFired = new NoteEvent();

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the graph ID to load
        /// </summary>
        public string GraphId
        {
            get => graphId;
            set => graphId = value;
        }

        /// <summary>
        /// Gets or sets the local graph TextAsset
        /// </summary>
        public TextAsset LocalGraph
        {
            get => localGraph;
            set => localGraph = value;
        }

        /// <summary>
        /// Gets whether a graph is currently loaded
        /// </summary>
        public bool IsLoaded => ForkTrack.IsLoaded;

        /// <summary>
        /// Gets the currently loaded graph
        /// </summary>
        public ForkTrackGraph CurrentGraph => ForkTrack.CurrentGraph;

        /// <summary>
        /// Gets or sets whether to force refresh from API even if a cached local graph is assigned
        /// </summary>
        public bool ForceRefresh
        {
            get => forceRefresh;
            set => forceRefresh = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SubscribeToEvents();

            if (loadTiming == LoadTiming.Awake)
            {
                LoadGraph();
            }
        }

        private void Start()
        {
            if (loadTiming == LoadTiming.Start)
            {
                LoadGraph();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually loads the configured graph
        /// </summary>
        public void LoadGraph()
        {
            if (!forceRefresh && localGraph != null)
            {
                ForkTrack.LoadGraphFromTextAsset(localGraph);
                return;
            }

            if (!string.IsNullOrEmpty(graphId))
            {
                var client = TokenAuthenticator.CreateClient();
                if (client == null)
                {
                    Debug.LogWarning("[ForkTrack] Cannot load graph from API: no API token configured.");
                    if (localGraph != null)
                    {
                        Debug.LogWarning("[ForkTrack] Falling back to local graph.");
                        ForkTrack.LoadGraphFromTextAsset(localGraph);
                    }
                    return;
                }

                StartCoroutine(client.FetchGraph(
                    graphId,
                    onSuccess: json =>
                    {
                        ForkTrack.LoadGraphFromJSON(json);
                        CacheGraphToFile(json, graphId);
                    },
                    onError: error =>
                    {
                        if (localGraph != null)
                        {
                            Debug.LogWarning($"[ForkTrack] API fetch failed: {error}. Falling back to local graph.");
                            ForkTrack.LoadGraphFromTextAsset(localGraph);
                        }
                        else
                        {
                            Debug.LogError($"[ForkTrack] API fetch failed and no local graph is set: {error}");
                        }
                    }
                ));
                return;
            }

            if (localGraph != null)
            {
                ForkTrack.LoadGraphFromTextAsset(localGraph);
                return;
            }

            Debug.LogWarning("[ForkTrack] No graph configured. Set either graphId or localGraph.");
        }

        /// <summary>
        /// Caches the fetched graph JSON to a versioned TextAsset file (Editor only)
        /// and assigns it to localGraph for offline fallback.
        /// </summary>
        private void CacheGraphToFile(string json, string id)
        {
#if UNITY_EDITOR
            try
            {
                string graphName = id;
                string major = "1";
                string minor = "0";

                // Attempt to extract version info from the JSON
                int majorIdx = json.IndexOf("\"major\":");
                if (majorIdx >= 0)
                {
                    int start = majorIdx + 8;
                    int end = start;
                    while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;
                    major = json.Substring(start, end - start).Trim();
                }

                int minorIdx = json.IndexOf("\"minor\":");
                if (minorIdx >= 0)
                {
                    int start = minorIdx + 8;
                    int end = start;
                    while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;
                    minor = json.Substring(start, end - start).Trim();
                }

                // Sanitize the graph name for use as a filename
                string sanitized = System.Text.RegularExpressions.Regex.Replace(graphName, @"[^a-zA-Z0-9_]", "_");
                string fileName = $"{sanitized}_v{major}_{minor}.json";
                string directory = "Assets/OnTapeRewind/Scripts/Forktrack";
                string filePath = $"{directory}/{fileName}";

                System.IO.Directory.CreateDirectory(directory);
                System.IO.File.WriteAllText(filePath, json);
                AssetDatabase.ImportAsset(filePath);

                var cachedAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
                if (cachedAsset != null)
                {
                    localGraph = cachedAsset;
                    Debug.Log($"[ForkTrack] Graph cached to {filePath} and assigned as localGraph.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ForkTrack] Failed to cache graph to file: {e.Message}");
            }
#endif
        }

        /// <summary>
        /// Saves progress to the configured slot
        /// </summary>
        public void SaveProgress()
        {
            ForkTrack.SaveProgress(saveSlotName);
        }

        /// <summary>
        /// Loads progress from the configured slot
        /// </summary>
        public void LoadProgress()
        {
            ForkTrack.LoadProgress(saveSlotName);
        }

        /// <summary>
        /// Completes a node by ID
        /// </summary>
        public void CompleteNode(string nodeId)
        {
            ForkTrack.CompleteNode(nodeId);
        }

        /// <summary>
        /// Unlocks a node by ID
        /// </summary>
        public void UnlockNode(string nodeId)
        {
            ForkTrack.UnlockNode(nodeId);
        }

        /// <summary>
        /// Resets a node by ID
        /// </summary>
        public void ResetNode(string nodeId)
        {
            ForkTrack.ResetNode(nodeId);
        }

        /// <summary>
        /// Resets all nodes and variables
        /// </summary>
        public void ResetAll()
        {
            ForkTrack.ResetAll();
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            ForkTrack.OnGraphLoaded += HandleGraphLoaded;
            ForkTrack.OnGraphLoadError += HandleGraphLoadError;
            ForkTrack.OnNodeUnlocked += HandleNodeUnlocked;
            ForkTrack.OnNodeCompleted += HandleNodeCompleted;
            ForkTrack.OnNodeReset += HandleNodeReset;
            ForkTrack.OnVariableChanged += HandleVariableChanged;
            ForkTrack.OnEventTriggered += HandleEventTriggered;
            ForkTrack.OnNoteFired += HandleNoteFired;
        }

        private void UnsubscribeFromEvents()
        {
            ForkTrack.OnGraphLoaded -= HandleGraphLoaded;
            ForkTrack.OnGraphLoadError -= HandleGraphLoadError;
            ForkTrack.OnNodeUnlocked -= HandleNodeUnlocked;
            ForkTrack.OnNodeCompleted -= HandleNodeCompleted;
            ForkTrack.OnNodeReset -= HandleNodeReset;
            ForkTrack.OnVariableChanged -= HandleVariableChanged;
            ForkTrack.OnEventTriggered -= HandleEventTriggered;
            ForkTrack.OnNoteFired -= HandleNoteFired;
        }

        #endregion

        #region Event Handlers

        private void HandleGraphLoaded(ForkTrackGraph graph)
        {
            onGraphLoaded?.Invoke(graph);

            if (autoLoadProgress)
            {
                ForkTrack.LoadProgress(saveSlotName);
            }
        }

        private void HandleGraphLoadError(string error)
        {
            onGraphLoadError?.Invoke(error);
        }

        private void HandleNodeUnlocked(ForkTrackNode node)
        {
            onNodeUnlocked?.Invoke(node);

            if (autoSaveProgress)
            {
                ForkTrack.SaveProgress(saveSlotName);
            }
        }

        private void HandleNodeCompleted(ForkTrackNode node)
        {
            onNodeCompleted?.Invoke(node);

            if (autoSaveProgress)
            {
                ForkTrack.SaveProgress(saveSlotName);
            }
        }

        private void HandleNodeReset(ForkTrackNode node)
        {
            onNodeReset?.Invoke(node);

            if (autoSaveProgress)
            {
                ForkTrack.SaveProgress(saveSlotName);
            }
        }

        private void HandleVariableChanged(ForkTrackVariable variable, object oldValue, object newValue)
        {
            onVariableChanged?.Invoke(variable, oldValue, newValue);

            if (autoSaveProgress)
            {
                ForkTrack.SaveProgress(saveSlotName);
            }
        }

        private void HandleEventTriggered(CustomEventData eventData)
        {
            onEventTriggered?.Invoke(eventData);
        }

        private void HandleNoteFired(ForkTrackNode node, NodeNote note)
        {
            onNoteFired?.Invoke(node, note);
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// When to automatically load the graph
        /// </summary>
        public enum LoadTiming
        {
            /// <summary>Do not auto-load (call LoadGraph manually)</summary>
            Manual,
            /// <summary>Load in Awake</summary>
            Awake,
            /// <summary>Load in Start</summary>
            Start
        }

        /// <summary>
        /// UnityEvent for variable changes (variable, oldValue, newValue)
        /// </summary>
        [Serializable]
        public class VariableChangedEvent : UnityEvent<ForkTrackVariable, object, object> { }

        /// <summary>
        /// UnityEvent for note firing (node, note)
        /// </summary>
        [Serializable]
        public class NoteEvent : UnityEvent<ForkTrackNode, NodeNote> { }

        #endregion
    }
}
