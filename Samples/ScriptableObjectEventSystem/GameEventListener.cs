using UnityEngine;
using UnityEngine.Events;

namespace UnityBestPractices.Samples
{
    /// <summary>
    /// Listens to a GameEvent ScriptableObject and invokes a UnityEvent response.
    /// Attach to any GameObject. Assign the GameEvent asset and the Response in the Inspector.
    /// </summary>
    public class GameEventListener : MonoBehaviour
    {
        #region Fields

        [SerializeField] private GameEvent _event;
        [SerializeField] private UnityEvent _response;

        #endregion

        #region MonoBehaviour Methods

        private void OnEnable()
        {
            if (_event != null)
            {
                _event.Subscribe(HandleEventRaised);
            }
        }

        private void OnDisable()
        {
            if (_event != null)
            {
                _event.Unsubscribe(HandleEventRaised);
            }
        }

        #endregion

        #region Private Methods

        private void HandleEventRaised()
        {
            _response?.Invoke();
        }

        #endregion
    }
}
