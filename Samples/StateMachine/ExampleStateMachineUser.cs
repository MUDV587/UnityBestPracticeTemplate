using UnityEngine;

namespace UnityBestPractices.Samples
{
    /// <summary>
    /// Example MonoBehaviour demonstrating how to wire two states into the StateMachine.
    /// Press Space to toggle between Idle and Moving states.
    /// </summary>
    public class ExampleStateMachineUser : MonoBehaviour
    {
        #region Fields

        private StateMachine _stateMachine;
        private IdleState _idleState;
        private MovingState _movingState;

        #endregion

        #region MonoBehaviour Methods

        private void Awake()
        {
            _stateMachine = new StateMachine();
            _idleState = new IdleState(this);
            _movingState = new MovingState(this);
        }

        private void Start()
        {
            _stateMachine.ChangeState(_idleState);
        }

        private void Update()
        {
            _stateMachine.Update();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                ToggleState();
            }
        }

        #endregion

        #region Private Methods

        private void ToggleState()
        {
            if (_stateMachine.CurrentState == _idleState)
            {
                _stateMachine.ChangeState(_movingState);
            }
            else
            {
                _stateMachine.ChangeState(_idleState);
            }
        }

        #endregion

        #region Nested State Classes

        private class IdleState : IState
        {
            private readonly ExampleStateMachineUser _owner;

            public IdleState(ExampleStateMachineUser owner) => _owner = owner;

            public void OnEnter() => Debug.Log("Entered Idle");
            public void OnUpdate() { }
            public void OnExit() => Debug.Log("Exited Idle");
        }

        private class MovingState : IState
        {
            private readonly ExampleStateMachineUser _owner;

            public MovingState(ExampleStateMachineUser owner) => _owner = owner;

            public void OnEnter() => Debug.Log("Entered Moving");

            public void OnUpdate()
            {
                _owner.transform.Translate(Vector3.forward * Time.deltaTime);
            }

            public void OnExit() => Debug.Log("Exited Moving");
        }

        #endregion
    }
}
