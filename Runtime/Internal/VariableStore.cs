using System;
using System.Collections.Generic;
using UnityEngine;
using ForkTrack.Core;

namespace ForkTrack.Internal
{
    /// <summary>
    /// Manages runtime variable state with operations and condition evaluation
    /// </summary>
    public class VariableStore
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();
        private readonly Dictionary<string, ForkTrackVariable> _definitions = new Dictionary<string, ForkTrackVariable>();
        private readonly Dictionary<string, ForkTrackVariable> _definitionsByName = new Dictionary<string, ForkTrackVariable>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Fired when a variable value changes
        /// Parameters: variable definition, old value, new value
        /// </summary>
        public event Action<ForkTrackVariable, object, object> OnVariableChanged;

        /// <summary>
        /// Initializes the variable store with graph variable definitions
        /// REQ-062: Sets all variables to their defaultValue
        /// </summary>
        public void Initialize(List<ForkTrackVariable> variables)
        {
            _values.Clear();
            _definitions.Clear();
            _definitionsByName.Clear();

            if (variables == null)
                return;

            foreach (var variable in variables)
            {
                if (string.IsNullOrEmpty(variable.id))
                    continue;

                _definitions[variable.id] = variable;

                if (!string.IsNullOrEmpty(variable.name))
                {
                    _definitionsByName[variable.name] = variable;
                }

                // Set to default value
                _values[variable.id] = variable.GetDefaultValue();
            }
        }

        /// <summary>
        /// Resets all variables to their default values
        /// </summary>
        public void Reset()
        {
            foreach (var kvp in _definitions)
            {
                var variable = kvp.Value;
                var oldValue = _values.ContainsKey(variable.id) ? _values[variable.id] : null;
                var newValue = variable.GetDefaultValue();

                _values[variable.id] = newValue;

                if (!Equals(oldValue, newValue))
                {
                    OnVariableChanged?.Invoke(variable, oldValue, newValue);
                }
            }
        }

        #region Get/Set Operations

        /// <summary>
        /// Gets a variable value by name (case-insensitive)
        /// REQ-100: GetVariable&lt;T&gt;(name)
        /// REQ-103: Log warning and return default if not found
        /// </summary>
        public T Get<T>(string variableName)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                Debug.LogWarning("[ForkTrack] GetVariable called with null or empty name");
                return default;
            }

            if (!_definitionsByName.TryGetValue(variableName, out var variable))
            {
                Debug.LogWarning($"[ForkTrack] Variable not found: {variableName}");
                return default;
            }

            if (!_values.TryGetValue(variable.id, out var value))
            {
                return default;
            }

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                Debug.LogWarning($"[ForkTrack] Cannot convert variable '{variableName}' to type {typeof(T).Name}");
                return default;
            }
        }

        /// <summary>
        /// Gets a variable value by ID
        /// </summary>
        public object GetById(string variableId)
        {
            if (string.IsNullOrEmpty(variableId))
                return null;

            _values.TryGetValue(variableId, out var value);
            return value;
        }

        /// <summary>
        /// Sets a variable value by name (case-insensitive)
        /// REQ-101: SetVariable&lt;T&gt;(name, value)
        /// </summary>
        public void Set<T>(string variableName, T value)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                Debug.LogWarning("[ForkTrack] SetVariable called with null or empty name");
                return;
            }

            if (!_definitionsByName.TryGetValue(variableName, out var variable))
            {
                Debug.LogWarning($"[ForkTrack] Variable not found: {variableName}");
                return;
            }

            SetById(variable.id, value);
        }

        /// <summary>
        /// Sets a variable value by ID
        /// </summary>
        public void SetById(string variableId, object value)
        {
            if (string.IsNullOrEmpty(variableId))
                return;

            if (!_definitions.TryGetValue(variableId, out var variable))
            {
                Debug.LogWarning($"[ForkTrack] Variable ID not found: {variableId}");
                return;
            }

            var oldValue = _values.ContainsKey(variableId) ? _values[variableId] : null;

            // Convert value to appropriate type
            object newValue = variable.GetVariableType() == VariableType.BOOLEAN
                ? Convert.ToBoolean(value)
                : Convert.ToSingle(value);

            _values[variableId] = newValue;

            // REQ-063: Fire OnVariableChanged
            if (!Equals(oldValue, newValue))
            {
                OnVariableChanged?.Invoke(variable, oldValue, newValue);
            }
        }

        #endregion

        #region Variable Operations

        /// <summary>
        /// Executes a variable action (SET, ADD, SUBTRACT, TOGGLE)
        /// REQ-070 through REQ-075
        /// </summary>
        public void ExecuteAction(VariableAction action)
        {
            if (action == null || string.IsNullOrEmpty(action.variableId))
            {
                return;
            }

            if (!_definitions.TryGetValue(action.variableId, out var variable))
            {
                Debug.LogWarning($"[ForkTrack] Variable action references unknown variable: {action.variableId}");
                return;
            }

            var varType = variable.GetVariableType();
            var operation = action.GetOperation();
            var currentValue = GetById(action.variableId);

            switch (operation)
            {
                case VariableOperation.SET:
                    // REQ-070: SET to specified value
                    if (varType == VariableType.BOOLEAN)
                    {
                        SetById(action.variableId, action.GetValueAsBool());
                    }
                    else
                    {
                        SetById(action.variableId, action.GetValueAsFloat());
                    }
                    break;

                case VariableOperation.ADD:
                    // REQ-071: ADD value to NUMBER
                    // REQ-074: Log warning if applied to BOOLEAN
                    if (varType == VariableType.BOOLEAN)
                    {
                        Debug.LogWarning($"[ForkTrack] Cannot ADD to BOOLEAN variable: {variable.name}");
                        return;
                    }
                    float addCurrent = currentValue != null ? Convert.ToSingle(currentValue) : 0f;
                    SetById(action.variableId, addCurrent + action.GetValueAsFloat());
                    break;

                case VariableOperation.SUBTRACT:
                    // REQ-072: SUBTRACT value from NUMBER
                    // REQ-074: Log warning if applied to BOOLEAN
                    if (varType == VariableType.BOOLEAN)
                    {
                        Debug.LogWarning($"[ForkTrack] Cannot SUBTRACT from BOOLEAN variable: {variable.name}");
                        return;
                    }
                    float subCurrent = currentValue != null ? Convert.ToSingle(currentValue) : 0f;
                    SetById(action.variableId, subCurrent - action.GetValueAsFloat());
                    break;

                case VariableOperation.TOGGLE:
                    // REQ-073: TOGGLE flips BOOLEAN
                    // REQ-075: Log warning if applied to NUMBER
                    if (varType == VariableType.NUMBER)
                    {
                        Debug.LogWarning($"[ForkTrack] Cannot TOGGLE NUMBER variable: {variable.name}");
                        return;
                    }
                    bool toggleCurrent = currentValue != null && Convert.ToBoolean(currentValue);
                    SetById(action.variableId, !toggleCurrent);
                    break;
            }
        }

        /// <summary>
        /// Executes multiple variable actions in order
        /// REQ-083: Execute in array order
        /// </summary>
        public void ExecuteActions(List<VariableAction> actions)
        {
            if (actions == null)
                return;

            foreach (var action in actions)
            {
                ExecuteAction(action);
            }
        }

        #endregion

        #region Condition Evaluation

        /// <summary>
        /// Evaluates a single variable condition
        /// REQ-091 through REQ-094
        /// </summary>
        public bool EvaluateCondition(VariableCondition condition)
        {
            if (condition == null || string.IsNullOrEmpty(condition.variableId))
            {
                return true; // No condition = satisfied
            }

            if (!_definitions.TryGetValue(condition.variableId, out var variable))
            {
                Debug.LogWarning($"[ForkTrack] Condition references unknown variable: {condition.variableId}");
                return false;
            }

            var varType = variable.GetVariableType();
            var op = condition.GetOperator();
            var currentValue = GetById(condition.variableId);

            // REQ-094: Comparison operators on BOOLEAN return false with warning
            if (varType == VariableType.BOOLEAN)
            {
                if (op != VariableOperator.EQ && op != VariableOperator.NE)
                {
                    Debug.LogWarning($"[ForkTrack] Cannot use operator {op} on BOOLEAN variable: {variable.name}");
                    return false;
                }

                bool currentBool = currentValue != null && Convert.ToBoolean(currentValue);
                bool conditionBool = condition.GetValueAsBool();

                return op == VariableOperator.EQ
                    ? currentBool == conditionBool
                    : currentBool != conditionBool;
            }

            // NUMBER comparison
            float currentNum = currentValue != null ? Convert.ToSingle(currentValue) : 0f;
            float conditionNum = condition.GetValueAsFloat();

            return op switch
            {
                VariableOperator.EQ => Mathf.Approximately(currentNum, conditionNum),
                VariableOperator.NE => !Mathf.Approximately(currentNum, conditionNum),
                VariableOperator.GT => currentNum > conditionNum,
                VariableOperator.LT => currentNum < conditionNum,
                VariableOperator.GTE => currentNum >= conditionNum,
                VariableOperator.LTE => currentNum <= conditionNum,
                _ => false
            };
        }

        /// <summary>
        /// Evaluates multiple conditions (all must be satisfied)
        /// REQ-090: All conditions must be satisfied
        /// </summary>
        public bool EvaluateConditions(List<VariableCondition> conditions)
        {
            if (conditions == null || conditions.Count == 0)
            {
                return true;
            }

            foreach (var condition in conditions)
            {
                if (!EvaluateCondition(condition))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets all variable definitions
        /// REQ-102: GetAllVariables()
        /// </summary>
        public List<ForkTrackVariable> GetAllVariables()
        {
            return new List<ForkTrackVariable>(_definitions.Values);
        }

        /// <summary>
        /// Gets a variable definition by name
        /// </summary>
        public ForkTrackVariable GetVariableByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            _definitionsByName.TryGetValue(name, out var variable);
            return variable;
        }

        /// <summary>
        /// Gets a variable definition by ID
        /// </summary>
        public ForkTrackVariable GetVariableById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            _definitions.TryGetValue(id, out var variable);
            return variable;
        }

        #endregion

        #region Persistence

        /// <summary>
        /// Exports current variable values for persistence
        /// </summary>
        public Dictionary<string, object> Export()
        {
            return new Dictionary<string, object>(_values);
        }

        /// <summary>
        /// Imports variable values from persistence
        /// </summary>
        public void Import(Dictionary<string, object> values)
        {
            if (values == null)
                return;

            foreach (var kvp in values)
            {
                if (_definitions.ContainsKey(kvp.Key))
                {
                    SetById(kvp.Key, kvp.Value);
                }
            }
        }

        #endregion
    }
}
