using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ForkTrack.Core;

namespace ForkTrack.Internal
{
    /// <summary>
    /// Manages save/load of game progress (node states and variable values).
    /// REQ-130: Save/load game progress
    /// </summary>
    internal class ProgressManager
    {
        private readonly string _savePath;

        internal ProgressManager()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "ForkTrack", "Progress");
        }

        /// <summary>
        /// Saves the current progress for a graph
        /// REQ-131: Save node states
        /// REQ-132: Save variable values
        /// </summary>
        public void SaveProgress(string graphId, ForkTrackGraph graph, VariableStore variables)
        {
            if (string.IsNullOrEmpty(graphId) || graph == null)
                return;

            try
            {
                EnsureSaveDirectory();

                var progress = new ProgressData
                {
                    graphId = graphId,
                    savedAt = DateTime.UtcNow.ToString("o"),
                    nodeStates = new List<NodeStateData>(),
                    variableValues = new List<VariableValueData>()
                };

                // Save node states
                foreach (var node in graph.nodes)
                {
                    progress.nodeStates.Add(new NodeStateData
                    {
                        nodeId = node.id,
                        state = node.State.ToString()
                    });
                }

                // Save variable values
                if (variables != null)
                {
                    var exported = variables.Export();
                    foreach (var kvp in exported)
                    {
                        progress.variableValues.Add(new VariableValueData
                        {
                            variableId = kvp.Key,
                            value = SerializeValue(kvp.Value)
                        });
                    }
                }

                var json = JsonUtility.ToJson(progress, true);
                var path = GetSavePath(graphId);
                File.WriteAllText(path, json);

                Debug.Log($"[ForkTrack] Progress saved for graph: {graphId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ForkTrack] Failed to save progress: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads saved progress for a graph
        /// REQ-133: Restore node states on load
        /// REQ-134: Restore variable values on load
        /// </summary>
        public bool LoadProgress(string graphId, ForkTrackGraph graph, VariableStore variables)
        {
            if (string.IsNullOrEmpty(graphId) || graph == null)
                return false;

            try
            {
                var path = GetSavePath(graphId);
                if (!File.Exists(path))
                {
                    Debug.Log($"[ForkTrack] No saved progress found for graph: {graphId}");
                    return false;
                }

                var json = File.ReadAllText(path);
                var progress = JsonUtility.FromJson<ProgressData>(json);

                if (progress == null || progress.graphId != graphId)
                {
                    Debug.LogWarning($"[ForkTrack] Invalid progress data for graph: {graphId}");
                    return false;
                }

                // Restore node states
                foreach (var nodeState in progress.nodeStates)
                {
                    var node = graph.GetNodeById(nodeState.nodeId);
                    if (node != null && Enum.TryParse<NodeState>(nodeState.state, out var state))
                    {
                        node.SetState(state);
                    }
                }

                // Restore variable values
                if (variables != null && progress.variableValues != null)
                {
                    var values = new Dictionary<string, object>();
                    foreach (var varData in progress.variableValues)
                    {
                        values[varData.variableId] = DeserializeValue(varData.value);
                    }
                    variables.Import(values);
                }

                Debug.Log($"[ForkTrack] Progress loaded for graph: {graphId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ForkTrack] Failed to load progress: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if saved progress exists for a graph
        /// </summary>
        public bool HasSavedProgress(string graphId)
        {
            if (string.IsNullOrEmpty(graphId))
                return false;

            return File.Exists(GetSavePath(graphId));
        }

        /// <summary>
        /// Deletes saved progress for a graph
        /// </summary>
        public void DeleteProgress(string graphId)
        {
            try
            {
                var path = GetSavePath(graphId);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Debug.Log($"[ForkTrack] Progress deleted for graph: {graphId}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ForkTrack] Failed to delete progress: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears all saved progress
        /// </summary>
        public void ClearAllProgress()
        {
            try
            {
                if (Directory.Exists(_savePath))
                {
                    Directory.Delete(_savePath, true);
                    Debug.Log("[ForkTrack] All progress cleared");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ForkTrack] Failed to clear all progress: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets list of all saved graph IDs
        /// </summary>
        public List<string> GetSavedGraphIds()
        {
            var ids = new List<string>();

            try
            {
                if (Directory.Exists(_savePath))
                {
                    var files = Directory.GetFiles(_savePath, "*.json");
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        ids.Add(fileName.Replace("_", "/"));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ForkTrack] Failed to get saved graph IDs: {ex.Message}");
            }

            return ids;
        }

        #region Helpers

        private void EnsureSaveDirectory()
        {
            if (!Directory.Exists(_savePath))
            {
                Directory.CreateDirectory(_savePath);
            }
        }

        private string GetSavePath(string graphId)
        {
            var safeId = graphId.Replace("/", "_").Replace("\\", "_");
            return Path.Combine(_savePath, $"{safeId}.json");
        }

        private string SerializeValue(object value)
        {
            if (value is float f)
                return $"f:{f}";
            if (value is bool b)
                return $"b:{b}";
            if (value is int i)
                return $"i:{i}";
            return $"s:{value}";
        }

        private object DeserializeValue(string serialized)
        {
            if (string.IsNullOrEmpty(serialized))
                return null;

            var parts = serialized.Split(new[] { ':' }, 2);
            if (parts.Length != 2)
                return serialized;

            switch (parts[0])
            {
                case "f":
                    return float.TryParse(parts[1], out var f) ? f : 0f;
                case "b":
                    return bool.TryParse(parts[1], out var b) && b;
                case "i":
                    return int.TryParse(parts[1], out var i) ? i : 0;
                default:
                    return parts[1];
            }
        }

        #endregion

        #region Data Classes

        [Serializable]
        private class ProgressData
        {
            public string graphId;
            public string savedAt;
            public List<NodeStateData> nodeStates;
            public List<VariableValueData> variableValues;
        }

        [Serializable]
        private class NodeStateData
        {
            public string nodeId;
            public string state;
        }

        [Serializable]
        private class VariableValueData
        {
            public string variableId;
            public string value;
        }

        #endregion
    }
}
