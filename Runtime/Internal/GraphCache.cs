using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ForkTrack.Core;

namespace ForkTrack.Internal
{
    /// <summary>
    /// Caches loaded graphs in memory and optionally on disk.
    /// REQ-015: Cache graphs locally after initial fetch
    /// </summary>
    internal class GraphCache
    {
        private readonly Dictionary<string, CachedGraph> _memoryCache = new Dictionary<string, CachedGraph>();
        private readonly string _diskCachePath;
        private readonly TimeSpan _defaultExpiry = TimeSpan.FromHours(24);

        internal GraphCache()
        {
            _diskCachePath = Path.Combine(Application.persistentDataPath, "ForkTrack", "GraphCache");
        }

        /// <summary>
        /// Gets a cached graph by ID from memory or disk
        /// REQ-016: Check cache before fetching from server
        /// </summary>
        public ForkTrackGraph Get(string graphId)
        {
            // Check memory cache first
            if (_memoryCache.TryGetValue(graphId, out var cached))
            {
                if (!cached.IsExpired)
                {
                    return cached.Graph;
                }
                _memoryCache.Remove(graphId);
            }

            // Check disk cache
            var diskGraph = LoadFromDisk(graphId);
            if (diskGraph != null)
            {
                // Add to memory cache
                _memoryCache[graphId] = new CachedGraph(diskGraph, DateTime.UtcNow.Add(_defaultExpiry));
                return diskGraph;
            }

            return null;
        }

        /// <summary>
        /// Caches a graph in memory and optionally on disk
        /// </summary>
        public void Set(string graphId, ForkTrackGraph graph, bool persistToDisk = true)
        {
            if (string.IsNullOrEmpty(graphId) || graph == null)
                return;

            var expiry = DateTime.UtcNow.Add(_defaultExpiry);
            _memoryCache[graphId] = new CachedGraph(graph, expiry);

            if (persistToDisk)
            {
                SaveToDisk(graphId, graph);
            }
        }

        /// <summary>
        /// Removes a graph from cache
        /// </summary>
        public void Remove(string graphId)
        {
            _memoryCache.Remove(graphId);
            DeleteFromDisk(graphId);
        }

        /// <summary>
        /// Clears all cached graphs
        /// </summary>
        public void Clear()
        {
            _memoryCache.Clear();
            ClearDiskCache();
        }

        /// <summary>
        /// Checks if a graph is cached and not expired
        /// </summary>
        public bool Contains(string graphId)
        {
            if (_memoryCache.TryGetValue(graphId, out var cached) && !cached.IsExpired)
            {
                return true;
            }
            return File.Exists(GetDiskPath(graphId));
        }

        #region Disk Operations

        private void SaveToDisk(string graphId, ForkTrackGraph graph)
        {
            try
            {
                EnsureCacheDirectory();
                var path = GetDiskPath(graphId);
                var json = JsonGraphLoader.SerializeToJson(graph);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ForkTrack] Failed to save graph to disk cache: {ex.Message}");
            }
        }

        private ForkTrackGraph LoadFromDisk(string graphId)
        {
            try
            {
                var path = GetDiskPath(graphId);
                if (!File.Exists(path))
                    return null;

                var json = File.ReadAllText(path);
                return JsonGraphLoader.LoadFromJson(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ForkTrack] Failed to load graph from disk cache: {ex.Message}");
                return null;
            }
        }

        private void DeleteFromDisk(string graphId)
        {
            try
            {
                var path = GetDiskPath(graphId);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ForkTrack] Failed to delete cached graph: {ex.Message}");
            }
        }

        private void ClearDiskCache()
        {
            try
            {
                if (Directory.Exists(_diskCachePath))
                {
                    Directory.Delete(_diskCachePath, true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ForkTrack] Failed to clear disk cache: {ex.Message}");
            }
        }

        private string GetDiskPath(string graphId)
        {
            // Sanitize graph ID for filesystem
            var safeId = graphId.Replace("/", "_").Replace("\\", "_");
            return Path.Combine(_diskCachePath, $"{safeId}.json");
        }

        private void EnsureCacheDirectory()
        {
            if (!Directory.Exists(_diskCachePath))
            {
                Directory.CreateDirectory(_diskCachePath);
            }
        }

        #endregion

        #region Nested Types

        private class CachedGraph
        {
            public ForkTrackGraph Graph { get; }
            public DateTime Expiry { get; }
            public bool IsExpired => DateTime.UtcNow > Expiry;

            public CachedGraph(ForkTrackGraph graph, DateTime expiry)
            {
                Graph = graph;
                Expiry = expiry;
            }
        }

        #endregion
    }
}
