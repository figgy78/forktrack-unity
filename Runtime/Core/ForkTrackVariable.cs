using System;

namespace ForkTrack.Core
{
    /// <summary>
    /// Represents a graph-level variable definition
    /// </summary>
    [Serializable]
    public class ForkTrackVariable
    {
        /// <summary>Unique identifier for this variable (e.g., "var_1704067200000")</summary>
        public string id;

        /// <summary>Display name of the variable (unique within graph)</summary>
        public string name;

        /// <summary>Type of the variable (NUMBER or BOOLEAN)</summary>
        public string type;

        /// <summary>Default value as a string (parsed based on type)</summary>
        public string defaultValue;

        public ForkTrackVariable() { }

        public ForkTrackVariable(string id, string name, VariableType type, object defaultValue)
        {
            this.id = id;
            this.name = name;
            this.type = type.ToString();
            this.defaultValue = defaultValue?.ToString() ?? GetDefaultForType(type);
        }

        /// <summary>
        /// Gets the variable type as an enum
        /// </summary>
        public VariableType GetVariableType()
        {
            if (string.IsNullOrEmpty(type) || type == "NUMBER")
                return VariableType.NUMBER;

            return type == "BOOLEAN" ? VariableType.BOOLEAN : VariableType.NUMBER;
        }

        /// <summary>
        /// Gets the default value as a typed object
        /// </summary>
        public object GetDefaultValue()
        {
            var varType = GetVariableType();

            if (string.IsNullOrEmpty(defaultValue))
                return varType == VariableType.BOOLEAN ? (object)false : 0f;

            if (varType == VariableType.BOOLEAN)
            {
                return defaultValue.ToLower() == "true";
            }

            if (float.TryParse(defaultValue, out float numValue))
                return numValue;

            return 0f;
        }

        private static string GetDefaultForType(VariableType type)
        {
            return type == VariableType.BOOLEAN ? "false" : "0";
        }
    }
}
