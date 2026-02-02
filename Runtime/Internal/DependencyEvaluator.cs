using System.Collections.Generic;
using UnityEngine;
using ForkTrack.Core;

namespace ForkTrack.Internal
{
    /// <summary>
    /// Evaluates node dependencies with AND/OR/NOT logic and variable conditions
    /// </summary>
    public class DependencyEvaluator
    {
        private readonly VariableStore _variableStore;

        public DependencyEvaluator(VariableStore variableStore = null)
        {
            _variableStore = variableStore;
        }

        /// <summary>
        /// Determines if a node can be unlocked based on its dependencies
        /// REQ-040 through REQ-046, REQ-090 through REQ-094
        /// </summary>
        /// <param name="node">The node to evaluate</param>
        /// <param name="graph">The graph containing all nodes and edges</param>
        /// <returns>True if all dependency conditions are met</returns>
        public bool CanUnlock(ForkTrackNode node, ForkTrackGraph graph)
        {
            if (node == null || graph == null)
            {
                Debug.LogError("[ForkTrack] CanUnlock called with null node or graph");
                return false;
            }

            // Already unlocked or completed? No need to check dependencies
            if (!node.IsLocked)
            {
                return true;
            }

            // Get all incoming edges (dependencies) for this node
            var dependencies = graph.GetEdgesByTarget(node.id);

            // REQ-040: No dependencies = always unlockable (root node)
            if (dependencies.Count == 0)
            {
                return true;
            }

            // Group dependencies by condition type
            var andDeps = new List<ForkTrackEdge>();
            var orDeps = new List<ForkTrackEdge>();
            var notDeps = new List<ForkTrackEdge>();

            foreach (var edge in dependencies)
            {
                switch (edge.GetCondition())
                {
                    case DependencyCondition.OR:
                        orDeps.Add(edge);
                        break;
                    case DependencyCondition.NOT:
                        notDeps.Add(edge);
                        break;
                    default:
                        andDeps.Add(edge);
                        break;
                }
            }

            // REQ-041: AND - all must be satisfied
            bool andSatisfied = true;
            foreach (var edge in andDeps)
            {
                if (!IsEdgeSatisfied(edge, graph))
                {
                    andSatisfied = false;
                    break;
                }
            }

            // REQ-042: OR - at least one must be satisfied
            bool orSatisfied = orDeps.Count == 0;
            if (!orSatisfied)
            {
                foreach (var edge in orDeps)
                {
                    if (IsEdgeSatisfied(edge, graph))
                    {
                        orSatisfied = true;
                        break;
                    }
                }
            }

            // REQ-043: NOT - all must NOT be satisfied
            bool notSatisfied = true;
            foreach (var edge in notDeps)
            {
                if (IsEdgeSatisfied(edge, graph))
                {
                    notSatisfied = false;
                    break;
                }
            }

            return andSatisfied && orSatisfied && notSatisfied;
        }

        /// <summary>
        /// Checks if a specific edge dependency is satisfied
        /// Including both node state and variable conditions
        /// </summary>
        public bool IsEdgeSatisfied(ForkTrackEdge edge, ForkTrackGraph graph)
        {
            if (edge == null || graph == null)
            {
                return false;
            }

            // Check node state requirement
            if (!IsNodeStateSatisfied(edge, graph))
            {
                return false;
            }

            // REQ-090: Check variable conditions (all must be satisfied)
            if (edge.HasVariableConditions && _variableStore != null)
            {
                foreach (var condition in edge.variableConditions)
                {
                    if (!_variableStore.EvaluateCondition(condition))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the node state requirement of an edge is satisfied
        /// </summary>
        private bool IsNodeStateSatisfied(ForkTrackEdge edge, ForkTrackGraph graph)
        {
            var sourceNode = graph.GetNodeById(edge.sourceNodeId);

            if (sourceNode == null)
            {
                Debug.LogWarning($"[ForkTrack] Edge {edge.id} references non-existent source node: {edge.sourceNodeId}");
                return false;
            }

            // REQ-044: OnUnlock - satisfied if source is Unlocked OR Completed
            // REQ-045: OnComplete - satisfied only if source is Completed
            switch (edge.GetRequiredState())
            {
                case RequiredState.OnUnlock:
                    return sourceNode.IsUnlocked || sourceNode.IsCompleted;

                case RequiredState.OnComplete:
                    return sourceNode.IsCompleted;

                default:
                    Debug.LogWarning($"[ForkTrack] Unknown RequiredState: {edge.requiredState}");
                    return false;
            }
        }

        /// <summary>
        /// Gets all nodes that depend on the specified node (outgoing edges)
        /// </summary>
        public List<ForkTrackNode> GetDependentNodes(string nodeId, ForkTrackGraph graph)
        {
            var result = new List<ForkTrackNode>();
            if (graph == null || string.IsNullOrEmpty(nodeId))
            {
                return result;
            }

            var dependentIds = graph.GetDependentNodeIds(nodeId);
            foreach (var id in dependentIds)
            {
                var node = graph.GetNodeById(id);
                if (node != null)
                {
                    result.Add(node);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets all nodes that can be unlocked after the specified node is completed
        /// Used for cascade unlock logic
        /// </summary>
        public List<ForkTrackNode> GetUnlockableNodes(string completedNodeId, ForkTrackGraph graph)
        {
            var result = new List<ForkTrackNode>();
            var dependentNodes = GetDependentNodes(completedNodeId, graph);

            foreach (var node in dependentNodes)
            {
                if (node.IsLocked && CanUnlock(node, graph))
                {
                    result.Add(node);
                }
            }

            return result;
        }
    }
}
