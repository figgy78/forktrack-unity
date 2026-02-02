using NUnit.Framework;
using ForkTrack.Core;
using ForkTrack.Internal;
using System.Collections.Generic;

namespace ForkTrack.Tests.Variables
{
    /// <summary>
    /// Tests for VariableStore operations (SET, ADD, SUBTRACT, TOGGLE)
    /// REQ-060 through REQ-083
    /// </summary>
    [TestFixture]
    public class VariableOperationTests
    {
        private VariableStore _store;
        private List<ForkTrackVariable> _variables;

        [SetUp]
        public void SetUp()
        {
            _store = new VariableStore();
            _variables = new List<ForkTrackVariable>
            {
                new ForkTrackVariable("var-coins", "coins", VariableType.NUMBER, 0f),
                new ForkTrackVariable("var-health", "health", VariableType.NUMBER, 100f),
                new ForkTrackVariable("var-haskey", "hasKey", VariableType.BOOLEAN, false),
                new ForkTrackVariable("var-isdead", "isDead", VariableType.BOOLEAN, false)
            };
            _store.Initialize(_variables);
        }

        #region Initialize Tests

        [Test]
        public void Initialize_ShouldSetVariablesToDefaultValues()
        {
            // REQ-062: Initialize all variables to defaultValue
            Assert.AreEqual(0f, _store.Get<float>("coins"));
            Assert.AreEqual(100f, _store.Get<float>("health"));
            Assert.AreEqual(false, _store.Get<bool>("hasKey"));
            Assert.AreEqual(false, _store.Get<bool>("isDead"));
        }

        #endregion

        #region GET/SET Tests

        [Test]
        public void Get_NonExistentVariable_ShouldReturnDefaultAndLogWarning()
        {
            // REQ-103: Log warning and return default if not found
            float result = _store.Get<float>("nonexistent");
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Set_NumberVariable_ShouldUpdateValue()
        {
            // REQ-101: SetVariable<T>(name, value)
            _store.Set("coins", 50f);
            Assert.AreEqual(50f, _store.Get<float>("coins"));
        }

        [Test]
        public void Set_BooleanVariable_ShouldUpdateValue()
        {
            _store.Set("hasKey", true);
            Assert.IsTrue(_store.Get<bool>("hasKey"));
        }

        [Test]
        public void Set_ShouldFireOnVariableChangedEvent()
        {
            // REQ-063: Fire OnVariableChanged
            ForkTrackVariable changedVar = null;
            object oldVal = null;
            object newVal = null;

            _store.OnVariableChanged += (v, o, n) =>
            {
                changedVar = v;
                oldVal = o;
                newVal = n;
            };

            _store.Set("coins", 100f);

            Assert.IsNotNull(changedVar);
            Assert.AreEqual("coins", changedVar.name);
            Assert.AreEqual(0f, oldVal);
            Assert.AreEqual(100f, newVal);
        }

        #endregion

        #region SET Operation Tests

        [Test]
        public void ExecuteAction_Set_Number_ShouldSetValue()
        {
            // REQ-070: SET to specified value
            var action = new VariableAction("va-1", "var-coins", TriggerType.OnComplete, VariableOperation.SET, 50f);

            _store.ExecuteAction(action);

            Assert.AreEqual(50f, _store.Get<float>("coins"));
        }

        [Test]
        public void ExecuteAction_Set_Boolean_ShouldSetValue()
        {
            var action = new VariableAction("va-1", "var-haskey", TriggerType.OnComplete, VariableOperation.SET, true);

            _store.ExecuteAction(action);

            Assert.IsTrue(_store.Get<bool>("hasKey"));
        }

        #endregion

        #region ADD Operation Tests

        [Test]
        public void ExecuteAction_Add_ShouldAddToNumber()
        {
            // REQ-071: ADD value to NUMBER
            var action = new VariableAction("va-1", "var-coins", TriggerType.OnComplete, VariableOperation.ADD, 25f);

            _store.ExecuteAction(action);

            Assert.AreEqual(25f, _store.Get<float>("coins"));
        }

        [Test]
        public void ExecuteAction_Add_Multiple_ShouldAccumulate()
        {
            var action1 = new VariableAction("va-1", "var-coins", TriggerType.OnComplete, VariableOperation.ADD, 10f);
            var action2 = new VariableAction("va-2", "var-coins", TriggerType.OnComplete, VariableOperation.ADD, 15f);

            _store.ExecuteAction(action1);
            _store.ExecuteAction(action2);

            Assert.AreEqual(25f, _store.Get<float>("coins"));
        }

        [Test]
        public void ExecuteAction_Add_ToBoolean_ShouldLogWarning()
        {
            // REQ-074: Log warning if ADD applied to BOOLEAN
            var action = new VariableAction("va-1", "var-haskey", TriggerType.OnComplete, VariableOperation.ADD, 1f);

            _store.ExecuteAction(action);

            // Value should remain unchanged
            Assert.IsFalse(_store.Get<bool>("hasKey"));
        }

        #endregion

        #region SUBTRACT Operation Tests

        [Test]
        public void ExecuteAction_Subtract_ShouldSubtractFromNumber()
        {
            // REQ-072: SUBTRACT value from NUMBER
            _store.Set("health", 100f);
            var action = new VariableAction("va-1", "var-health", TriggerType.OnComplete, VariableOperation.SUBTRACT, 25f);

            _store.ExecuteAction(action);

            Assert.AreEqual(75f, _store.Get<float>("health"));
        }

        [Test]
        public void ExecuteAction_Subtract_CanGoNegative()
        {
            _store.Set("coins", 10f);
            var action = new VariableAction("va-1", "var-coins", TriggerType.OnComplete, VariableOperation.SUBTRACT, 25f);

            _store.ExecuteAction(action);

            Assert.AreEqual(-15f, _store.Get<float>("coins"));
        }

        [Test]
        public void ExecuteAction_Subtract_ToBoolean_ShouldLogWarning()
        {
            // REQ-074: Log warning if SUBTRACT applied to BOOLEAN
            var action = new VariableAction("va-1", "var-haskey", TriggerType.OnComplete, VariableOperation.SUBTRACT, 1f);

            _store.ExecuteAction(action);

            Assert.IsFalse(_store.Get<bool>("hasKey"));
        }

        #endregion

        #region TOGGLE Operation Tests

        [Test]
        public void ExecuteAction_Toggle_ShouldFlipBoolean()
        {
            // REQ-073: TOGGLE flips BOOLEAN
            var action = new VariableAction("va-1", "var-haskey", TriggerType.OnComplete, VariableOperation.TOGGLE, null);

            _store.ExecuteAction(action);

            Assert.IsTrue(_store.Get<bool>("hasKey"));

            _store.ExecuteAction(action);

            Assert.IsFalse(_store.Get<bool>("hasKey"));
        }

        [Test]
        public void ExecuteAction_Toggle_OnNumber_ShouldLogWarning()
        {
            // REQ-075: Log warning if TOGGLE applied to NUMBER
            _store.Set("coins", 50f);
            var action = new VariableAction("va-1", "var-coins", TriggerType.OnComplete, VariableOperation.TOGGLE, null);

            _store.ExecuteAction(action);

            // Value should remain unchanged
            Assert.AreEqual(50f, _store.Get<float>("coins"));
        }

        #endregion

        #region ExecuteActions Tests

        [Test]
        public void ExecuteActions_ShouldExecuteInOrder()
        {
            // REQ-083: Execute in array order
            var actions = new List<VariableAction>
            {
                new VariableAction("va-1", "var-coins", TriggerType.OnComplete, VariableOperation.SET, 100f),
                new VariableAction("va-2", "var-coins", TriggerType.OnComplete, VariableOperation.SUBTRACT, 30f),
                new VariableAction("va-3", "var-coins", TriggerType.OnComplete, VariableOperation.ADD, 10f)
            };

            _store.ExecuteActions(actions);

            // 100 - 30 + 10 = 80
            Assert.AreEqual(80f, _store.Get<float>("coins"));
        }

        #endregion

        #region Reset Tests

        [Test]
        public void Reset_ShouldRestoreDefaultValues()
        {
            _store.Set("coins", 999f);
            _store.Set("hasKey", true);

            _store.Reset();

            Assert.AreEqual(0f, _store.Get<float>("coins"));
            Assert.IsFalse(_store.Get<bool>("hasKey"));
        }

        #endregion

        #region Export/Import Tests

        [Test]
        public void Export_ShouldReturnCurrentValues()
        {
            _store.Set("coins", 50f);
            _store.Set("hasKey", true);

            var exported = _store.Export();

            Assert.AreEqual(50f, exported["var-coins"]);
            Assert.AreEqual(true, exported["var-haskey"]);
        }

        [Test]
        public void Import_ShouldRestoreValues()
        {
            var values = new Dictionary<string, object>
            {
                { "var-coins", 75f },
                { "var-haskey", true }
            };

            _store.Import(values);

            Assert.AreEqual(75f, _store.Get<float>("coins"));
            Assert.IsTrue(_store.Get<bool>("hasKey"));
        }

        #endregion
    }
}
