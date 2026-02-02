using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using ForkTrack;
using ForkTrack.Core;

namespace ForkTrack.Samples.QuestSystem
{
    /// <summary>
    /// Quest manager that integrates ForkTrack with a game's quest system.
    /// Demonstrates category filtering, progress tracking, and quest UI integration.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        [Header("Graph Configuration")]
        [SerializeField] private TextAsset questGraph;

        [Header("Quest Categories")]
        [SerializeField] private string mainQuestCategory = "Main Quest";
        [SerializeField] private string sideQuestCategory = "Side Quest";

        [Header("Events")]
        [SerializeField] private UnityEvent<QuestInfo> onQuestUnlocked;
        [SerializeField] private UnityEvent<QuestInfo> onQuestCompleted;
        [SerializeField] private UnityEvent<int, int> onProgressChanged; // completed, total

        /// <summary>
        /// Information about a quest (ForkTrack node wrapper)
        /// </summary>
        [System.Serializable]
        public class QuestInfo
        {
            public string id;
            public string displayName;
            public string description;
            public string category;
            public NodeState state;
            public bool isMainQuest;

            public QuestInfo(ForkTrackNode node, ForkTrackGraph graph)
            {
                id = node.id;
                displayName = node.GetDisplayName();
                description = node.description;
                state = node.State;

                // Find category name
                if (!string.IsNullOrEmpty(node.category))
                {
                    foreach (var cat in graph.categories)
                    {
                        if (cat.id == node.category)
                        {
                            category = cat.name;
                            break;
                        }
                    }
                }
            }
        }

        private Dictionary<string, QuestInfo> _quests = new Dictionary<string, QuestInfo>();

        private void Start()
        {
            // Subscribe to ForkTrack events
            ForkTrack.OnGraphLoaded += HandleGraphLoaded;
            ForkTrack.OnNodeUnlocked += HandleNodeUnlocked;
            ForkTrack.OnNodeCompleted += HandleNodeCompleted;

            // Load the quest graph
            if (questGraph != null)
            {
                ForkTrack.LoadGraphFromJSON(questGraph.text);
            }
        }

        private void OnDestroy()
        {
            ForkTrack.OnGraphLoaded -= HandleGraphLoaded;
            ForkTrack.OnNodeUnlocked -= HandleNodeUnlocked;
            ForkTrack.OnNodeCompleted -= HandleNodeCompleted;
        }

        #region Public API

        /// <summary>
        /// Gets all quests
        /// </summary>
        public List<QuestInfo> GetAllQuests()
        {
            return new List<QuestInfo>(_quests.Values);
        }

        /// <summary>
        /// Gets quests by state
        /// </summary>
        public List<QuestInfo> GetQuestsByState(NodeState state)
        {
            var result = new List<QuestInfo>();
            foreach (var quest in _quests.Values)
            {
                if (quest.state == state)
                    result.Add(quest);
            }
            return result;
        }

        /// <summary>
        /// Gets active (unlocked) quests
        /// </summary>
        public List<QuestInfo> GetActiveQuests()
        {
            return GetQuestsByState(NodeState.Unlocked);
        }

        /// <summary>
        /// Gets completed quests
        /// </summary>
        public List<QuestInfo> GetCompletedQuests()
        {
            return GetQuestsByState(NodeState.Completed);
        }

        /// <summary>
        /// Gets main quests only
        /// </summary>
        public List<QuestInfo> GetMainQuests()
        {
            var result = new List<QuestInfo>();
            foreach (var quest in _quests.Values)
            {
                if (quest.isMainQuest)
                    result.Add(quest);
            }
            return result;
        }

        /// <summary>
        /// Gets side quests only
        /// </summary>
        public List<QuestInfo> GetSideQuests()
        {
            var result = new List<QuestInfo>();
            foreach (var quest in _quests.Values)
            {
                if (!quest.isMainQuest)
                    result.Add(quest);
            }
            return result;
        }

        /// <summary>
        /// Gets progress (completed / total)
        /// </summary>
        public (int completed, int total) GetProgress()
        {
            int completed = 0;
            int total = _quests.Count;

            foreach (var quest in _quests.Values)
            {
                if (quest.state == NodeState.Completed)
                    completed++;
            }

            return (completed, total);
        }

        /// <summary>
        /// Completes a quest by ID
        /// </summary>
        public bool CompleteQuest(string questId)
        {
            return ForkTrack.CompleteNode(questId);
        }

        /// <summary>
        /// Saves quest progress
        /// </summary>
        public void SaveProgress()
        {
            ForkTrack.SaveProgress("quest_progress");
            Debug.Log("[QuestManager] Progress saved");
        }

        /// <summary>
        /// Loads quest progress
        /// </summary>
        public void LoadProgress()
        {
            if (ForkTrack.HasSavedProgress("quest_progress"))
            {
                ForkTrack.LoadProgress("quest_progress");
                RefreshQuestStates();
                Debug.Log("[QuestManager] Progress loaded");
            }
        }

        #endregion

        #region Event Handlers

        private void HandleGraphLoaded(ForkTrackGraph graph)
        {
            _quests.Clear();

            // Build quest dictionary
            foreach (var node in graph.nodes)
            {
                var questInfo = new QuestInfo(node, graph);

                // Check if main quest
                questInfo.isMainQuest = questInfo.category == mainQuestCategory;

                _quests[node.id] = questInfo;
            }

            Debug.Log($"[QuestManager] Loaded {_quests.Count} quests");

            // Fire initial progress
            var progress = GetProgress();
            onProgressChanged?.Invoke(progress.completed, progress.total);
        }

        private void HandleNodeUnlocked(ForkTrackNode node)
        {
            if (_quests.TryGetValue(node.id, out var quest))
            {
                quest.state = NodeState.Unlocked;
                onQuestUnlocked?.Invoke(quest);
                Debug.Log($"[QuestManager] Quest unlocked: {quest.displayName}");
            }
        }

        private void HandleNodeCompleted(ForkTrackNode node)
        {
            if (_quests.TryGetValue(node.id, out var quest))
            {
                quest.state = NodeState.Completed;
                onQuestCompleted?.Invoke(quest);

                var progress = GetProgress();
                onProgressChanged?.Invoke(progress.completed, progress.total);

                Debug.Log($"[QuestManager] Quest completed: {quest.displayName} ({progress.completed}/{progress.total})");
            }
        }

        #endregion

        #region Private Methods

        private void RefreshQuestStates()
        {
            foreach (var node in ForkTrack.GetAllNodes())
            {
                if (_quests.TryGetValue(node.id, out var quest))
                {
                    quest.state = node.State;
                }
            }

            var progress = GetProgress();
            onProgressChanged?.Invoke(progress.completed, progress.total);
        }

        #endregion
    }
}
