using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Scripts.Core
{
    public class EnemySpawner : MonoBehaviour
    {
        public float spawnRadius;
        
        public List<MovableEntitiy> enemyPrefabs;

        public float stepSize = 1;

        public float offset = 0.5f;

        public int spawnEnemyInterval = 3;
        // Let's keep this to 1 for now
        public int numberOfEnemiesToSpawnPerInterval = 1;
        
        private int _currentSpawnInterval;


        private readonly List<Enemy> _aliveEnemies = new List<Enemy>();
        private readonly List<Ally> _aliveAllies = new List<Ally>();

        public static EnemySpawner Instance;
        
        void Awake()
        {
            Instance = this;
        }

        void SpawnEnemy()
        {
            var enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
            
            float angle = Random.Range(0f, Mathf.PI * 2);

            // 2. Calculate the x and y positions
            float x = Mathf.Cos(angle) * spawnRadius;
            float y = Mathf.Sin(angle) * spawnRadius;

            // 3. Create the position vector (relative to the spawner's center)
            Vector3 spawnPosition = new Vector3(x, y, 0) + transform.position;
            
            var newEntity = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            
            newEntity.Init(offset, stepSize);
            
            Vector2 direction = (Vector2)transform.position - (Vector2)spawnPosition;
            
            newEntity.transform.up = direction;

            newEntity.OnDestroyed += EnemyDestroyed;
            if (newEntity is Enemy enemy)
            {
                _aliveEnemies.Add(enemy);
            }
            else if (newEntity is Ally ally)
            {
                _aliveAllies.Add(ally);
            }
        }

        private void EnemyDestroyed(MovableEntitiy entity)
        {

            GameManager.Instance.UpdateCombo(entity.Detected);
            if (entity is Enemy enemy && _aliveEnemies.Contains(enemy))
            {
                _aliveEnemies.Remove(enemy);
                Debug.Log("enemy Destroyed"+ enemy.name);
            }
            else if (entity is Ally ally && _aliveAllies.Contains(ally))
            {
                _aliveAllies.Remove(ally);
                Debug.Log("ally Destroyed"+ ally.name);
            }
        }


        public void UpdateEnemies()
        {
            for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
            {
                _aliveEnemies[i].UpdatePosition();
            }
            
            for (int i = _aliveAllies.Count - 1; i >= 0; i--)
            {
                _aliveAllies[i].UpdatePosition();
            }
            
            if (_currentSpawnInterval < 1)
            {
                _currentSpawnInterval = spawnEnemyInterval;
                SpawnEnemies();
            }
            _currentSpawnInterval--;
        }

        private void SpawnEnemies()
        {
            for (int i = 0; i < numberOfEnemiesToSpawnPerInterval; i++)
            {
                SpawnEnemy();
            }
        }
    }
}
