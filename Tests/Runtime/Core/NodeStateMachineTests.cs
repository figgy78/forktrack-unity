using NUnit.Framework;
using ForkTrack.Core;
using System;

namespace ForkTrack.Tests.Core
{
    /// <summary>
    /// Tests for ForkTrackNode state machine transitions
    /// REQ-020 through REQ-026
    /// </summary>
    [TestFixture]
    public class NodeStateMachineTests
    {
        private ForkTrackNode _node;

        [SetUp]
        public void SetUp()
        {
            _node = new ForkTrackNode
            {
                id = "test-node",
                objectId = "obj-1",
                actionId = "act-1"
            };
        }

        #region Initial State Tests

        [Test]
        public void NewNode_ShouldBeLocked()
        {
            // REQ-020: The system SHALL support three node states
            Assert.AreEqual(NodeState.Locked, _node.State);
            Assert.IsTrue(_node.IsLocked);
            Assert.IsFalse(_node.IsUnlocked);
            Assert.IsFalse(_node.IsCompleted);
        }

        #endregion

        #region Unlock Tests

        [Test]
        public void Unlock_FromLocked_ShouldTransitionToUnlocked()
        {
            // REQ-021: WHEN a Locked node is unlocked THEN the node state SHALL transition to Unlocked
            _node.Unlock();

            Assert.AreEqual(NodeState.Unlocked, _node.State);
            Assert.IsFalse(_node.IsLocked);
            Assert.IsTrue(_node.IsUnlocked);
            Assert.IsFalse(_node.IsCompleted);
        }

        [Test]
        public void Unlock_FromUnlocked_ShouldRemainUnlocked_Idempotent()
        {
            // Idempotent behavior
            _node.Unlock();
            _node.Unlock();

            Assert.AreEqual(NodeState.Unlocked, _node.State);
        }

        [Test]
        public void Unlock_FromCompleted_ShouldRemainCompleted()
        {
            // Cannot unlock a completed node
            _node.Unlock();
            _node.Complete();

            _node.Unlock();

            Assert.AreEqual(NodeState.Completed, _node.State);
        }

        #endregion

        #region Complete Tests

        [Test]
        public void Complete_FromUnlocked_ShouldTransitionToCompleted()
        {
            // REQ-022: WHEN an Unlocked node is completed THEN the node state SHALL transition to Completed
            _node.Unlock();
            _node.Complete();

            Assert.AreEqual(NodeState.Completed, _node.State);
            Assert.IsFalse(_node.IsLocked);
            Assert.IsFalse(_node.IsUnlocked);
            Assert.IsTrue(_node.IsCompleted);
        }

        [Test]
        public void Complete_FromCompleted_ShouldRemainCompleted_Idempotent()
        {
            // REQ-023: WHEN a Completed node is completed again THEN the state SHALL remain Completed
            _node.Unlock();
            _node.Complete();
            _node.Complete();

            Assert.AreEqual(NodeState.Completed, _node.State);
        }

        [Test]
        public void Complete_FromLocked_ShouldThrowInvalidOperationException()
        {
            // REQ-024: WHEN a Locked node is completed THEN the system SHALL throw InvalidOperationException
            Assert.Throws<InvalidOperationException>(() => _node.Complete());
        }

        #endregion

        #region AutoComplete Tests

        [Test]
        public void Unlock_WithAutoCompleteOnUnlock_ShouldTransitionToCompleted()
        {
            // REQ-025: IF autoCompleteOnUnlock is true THEN the system SHALL transition directly to Completed
            _node.autoCompleteOnUnlock = true;

            _node.Unlock();

            Assert.AreEqual(NodeState.Completed, _node.State);
        }

        [Test]
        public void Unlock_WithoutAutoCompleteOnUnlock_ShouldStayUnlocked()
        {
            _node.autoCompleteOnUnlock = false;

            _node.Unlock();

            Assert.AreEqual(NodeState.Unlocked, _node.State);
        }

        #endregion

        #region Reset Tests

        [Test]
        public void Reset_FromUnlocked_ShouldTransitionToLocked()
        {
            _node.Unlock();
            _node.Reset();

            Assert.AreEqual(NodeState.Locked, _node.State);
        }

        [Test]
        public void Reset_FromCompleted_ShouldTransitionToLocked()
        {
            _node.Unlock();
            _node.Complete();
            _node.Reset();

            Assert.AreEqual(NodeState.Locked, _node.State);
        }

        [Test]
        public void Reset_FromLocked_ShouldRemainLocked_Idempotent()
        {
            _node.Reset();

            Assert.AreEqual(NodeState.Locked, _node.State);
        }

        #endregion

        #region GetDisplayName Tests

        [Test]
        public void GetDisplayName_WithObjectAndAction_ShouldReturnCombination()
        {
            _node.objectName = "Fred";
            _node.actionName = "Interact";
            _node.title = null;

            string displayName = _node.GetDisplayName();

            Assert.AreEqual("Fred - Interact", displayName);
        }

        [Test]
        public void GetDisplayName_WithObjectActionAndTitle_ShouldIncludeTitle()
        {
            _node.objectName = "Fred";
            _node.actionName = "Interact";
            _node.title = "First Meeting";

            string displayName = _node.GetDisplayName();

            Assert.AreEqual("Fred - Interact: First Meeting", displayName);
        }

        [Test]
        public void GetDisplayName_WithOnlyTitle_ShouldReturnTitle()
        {
            _node.objectName = null;
            _node.actionName = null;
            _node.title = "Legacy Title";

            string displayName = _node.GetDisplayName();

            Assert.AreEqual("Legacy Title", displayName);
        }

        [Test]
        public void GetDisplayName_WithNothing_ShouldReturnId()
        {
            _node.objectName = null;
            _node.actionName = null;
            _node.title = null;

            string displayName = _node.GetDisplayName();

            Assert.AreEqual("test-node", displayName);
        }

        #endregion
    }
}
