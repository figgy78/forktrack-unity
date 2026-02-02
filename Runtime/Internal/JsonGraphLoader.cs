using System;
using System.Collections.Generic;
using UnityEngine;
using ForkTrack.Core;

namespace ForkTrack.Internal
{
    /// <summary>
    /// Loads and parses ForkTrack graph JSON with schema version handling
    /// </summary>
    public static class JsonGraphLoader
    {
        private const string CURRENT_SCHEMA_VERSION = "1.2.0";

        #region JSON Wrapper Classes (for Unity JsonUtility)

        [Serializable]
        private class JsonRoot
        {
            public string schemaVersion;
            public JsonVersion version;
            public List<JsonCategory> categories;
            public List<JsonGroup> groups;
            public List<JsonObject> objects;
            public List<JsonAction> actions;
            public List<JsonVariable> variables;
            public List<JsonNode> nodes;
            public List<JsonEdge> edges;
        }

        [Serializable]
        private class JsonVersion
        {
            public int major;
            public int minor;
        }

        [Serializable]
        private class JsonCategory
        {
            public string id;
            public string name;
            public string color;
        }

        [Serializable]
        private class JsonGroup
        {
            public string id;
            public string name;
            public string color;
            public JsonPosition position;
            public JsonSize size;
        }

        [Serializable]
        private class JsonObject
        {
            public string id;
            public string name;
            public string category;
        }

        [Serializable]
        private class JsonAction
        {
            public string id;
            public string name;
        }

        [Serializable]
        private class JsonVariable
        {
            public string id;
            public string name;
            public string type;
            public string defaultValue;
        }

        [Serializable]
        private class JsonNode
        {
            public string id;
            public JsonPosition position;
            public string type;
            public JsonNodeData data;
        }

        [Serializable]
        private class JsonNodeData
        {
            public string objectId;
            public string actionId;
            public string title;
            public string description;
            public string category;
            public string groupId;
            public bool autoCompleteOnUnlock;
            public string music;
            public List<JsonNote> notes;
            public List<JsonTodo> todos;
            public List<JsonCustomEvent> customEvents;
            public List<JsonVariableAction> variableActions;
            // Denormalized names from API (optional, used if present)
            public string @object;
            public string action;
        }

        [Serializable]
        private class JsonNote
        {
            public string text;
            public string trigger;
        }

        [Serializable]
        private class JsonTodo
        {
            public string title;
            public string description;
            public string responsible;
            public bool resolved;
        }

        [Serializable]
        private class JsonCustomEvent
        {
            public string propertyId;
            public string trigger;
            public JsonCustomEventValue[] values;  // Array of value objects from API
            public float weight;
            public float delay;
            public float amount;
            public string text;  // Optional text/note associated with the event
        }

        [Serializable]
        private class JsonCustomEventValue
        {
            public string text;    // Display text
            public string value;   // The actual value to use
            public float weight;   // Per-value weight
            public float delay;    // Per-value delay
            public float amount;   // Per-value amount
        }

        [Serializable]
        private class JsonVariableAction
        {
            public string id;
            public string variableId;
            public string trigger;
            public string operation;
            public string value;
        }

        [Serializable]
        private class JsonEdge
        {
            public string id;
            public string sourceNodeId;
            public string targetNodeId;
            public JsonEdgeData data;
        }

        [Serializable]
        private class JsonEdgeData
        {
            public string requiredState;
            public string condition;
            public List<JsonVariableCondition> variableConditions;
        }

        [Serializable]
        private class JsonVariableCondition
        {
            public string id;
            public string variableId;
            public string @operator;
            public string value;
        }

        [Serializable]
        private class JsonPosition
        {
            public float x;
            public float y;
        }

        [Serializable]
        private class JsonSize
        {
            public float width;
            public float height;
        }

        #endregion

        /// <summary>
        /// Loads a ForkTrack graph from JSON string
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <returns>Parsed ForkTrackGraph</returns>
        /// <exception cref="ArgumentNullException">If json is null or empty</exception>
        /// <exception cref="FormatException">If JSON is malformed or invalid</exception>
        public static ForkTrackGraph LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentNullException(nameof(json), "JSON string cannot be null or empty");
            }

            // Pre-process JSON to normalize custom event values
            // The web app stores single-value events as "values": "string"
            // but multi-value events as "values": [{ value: "..." }]
            // Unity's JsonUtility needs arrays, so we normalize string values to array format
            json = NormalizeCustomEventValues(json);

            JsonRoot root;
            try
            {
                root = JsonUtility.FromJson<JsonRoot>(json);
            }
            catch (Exception ex)
            {
                throw new FormatException($"Failed to parse JSON: {ex.Message}", ex);
            }

            if (root == null)
            {
                throw new FormatException("Failed to parse JSON: result was null");
            }

            // Validate schema version
            if (string.IsNullOrEmpty(root.schemaVersion))
            {
                throw new FormatException("Invalid JSON: missing schemaVersion");
            }

            // Create graph and populate
            var graph = new ForkTrackGraph
            {
                schemaVersion = root.schemaVersion
            };

            // Detect schema version for migration
            var version = ParseSchemaVersion(root.schemaVersion);
            bool isV10 = version.Major < 1 || (version.Major == 1 && version.Minor == 0);
            bool isV11 = version.Major == 1 && version.Minor == 1;
            bool isV12OrLater = version.Major > 1 || (version.Major == 1 && version.Minor >= 2);

            // Load categories
            if (root.categories != null)
            {
                foreach (var cat in root.categories)
                {
                    graph.categories.Add(new ForkTrackCategory(cat.id, cat.name, cat.color));
                }
            }

            // Load groups
            if (root.groups != null)
            {
                foreach (var grp in root.groups)
                {
                    var group = new ForkTrackGroup(grp.id, grp.name, grp.color);
                    if (grp.position != null)
                        group.position = new Vector2(grp.position.x, grp.position.y);
                    if (grp.size != null)
                        group.size = new Vector2(grp.size.width, grp.size.height);
                    graph.groups.Add(group);
                }
            }

            // Load objects (v1.1.0+, empty for v1.0.0)
            if (root.objects != null)
            {
                foreach (var obj in root.objects)
                {
                    graph.objects.Add(new ForkTrackObject(obj.id, obj.name, obj.category));
                }
            }

            // Load actions (v1.1.0+, empty for v1.0.0)
            if (root.actions != null)
            {
                foreach (var act in root.actions)
                {
                    graph.actions.Add(new ForkTrackAction(act.id, act.name));
                }
            }

            // Load variables (v1.2.0+, empty for v1.0.0/v1.1.0)
            if (root.variables != null)
            {
                foreach (var v in root.variables)
                {
                    var varType = ParseVariableType(v.type);
                    var defaultVal = varType == VariableType.BOOLEAN
                        ? (object)(v.defaultValue?.ToLower() == "true")
                        : ParseFloat(v.defaultValue);

                    graph.variables.Add(new ForkTrackVariable(v.id, v.name, varType, defaultVal));
                }
            }

            // Load nodes
            if (root.nodes == null || root.nodes.Count == 0)
            {
                throw new FormatException("Invalid JSON: nodes array is required and must not be empty");
            }

            foreach (var jsonNode in root.nodes)
            {
                if (string.IsNullOrEmpty(jsonNode.id))
                {
                    throw new FormatException("Invalid node: missing id");
                }

                var node = new ForkTrackNode
                {
                    id = jsonNode.id,
                    position = jsonNode.position != null
                        ? new Vector2(jsonNode.position.x, jsonNode.position.y)
                        : Vector2.zero
                };

                if (jsonNode.data != null)
                {
                    var data = jsonNode.data;

                    // v1.1.0+ fields (null for v1.0.0)
                    node.objectId = data.objectId;
                    node.actionId = data.actionId;
                    node.title = data.title;
                    node.description = data.description ?? "";
                    node.categoryId = data.category;
                    node.groupId = data.groupId;
                    node.autoCompleteOnUnlock = data.autoCompleteOnUnlock;
                    node.music = data.music;

                    // Use denormalized names from API if present
                    // These come from the server's JOIN query and are more reliable
                    if (!string.IsNullOrEmpty(data.@object))
                    {
                        node.objectName = data.@object;
                    }
                    if (!string.IsNullOrEmpty(data.action))
                    {
                        node.actionName = data.action;
                    }

                    // Notes
                    if (data.notes != null)
                    {
                        foreach (var note in data.notes)
                        {
                            var trigger = note.trigger == "OnUnlock" ? TriggerType.OnUnlock : TriggerType.OnComplete;
                            node.notes.Add(new NodeNote(note.text, trigger));
                        }
                    }

                    // Todos
                    if (data.todos != null)
                    {
                        foreach (var todo in data.todos)
                        {
                            node.todos.Add(new NodeTodo(todo.title, todo.description, todo.responsible, todo.resolved));
                        }
                    }

                    // Custom events
                    if (data.customEvents != null)
                    {
                        foreach (var evt in data.customEvents)
                        {
                            // Extract the actual values from the value objects
                            var valueList = new List<string>(); 
                            string firstValueText = null;  // Store first value's text as fallback
                            if (evt.values != null)
                            {
                                foreach (var v in evt.values)
                                {
                                    // The actual value is in the 'value' field of each object
                                    if (!string.IsNullOrEmpty(v.value))
                                        valueList.Add(v.value);
                                    else if (!string.IsNullOrEmpty(v.text))
                                        valueList.Add(v.text);  // Fallback to text if value is empty

                                    // Capture first value's text as fallback for event-level text
                                    if (firstValueText == null && !string.IsNullOrEmpty(v.text))
                                        firstValueText = v.text;
                                }
                            }

                            // Use event-level text, or fall back to first value's text
                            string eventText = !string.IsNullOrEmpty(evt.text) ? evt.text : firstValueText;

                            var customEvent = new CustomEvent
                            {
                                propertyId = evt.propertyId,
                                trigger = evt.trigger,
                                values = valueList,
                                weight = evt.weight,
                                delay = evt.delay,
                                amount = evt.amount,
                                text = eventText
                            };
                            node.customEvents.Add(customEvent);
                        }
                    }

                    // Variable actions (v1.2.0+)
                    if (data.variableActions != null)
                    {
                        foreach (var va in data.variableActions)
                        {
                            var trigger = va.trigger == "OnUnlock" ? TriggerType.OnUnlock : TriggerType.OnComplete;
                            var op = ParseVariableOperation(va.operation);
                            node.variableActions.Add(new VariableAction(va.id, va.variableId, trigger, op, va.value));
                        }
                    }
                }

                graph.nodes.Add(node);
            }

            // Load edges
            if (root.edges != null)
            {
                foreach (var jsonEdge in root.edges)
                {
                    if (string.IsNullOrEmpty(jsonEdge.id) ||
                        string.IsNullOrEmpty(jsonEdge.sourceNodeId) ||
                        string.IsNullOrEmpty(jsonEdge.targetNodeId))
                    {
                        throw new FormatException("Invalid edge: missing id, sourceNodeId, or targetNodeId");
                    }

                    var requiredState = RequiredState.OnComplete;
                    var condition = DependencyCondition.AND;

                    if (jsonEdge.data != null)
                    {
                        requiredState = jsonEdge.data.requiredState == "OnUnlock"
                            ? RequiredState.OnUnlock
                            : RequiredState.OnComplete;

                        condition = ParseDependencyCondition(jsonEdge.data.condition);
                    }

                    var edge = new ForkTrackEdge(jsonEdge.id, jsonEdge.sourceNodeId, jsonEdge.targetNodeId, requiredState, condition);

                    // Variable conditions (v1.2.0+)
                    if (jsonEdge.data?.variableConditions != null)
                    {
                        foreach (var vc in jsonEdge.data.variableConditions)
                        {
                            var op = ParseVariableOperator(vc.@operator);
                            edge.variableConditions.Add(new VariableCondition(vc.id, vc.variableId, op, vc.value));
                        }
                    }

                    graph.edges.Add(edge);
                }
            }

            // Initialize lookup dictionaries
            graph.Initialize();

            return graph;
        }

        /// <summary>
        /// Loads a graph from a TextAsset
        /// </summary>
        public static ForkTrackGraph LoadFromTextAsset(TextAsset textAsset)
        {
            if (textAsset == null)
            {
                throw new ArgumentNullException(nameof(textAsset));
            }

            return LoadFromJson(textAsset.text);
        }

        #region Serialization

        /// <summary>
        /// Serializes a ForkTrackGraph to JSON string
        /// </summary>
        public static string SerializeToJson(ForkTrackGraph graph)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));

            var root = new JsonRoot
            {
                schemaVersion = CURRENT_SCHEMA_VERSION,
                version = new JsonVersion { major = 1, minor = 2 },
                categories = new List<JsonCategory>(),
                groups = new List<JsonGroup>(),
                objects = new List<JsonObject>(),
                actions = new List<JsonAction>(),
                variables = new List<JsonVariable>(),
                nodes = new List<JsonNode>(),
                edges = new List<JsonEdge>()
            };

            // Serialize categories
            foreach (var cat in graph.categories)
            {
                root.categories.Add(new JsonCategory
                {
                    id = cat.id,
                    name = cat.name,
                    color = cat.color
                });
            }

            // Serialize groups
            foreach (var grp in graph.groups)
            {
                root.groups.Add(new JsonGroup
                {
                    id = grp.id,
                    name = grp.name,
                    color = grp.color,
                    position = new JsonPosition { x = grp.position.x, y = grp.position.y },
                    size = new JsonSize { width = grp.size.x, height = grp.size.y }
                });
            }

            // Serialize objects
            foreach (var obj in graph.objects)
            {
                root.objects.Add(new JsonObject
                {
                    id = obj.id,
                    name = obj.name,
                    category = obj.category
                });
            }

            // Serialize actions
            foreach (var act in graph.actions)
            {
                root.actions.Add(new JsonAction
                {
                    id = act.id,
                    name = act.name
                });
            }

            // Serialize variables
            foreach (var v in graph.variables)
            {
                root.variables.Add(new JsonVariable
                {
                    id = v.id,
                    name = v.name,
                    type = v.type.ToString().ToLower(),
                    defaultValue = v.defaultValue?.ToString() ?? ""
                });
            }

            // Serialize nodes
            foreach (var node in graph.nodes)
            {
                var jsonNode = new JsonNode
                {
                    id = node.id,
                    position = new JsonPosition { x = node.position.x, y = node.position.y },
                    type = "Node",
                    data = new JsonNodeData
                    {
                        objectId = node.objectId,
                        actionId = node.actionId,
                        title = node.title,
                        description = node.description,
                        category = node.categoryId,
                        groupId = node.groupId,
                        autoCompleteOnUnlock = node.autoCompleteOnUnlock,
                        music = node.music,
                        notes = new List<JsonNote>(),
                        todos = new List<JsonTodo>(),
                        customEvents = new List<JsonCustomEvent>(),
                        variableActions = new List<JsonVariableAction>()
                    }
                };

                // Serialize notes
                foreach (var note in node.notes)
                {
                    jsonNode.data.notes.Add(new JsonNote
                    {
                        text = note.text,
                        trigger = note.trigger.ToString()
                    });
                }

                // Serialize todos
                foreach (var todo in node.todos)
                {
                    jsonNode.data.todos.Add(new JsonTodo
                    {
                        title = todo.title,
                        description = todo.description,
                        responsible = todo.responsible,
                        resolved = todo.resolved
                    });
                }

                // Serialize custom events
                foreach (var evt in node.customEvents)
                {
                    // Convert string values back to JsonCustomEventValue objects
                    JsonCustomEventValue[] valueObjects = null;
                    if (evt.values != null && evt.values.Count > 0)
                    {
                        valueObjects = new JsonCustomEventValue[evt.values.Count];
                        for (int i = 0; i < evt.values.Count; i++)
                        {
                            valueObjects[i] = new JsonCustomEventValue { value = evt.values[i] };
                        }
                    }

                    jsonNode.data.customEvents.Add(new JsonCustomEvent
                    {
                        propertyId = evt.propertyId,
                        trigger = evt.trigger,
                        values = valueObjects,
                        weight = evt.weight,
                        delay = evt.delay,
                        amount = evt.amount,
                        text = evt.text
                    });
                }

                // Serialize variable actions
                foreach (var va in node.variableActions)
                {
                    jsonNode.data.variableActions.Add(new JsonVariableAction
                    {
                        id = va.id,
                        variableId = va.variableId,
                        trigger = va.trigger.ToString(),
                        operation = va.operation.ToString(),
                        value = va.value?.ToString() ?? ""
                    });
                }

                root.nodes.Add(jsonNode);
            }

            // Serialize edges
            foreach (var edge in graph.edges)
            {
                var jsonEdge = new JsonEdge
                {
                    id = edge.id,
                    sourceNodeId = edge.sourceNodeId,
                    targetNodeId = edge.targetNodeId,
                    data = new JsonEdgeData
                    {
                        requiredState = edge.requiredState.ToString(),
                        condition = edge.condition.ToString(),
                        variableConditions = new List<JsonVariableCondition>()
                    }
                };

                foreach (var vc in edge.variableConditions)
                {
                    jsonEdge.data.variableConditions.Add(new JsonVariableCondition
                    {
                        id = vc.id,
                        variableId = vc.variableId,
                        @operator = vc.@operator ?? "==",
                        value = vc.value ?? ""
                    });
                }

                root.edges.Add(jsonEdge);
            }

            return JsonUtility.ToJson(root, true);
        }

        #endregion

        #region Parsing Helpers

        private static (int Major, int Minor, int Patch) ParseSchemaVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
                return (1, 0, 0);

            var parts = version.Split('.');
            int major = parts.Length > 0 ? ParseInt(parts[0]) : 1;
            int minor = parts.Length > 1 ? ParseInt(parts[1]) : 0;
            int patch = parts.Length > 2 ? ParseInt(parts[2]) : 0;

            return (major, minor, patch);
        }

        private static int ParseInt(string s)
        {
            return int.TryParse(s, out int result) ? result : 0;
        }

        private static float ParseFloat(string s)
        {
            if (string.IsNullOrEmpty(s))
                return 0f;
            return float.TryParse(s, out float result) ? result : 0f;
        }

        private static VariableType ParseVariableType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return VariableType.NUMBER;

            // Handle both lowercase (web) and uppercase (C#) formats
            return type.ToLower() switch
            {
                "boolean" => VariableType.BOOLEAN,
                "bool" => VariableType.BOOLEAN,
                _ => VariableType.NUMBER
            };
        }

        private static VariableOperation ParseVariableOperation(string operation)
        {
            if (string.IsNullOrEmpty(operation))
                return VariableOperation.SET;

            return operation.ToUpper() switch
            {
                "ADD" => VariableOperation.ADD,
                "SUBTRACT" => VariableOperation.SUBTRACT,
                "TOGGLE" => VariableOperation.TOGGLE,
                _ => VariableOperation.SET
            };
        }

        private static VariableOperator ParseVariableOperator(string op)
        {
            return op switch
            {
                "!=" => VariableOperator.NE,
                ">" => VariableOperator.GT,
                "<" => VariableOperator.LT,
                ">=" => VariableOperator.GTE,
                "<=" => VariableOperator.LTE,
                _ => VariableOperator.EQ
            };
        }

        private static DependencyCondition ParseDependencyCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition))
                return DependencyCondition.AND;

            return condition.ToUpper() switch
            {
                "OR" => DependencyCondition.OR,
                "NOT" => DependencyCondition.NOT,
                _ => DependencyCondition.AND
            };
        }

        /// <summary>
        /// Normalizes custom event "values" fields in JSON.
        /// The web app stores single-value events as "values": "string"
        /// but multi-value events as "values": [{ value: "..." }].
        /// This method converts string values to array format for Unity's JsonUtility.
        /// </summary>
        private static string NormalizeCustomEventValues(string json)
        {
            if (string.IsNullOrEmpty(json))
                return json;

            var result = new System.Text.StringBuilder();
            int i = 0;
            int len = json.Length;

            while (i < len)
            {
                // Look for "values": pattern
                if (i + 10 < len && json.Substring(i, 9) == "\"values\":")
                {
                    result.Append("\"values\":");
                    i += 9;

                    // Skip whitespace
                    while (i < len && char.IsWhiteSpace(json[i]))
                    {
                        result.Append(json[i]);
                        i++;
                    }

                    if (i >= len)
                        break;

                    // Check what follows - if it's a quote (string value), convert to array
                    if (json[i] == '"')
                    {
                        // It's a string value - extract it and convert to array format
                        i++; // skip opening quote
                        var stringValue = new System.Text.StringBuilder();
                        while (i < len && json[i] != '"')
                        {
                            // Handle escape sequences
                            if (json[i] == '\\' && i + 1 < len)
                            {
                                stringValue.Append(json[i]);
                                i++;
                                if (i < len)
                                {
                                    stringValue.Append(json[i]);
                                    i++;
                                }
                            }
                            else
                            {
                                stringValue.Append(json[i]);
                                i++;
                            }
                        }
                        if (i < len)
                            i++; // skip closing quote

                        // Convert string to array format: "value" -> [{"value":"value"}]
                        string val = stringValue.ToString();
                        if (string.IsNullOrEmpty(val))
                        {
                            result.Append("[]");
                        }
                        else
                        {
                            result.Append("[{\"value\":\"");
                            result.Append(val);
                            result.Append("\"}]");
                        }
                    }
                    else
                    {
                        // It's not a string (probably array or null) - copy as-is
                        result.Append(json[i]);
                        i++;
                    }
                }
                else
                {
                    result.Append(json[i]);
                    i++;
                }
            }

            return result.ToString();
        }

        #endregion
    }
}
