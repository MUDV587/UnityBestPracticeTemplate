using System.Collections;
using UnityEngine;

namespace UnityBestPractices.Samples
{
    /// <summary>
    /// A pooled projectile that returns itself to the pool after a delay.
    /// Uses a coroutine so it works on Unity 2022.3+ (there is no Awaitable on 2022.3).
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
            StartCoroutine(ReturnAfterDelay());
        }

        #endregion

        #region MonoBehaviour Methods

        private void Update()
        {
            transform.Translate(Vector3.forward * (_speed * Time.deltaTime));
        }

        #endregion

        #region Private Methods

        private IEnumerator ReturnAfterDelay()
        {
            yield return new WaitForSeconds(_lifetime);

            if (this == null || _spawner == null)
            {
                yield break;
            }

            _spawner.ReturnToPool(this);
        }

        #endregion
    }
}
