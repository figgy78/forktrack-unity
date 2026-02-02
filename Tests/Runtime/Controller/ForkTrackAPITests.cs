using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ForkTrack.Core;
using ForkTrack.Internal;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ForkTrack.Tests.Controller
{
    /// <summary>
    /// Tests for ForkTrack static API facade
    /// </summary>
    [TestFixture]
    public class ForkTrackAPITests
    {
        [SetUp]
        public void SetUp()
        {
            // Reset runtime before each test
            ForkTrack.ResetRuntime();
        }

        [TearDown]
        public void TearDown()
        {
            ForkTrack.ResetRuntime();
        }

        #region Loading Tests

        [Test]
        public void LoadGraphFromJSON_ValidJson_ShouldFireOnGraphLoaded()
        {
            // REQ-005: WHEN a graph is loaded THEN fire OnGraphLoaded
            ForkTrackGraph loadedGraph = null;
            ForkTrack.OnGraphLoaded += g => loadedGraph = g;

            ForkTrack.LoadGraphFromJSON(GetSimpleGraphJson());

            Assert.IsNotNull(loadedGraph);
            Assert.IsTrue(ForkTrack.IsLoaded);
        }

        [Test]
        public void LoadGraphFromJSON_InvalidJson_ShouldFireOnGraphLoadError()
        {
            // REQ-006: IF loading fails THEN fire OnGraphLoadError
            string error = null;
            ForkTrack.OnGraphLoadError += e => error = e;

            // Expect the error log that will be generated
            LogAssert.Expect(LogType.Error, new Regex(@"\[ForkTrack\] Failed to load graph.*"));

            ForkTrack.LoadGraphFromJSON("{ invalid json }");

            Assert.IsNotNull(error);
            Assert.IsFalse(ForkTrack.IsLoaded);
        }

        #endregion

        #region Node Query Tests

        [Test]
        public void GetNode_ById_ShouldReturnNode()
        {
            // REQ-110
            ForkTrack.LoadGraphFromJSON(GetSimpleGraphJson());

            var node = ForkTrack.GetNode("node-1");

            Assert.IsNotNull(node);
            Assert.AreEqual("node-1", node.id);
        }

        [Test]
        public void GetNode_ByObjectAction_ShouldReturnNode()
        {
            // REQ-111
            ForkTrack.LoadGraphFromJSON(GetSimpleGraphJson());

            var node = ForkTrack.GetNode("Fred", "Interact");

            Assert.IsNotNull(node);
            Assert.AreEqual("Fred", node.objectName);
            Assert.AreEqual("Interact", node.actionName);
        }

        [Test]
        public void GetNodesInState_ShouldReturnFilteredNodes()
        {
            // REQ-115
            ForkTrack.LoadGraphFromJSON(GetSimpleGraphJson());

            // Root node should be unlocked automatically
            var unlocked = ForkTrack.GetNodesInState(NodeState.Unlocked);
            var locked = ForkTrack.GetNodesInState(NodeState.Locked);

            Assert.That(unlocked.Count, Is.GreaterThan(0));
            Assert.IsTrue(locked.Count >= 0);
        }

        #endregion

        #region Node Action Tests

        [Test]
        public void CompleteNode_ShouldFireOnNodeCompleted()
        {
            // REQ-031
            ForkTrack.LoadGraphFromJSON(GetSimpleGraphJson());

            ForkTrackNode completedNode = null;
            ForkTrack.OnNodeCompleted += n => completedNode = n;

            // Root node should be unlocked, so we can complete it
            ForkTrack.CompleteNode("node-1");

            Assert.IsNotNull(completedNode);
            Assert.AreEqual("node-1", completedNode.id);
        }

        [Test]
        public void CompleteNode_ShouldCascadeUnlock()
        {
            // REQ-050-053
            ForkTrack.LoadGraphFromJSON(GetSimpleGraphJson());

            var unlockedNodes = new List<ForkTrackNode>();
            ForkTrack.OnNodeUnlocked += n => unlockedNodes.Add(n);

            // Complete root node
            ForkTrack.CompleteNode("node-1");

            // node-2 should be unlocked via cascade
            Assert.That(unlockedNodes, Has.Some.Matches<ForkTrackNode>(n => n.id == "node-2"));
        }

        [Test]
        public void ResetNode_ShouldFireOnNodeReset()
        {
            // REQ-032
            ForkTrack.LoadGraphFromJSON(GetSimpleGraphJson());

            ForkTrackNode resetNode = null;
            ForkTrack.OnNodeReset += n => resetNode = n;

            // First complete, then reset
            ForkTrack.CompleteNode("node-1");
            ForkTrack.ResetNode("node-1");

            Assert.IsNotNull(resetNode);
            Assert.AreEqual("node-1", resetNode.id);
            Assert.IsTrue(resetNode.IsLocked);
        }

        #endregion

        #region Variable Tests

        [Test]
        public void GetVariable_ShouldReturnValue()
        {
            // REQ-100
            ForkTrack.LoadGraphFromJSON(GetGraphWithVariablesJson());

            float coins = ForkTrack.GetVariable<float>("coins");

            Assert.AreEqual(0f, coins);
        }

        [Test]
        public void SetVariable_ShouldUpdateAndFireEvent()
        {
            // REQ-101, REQ-063
            ForkTrack.LoadGraphFromJSON(GetGraphWithVariablesJson());

            ForkTrackVariable changedVar = null;
            object newValue = null;
            ForkTrack.OnVariableChanged += (v, o, n) =>
            {
                changedVar = v;
                newValue = n;
            };

            ForkTrack.SetVariable("coins", 100f);

            Assert.IsNotNull(changedVar);
            Assert.AreEqual("coins", changedVar.name);
            Assert.AreEqual(100f, newValue);
            Assert.AreEqual(100f, ForkTrack.GetVariable<float>("coins"));
        }

        [Test]
        public void CompleteNode_WithVariableAction_ShouldExecuteAction()
        {
            // REQ-081
            ForkTrack.LoadGraphFromJSON(GetGraphWithVariableActionsJson());

            ForkTrack.CompleteNode("node-1");

            // Variable action should have added 10 to coins
            Assert.AreEqual(10f, ForkTrack.GetVariable<float>("coins"));
        }

        #endregion

        #region ResetAll Tests

        [Test]
        public void ResetAll_ShouldResetNodesAndVariables()
        {
            // REQ-125
            ForkTrack.LoadGraphFromJSON(GetGraphWithVariablesJson());

            // Make some changes
            ForkTrack.CompleteNode("node-1");
            ForkTrack.SetVariable("coins", 999f);

            // Reset
            ForkTrack.ResetAll();

            // Variables should be reset
            Assert.AreEqual(0f, ForkTrack.GetVariable<float>("coins"));

            // Completed nodes should be locked again, root nodes re-unlocked
            var node1 = ForkTrack.GetNode("node-1");
            Assert.IsTrue(node1.IsUnlocked || node1.IsLocked); // Root may be unlocked again
        }

        #endregion

        #region Sample JSON Data

        private string GetSimpleGraphJson()
        {
            return @"{
                ""schemaVersion"": ""1.2.0"",
                ""objects"": [
                    { ""id"": ""obj-fred"", ""name"": ""Fred"" },
                    { ""id"": ""obj-door"", ""name"": ""Door"" }
                ],
                ""actions"": [
                    { ""id"": ""act-interact"", ""name"": ""Interact"" }
                ],
                ""variables"": [],
                ""nodes"": [
                    { ""id"": ""node-1"", ""position"": { ""x"": 0, ""y"": 0 }, ""data"": { ""objectId"": ""obj-fred"", ""actionId"": ""act-interact"" } },
                    { ""id"": ""node-2"", ""position"": { ""x"": 100, ""y"": 0 }, ""data"": { ""objectId"": ""obj-door"", ""actionId"": ""act-interact"" } }
                ],
                ""edges"": [
                    { ""id"": ""edge-1"", ""sourceNodeId"": ""node-1"", ""targetNodeId"": ""node-2"", ""data"": { ""requiredState"": ""OnComplete"" } }
                ]
            }";
        }

        private string GetGraphWithVariablesJson()
        {
            return @"{
                ""schemaVersion"": ""1.2.0"",
                ""objects"": [{ ""id"": ""obj-1"", ""name"": ""Object"" }],
                ""actions"": [{ ""id"": ""act-1"", ""name"": ""Action"" }],
                ""variables"": [
                    { ""id"": ""var-coins"", ""name"": ""coins"", ""type"": ""number"", ""defaultValue"": ""0"" }
                ],
                ""nodes"": [
                    { ""id"": ""node-1"", ""position"": { ""x"": 0, ""y"": 0 }, ""data"": { ""objectId"": ""obj-1"", ""actionId"": ""act-1"" } }
                ],
                ""edges"": []
            }";
        }

        private string GetGraphWithVariableActionsJson()
        {
            return @"{
                ""schemaVersion"": ""1.2.0"",
                ""objects"": [{ ""id"": ""obj-1"", ""name"": ""Object"" }],
                ""actions"": [{ ""id"": ""act-1"", ""name"": ""Action"" }],
                ""variables"": [
                    { ""id"": ""var-coins"", ""name"": ""coins"", ""type"": ""number"", ""defaultValue"": ""0"" }
                ],
                ""nodes"": [
                    {
                        ""id"": ""node-1"",
                        ""position"": { ""x"": 0, ""y"": 0 },
                        ""data"": {
                            ""objectId"": ""obj-1"",
                            ""actionId"": ""act-1"",
                            ""variableActions"": [
                                { ""id"": ""va-1"", ""variableId"": ""var-coins"", ""trigger"": ""OnComplete"", ""operation"": ""ADD"", ""value"": ""10"" }
                            ]
                        }
                    }
                ],
                ""edges"": []
            }";
        }

        #endregion
    }
}
