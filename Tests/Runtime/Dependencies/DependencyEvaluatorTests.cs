using NUnit.Framework;
using ForkTrack.Core;
using ForkTrack.Internal;
using System.Collections.Generic;

namespace ForkTrack.Tests.Dependencies
{
    /// <summary>
    /// Tests for DependencyEvaluator - AND/OR/NOT logic
    /// REQ-040 through REQ-046
    /// </summary>
    [TestFixture]
    public class DependencyEvaluatorTests
    {
        private DependencyEvaluator _evaluator;
        private VariableStore _variableStore;

        [SetUp]
        public void SetUp()
        {
            _variableStore = new VariableStore();
            _evaluator = new DependencyEvaluator(_variableStore);
        }

        #region No Dependencies Tests

        [Test]
        public void CanUnlock_NodeWithNoDependencies_ShouldReturnTrue()
        {
            // REQ-040: No dependencies = always unlockable
            var graph = CreateSimpleGraph();
            var rootNode = graph.GetNodeById("node-1");

            bool result = _evaluator.CanUnlock(rootNode, graph);

            Assert.IsTrue(result);
        }

        #endregion

        #region AND Logic Tests

        [Test]
        public void CanUnlock_AndCondition_AllSatisfied_ShouldReturnTrue()
        {
            // REQ-041: AND - all must be satisfied
            var graph = CreateGraphWithAndDependencies();

            // Complete both source nodes
            graph.GetNodeById("node-1").Unlock();
            graph.GetNodeById("node-1").Complete();
            graph.GetNodeById("node-2").Unlock();
            graph.GetNodeById("node-2").Complete();

            var targetNode = graph.GetNodeById("node-3");
            bool result = _evaluator.CanUnlock(targetNode, graph);

            Assert.IsTrue(result);
        }

        [Test]
        public void CanUnlock_AndCondition_OnlyOneSatisfied_ShouldReturnFalse()
        {
            // REQ-041: AND - all must be satisfied
            var graph = CreateGraphWithAndDependencies();

            // Complete only one source node
            graph.GetNodeById("node-1").Unlock();
            graph.GetNodeById("node-1").Complete();
            // node-2 remains locked

            var targetNode = graph.GetNodeById("node-3");
            bool result = _evaluator.CanUnlock(targetNode, graph);

            Assert.IsFalse(result);
        }

        #endregion

        #region OR Logic Tests

        [Test]
        public void CanUnlock_OrCondition_OneSatisfied_ShouldReturnTrue()
        {
            // REQ-042: OR - at least one must be satisfied
            var graph = CreateGraphWithOrDependencies();

            // Complete only one source node
            graph.GetNodeById("node-1").Unlock();
            graph.GetNodeById("node-1").Complete();

            var targetNode = graph.GetNodeById("node-3");
            bool result = _evaluator.CanUnlock(targetNode, graph);

            Assert.IsTrue(result);
        }

        [Test]
        public void CanUnlock_OrCondition_NoneSatisfied_ShouldReturnFalse()
        {
            // REQ-042: OR - at least one must be satisfied
            var graph = CreateGraphWithOrDependencies();

            // No source nodes completed
            var targetNode = graph.GetNodeById("node-3");
            bool result = _evaluator.CanUnlock(targetNode, graph);

            Assert.IsFalse(result);
        }

        #endregion

        #region NOT Logic Tests

        [Test]
        public void CanUnlock_NotCondition_SourceNotCompleted_ShouldReturnTrue()
        {
            // REQ-043: NOT - source must NOT be satisfied
            var graph = CreateGraphWithNotDependency();

            // Source node remains locked
            var targetNode = graph.GetNodeById("node-2");
            bool result = _evaluator.CanUnlock(targetNode, graph);

            Assert.IsTrue(result);
        }

        [Test]
        public void CanUnlock_NotCondition_SourceCompleted_ShouldReturnFalse()
        {
            // REQ-043: NOT - source must NOT be satisfied
            var graph = CreateGraphWithNotDependency();

            // Complete source node
            graph.GetNodeById("node-1").Unlock();
            graph.GetNodeById("node-1").Complete();

            var targetNode = graph.GetNodeById("node-2");
            bool result = _evaluator.CanUnlock(targetNode, graph);

            Assert.IsFalse(result);
        }

        #endregion

        #region RequiredState Tests

        [Test]
        public void CanUnlock_OnUnlockRequired_SourceUnlocked_ShouldReturnTrue()
        {
            // REQ-044: OnUnlock satisfied if source is Unlocked OR Completed
            var graph = CreateGraphWithOnUnlockDependency();

            graph.GetNodeById("node-1").Unlock();
            // Not completed, just unlocked

            var targetNode = graph.GetNodeById("node-2");
            bool result = _evaluator.CanUnlock(targetNode, graph);

            Assert.IsTrue(result);
        }

        [Test]
        public void CanUnlock_OnCompleteRequired_SourceOnlyUnlocked_ShouldReturnFalse()
        {
            // REQ-045: OnComplete satisfied only if source is Completed
            var graph = CreateSimpleGraph();

            graph.GetNodeById("node-1").Unlock();
            // Not completed, just unlocked

            var targetNode = graph.GetNodeById("node-2");
            bool result = _evaluator.CanUnlock(targetNode, graph);

            Assert.IsFalse(result);
        }

        [Test]
        public void CanUnlock_OnCompleteRequired_SourceCompleted_ShouldReturnTrue()
        {
            // REQ-045: OnComplete satisfied only if source is Completed
            var graph = CreateSimpleGraph();

            graph.GetNodeById("node-1").Unlock();
            graph.GetNodeById("node-1").Complete();

            var targetNode = graph.GetNodeById("node-2");
            bool result = _evaluator.CanUnlock(targetNode, graph);

            Assert.IsTrue(result);
        }

        #endregion

        #region Variable Conditions Tests

        [Test]
        public void CanUnlock_VariableConditionSatisfied_ShouldReturnTrue()
        {
            // REQ-090: Variable conditions must all be satisfied
            var graph = CreateGraphWithVariableCondition();

            // Complete source node
            graph.GetNodeById("node-1").Unlock();
            graph.GetNodeById("node-1").Complete();

            // Set variable to satisfy condition
            _variableStore.Initialize(graph.variables);
            _variableStore.Set("hasKey", true);

            var targetNode = graph.GetNodeById("node-2");
            bool result = _evaluator.CanUnlock(targetNode, graph);

            Assert.IsTrue(result);
        }

        [Test]
        public void CanUnlock_VariableConditionNotSatisfied_ShouldReturnFalse()
        {
            // REQ-090: Variable conditions must all be satisfied
            var graph = CreateGraphWithVariableCondition();

            // Complete source node
            graph.GetNodeById("node-1").Unlock();
            graph.GetNodeById("node-1").Complete();

            // Variable remains at default (false)
            _variableStore.Initialize(graph.variables);

            var targetNode = graph.GetNodeById("node-2");
            bool result = _evaluator.CanUnlock(targetNode, graph);

            Assert.IsFalse(result);
        }

        #endregion

        #region Helper Methods

        private ForkTrackGraph CreateSimpleGraph()
        {
            var graph = new ForkTrackGraph();
            graph.nodes.Add(new ForkTrackNode { id = "node-1" });
            graph.nodes.Add(new ForkTrackNode { id = "node-2" });
            graph.edges.Add(new ForkTrackEdge("edge-1", "node-1", "node-2", RequiredState.OnComplete, DependencyCondition.AND));
            graph.Initialize();
            return graph;
        }

        private ForkTrackGraph CreateGraphWithAndDependencies()
        {
            var graph = new ForkTrackGraph();
            graph.nodes.Add(new ForkTrackNode { id = "node-1" });
            graph.nodes.Add(new ForkTrackNode { id = "node-2" });
            graph.nodes.Add(new ForkTrackNode { id = "node-3" });
            graph.edges.Add(new ForkTrackEdge("edge-1", "node-1", "node-3", RequiredState.OnComplete, DependencyCondition.AND));
            graph.edges.Add(new ForkTrackEdge("edge-2", "node-2", "node-3", RequiredState.OnComplete, DependencyCondition.AND));
            graph.Initialize();
            return graph;
        }

        private ForkTrackGraph CreateGraphWithOrDependencies()
        {
            var graph = new ForkTrackGraph();
            graph.nodes.Add(new ForkTrackNode { id = "node-1" });
            graph.nodes.Add(new ForkTrackNode { id = "node-2" });
            graph.nodes.Add(new ForkTrackNode { id = "node-3" });
            graph.edges.Add(new ForkTrackEdge("edge-1", "node-1", "node-3", RequiredState.OnComplete, DependencyCondition.OR));
            graph.edges.Add(new ForkTrackEdge("edge-2", "node-2", "node-3", RequiredState.OnComplete, DependencyCondition.OR));
            graph.Initialize();
            return graph;
        }

        private ForkTrackGraph CreateGraphWithNotDependency()
        {
            var graph = new ForkTrackGraph();
            graph.nodes.Add(new ForkTrackNode { id = "node-1" });
            graph.nodes.Add(new ForkTrackNode { id = "node-2" });
            graph.edges.Add(new ForkTrackEdge("edge-1", "node-1", "node-2", RequiredState.OnComplete, DependencyCondition.NOT));
            graph.Initialize();
            return graph;
        }

        private ForkTrackGraph CreateGraphWithOnUnlockDependency()
        {
            var graph = new ForkTrackGraph();
            graph.nodes.Add(new ForkTrackNode { id = "node-1" });
            graph.nodes.Add(new ForkTrackNode { id = "node-2" });
            graph.edges.Add(new ForkTrackEdge("edge-1", "node-1", "node-2", RequiredState.OnUnlock, DependencyCondition.AND));
            graph.Initialize();
            return graph;
        }

        private ForkTrackGraph CreateGraphWithVariableCondition()
        {
            var graph = new ForkTrackGraph();
            graph.nodes.Add(new ForkTrackNode { id = "node-1" });
            graph.nodes.Add(new ForkTrackNode { id = "node-2" });

            graph.variables.Add(new ForkTrackVariable("var-1", "hasKey", VariableType.BOOLEAN, false));

            var edge = new ForkTrackEdge("edge-1", "node-1", "node-2", RequiredState.OnComplete, DependencyCondition.AND);
            edge.variableConditions.Add(new VariableCondition("vc-1", "var-1", VariableOperator.EQ, true));
            graph.edges.Add(edge);

            graph.Initialize();
            return graph;
        }

        #endregion
    }
}
