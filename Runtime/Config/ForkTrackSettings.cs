using UnityEngine;
using ForkTrack.Core;

namespace ForkTrack.Config
{
    /// <summary>
    /// Project-wide configuration for ForkTrack.
    /// Create via Assets > Create > ForkTrack > Settings.
    /// REQ-181: ForkTrackSettings ScriptableObject for project-wide configuration
    /// </summary>
    [CreateAssetMenu(fileName = "ForkTrackSettings", menuName = "ForkTrack/Settings", order = 1)]
    public class ForkTrackSettings : ScriptableObject
    {
        #region Singleton

        private static ForkTrackSettings _instance;

        /// <summary>
        /// Gets the ForkTrackSettings instance (loads from Resources if needed)
        /// </summary>
        public static ForkTrackSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ForkTrackSettings>("ForkTrackSettings");
                    if (_instance == null)
                    {
                        Debug.LogWarning("[ForkTrack] No ForkTrackSettings found in Resources. Using defaults.");
                        _instance = CreateInstance<ForkTrackSettings>();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region API Configuration

        [Header("API Configuration")]
        [Tooltip("ForkTrack API URL")]
        public string apiUrl = "https://forktrack.pixfork.com";

        [Tooltip("Default graph ID to load")]
        public string defaultGraphId;

        #endregion

        #region Cache Configuration

        [Header("Cache Configuration")]
        [Tooltip("How many hours before cached graphs are considered stale")]
        [Range(1, 168)]
        public int cacheFreshnessHours = 24;

        [Tooltip("Cache location (uses Application.persistentDataPath by default)")]
        public string cacheLocation;

        #endregion

        #region Progress Configuration

        [Header("Progress Configuration")]
        [Tooltip("Automatically save progress when node states change")]
        public bool autoSaveProgress = false;

        [Tooltip("Automatically load progress when a graph is loaded")]
        public bool autoLoadProgress = false;

        [Tooltip("Default save slot name")]
        public string defaultSaveSlot = "default";

        #endregion

        #region Offline Configuration

        [Header("Offline Configuration")]
        [Tooltip("TextAsset to use as fallback when API is unavailable")]
        public TextAsset fallbackGraph;

        #endregion

        #region Debug Configuration

        [Header("Debug Configuration")]
        [Tooltip("Log level for ForkTrack messages")]
        public LogLevel logLevel = LogLevel.Warning;

        [Tooltip("Validate SSL certificates (disable only for development)")]
        public bool validateSSL = true;

        #endregion

        #region Helper Properties

        /// <summary>
        /// Gets the effective cache location (defaults to persistentDataPath if not set)
        /// </summary>
        public string EffectiveCacheLocation =>
            string.IsNullOrEmpty(cacheLocation)
                ? Application.persistentDataPath
                : cacheLocation;

        /// <summary>
        /// Checks if logging is enabled at the specified level
        /// </summary>
        public bool ShouldLog(LogLevel level)
        {
            return level <= logLevel;
        }

        #endregion

        #region Validation

        private void OnValidate()
        {
            // Ensure API URL doesn't have trailing slash
            if (!string.IsNullOrEmpty(apiUrl) && apiUrl.EndsWith("/"))
            {
                apiUrl = apiUrl.TrimEnd('/');
            }
        }

        #endregion
    }
}
