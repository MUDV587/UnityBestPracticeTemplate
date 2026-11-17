using UnityEngine;
using UnityEngine.Pool;

namespace UnityBestPractices.Samples
{
    /// <summary>
    /// Spawns pooled projectiles on a timer using Unity's built-in ObjectPool.
    /// </summary>
    public class PooledSpawner : MonoBehaviour
    {
        #region Fields

        [SerializeField] private PooledProjectile _projectilePrefab;
        [SerializeField] private float _spawnInterval = 1f;
        [SerializeField] private int _defaultCapacity = 10;
        [SerializeField] private int _maxSize = 50;

        private ObjectPool<PooledProjectile> _pool;
        private float _timer;

        #endregion

        #region Properties

        public ObjectPool<PooledProjectile> Pool => _pool;

        #endregion

        #region MonoBehaviour Methods

        private void Awake()
        {
            _pool = new ObjectPool<PooledProjectile>(
                createFunc: CreateProjectile,
                actionOnGet: OnTakeFromPool,
                actionOnRelease: OnReturnedToPool,
                actionOnDestroy: OnDestroyProjectile,
                collectionCheck: false,
                defaultCapacity: _defaultCapacity,
                maxSize: _maxSize);
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            if (_timer >= _spawnInterval)
            {
                _timer = 0f;
                SpawnProjectile();
            }
        }

        #endregion

        #region Public Methods

        public void ReturnToPool(PooledProjectile projectile)
        {
            _pool.Release(projectile);
        }

        #endregion

        #region Private Methods

        private void SpawnProjectile()
        {
            var projectile = _pool.Get();
            projectile.transform.SetPositionAndRotation(transform.position, transform.rotation);
            projectile.Initialize(this);
        }

        private PooledProjectile CreateProjectile()
        {
            return Instantiate(_projectilePrefab);
        }

        private void OnTakeFromPool(PooledProjectile projectile)
        {
            projectile.gameObject.SetActive(true);
        }

        private void OnReturnedToPool(PooledProjectile projectile)
        {
            projectile.gameObject.SetActive(false);
        }

        private void OnDestroyProjectile(PooledProjectile projectile)
        {
            Destroy(projectile.gameObject);
        }

        #endregion
    }
}
