using NUnit.Framework;
using ForkTrack.Core;
using ForkTrack.Internal;
using System.Collections.Generic;

namespace ForkTrack.Tests.Variables
{
    /// <summary>
    /// Tests for VariableStore condition evaluation
    /// REQ-090 through REQ-094
    /// </summary>
    [TestFixture]
    public class VariableConditionTests
    {
        private VariableStore _store;
        private List<ForkTrackVariable> _variables;

        [SetUp]
        public void SetUp()
        {
            _store = new VariableStore();
            _variables = new List<ForkTrackVariable>
            {
                new ForkTrackVariable("var-coins", "coins", VariableType.NUMBER, 50f),
                new ForkTrackVariable("var-haskey", "hasKey", VariableType.BOOLEAN, false)
            };
            _store.Initialize(_variables);
        }

        #region NUMBER Equal Tests

        [Test]
        public void EvaluateCondition_NumberEqual_Match_ShouldReturnTrue()
        {
            // REQ-091: == operator for NUMBER
            var condition = new VariableCondition("vc-1", "var-coins", VariableOperator.EQ, 50f);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsTrue(result);
        }

        [Test]
        public void EvaluateCondition_NumberEqual_NoMatch_ShouldReturnFalse()
        {
            var condition = new VariableCondition("vc-1", "var-coins", VariableOperator.EQ, 100f);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsFalse(result);
        }

        #endregion

        #region NUMBER NotEqual Tests

        [Test]
        public void EvaluateCondition_NumberNotEqual_Different_ShouldReturnTrue()
        {
            // REQ-092: != operator for NUMBER
            var condition = new VariableCondition("vc-1", "var-coins", VariableOperator.NE, 100f);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsTrue(result);
        }

        [Test]
        public void EvaluateCondition_NumberNotEqual_Same_ShouldReturnFalse()
        {
            var condition = new VariableCondition("vc-1", "var-coins", VariableOperator.NE, 50f);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsFalse(result);
        }

        #endregion

        #region NUMBER Comparison Tests

        [Test]
        public void EvaluateCondition_GreaterThan_True()
        {
            // REQ-093: > operator for NUMBER
            var condition = new VariableCondition("vc-1", "var-coins", VariableOperator.GT, 25f);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsTrue(result);
        }

        [Test]
        public void EvaluateCondition_GreaterThan_False()
        {
            var condition = new VariableCondition("vc-1", "var-coins", VariableOperator.GT, 75f);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsFalse(result);
        }

        [Test]
        public void EvaluateCondition_LessThan_True()
        {
            // REQ-093: < operator for NUMBER
            var condition = new VariableCondition("vc-1", "var-coins", VariableOperator.LT, 100f);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsTrue(result);
        }

        [Test]
        public void EvaluateCondition_LessThan_False()
        {
            var condition = new VariableCondition("vc-1", "var-coins", VariableOperator.LT, 25f);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsFalse(result);
        }

        [Test]
        public void EvaluateCondition_GreaterOrEqual_Equal()
        {
            // REQ-093: >= operator for NUMBER
            var condition = new VariableCondition("vc-1", "var-coins", VariableOperator.GTE, 50f);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsTrue(result);
        }

        [Test]
        public void EvaluateCondition_GreaterOrEqual_Greater()
        {
            var condition = new VariableCondition("vc-1", "var-coins", VariableOperator.GTE, 25f);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsTrue(result);
        }

        [Test]
        public void EvaluateCondition_GreaterOrEqual_Less()
        {
            var condition = new VariableCondition("vc-1", "var-coins", VariableOperator.GTE, 75f);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsFalse(result);
        }

        [Test]
        public void EvaluateCondition_LessOrEqual_Equal()
        {
            // REQ-093: <= operator for NUMBER
            var condition = new VariableCondition("vc-1", "var-coins", VariableOperator.LTE, 50f);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsTrue(result);
        }

        [Test]
        public void EvaluateCondition_LessOrEqual_Less()
        {
            var condition = new VariableCondition("vc-1", "var-coins", VariableOperator.LTE, 100f);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsTrue(result);
        }

        [Test]
        public void EvaluateCondition_LessOrEqual_Greater()
        {
            var condition = new VariableCondition("vc-1", "var-coins", VariableOperator.LTE, 25f);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsFalse(result);
        }

        #endregion

        #region BOOLEAN Tests

        [Test]
        public void EvaluateCondition_BooleanEqual_Match()
        {
            // REQ-091: == operator for BOOLEAN
            var condition = new VariableCondition("vc-1", "var-haskey", VariableOperator.EQ, false);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsTrue(result);
        }

        [Test]
        public void EvaluateCondition_BooleanEqual_NoMatch()
        {
            var condition = new VariableCondition("vc-1", "var-haskey", VariableOperator.EQ, true);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsFalse(result);
        }

        [Test]
        public void EvaluateCondition_BooleanNotEqual_Different()
        {
            // REQ-092: != operator for BOOLEAN
            var condition = new VariableCondition("vc-1", "var-haskey", VariableOperator.NE, true);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsTrue(result);
        }

        [Test]
        public void EvaluateCondition_BooleanNotEqual_Same()
        {
            var condition = new VariableCondition("vc-1", "var-haskey", VariableOperator.NE, false);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsFalse(result);
        }

        [Test]
        public void EvaluateCondition_ComparisonOnBoolean_ShouldReturnFalse()
        {
            // REQ-094: Comparison operator on BOOLEAN returns false
            var condition = new VariableCondition("vc-1", "var-haskey", VariableOperator.GT, true);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsFalse(result);
        }

        #endregion

        #region Multiple Conditions Tests

        [Test]
        public void EvaluateConditions_AllSatisfied_ShouldReturnTrue()
        {
            // REQ-090: All conditions must be satisfied
            _store.Set("coins", 100f);
            _store.Set("hasKey", true);

            var conditions = new List<VariableCondition>
            {
                new VariableCondition("vc-1", "var-coins", VariableOperator.GTE, 50f),
                new VariableCondition("vc-2", "var-haskey", VariableOperator.EQ, true)
            };

            bool result = _store.EvaluateConditions(conditions);

            Assert.IsTrue(result);
        }

        [Test]
        public void EvaluateConditions_OneFails_ShouldReturnFalse()
        {
            // REQ-090: All conditions must be satisfied
            _store.Set("coins", 100f);
            _store.Set("hasKey", false); // This will fail the second condition

            var conditions = new List<VariableCondition>
            {
                new VariableCondition("vc-1", "var-coins", VariableOperator.GTE, 50f),
                new VariableCondition("vc-2", "var-haskey", VariableOperator.EQ, true)
            };

            bool result = _store.EvaluateConditions(conditions);

            Assert.IsFalse(result);
        }

        [Test]
        public void EvaluateConditions_EmptyList_ShouldReturnTrue()
        {
            var conditions = new List<VariableCondition>();

            bool result = _store.EvaluateConditions(conditions);

            Assert.IsTrue(result);
        }

        [Test]
        public void EvaluateConditions_NullList_ShouldReturnTrue()
        {
            bool result = _store.EvaluateConditions(null);

            Assert.IsTrue(result);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void EvaluateCondition_UnknownVariable_ShouldReturnFalse()
        {
            var condition = new VariableCondition("vc-1", "var-unknown", VariableOperator.EQ, 0f);

            bool result = _store.EvaluateCondition(condition);

            Assert.IsFalse(result);
        }

        [Test]
        public void EvaluateCondition_NullCondition_ShouldReturnTrue()
        {
            bool result = _store.EvaluateCondition(null);

            Assert.IsTrue(result);
        }

        #endregion
    }
}
