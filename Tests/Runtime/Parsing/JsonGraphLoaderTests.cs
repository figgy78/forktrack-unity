using NUnit.Framework;
using ForkTrack.Core;
using ForkTrack.Internal;
using System;

namespace ForkTrack.Tests.Parsing
{
    /// <summary>
    /// Tests for JsonGraphLoader - schema parsing and version handling
    /// REQ-001 through REQ-014
    /// </summary>
    [TestFixture]
    public class JsonGraphLoaderTests
    {
        #region Basic Loading Tests

        [Test]
        public void LoadFromJson_ValidV12Json_ShouldParseSuccessfully()
        {
            // REQ-012: The system SHALL support ForkTrack JSON schema v1.2.0
            string json = GetV12SampleJson();

            var graph = JsonGraphLoader.LoadFromJson(json);

            Assert.IsNotNull(graph);
            Assert.AreEqual("1.2.0", graph.schemaVersion);
            Assert.AreEqual(3, graph.nodes.Count);
            Assert.AreEqual(2, graph.edges.Count);
            Assert.AreEqual(2, graph.variables.Count);
        }

        [Test]
        public void LoadFromJson_ValidV11Json_ShouldParseWithEmptyVariables()
        {
            // REQ-014: WHEN loading v1.1.0 schema THEN variables SHALL be empty arrays
            string json = GetV11SampleJson();

            var graph = JsonGraphLoader.LoadFromJson(json);

            Assert.IsNotNull(graph);
            Assert.AreEqual("1.1.0", graph.schemaVersion);
            Assert.AreEqual(0, graph.variables.Count);
            Assert.AreEqual(2, graph.objects.Count);
            Assert.AreEqual(2, graph.actions.Count);
        }

        [Test]
        public void LoadFromJson_ValidV10Json_ShouldParseWithNullObjectActionIds()
        {
            // REQ-013: WHEN loading v1.0.0 schema THEN objectId/actionId SHALL be null
            string json = GetV10SampleJson();

            var graph = JsonGraphLoader.LoadFromJson(json);

            Assert.IsNotNull(graph);
            Assert.AreEqual("1.0.0", graph.schemaVersion);
            Assert.AreEqual(0, graph.objects.Count);
            Assert.AreEqual(0, graph.actions.Count);
            Assert.AreEqual(0, graph.variables.Count);

            // Nodes should have null objectId/actionId but preserved title
            var node = graph.GetNodeById("node-1");
            Assert.IsNull(node.objectId);
            Assert.IsNull(node.actionId);
            Assert.AreEqual("Start Quest", node.title);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void LoadFromJson_NullJson_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => JsonGraphLoader.LoadFromJson(null));
        }

        [Test]
        public void LoadFromJson_EmptyJson_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => JsonGraphLoader.LoadFromJson(""));
        }

        [Test]
        public void LoadFromJson_MalformedJson_ShouldThrowFormatException()
        {
            string malformed = "{ not valid json }";

            Assert.Throws<FormatException>(() => JsonGraphLoader.LoadFromJson(malformed));
        }

        [Test]
        public void LoadFromJson_MissingSchemaVersion_ShouldThrowFormatException()
        {
            string json = @"{ ""nodes"": [], ""edges"": [] }";

            var ex = Assert.Throws<FormatException>(() => JsonGraphLoader.LoadFromJson(json));
            Assert.That(ex.Message, Does.Contain("schemaVersion"));
        }

        [Test]
        public void LoadFromJson_EmptyNodesArray_ShouldThrowFormatException()
        {
            string json = @"{ ""schemaVersion"": ""1.2.0"", ""nodes"": [], ""edges"": [] }";

            var ex = Assert.Throws<FormatException>(() => JsonGraphLoader.LoadFromJson(json));
            Assert.That(ex.Message, Does.Contain("nodes"));
        }

        #endregion

        #region Node Parsing Tests

        [Test]
        public void LoadFromJson_NodeWithVariableActions_ShouldParseActions()
        {
            string json = GetV12SampleJson();

            var graph = JsonGraphLoader.LoadFromJson(json);
            var node = graph.GetNodeById("node-1");

            Assert.AreEqual(1, node.variableActions.Count);
            var action = node.variableActions[0];
            Assert.AreEqual("var-coins", action.variableId);
            Assert.AreEqual(VariableOperation.ADD, action.GetOperation());
            Assert.AreEqual(10f, action.GetValueAsFloat());
        }

        [Test]
        public void LoadFromJson_NodeWithAutoComplete_ShouldSetFlag()
        {
            string json = GetV12SampleJson();

            var graph = JsonGraphLoader.LoadFromJson(json);
            var node = graph.GetNodeById("node-3");

            Assert.IsTrue(node.autoCompleteOnUnlock);
        }

        [Test]
        public void LoadFromJson_NodeWithNotes_ShouldParseNotes()
        {
            string json = GetV12SampleJson();

            var graph = JsonGraphLoader.LoadFromJson(json);
            var node = graph.GetNodeById("node-1");

            Assert.AreEqual(1, node.notes.Count);
            Assert.AreEqual("This starts the main quest", node.notes[0].text);
            Assert.AreEqual(TriggerType.OnUnlock, node.notes[0].GetTriggerType());
        }

        #endregion

        #region Edge Parsing Tests

        [Test]
        public void LoadFromJson_EdgeWithVariableConditions_ShouldParseConditions()
        {
            string json = GetV12SampleJson();

            var graph = JsonGraphLoader.LoadFromJson(json);
            var edges = graph.GetEdgesByTarget("node-3");

            Assert.AreEqual(1, edges.Count);
            var edge = edges[0];
            Assert.AreEqual(2, edge.variableConditions.Count);

            var condition1 = edge.variableConditions[0];
            Assert.AreEqual("var-haskey", condition1.variableId);
            Assert.AreEqual(VariableOperator.EQ, condition1.GetOperator());
            Assert.IsTrue(condition1.GetValueAsBool());

            var condition2 = edge.variableConditions[1];
            Assert.AreEqual("var-coins", condition2.variableId);
            Assert.AreEqual(VariableOperator.GTE, condition2.GetOperator());
            Assert.AreEqual(50f, condition2.GetValueAsFloat());
        }

        [Test]
        public void LoadFromJson_EdgeWithRequiredStateOnUnlock_ShouldParse()
        {
            string json = @"{
                ""schemaVersion"": ""1.2.0"",
                ""nodes"": [
                    { ""id"": ""n1"", ""position"": { ""x"": 0, ""y"": 0 }, ""data"": {} },
                    { ""id"": ""n2"", ""position"": { ""x"": 0, ""y"": 0 }, ""data"": {} }
                ],
                ""edges"": [
                    { ""id"": ""e1"", ""sourceNodeId"": ""n1"", ""targetNodeId"": ""n2"", ""data"": { ""requiredState"": ""OnUnlock"" } }
                ]
            }";

            var graph = JsonGraphLoader.LoadFromJson(json);
            var edge = graph.edges[0];

            Assert.AreEqual(RequiredState.OnUnlock, edge.GetRequiredState());
        }

        #endregion

        #region Variable Parsing Tests

        [Test]
        public void LoadFromJson_NumberVariable_ShouldParseCorrectly()
        {
            string json = GetV12SampleJson();

            var graph = JsonGraphLoader.LoadFromJson(json);
            var variable = graph.GetVariableByName("coins");

            Assert.IsNotNull(variable);
            Assert.AreEqual(VariableType.NUMBER, variable.GetVariableType());
            Assert.AreEqual(0f, variable.GetDefaultValue());
        }

        [Test]
        public void LoadFromJson_BooleanVariable_ShouldParseCorrectly()
        {
            string json = GetV12SampleJson();

            var graph = JsonGraphLoader.LoadFromJson(json);
            var variable = graph.GetVariableByName("hasKey");

            Assert.IsNotNull(variable);
            Assert.AreEqual(VariableType.BOOLEAN, variable.GetVariableType());
            Assert.AreEqual(false, variable.GetDefaultValue());
        }

        #endregion

        #region Graph Initialization Tests

        [Test]
        public void LoadFromJson_ShouldInitializeNodeLookups()
        {
            // REQ-110: O(1) node lookup by ID
            string json = GetV12SampleJson();

            var graph = JsonGraphLoader.LoadFromJson(json);

            Assert.IsNotNull(graph.GetNodeById("node-1"));
            Assert.IsNotNull(graph.GetNodeById("node-2"));
            Assert.IsNotNull(graph.GetNodeById("node-3"));
            Assert.IsNull(graph.GetNodeById("nonexistent"));
        }

        [Test]
        public void LoadFromJson_ShouldResolveObjectActionNames()
        {
            string json = GetV12SampleJson();

            var graph = JsonGraphLoader.LoadFromJson(json);
            var node = graph.GetNodeById("node-1");

            Assert.AreEqual("Fred", node.objectName);
            Assert.AreEqual("Interact", node.actionName);
        }

        #endregion

        #region Sample JSON Data

        private string GetV10SampleJson()
        {
            return @"{
                ""schemaVersion"": ""1.0.0"",
                ""categories"": [{ ""id"": ""main"", ""name"": ""Main"", ""color"": ""#ec4899"" }],
                ""nodes"": [
                    { ""id"": ""node-1"", ""position"": { ""x"": 100, ""y"": 100 }, ""data"": { ""title"": ""Start Quest"" } },
                    { ""id"": ""node-2"", ""position"": { ""x"": 200, ""y"": 100 }, ""data"": { ""title"": ""End Quest"" } }
                ],
                ""edges"": [
                    { ""id"": ""edge-1"", ""sourceNodeId"": ""node-1"", ""targetNodeId"": ""node-2"", ""data"": { ""requiredState"": ""OnComplete"" } }
                ]
            }";
        }

        private string GetV11SampleJson()
        {
            return @"{
                ""schemaVersion"": ""1.1.0"",
                ""objects"": [
                    { ""id"": ""obj-fred"", ""name"": ""Fred"" },
                    { ""id"": ""obj-door"", ""name"": ""Door"" }
                ],
                ""actions"": [
                    { ""id"": ""act-interact"", ""name"": ""Interact"" },
                    { ""id"": ""act-open"", ""name"": ""Open"" }
                ],
                ""nodes"": [
                    { ""id"": ""node-1"", ""position"": { ""x"": 100, ""y"": 100 }, ""data"": { ""objectId"": ""obj-fred"", ""actionId"": ""act-interact"" } },
                    { ""id"": ""node-2"", ""position"": { ""x"": 200, ""y"": 100 }, ""data"": { ""objectId"": ""obj-door"", ""actionId"": ""act-open"" } }
                ],
                ""edges"": [
                    { ""id"": ""edge-1"", ""sourceNodeId"": ""node-1"", ""targetNodeId"": ""node-2"", ""data"": { ""requiredState"": ""OnComplete"" } }
                ]
            }";
        }

        private string GetV12SampleJson()
        {
            return @"{
                ""schemaVersion"": ""1.2.0"",
                ""categories"": [{ ""id"": ""main"", ""name"": ""Main"", ""color"": ""#ec4899"" }],
                ""groups"": [{ ""id"": ""group-1"", ""name"": ""Tutorial"", ""position"": { ""x"": 50, ""y"": 50 }, ""size"": { ""width"": 600, ""height"": 200 } }],
                ""objects"": [
                    { ""id"": ""obj-fred"", ""name"": ""Fred"" },
                    { ""id"": ""obj-chest"", ""name"": ""Chest"" },
                    { ""id"": ""obj-door"", ""name"": ""Door"" }
                ],
                ""actions"": [
                    { ""id"": ""act-interact"", ""name"": ""Interact"" },
                    { ""id"": ""act-open"", ""name"": ""Open"" }
                ],
                ""variables"": [
                    { ""id"": ""var-coins"", ""name"": ""coins"", ""type"": ""number"", ""defaultValue"": ""0"" },
                    { ""id"": ""var-haskey"", ""name"": ""hasKey"", ""type"": ""boolean"", ""defaultValue"": ""false"" }
                ],
                ""nodes"": [
                    {
                        ""id"": ""node-1"",
                        ""position"": { ""x"": 100, ""y"": 100 },
                        ""data"": {
                            ""objectId"": ""obj-fred"",
                            ""actionId"": ""act-interact"",
                            ""groupId"": ""group-1"",
                            ""notes"": [{ ""text"": ""This starts the main quest"", ""trigger"": ""OnUnlock"" }],
                            ""variableActions"": [
                                { ""id"": ""va-1"", ""variableId"": ""var-coins"", ""trigger"": ""OnComplete"", ""operation"": ""ADD"", ""value"": ""10"" }
                            ]
                        }
                    },
                    {
                        ""id"": ""node-2"",
                        ""position"": { ""x"": 300, ""y"": 100 },
                        ""data"": {
                            ""objectId"": ""obj-chest"",
                            ""actionId"": ""act-open"",
                            ""variableActions"": [
                                { ""id"": ""va-2"", ""variableId"": ""var-haskey"", ""trigger"": ""OnComplete"", ""operation"": ""SET"", ""value"": ""true"" }
                            ]
                        }
                    },
                    {
                        ""id"": ""node-3"",
                        ""position"": { ""x"": 500, ""y"": 100 },
                        ""data"": {
                            ""objectId"": ""obj-door"",
                            ""actionId"": ""act-open"",
                            ""autoCompleteOnUnlock"": true
                        }
                    }
                ],
                ""edges"": [
                    { ""id"": ""edge-1"", ""sourceNodeId"": ""node-1"", ""targetNodeId"": ""node-2"", ""data"": { ""requiredState"": ""OnComplete"" } },
                    {
                        ""id"": ""edge-2"",
                        ""sourceNodeId"": ""node-2"",
                        ""targetNodeId"": ""node-3"",
                        ""data"": {
                            ""requiredState"": ""OnComplete"",
                            ""variableConditions"": [
                                { ""id"": ""vc-1"", ""variableId"": ""var-haskey"", ""operator"": ""=="", ""value"": ""true"" },
                                { ""id"": ""vc-2"", ""variableId"": ""var-coins"", ""operator"": "">="", ""value"": ""50"" }
                            ]
                        }
                    }
                ]
            }";
        }

        #endregion
    }
}
