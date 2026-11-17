using UnityEngine;

namespace UnityBestPractices.Samples
{
    /// <summary>
    /// A generic state machine. Call ChangeState() to transition;
    /// call Update() from the owner's Update() method.
    /// </summary>
    public class StateMachine
    {
        #region Fields

        private IState _currentState;

        #endregion

        #region Properties

        public IState CurrentState => _currentState;

        #endregion

        #region Public Methods

        public void ChangeState(IState newState)
        {
            _currentState?.OnExit();
            _currentState = newState;
            _currentState?.OnEnter();
        }

        public void Update()
        {
            _currentState?.OnUpdate();
        }

        #endregion
    }
}
