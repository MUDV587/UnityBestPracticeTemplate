using System.Collections;
using UnityEngine;

namespace UnityBestPractices.Samples
{
    /// <summary>
    /// A pooled projectile that returns itself to the pool after a delay.
    /// Uses a coroutine so it works on Unity 2022.3+.
    /// </summary>
    public class PooledProjectile : MonoBehaviour
    {
        #region Fields

        [SerializeField] private float m_Speed = 10f;
        [SerializeField] private float m_Lifetime = 3f;

        private PooledSpawner m_Spawner;

        #endregion

        #region Public Methods

        public void Initialize(PooledSpawner spawner)
        {
            m_Spawner = spawner;
            StartCoroutine(ReturnAfterDelay());
        }

        #endregion

        #region MonoBehaviour Methods

        private void Update()
        {
            transform.Translate(Vector3.forward * (m_Speed * Time.deltaTime));
        }

        #endregion

        #region Private Methods

        private IEnumerator ReturnAfterDelay()
        {
            yield return new WaitForSeconds(m_Lifetime);

            if (this == null || m_Spawner == null)
            {
                yield break;
            }

            m_Spawner.ReturnToPool(this);
        }

        #endregion
    }
}
