using System;

namespace ForkTrack.Core
{
    /// <summary>
    /// A condition on an edge that checks a variable value
    /// </summary>
    [Serializable]
    public class VariableCondition
    {
        /// <summary>Unique identifier for this condition (e.g., "vc_1704067200000")</summary>
        public string id;

        /// <summary>ID of the variable to check</summary>
        public string variableId;

        /// <summary>Comparison operator (==, !=, >, &lt;, >=, &lt;=)</summary>
        public string @operator;

        /// <summary>Value to compare against (as string, parsed based on variable type)</summary>
        public string value;

        public VariableCondition() { }

        public VariableCondition(string id, string variableId, VariableOperator op, object value)
        {
            this.id = id;
            this.variableId = variableId;
            this.@operator = OperatorToString(op);
            this.value = value?.ToString();
        }

        /// <summary>
        /// Gets the operator as an enum
        /// </summary>
        public VariableOperator GetOperator()
        {
            if (string.IsNullOrEmpty(@operator))
                return VariableOperator.EQ;

            switch (@operator)
            {
                case "!=": return VariableOperator.NE;
                case ">": return VariableOperator.GT;
                case "<": return VariableOperator.LT;
                case ">=": return VariableOperator.GTE;
                case "<=": return VariableOperator.LTE;
                default: return VariableOperator.EQ;
            }
        }

        /// <summary>
        /// Gets the value as a float (for NUMBER comparisons)
        /// </summary>
        public float GetValueAsFloat()
        {
            if (string.IsNullOrEmpty(value))
                return 0f;

            if (float.TryParse(value, out float result))
                return result;

            return 0f;
        }

        /// <summary>
        /// Gets the value as a bool (for BOOLEAN comparisons)
        /// </summary>
        public bool GetValueAsBool()
        {
            if (string.IsNullOrEmpty(value))
                return false;

            return value.ToLower() == "true";
        }

        private static string OperatorToString(VariableOperator op)
        {
            switch (op)
            {
                case VariableOperator.NE: return "!=";
                case VariableOperator.GT: return ">";
                case VariableOperator.LT: return "<";
                case VariableOperator.GTE: return ">=";
                case VariableOperator.LTE: return "<=";
                default: return "==";
            }
        }
    }
}
