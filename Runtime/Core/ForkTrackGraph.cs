using System;
using System.Collections.Generic;

namespace ForkTrack.Core
{
    /// <summary>
    /// Container for a complete ForkTrack graph with all nodes, edges, and metadata
    /// </summary>
    [Serializable]
    public class ForkTrackGraph
    {
        #region Metadata

        /// <summary>Unique identifier for this graph</summary>
        public string id;

        /// <summary>Display name of the graph</summary>
        public string name;

        /// <summary>Schema version of the loaded JSON (e.g., "1.2.0")</summary>
        public string schemaVersion;

        #endregion

        #region Data Collections

        /// <summary>All nodes in the graph</summary>
        public List<ForkTrackNode> nodes = new List<ForkTrackNode>();

        /// <summary>All edges (dependencies) in the graph</summary>
        public List<ForkTrackEdge> edges = new List<ForkTrackEdge>();

        /// <summary>All variable definitions (v1.2.0)</summary>
        public List<ForkTrackVariable> variables = new List<ForkTrackVariable>();

        /// <summary>All object definitions (v1.1.0+)</summary>
        public List<ForkTrackObject> objects = new List<ForkTrackObject>();

        /// <summary>All action definitions (v1.1.0+)</summary>
        public List<ForkTrackAction> actions = new List<ForkTrackAction>();

        /// <summary>All category definitions</summary>
        public List<ForkTrackCategory> categories = new List<ForkTrackCategory>();

        /// <summary>All group definitions</summary>
        public List<ForkTrackGroup> groups = new List<ForkTrackGroup>();

        /// <summary>All location definitions</summary>
        public List<ForkTrackLocation> locations = new List<ForkTrackLocation>();

        #endregion

        #region Lookup Dictionaries (Non-serialized, built at runtime)

        [NonSerialized]
        private Dictionary<string, ForkTrackNode> _nodesById;

        [NonSerialized]
        private Dictionary<string, List<ForkTrackEdge>> _edgesByTarget;

        [NonSerialized]
        private Dictionary<string, List<ForkTrackEdge>> _edgesBySource;

        [NonSerialized]
        private Dictionary<string, ForkTrackObject> _objectsById;

        [NonSerialized]
        private Dictionary<string, ForkTrackAction> _actionsById;

        [NonSerialized]
        private Dictionary<string, ForkTrackVariable> _variablesById;

        [NonSerialized]
        private Dictionary<string, ForkTrackVariable> _variablesByName;

        [NonSerialized]
        private Dictionary<string, ForkTrackCategory> _categoriesById;

        [NonSerialized]
        private Dictionary<string, ForkTrackGroup> _groupsById;

        [NonSerialized]
        private Dictionary<string, ForkTrackLocation> _locationsById;

        [NonSerialized]
        private bool _initialized;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes lookup dictionaries and resolves object/action names.
        /// Must be called after loading before accessing lookups.
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
                return;

            BuildDictionaries();
            ResolveNodeNames();
            _initialized = true;
        }

        private void BuildDictionaries()
        {
            // Nodes by ID
            _nodesById = new Dictionary<string, ForkTrackNode>();
            foreach (var node in nodes)
            {
                if (!string.IsNullOrEmpty(node.id))
                    _nodesById[node.id] = node;
            }

            // Edges by target node
            _edgesByTarget = new Dictionary<string, List<ForkTrackEdge>>();
            _edgesBySource = new Dictionary<string, List<ForkTrackEdge>>();
            foreach (var edge in edges)
            {
                // By target
                if (!string.IsNullOrEmpty(edge.targetNodeId))
                {
                    if (!_edgesByTarget.ContainsKey(edge.targetNodeId))
                        _edgesByTarget[edge.targetNodeId] = new List<ForkTrackEdge>();
                    _edgesByTarget[edge.targetNodeId].Add(edge);
                }

                // By source
                if (!string.IsNullOrEmpty(edge.sourceNodeId))
                {
                    if (!_edgesBySource.ContainsKey(edge.sourceNodeId))
                        _edgesBySource[edge.sourceNodeId] = new List<ForkTrackEdge>();
                    _edgesBySource[edge.sourceNodeId].Add(edge);
                }
            }

            // Objects by ID
            _objectsById = new Dictionary<string, ForkTrackObject>();
            foreach (var obj in objects)
            {
                if (!string.IsNullOrEmpty(obj.id))
                    _objectsById[obj.id] = obj;
            }

            // Actions by ID
            _actionsById = new Dictionary<string, ForkTrackAction>();
            foreach (var action in actions)
            {
                if (!string.IsNullOrEmpty(action.id))
                    _actionsById[action.id] = action;
            }

            // Variables by ID and Name
            _variablesById = new Dictionary<string, ForkTrackVariable>();
            _variablesByName = new Dictionary<string, ForkTrackVariable>(StringComparer.OrdinalIgnoreCase);
            foreach (var variable in variables)
            {
                if (!string.IsNullOrEmpty(variable.id))
                    _variablesById[variable.id] = variable;
                if (!string.IsNullOrEmpty(variable.name))
                    _variablesByName[variable.name] = variable;
            }

            // Categories by ID
            _categoriesById = new Dictionary<string, ForkTrackCategory>();
            foreach (var category in categories)
            {
                if (!string.IsNullOrEmpty(category.id))
                    _categoriesById[category.id] = category;
            }

            // Groups by ID
            _groupsById = new Dictionary<string, ForkTrackGroup>();
            foreach (var group in groups)
            {
                if (!string.IsNullOrEmpty(group.id))
                    _groupsById[group.id] = group;
            }

            // Locations by ID
            _locationsById = new Dictionary<string, ForkTrackLocation>();
            foreach (var location in locations)
            {
                if (!string.IsNullOrEmpty(location.id))
                    _locationsById[location.id] = location;
            }
        }

        private void ResolveNodeNames()
        {
            foreach (var node in nodes)
            {
                // Resolve object name
                if (!string.IsNullOrEmpty(node.objectId) && _objectsById.TryGetValue(node.objectId, out var obj))
                {
                    node.objectName = obj.name;
                }

                // Resolve action name
                if (!string.IsNullOrEmpty(node.actionId) && _actionsById.TryGetValue(node.actionId, out var action))
                {
                    node.actionName = action.name;
                }

                // Resolve category name and color
                if (!string.IsNullOrEmpty(node.categoryId) && _categoriesById.TryGetValue(node.categoryId, out var category))
                {
                    node.categoryName = category.name;
                    node.categoryColor = category.color;
                }

                // Resolve group name
                if (!string.IsNullOrEmpty(node.groupId) && _groupsById.TryGetValue(node.groupId, out var group))
                {
                    node.groupName = group.name;
                }

                // Resolve location name
                if (!string.IsNullOrEmpty(node.locationId) && _locationsById.TryGetValue(node.locationId, out var location))
                {
                    node.locationName = location.name;
                }
            }
        }

        #endregion

        #region Node Lookups

        /// <summary>
        /// Gets a node by its ID. O(1) complexity.
        /// </summary>
        public ForkTrackNode GetNodeById(string nodeId)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(nodeId))
                return null;

            _nodesById.TryGetValue(nodeId, out var node);
            return node;
        }

        /// <summary>
        /// Gets a node by object and action names. O(n) complexity.
        /// </summary>
        public ForkTrackNode GetNode(string objectName, string actionName)
        {
            EnsureInitialized();

            foreach (var node in nodes)
            {
                if (string.Equals(node.objectName, objectName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(node.actionName, actionName, StringComparison.OrdinalIgnoreCase))
                {
                    return node;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a node by title (case-insensitive). O(n) complexity.
        /// </summary>
        public ForkTrackNode GetNodeByTitle(string title)
        {
            EnsureInitialized();

            foreach (var node in nodes)
            {
                if (string.Equals(node.title, title, StringComparison.OrdinalIgnoreCase))
                {
                    return node;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets all nodes with the specified object name.
        /// </summary>
        public List<ForkTrackNode> GetNodesByObject(string objectName)
        {
            EnsureInitialized();

            var result = new List<ForkTrackNode>();
            foreach (var node in nodes)
            {
                if (string.Equals(node.objectName, objectName, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(node);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets all nodes with the specified action name.
        /// </summary>
        public List<ForkTrackNode> GetNodesByAction(string actionName)
        {
            EnsureInitialized();

            var result = new List<ForkTrackNode>();
            foreach (var node in nodes)
            {
                if (string.Equals(node.actionName, actionName, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(node);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets all nodes in the specified state.
        /// </summary>
        public List<ForkTrackNode> GetNodesInState(NodeState state)
        {
            EnsureInitialized();

            var result = new List<ForkTrackNode>();
            foreach (var node in nodes)
            {
                if (node.State == state)
                {
                    result.Add(node);
                }
            }
            return result;
        }

        #endregion

        #region Edge Lookups

        /// <summary>
        /// Gets all edges where the specified node is the target (incoming edges).
        /// </summary>
        public List<ForkTrackEdge> GetEdgesByTarget(string nodeId)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(nodeId))
                return new List<ForkTrackEdge>();

            if (_edgesByTarget.TryGetValue(nodeId, out var edgeList))
                return edgeList;

            return new List<ForkTrackEdge>();
        }

        /// <summary>
        /// Gets all edges where the specified node is the source (outgoing edges).
        /// </summary>
        public List<ForkTrackEdge> GetEdgesBySource(string nodeId)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(nodeId))
                return new List<ForkTrackEdge>();

            if (_edgesBySource.TryGetValue(nodeId, out var edgeList))
                return edgeList;

            return new List<ForkTrackEdge>();
        }

        /// <summary>
        /// Gets all node IDs that depend on the specified node.
        /// </summary>
        public List<string> GetDependentNodeIds(string nodeId)
        {
            var edgeList = GetEdgesBySource(nodeId);
            var result = new List<string>();
            var seen = new HashSet<string>();

            foreach (var edge in edgeList)
            {
                if (!seen.Contains(edge.targetNodeId))
                {
                    seen.Add(edge.targetNodeId);
                    result.Add(edge.targetNodeId);
                }
            }

            return result;
        }

        #endregion

        #region Variable Lookups

        /// <summary>
        /// Gets a variable by its ID.
        /// </summary>
        public ForkTrackVariable GetVariableById(string variableId)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(variableId))
                return null;

            _variablesById.TryGetValue(variableId, out var variable);
            return variable;
        }

        /// <summary>
        /// Gets a variable by its name (case-insensitive).
        /// </summary>
        public ForkTrackVariable GetVariableByName(string variableName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(variableName))
                return null;

            _variablesByName.TryGetValue(variableName, out var variable);
            return variable;
        }

        #endregion

        #region Category Lookups

        /// <summary>
        /// Gets a category by its ID.
        /// </summary>
        public ForkTrackCategory GetCategoryById(string categoryId)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(categoryId))
                return null;

            _categoriesById.TryGetValue(categoryId, out var category);
            return category;
        }

        #endregion

        #region Group Lookups

        /// <summary>
        /// Gets a group by its ID.
        /// </summary>
        public ForkTrackGroup GetGroupById(string groupId)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(groupId))
                return null;

            _groupsById.TryGetValue(groupId, out var group);
            return group;
        }

        #endregion

        #region Location Lookups

        /// <summary>
        /// Gets a location by its ID.
        /// </summary>
        public ForkTrackLocation GetLocationById(string locationId)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(locationId))
                return null;

            _locationsById.TryGetValue(locationId, out var location);
            return location;
        }

        /// <summary>
        /// Gets all nodes assigned to the specified location name (case-insensitive).
        /// </summary>
        public List<ForkTrackNode> GetNodesByLocation(string locationName)
        {
            EnsureInitialized();

            var result = new List<ForkTrackNode>();
            foreach (var node in nodes)
            {
                if (string.Equals(node.locationName, locationName, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(node);
                }
            }
            return result;
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Gets the total number of nodes in the graph.
        /// </summary>
        public int NodeCount => nodes?.Count ?? 0;

        /// <summary>
        /// Gets the total number of edges in the graph.
        /// </summary>
        public int EdgeCount => edges?.Count ?? 0;

        /// <summary>
        /// Gets the total number of variables in the graph.
        /// </summary>
        public int VariableCount => variables?.Count ?? 0;

        /// <summary>
        /// Gets the total number of locations in the graph.
        /// </summary>
        public int LocationCount => locations?.Count ?? 0;

        #endregion

        #region Private Helpers

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        #endregion
    }
}
