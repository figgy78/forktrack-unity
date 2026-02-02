using System;
using System.Collections.Generic;

namespace ForkTrack.Core
{
    /// <summary>
    /// Represents a dependency edge between two nodes
    /// </summary>
    [Serializable]
    public class ForkTrackEdge
    {
        /// <summary>Unique identifier for this edge</summary>
        public string id;

        /// <summary>ID of the source node (the dependency)</summary>
        public string sourceNodeId;

        /// <summary>ID of the target node (depends on source)</summary>
        public string targetNodeId;

        /// <summary>Required state of source node for edge satisfaction</summary>
        public string requiredState;

        /// <summary>Logical condition type (AND, OR, NOT)</summary>
        public string condition;

        /// <summary>Variable conditions that must be met (v1.2.0)</summary>
        public List<VariableCondition> variableConditions = new List<VariableCondition>();

        public ForkTrackEdge() { }

        public ForkTrackEdge(string id, string sourceNodeId, string targetNodeId,
            RequiredState requiredState = RequiredState.OnComplete,
            DependencyCondition condition = DependencyCondition.AND)
        {
            this.id = id;
            this.sourceNodeId = sourceNodeId;
            this.targetNodeId = targetNodeId;
            this.requiredState = requiredState.ToString();
            this.condition = condition.ToString();
        }

        /// <summary>
        /// Gets the required state as an enum
        /// </summary>
        public RequiredState GetRequiredState()
        {
            if (string.IsNullOrEmpty(requiredState))
                return RequiredState.OnComplete;

            return requiredState == "OnUnlock" ? RequiredState.OnUnlock : RequiredState.OnComplete;
        }

        /// <summary>
        /// Gets the dependency condition as an enum
        /// </summary>
        public DependencyCondition GetCondition()
        {
            if (string.IsNullOrEmpty(condition))
                return DependencyCondition.AND;

            switch (condition.ToUpper())
            {
                case "OR": return DependencyCondition.OR;
                case "NOT": return DependencyCondition.NOT;
                default: return DependencyCondition.AND;
            }
        }

        /// <summary>
        /// Returns true if this edge has variable conditions
        /// </summary>
        public bool HasVariableConditions => variableConditions != null && variableConditions.Count > 0;
    }
}
