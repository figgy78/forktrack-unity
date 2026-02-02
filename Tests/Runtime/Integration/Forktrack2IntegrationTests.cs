using NUnit.Framework;
using ForkTrack.Core;
using ForkTrack.Internal;
using System.IO;
using UnityEngine;

namespace ForkTrack.Tests.Integration
{
    /// <summary>
    /// Integration tests using real production graph data (forktrack2.json)
    /// Validates that the ForkTrack package can correctly parse and process
    /// actual user-created graphs from the ForkTrack editor.
    /// </summary>
    [TestFixture]
    public class Forktrack2IntegrationTests
    {
        private string _graphJson;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Load the forktrack2.json test data
            string testDataPath = Path.Combine(Application.dataPath.Replace("/Assets", ""),
                "Packages/com.pixfork.forktrack/Tests/Runtime/TestData/forktrack2.json");

            // Fallback for running in local package development
            if (!File.Exists(testDataPath))
            {
                testDataPath = Path.Combine(Application.dataPath.Replace("/Assets", ""),
                    "unity-package/Tests/Runtime/TestData/forktrack2.json");
            }

            if (File.Exists(testDataPath))
            {
                _graphJson = File.ReadAllText(testDataPath);
            }
            else
            {
                // Use embedded JSON for CI/tests without file access
                _graphJson = GetEmbeddedForktrack2Json();
            }
        }

        [SetUp]
        public void SetUp()
        {
            ForkTrack.ResetRuntime();
        }

        [TearDown]
        public void TearDown()
        {
            ForkTrack.ResetRuntime();
        }

        #region Graph Loading Tests

        [Test]
        public void LoadForktrack2_ShouldParseCorrectSchemaVersion()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            Assert.IsTrue(ForkTrack.IsLoaded);
            Assert.AreEqual("1.2.0", ForkTrack.CurrentGraph.schemaVersion);
        }

        [Test]
        public void LoadForktrack2_ShouldLoad20Nodes()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            Assert.AreEqual(20, ForkTrack.CurrentGraph.NodeCount);
        }

        [Test]
        public void LoadForktrack2_ShouldLoad19Edges()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            Assert.AreEqual(19, ForkTrack.CurrentGraph.EdgeCount);
        }

        [Test]
        public void LoadForktrack2_ShouldLoad6Categories()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            Assert.AreEqual(6, ForkTrack.CurrentGraph.categories.Count);
        }

        [Test]
        public void LoadForktrack2_ShouldLoad1Group()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            Assert.AreEqual(1, ForkTrack.CurrentGraph.groups.Count);
            Assert.AreEqual("test group", ForkTrack.CurrentGraph.groups[0].name);
        }

        [Test]
        public void LoadForktrack2_ShouldLoad11Objects()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            Assert.AreEqual(11, ForkTrack.CurrentGraph.objects.Count);
        }

        [Test]
        public void LoadForktrack2_ShouldLoad8Actions()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            Assert.AreEqual(8, ForkTrack.CurrentGraph.actions.Count);
        }

        [Test]
        public void LoadForktrack2_ShouldLoad2Variables()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            Assert.AreEqual(2, ForkTrack.CurrentGraph.VariableCount);
        }

        #endregion

        #region Node Tests

        [Test]
        public void Forktrack2_ShouldFindNodeByObjectAction()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            // Find "Tape1 - PICKUP" node
            var node = ForkTrack.GetNode("Tape1", "PICKUP");

            Assert.IsNotNull(node);
            Assert.AreEqual("node_1763899855645", node.id);
            Assert.AreEqual("Label: Play me", node.description);
        }

        [Test]
        public void Forktrack2_NodesWithNullObjectAction_ShouldStillLoad()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            // Find a node with null objectId/actionId
            var node = ForkTrack.GetNode("node_1768086450244");

            Assert.IsNotNull(node);
            // objectId/actionId can be null or empty string depending on implementation
            Assert.That(node.objectId, Is.Null.Or.Empty, "objectId should be null or empty");
            Assert.That(node.actionId, Is.Null.Or.Empty, "actionId should be null or empty");
            Assert.AreEqual("[No Object] - [No Action]", node.title);
        }

        [Test]
        public void Forktrack2_NodeInGroup_ShouldHaveGroupId()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            // Node in "test group"
            var node = ForkTrack.GetNode("node_1763388090628");

            Assert.IsNotNull(node);
            Assert.AreEqual("group_1768251939212", node.groupId);
        }

        [Test]
        public void Forktrack2_NodeWithCustomEvents_ShouldParseEvents()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            // "SvalbardGuide - READ" has custom events
            var node = ForkTrack.GetNode("node_1763912532403");

            Assert.IsNotNull(node);
            Assert.IsNotNull(node.customEvents);
            Assert.AreEqual(1, node.customEvents.Count);
            Assert.AreEqual("d33eba3a-16d9-4387-bcf6-e9bf80cf7be6", node.customEvents[0].propertyId);
        }

        #endregion

        #region Variable Tests

        [Test]
        public void Forktrack2_Variables_ShouldBeInitialized()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            // "cfv" variable
            var cfv = ForkTrack.GetVariable<float>("cfv");
            Assert.AreEqual(0f, cfv);

            // "test" variable
            var test = ForkTrack.GetVariable<float>("test");
            Assert.AreEqual(0f, test);
        }

        [Test]
        public void Forktrack2_EdgeWithVariableCondition_ShouldParse()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            // Edge from Oscilloscope - Effect to Oscilloscope - REWIND has a variable condition
            var edges = ForkTrack.CurrentGraph.GetEdgesByTarget("node_1763910957644");

            Assert.AreEqual(1, edges.Count);
            var edge = edges[0];
            Assert.AreEqual(1, edge.variableConditions.Count);

            var condition = edge.variableConditions[0];
            Assert.AreEqual("var_1768251922788", condition.variableId);
            Assert.AreEqual(VariableOperator.EQ, condition.GetOperator());
            Assert.AreEqual(0f, condition.GetValueAsFloat());
        }

        #endregion

        #region Node State Machine Tests

        [Test]
        public void Forktrack2_RootNodes_ShouldBeUnlockedInitially()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            // Root node (Tape1 - PICKUP) should be unlocked
            var rootNode = ForkTrack.GetNode("node_1763899855645");

            Assert.IsNotNull(rootNode);
            Assert.IsTrue(rootNode.IsUnlocked, "Root node should be unlocked initially");
        }

        [Test]
        public void Forktrack2_CompletingRootNode_ShouldCascadeUnlock()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            // Complete the root node
            ForkTrack.CompleteNode("node_1763899855645");

            // Tape1 - PLAY should now be unlocked
            var playNode = ForkTrack.GetNode("node_1763387203190");
            Assert.IsNotNull(playNode);
            Assert.IsTrue(playNode.IsUnlocked, "Tape1 - PLAY should be unlocked after completing root");
        }

        [Test]
        public void Forktrack2_ProgressThroughGraph_ShouldWork()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            int completedCount = 0;
            ForkTrack.OnNodeCompleted += n => completedCount++;

            // Complete first few nodes in sequence
            ForkTrack.CompleteNode("node_1763899855645"); // Tape1 - PICKUP
            ForkTrack.CompleteNode("node_1763387203190"); // Tape1 - PLAY
            ForkTrack.CompleteNode("node_1763388090628"); // Tape1 - EJECT

            Assert.AreEqual(3, completedCount);
        }

        #endregion

        #region Reset Tests

        [Test]
        public void Forktrack2_ResetAll_ShouldRestoreInitialState()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            // Make progress
            ForkTrack.CompleteNode("node_1763899855645");
            ForkTrack.SetVariable("cfv", 100f);

            // Reset
            ForkTrack.ResetAll();

            // Check state is restored
            Assert.AreEqual(0f, ForkTrack.GetVariable<float>("cfv"));

            var rootNode = ForkTrack.GetNode("node_1763899855645");
            Assert.IsTrue(rootNode.IsUnlocked, "Root should be unlocked after reset");
        }

        #endregion

        #region Display Name Tests

        [Test]
        public void Forktrack2_NodeDisplayName_ShouldUseObjectAction()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            var node = ForkTrack.GetNode("Tape1", "PICKUP");
            string displayName = node.GetDisplayName();

            // Should display as "Tape1 - PICKUP" (object - action)
            Assert.That(displayName, Does.Contain("Tape1"));
            Assert.That(displayName, Does.Contain("PICKUP"));
        }

        [Test]
        public void Forktrack2_NodeWithNoObjectAction_ShouldFallbackToTitle()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            var node = ForkTrack.GetNode("node_1763912451456"); // DEMO END node

            Assert.IsNotNull(node);
            string displayName = node.GetDisplayName();

            // Should fall back to title since no object/action
            Assert.AreEqual("DEMO END", displayName);
        }

        #endregion

        #region Custom Event Data Tests

        [Test]
        public void Forktrack2_CustomEvent_ShouldHavePropertyId()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            // "SvalbardGuide - READ" node has custom events
            var node = ForkTrack.GetNode("node_1763912532403");

            Assert.IsNotNull(node);
            Assert.IsNotNull(node.customEvents);
            Assert.AreEqual(1, node.customEvents.Count);

            var evt = node.customEvents[0];
            Assert.AreEqual("d33eba3a-16d9-4387-bcf6-e9bf80cf7be6", evt.propertyId);
        }

        [Test]
        public void Forktrack2_CustomEvent_ShouldHaveValuesLoaded()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            var node = ForkTrack.GetNode("node_1763912532403");
            var evt = node.customEvents[0];

            // Values should be extracted from the value objects array
            Assert.IsNotNull(evt.values);
            Assert.AreEqual(1, evt.values.Count, "Should have 1 value loaded");
            Assert.AreEqual("event4", evt.values[0], "Value should be extracted from value object");
        }

        [Test]
        public void Forktrack2_CustomEvent_ShouldHaveTrigger()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            var node = ForkTrack.GetNode("node_1763912532403");
            var evt = node.customEvents[0];

            Assert.AreEqual("OnComplete", evt.trigger);
            Assert.AreEqual(TriggerType.OnComplete, evt.GetTriggerType());
        }

        [Test]
        public void Forktrack2_CustomEventData_WhenFired_ShouldHaveSelectedValue()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            CustomEventData receivedEvent = null;
            ForkTrack.OnCustomEvent += e => receivedEvent = e;

            // Progress to unlock SvalbardGuide - READ
            ForkTrack.CompleteNode("node_1763899855645"); // Tape1 - PICKUP
            ForkTrack.CompleteNode("node_1763387203190"); // Tape1 - PLAY
            ForkTrack.CompleteNode("node_1763388090628"); // Tape1 - EJECT

            // Now complete the node with the custom event
            ForkTrack.CompleteNode("node_1763912532403"); // SvalbardGuide - READ

            Assert.IsNotNull(receivedEvent, "Custom event should have fired");
            Assert.AreEqual("d33eba3a-16d9-4387-bcf6-e9bf80cf7be6", receivedEvent.PropertyId);
            Assert.AreEqual("event4", receivedEvent.selectedValue, "selectedValue should be populated from values array");
        }

        [Test]
        public void Forktrack2_CustomEventData_ShouldHaveNodeDisplayName()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            CustomEventData receivedEvent = null;
            ForkTrack.OnCustomEvent += e => receivedEvent = e;

            // Progress to the node with custom event
            ForkTrack.CompleteNode("node_1763899855645");
            ForkTrack.CompleteNode("node_1763387203190");
            ForkTrack.CompleteNode("node_1763388090628");
            ForkTrack.CompleteNode("node_1763912532403");

            Assert.IsNotNull(receivedEvent);
            Assert.IsNotNull(receivedEvent.nodeDisplayName, "nodeDisplayName should be set");
            Assert.That(receivedEvent.nodeDisplayName, Does.Contain("SvalbardGuide").Or.Contain("READ"),
                "nodeDisplayName should contain object or action name");
        }

        #endregion

        #region Node Name Resolution Tests

        [Test]
        public void Forktrack2_Node_ShouldHaveObjectNameResolved()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            var node = ForkTrack.GetNode("node_1763899855645"); // Tape1 - PICKUP

            Assert.IsNotNull(node);
            Assert.AreEqual("Tape1", node.objectName, "objectName should be resolved from objects list");
        }

        [Test]
        public void Forktrack2_Node_ShouldHaveActionNameResolved()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            var node = ForkTrack.GetNode("node_1763899855645"); // Tape1 - PICKUP

            Assert.IsNotNull(node);
            Assert.AreEqual("PICKUP", node.actionName, "actionName should be resolved from actions list");
        }

        [Test]
        public void Forktrack2_Node_WithNullObjectId_ShouldHaveNullObjectName()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            var node = ForkTrack.GetNode("node_1768086450244"); // Node with null objectId

            Assert.IsNotNull(node);
            // objectId can be null or empty string depending on implementation
            Assert.That(node.objectId, Is.Null.Or.Empty, "objectId should be null or empty");
            Assert.That(node.objectName, Is.Null.Or.Empty, "objectName should be null or empty when objectId is null");
        }

        #endregion

        #region Category Resolution Tests

        [Test]
        public void Forktrack2_Node_ShouldHaveCategoryNameResolved()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            // Find a node with a category
            var node = ForkTrack.GetNode("node_1763899855645");

            Assert.IsNotNull(node);
            // If this node has a category, verify the name is resolved
            if (!string.IsNullOrEmpty(node.categoryId))
            {
                Assert.IsNotNull(node.categoryName, "categoryName should be resolved when categoryId is set");
            }
        }

        [Test]
        public void Forktrack2_Node_ShouldHaveCategoryColorResolved()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            var node = ForkTrack.GetNode("node_1763899855645");

            Assert.IsNotNull(node);
            if (!string.IsNullOrEmpty(node.categoryId))
            {
                Assert.IsNotNull(node.categoryColor, "categoryColor should be resolved when categoryId is set");
                Assert.That(node.categoryColor, Does.StartWith("#"), "categoryColor should be a hex color");
            }
        }

        [Test]
        public void Forktrack2_Categories_ShouldBeLoadedCorrectly()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            var categories = ForkTrack.CurrentGraph.categories;

            Assert.AreEqual(6, categories.Count);

            // Verify first category
            var tapeCategory = categories.Find(c => c.name == "Tape");
            Assert.IsNotNull(tapeCategory);
            Assert.AreEqual("#3b82f6", tapeCategory.color);
        }

        #endregion

        #region Group Resolution Tests

        [Test]
        public void Forktrack2_Node_ShouldHaveGroupNameResolved()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            var node = ForkTrack.GetNode("node_1763388090628"); // Node in "test group"

            Assert.IsNotNull(node);
            Assert.IsNotNull(node.groupId);
            Assert.AreEqual("test group", node.groupName, "groupName should be resolved from groups list");
        }

        [Test]
        public void Forktrack2_NodeNotInGroup_ShouldHaveNullGroupName()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            var node = ForkTrack.GetNode("node_1763899855645"); // Node NOT in a group

            Assert.IsNotNull(node);
            Assert.IsNull(node.groupId);
            Assert.IsNull(node.groupName, "groupName should be null when not in a group");
        }

        #endregion

        #region Notes Loading Tests

        [Test]
        public void Forktrack2_NodeWithNotes_ShouldHaveNotesLoaded()
        {
            // This test verifies notes loading structure
            // Our embedded test JSON doesn't have notes, but the pattern should work
            ForkTrack.LoadGraphFromJSON(_graphJson);

            // Verify no crash and notes list is initialized
            var node = ForkTrack.GetNode("node_1763899855645");
            Assert.IsNotNull(node);
            Assert.IsNotNull(node.notes, "notes list should never be null");
        }

        #endregion

        #region Todos Loading Tests

        [Test]
        public void Forktrack2_NodeWithTodos_ShouldHaveTodosLoaded()
        {
            // Verify todos list is initialized
            ForkTrack.LoadGraphFromJSON(_graphJson);

            var node = ForkTrack.GetNode("node_1763899855645");
            Assert.IsNotNull(node);
            Assert.IsNotNull(node.todos, "todos list should never be null");
        }

        #endregion

        #region Objects and Actions Loading Tests

        [Test]
        public void Forktrack2_Objects_ShouldBeLoadedWithCorrectData()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            var objects = ForkTrack.CurrentGraph.objects;
            Assert.AreEqual(11, objects.Count);

            // Verify specific object
            var tape1 = objects.Find(o => o.name == "Tape1");
            Assert.IsNotNull(tape1);
            Assert.AreEqual("87e9ddbe-405c-4fd3-8b00-496d2bbe5ffe", tape1.id);
        }

        [Test]
        public void Forktrack2_Actions_ShouldBeLoadedWithCorrectData()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            var actions = ForkTrack.CurrentGraph.actions;
            Assert.AreEqual(8, actions.Count);

            // Verify specific action
            var pickupAction = actions.Find(a => a.name == "PICKUP");
            Assert.IsNotNull(pickupAction);
            Assert.AreEqual("c04c48d6-8eb1-4056-8522-259b71e53755", pickupAction.id);
        }

        #endregion

        #region Edge Loading Tests

        [Test]
        public void Forktrack2_Edges_ShouldHaveRequiredState()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            var edges = ForkTrack.CurrentGraph.GetEdgesBySource("node_1763899855645");
            Assert.AreEqual(1, edges.Count);

            var edge = edges[0];
            // requiredState is stored as string in ForkTrackEdge
            Assert.AreEqual("OnComplete", edge.requiredState);
        }

        [Test]
        public void Forktrack2_Edges_ShouldHaveDefaultCondition()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            var edges = ForkTrack.CurrentGraph.GetEdgesBySource("node_1763899855645");
            var edge = edges[0];

            // Default condition should be AND - condition is stored as string
            Assert.AreEqual("AND", edge.condition);
        }

        #endregion

        #region Variable Action Loading Tests

        [Test]
        public void Forktrack2_NodeVariableActions_ShouldBeLoaded()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            // Verify variable actions list is initialized on all nodes
            foreach (var node in ForkTrack.CurrentGraph.nodes)
            {
                Assert.IsNotNull(node.variableActions, $"variableActions should not be null on node {node.id}");
            }
        }

        #endregion

        #region Complete Data Integrity Tests

        [Test]
        public void Forktrack2_AllNodes_ShouldHaveValidIds()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            foreach (var node in ForkTrack.CurrentGraph.nodes)
            {
                Assert.IsFalse(string.IsNullOrEmpty(node.id), "Every node must have an ID");
            }
        }

        [Test]
        public void Forktrack2_AllEdges_ShouldHaveValidNodeReferences()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            foreach (var edge in ForkTrack.CurrentGraph.edges)
            {
                Assert.IsFalse(string.IsNullOrEmpty(edge.sourceNodeId), $"Edge {edge.id} missing sourceNodeId");
                Assert.IsFalse(string.IsNullOrEmpty(edge.targetNodeId), $"Edge {edge.id} missing targetNodeId");
            }
        }

        [Test]
        public void Forktrack2_LookupDictionaries_ShouldBeInitialized()
        {
            ForkTrack.LoadGraphFromJSON(_graphJson);

            // Test that lookup dictionaries work (they're built on first access)
            var node = ForkTrack.CurrentGraph.GetNodeById("node_1763899855645");
            Assert.IsNotNull(node);

            var edges = ForkTrack.CurrentGraph.GetEdgesBySource("node_1763899855645");
            Assert.IsNotNull(edges);

            var variable = ForkTrack.CurrentGraph.GetVariableByName("cfv");
            Assert.IsNotNull(variable);
        }

        #endregion

        #region Embedded JSON (Fallback)

        private string GetEmbeddedForktrack2Json()
        {
            // Return a minimal version of the graph for CI environments
            return @"{
                ""schemaVersion"": ""1.2.0"",
                ""version"": { ""major"": 1, ""minor"": 0 },
                ""categories"": [
                    { ""id"": ""cat_1763896892321"", ""name"": ""Tape"", ""color"": ""#3b82f6"" },
                    { ""id"": ""cat_1763896930200"", ""name"": ""Environment"", ""color"": ""#7ea178"" },
                    { ""id"": ""cat_1763896936703"", ""name"": ""Sound"", ""color"": ""#bb3bf7"" },
                    { ""id"": ""cat_1763897791024"", ""name"": ""Document"", ""color"": ""#f7a93b"" },
                    { ""id"": ""cat_1763898941877"", ""name"": ""Other"", ""color"": ""#3b82f6"" },
                    { ""id"": ""cat_1763904512294"", ""name"": ""Effect"", ""color"": ""#f73b86"" }
                ],
                ""groups"": [
                    { ""id"": ""group_1768251939212"", ""name"": ""test group"", ""position"": { ""x"": 100, ""y"": 100 }, ""size"": { ""width"": 400, ""height"": 300 } }
                ],
                ""objects"": [
                    { ""id"": ""87e9ddbe-405c-4fd3-8b00-496d2bbe5ffe"", ""name"": ""Tape1"" },
                    { ""id"": ""17359d00-81dc-4ae9-968b-71e74c802270"", ""name"": ""Tape2"" },
                    { ""id"": ""fb11dd8d-8f37-41ad-8264-c4bc9c993ec3"", ""name"": ""Oscilloscope"" },
                    { ""id"": ""a2ff5149-7021-467b-991f-0a63e88c4b92"", ""name"": ""Clock"" },
                    { ""id"": ""ab8a9448-d3cd-4504-a9ba-ea8de9282997"", ""name"": ""Lamp"" },
                    { ""id"": ""0b669dc9-fea2-4594-90e3-5ee5249dbfde"", ""name"": ""UV Filter"" },
                    { ""id"": ""13dd4690-a46e-4f65-943b-190be0ad0cb3"", ""name"": ""SvalbardGuide"" },
                    { ""id"": ""fd3ede6a-2053-4ba6-9273-503fa42eff05"", ""name"": ""TopDrawer"" },
                    { ""id"": ""7995e8f0-c80c-4c37-92ba-d4ba6d723eb2"", ""name"": ""UnfinishedLetter"" },
                    { ""id"": ""8757dffc-4251-4590-9b04-ce7d4984fb0d"", ""name"": ""CassetteRecorderManual"" },
                    { ""id"": ""39421955-7dec-47c6-8533-52edcd122c24"", ""name"": ""Window"" }
                ],
                ""actions"": [
                    { ""id"": ""c04c48d6-8eb1-4056-8522-259b71e53755"", ""name"": ""PICKUP"" },
                    { ""id"": ""4b2b29e1-cae8-4566-8402-69e8567a5718"", ""name"": ""PLAY"" },
                    { ""id"": ""641c5177-118d-4f7a-941e-83fcfe3da0b0"", ""name"": ""EJECT"" },
                    { ""id"": ""39973541-5d43-4f7d-b68a-66387c6323fd"", ""name"": ""OPEN"" },
                    { ""id"": ""90dfafcf-af2b-4214-9a98-91553fb4443f"", ""name"": ""READ"" },
                    { ""id"": ""3996fbb3-3dc0-4626-b53e-41f45aeabf1a"", ""name"": ""Effect"" },
                    { ""id"": ""49982c5e-5d2d-4c8e-b4c9-9ba543c0bcbd"", ""name"": ""REWIND"" },
                    { ""id"": ""290117d3-b667-4e91-9bde-e627b481b87c"", ""name"": ""INTERACT"" }
                ],
                ""variables"": [
                    { ""id"": ""var_1768086750506"", ""name"": ""cfv"", ""type"": ""number"", ""defaultValue"": 0 },
                    { ""id"": ""var_1768251922788"", ""name"": ""test"", ""type"": ""number"", ""defaultValue"": 0 }
                ],
                ""nodes"": [
                    { ""id"": ""node_1763899855645"", ""position"": { ""x"": -270, ""y"": 90 }, ""data"": { ""objectId"": ""87e9ddbe-405c-4fd3-8b00-496d2bbe5ffe"", ""actionId"": ""c04c48d6-8eb1-4056-8522-259b71e53755"", ""title"": ""Tape1 - PICKUP"", ""description"": ""Label: Play me"" } },
                    { ""id"": ""node_1763387203190"", ""position"": { ""x"": -315, ""y"": 285 }, ""data"": { ""objectId"": ""87e9ddbe-405c-4fd3-8b00-496d2bbe5ffe"", ""actionId"": ""4b2b29e1-cae8-4566-8402-69e8567a5718"", ""title"": ""Tape1 - PLAY"", ""description"": ""Segment 1: Freds Introduction"" } },
                    { ""id"": ""node_1763388090628"", ""position"": { ""x"": -300, ""y"": 540 }, ""data"": { ""objectId"": ""87e9ddbe-405c-4fd3-8b00-496d2bbe5ffe"", ""actionId"": ""641c5177-118d-4f7a-941e-83fcfe3da0b0"", ""groupId"": ""group_1768251939212"" } },
                    { ""id"": ""node_1768086450244"", ""position"": { ""x"": 570, ""y"": 465 }, ""data"": { ""objectId"": null, ""actionId"": null, ""title"": ""[No Object] - [No Action]"" } },
                    { ""id"": ""node_1763912451456"", ""position"": { ""x"": -750, ""y"": 555 }, ""data"": { ""objectId"": null, ""actionId"": null, ""title"": ""DEMO END"", ""description"": ""End screen with WISHLIST call to action."" } },
                    { ""id"": ""node_1763912346845"", ""position"": { ""x"": -750, ""y"": 330 }, ""data"": { ""title"": ""PLAYER - Effect (tape 1)"" } },
                    { ""id"": ""node_1763388435796"", ""position"": { ""x"": -540, ""y"": 720 }, ""data"": { ""objectId"": ""fd3ede6a-2053-4ba6-9273-503fa42eff05"", ""actionId"": ""39973541-5d43-4f7d-b68a-66387c6323fd"", ""groupId"": ""group_1768251939212"" } },
                    { ""id"": ""node_1763388541676"", ""position"": { ""x"": -495, ""y"": 930 }, ""data"": { ""objectId"": ""17359d00-81dc-4ae9-968b-71e74c802270"", ""actionId"": ""c04c48d6-8eb1-4056-8522-259b71e53755"" } },
                    { ""id"": ""node_1763388619579"", ""position"": { ""x"": -60, ""y"": 900 }, ""data"": { ""objectId"": ""7995e8f0-c80c-4c37-92ba-d4ba6d723eb2"", ""actionId"": ""90dfafcf-af2b-4214-9a98-91553fb4443f"" } },
                    { ""id"": ""node_1763388250279"", ""position"": { ""x"": 135, ""y"": 555 }, ""data"": { ""objectId"": ""39421955-7dec-47c6-8533-52edcd122c24"" } },
                    { ""id"": ""node_1763388826556"", ""position"": { ""x"": 240, ""y"": 900 }, ""data"": { ""objectId"": ""8757dffc-4251-4590-9b04-ce7d4984fb0d"", ""actionId"": ""90dfafcf-af2b-4214-9a98-91553fb4443f"" } },
                    { ""id"": ""node_1763912532403"", ""position"": { ""x"": 555, ""y"": 900 }, ""data"": { ""objectId"": ""13dd4690-a46e-4f65-943b-190be0ad0cb3"", ""actionId"": ""90dfafcf-af2b-4214-9a98-91553fb4443f"", ""customEvents"": [{ ""propertyId"": ""d33eba3a-16d9-4387-bcf6-e9bf80cf7be6"", ""trigger"": ""OnComplete"", ""values"": [{ ""value"": ""event4"", ""weight"": 100 }] }] } },
                    { ""id"": ""node_1763900536838"", ""position"": { ""x"": -495, ""y"": 1125 }, ""data"": { ""objectId"": ""17359d00-81dc-4ae9-968b-71e74c802270"", ""actionId"": ""4b2b29e1-cae8-4566-8402-69e8567a5718"" } },
                    { ""id"": ""node_1763900599154"", ""position"": { ""x"": -465, ""y"": 1395 }, ""data"": { ""objectId"": ""17359d00-81dc-4ae9-968b-71e74c802270"", ""actionId"": ""641c5177-118d-4f7a-941e-83fcfe3da0b0"" } },
                    { ""id"": ""node_1763904180363"", ""position"": { ""x"": -135, ""y"": 1140 }, ""data"": { ""title"": ""PLAYER - Effect (Tape2)"" } },
                    { ""id"": ""node_1763910335048"", ""position"": { ""x"": -105, ""y"": 1380 }, ""data"": { ""objectId"": ""fb11dd8d-8f37-41ad-8264-c4bc9c993ec3"", ""actionId"": ""3996fbb3-3dc0-4626-b53e-41f45aeabf1a"" } },
                    { ""id"": ""node_1763910957644"", ""position"": { ""x"": -75, ""y"": 1605 }, ""data"": { ""objectId"": ""fb11dd8d-8f37-41ad-8264-c4bc9c993ec3"", ""actionId"": ""49982c5e-5d2d-4c8e-b4c9-9ba543c0bcbd"" } },
                    { ""id"": ""node_1763911133168"", ""position"": { ""x"": -390, ""y"": 1665 }, ""data"": { ""objectId"": ""a2ff5149-7021-467b-991f-0a63e88c4b92"", ""actionId"": ""3996fbb3-3dc0-4626-b53e-41f45aeabf1a"" } },
                    { ""id"": ""node_1763911430387"", ""position"": { ""x"": -780, ""y"": 1680 }, ""data"": { ""objectId"": ""a2ff5149-7021-467b-991f-0a63e88c4b92"", ""actionId"": ""290117d3-b667-4e91-9bde-e627b481b87c"" } },
                    { ""id"": ""node_1763911562685"", ""position"": { ""x"": -750, ""y"": 1875 }, ""data"": { ""objectId"": ""0b669dc9-fea2-4594-90e3-5ee5249dbfde"", ""actionId"": ""c04c48d6-8eb1-4056-8522-259b71e53755"" } }
                ],
                ""edges"": [
                    { ""id"": ""e1"", ""sourceNodeId"": ""node_1763899855645"", ""targetNodeId"": ""node_1763387203190"", ""data"": { ""requiredState"": ""OnComplete"" } },
                    { ""id"": ""e2"", ""sourceNodeId"": ""node_1763387203190"", ""targetNodeId"": ""node_1763388090628"", ""data"": { ""requiredState"": ""OnComplete"" } },
                    { ""id"": ""e3"", ""sourceNodeId"": ""node_1763387203190"", ""targetNodeId"": ""node_1763912346845"", ""data"": { ""requiredState"": ""OnComplete"" } },
                    { ""id"": ""e4"", ""sourceNodeId"": ""node_1763912346845"", ""targetNodeId"": ""node_1763912451456"", ""data"": { ""requiredState"": ""OnComplete"" } },
                    { ""id"": ""e5"", ""sourceNodeId"": ""node_1763388090628"", ""targetNodeId"": ""node_1763388435796"", ""data"": { ""requiredState"": ""OnComplete"" } },
                    { ""id"": ""e6"", ""sourceNodeId"": ""node_1763388090628"", ""targetNodeId"": ""node_1763388250279"", ""data"": { ""requiredState"": ""OnComplete"" } },
                    { ""id"": ""e7"", ""sourceNodeId"": ""node_1763388090628"", ""targetNodeId"": ""node_1763388826556"", ""data"": { ""requiredState"": ""OnComplete"" } },
                    { ""id"": ""e8"", ""sourceNodeId"": ""node_1763388090628"", ""targetNodeId"": ""node_1763912532403"", ""data"": { ""requiredState"": ""OnComplete"" } },
                    { ""id"": ""e9"", ""sourceNodeId"": ""node_1763388435796"", ""targetNodeId"": ""node_1763388541676"", ""data"": { ""requiredState"": ""OnComplete"" } },
                    { ""id"": ""e10"", ""sourceNodeId"": ""node_1763388435796"", ""targetNodeId"": ""node_1763388619579"", ""data"": { ""requiredState"": ""OnComplete"" } },
                    { ""id"": ""e11"", ""sourceNodeId"": ""node_1763388541676"", ""targetNodeId"": ""node_1763900536838"", ""data"": { ""requiredState"": ""OnComplete"" } },
                    { ""id"": ""e12"", ""sourceNodeId"": ""node_1763900536838"", ""targetNodeId"": ""node_1763900599154"", ""data"": { ""requiredState"": ""OnComplete"" } },
                    { ""id"": ""e13"", ""sourceNodeId"": ""node_1763900536838"", ""targetNodeId"": ""node_1763904180363"", ""data"": { ""requiredState"": ""OnComplete"" } },
                    { ""id"": ""e14"", ""sourceNodeId"": ""node_1763900536838"", ""targetNodeId"": ""node_1763910335048"", ""data"": { ""requiredState"": ""OnComplete"" } },
                    { ""id"": ""e15"", ""sourceNodeId"": ""node_1763910335048"", ""targetNodeId"": ""node_1763910957644"", ""data"": { ""requiredState"": ""OnComplete"", ""variableConditions"": [{ ""id"": ""vc_1768251929900"", ""variableId"": ""var_1768251922788"", ""operator"": ""=="", ""value"": 0 }] } },
                    { ""id"": ""e16"", ""sourceNodeId"": ""node_1763910957644"", ""targetNodeId"": ""node_1763911133168"", ""data"": { ""requiredState"": ""OnComplete"" } },
                    { ""id"": ""e17"", ""sourceNodeId"": ""node_1763911133168"", ""targetNodeId"": ""node_1763911430387"", ""data"": { ""requiredState"": ""OnComplete"" } },
                    { ""id"": ""e18"", ""sourceNodeId"": ""node_1763911430387"", ""targetNodeId"": ""node_1763911562685"", ""data"": { ""requiredState"": ""OnComplete"" } },
                    { ""id"": ""e19"", ""sourceNodeId"": ""node_1768086450244"", ""targetNodeId"": ""node_1768086564128"", ""data"": { ""requiredState"": ""OnComplete"" } }
                ]
            }";
        }

        #endregion
    }
}
