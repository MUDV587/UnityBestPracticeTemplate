using System;
using UnityEngine;

namespace UnityBestPractices.Samples
{
    /// <summary>
    /// A ScriptableObject-based event channel. Raise it from anywhere;
    /// GameEventListeners subscribe and respond without direct references.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "Best Practices/Game Event")]
    public class GameEvent : ScriptableObject
    {
        #region Events

        public static event Action<GameEvent> OnAnyEventRaised;
        private event Action _onRaised;

        #endregion

        #region Public Methods

        public void Subscribe(Action listener)
        {
            _onRaised += listener;
        }

        public void Unsubscribe(Action listener)
        {
            _onRaised -= listener;
        }

        public void Raise()
        {
            _onRaised?.Invoke();
            OnAnyEventRaised?.Invoke(this);
        }

        #endregion
    }
}
