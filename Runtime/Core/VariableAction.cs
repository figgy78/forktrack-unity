using System;

namespace ForkTrack.Core
{
    /// <summary>
    /// An action to modify a variable when a node trigger fires
    /// </summary>
    [Serializable]
    public class VariableAction
    {
        /// <summary>Unique identifier for this action (e.g., "va_1704067200000")</summary>
        public string id;

        /// <summary>ID of the variable to modify</summary>
        public string variableId;

        /// <summary>When to execute the action (OnUnlock or OnComplete)</summary>
        public string trigger;

        /// <summary>Operation to perform (SET, ADD, SUBTRACT, TOGGLE)</summary>
        public string operation;

        /// <summary>Value to use for the operation (as string, parsed based on variable type)</summary>
        public string value;

        public VariableAction() { }

        public VariableAction(string id, string variableId, TriggerType trigger, VariableOperation operation, object value)
        {
            this.id = id;
            this.variableId = variableId;
            this.trigger = trigger.ToString();
            this.operation = operation.ToString();
            this.value = value?.ToString();
        }

        /// <summary>
        /// Gets the trigger type as an enum
        /// </summary>
        public TriggerType GetTriggerType()
        {
            if (string.IsNullOrEmpty(trigger))
                return TriggerType.OnComplete;

            return trigger == "OnUnlock" ? TriggerType.OnUnlock : TriggerType.OnComplete;
        }

        /// <summary>
        /// Gets the operation as an enum
        /// </summary>
        public VariableOperation GetOperation()
        {
            if (string.IsNullOrEmpty(operation))
                return VariableOperation.SET;

            switch (operation.ToUpper())
            {
                case "ADD": return VariableOperation.ADD;
                case "SUBTRACT": return VariableOperation.SUBTRACT;
                case "TOGGLE": return VariableOperation.TOGGLE;
                default: return VariableOperation.SET;
            }
        }

        /// <summary>
        /// Gets the value as a float (for NUMBER operations)
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
        /// Gets the value as a bool (for SET BOOLEAN operations)
        /// </summary>
        public bool GetValueAsBool()
        {
            if (string.IsNullOrEmpty(value))
                return false;

            return value.ToLower() == "true";
        }
    }
}
