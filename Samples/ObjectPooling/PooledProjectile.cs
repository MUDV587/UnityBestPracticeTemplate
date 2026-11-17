using UnityEngine;

namespace UnityBestPractices.Samples
{
    /// <summary>
    /// A pooled projectile that returns itself to the pool after a delay.
    /// Uses Unity 6's Awaitable instead of coroutines.
    /// </summary>
    public class PooledProjectile : MonoBehaviour
    {
        #region Fields

        [SerializeField] private float _speed = 10f;
        [SerializeField] private float _lifetime = 3f;

        private PooledSpawner _spawner;

        #endregion

        #region Public Methods

        public void Initialize(PooledSpawner spawner)
        {
            _spawner = spawner;
            ReturnAfterDelay();
        }

        #endregion

        #region MonoBehaviour Methods

        private void Update()
        {
            transform.Translate(Vector3.forward * (_speed * Time.deltaTime));
        }

        #endregion

        #region Private Methods

        private async void ReturnAfterDelay()
        {
            await Awaitable.WaitForSecondsAsync(_lifetime);

            if (this == null || _spawner == null)
            {
                return;
            }

            _spawner.ReturnToPool(this);
        }

        #endregion
    }
}
