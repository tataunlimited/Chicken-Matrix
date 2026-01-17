using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Scripts.Core
{
    public class EnemySpawner : MonoBehaviour
    {
        public float spawnRadius;

        [Header("Entity Prefabs")]
        public MovableEntitiy friendlyPrefab;
        public MovableEntitiy enemyPrefab;

        public float stepSize = 1;

        public float offset = 0.5f;

        [Header("Spin Attack Settings")]
        [Tooltip("Maximum step value - entities pushed beyond this are destroyed")]
        public int maxStep = 4;
        [Tooltip("Sorting order for revealed entities")]
        public int revealSortingOrder = 1200;

        private int _currentSpawnInterval;
        private int _spawnCycleCounter;
        private int _entityPatternIndex;


        private readonly List<Enemy> _aliveEnemies = new List<Enemy>();
        private readonly List<Ally> _aliveAllies = new List<Ally>();

        public static EnemySpawner Instance;
        
        void Awake()
        {
            Instance = this;
        }

        void SpawnEntity(MovableEntitiy prefab)
        {
            float angle = Random.Range(0f, Mathf.PI * 2);

            float x = Mathf.Cos(angle) * spawnRadius;
            float y = Mathf.Sin(angle) * spawnRadius;

            Vector3 spawnPosition = new Vector3(x, y, 0) + transform.position;

            var newEntity = Instantiate(prefab, spawnPosition, Quaternion.identity);

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

        private MovableEntitiy GetEntityPrefabForCombo(int combo)
        {
            // Combo 1-10: Friendly only
            if (combo <= 10)
                return friendlyPrefab;

            // Combo 11-20: Enemy only
            if (combo <= 20)
                return enemyPrefab;

            // Combo 21-30: Alternate Friend - Enemy
            if (combo <= 30)
            {
                bool isFriendly = (_entityPatternIndex % 2) == 0;
                return isFriendly ? friendlyPrefab : enemyPrefab;
            }

            // Combo 31-40: Friend - Friend - Enemy pattern
            if (combo <= 40)
            {
                int patternPos = _entityPatternIndex % 3;
                return patternPos < 2 ? friendlyPrefab : enemyPrefab;
            }

            // Combo 41-50: Enemy - Enemy - Friend pattern
            if (combo <= 50)
            {
                int patternPos = _entityPatternIndex % 3;
                return patternPos < 2 ? enemyPrefab : friendlyPrefab;
            }

            // Combo 51-60: Friend - Friend - Enemy - Enemy pattern
            if (combo <= 60)
            {
                int patternPos = _entityPatternIndex % 4;
                return patternPos < 2 ? friendlyPrefab : enemyPrefab;
            }

            // Combo 61-70: F-F-F-E-E-F-F-E-E-E-F-F-E-E-F-F-F (17-element pattern)
            if (combo <= 70)
            {
                int[] pattern = { 0, 0, 0, 1, 1, 0, 0, 1, 1, 1, 0, 0, 1, 1, 0, 0, 0 };
                int patternPos = _entityPatternIndex % pattern.Length;
                return pattern[patternPos] == 0 ? friendlyPrefab : enemyPrefab;
            }

            // Combo 71-100: Random
            return Random.value < 0.5f ? friendlyPrefab : enemyPrefab;
        }

        private int GetSpawnIntervalForCombo(int combo)
        {
            if (combo <= 20) return 3;
            if (combo <= 70) return 2;
            return 2; // 71-100 base is also 2
        }

        private bool ShouldSpawnThisInterval(int combo)
        {
            // Combo 1-20: spawn every 3 intervals (handled by interval counter)
            if (combo <= 20)
                return true;

            // Combo 21-40: 2-1 pattern (spawn, wait, spawn, wait, wait)
            // Cycle of 5: spawn at 0, spawn at 2
            if (combo <= 40)
            {
                int cyclePos = _spawnCycleCounter % 5;
                return cyclePos == 0 || cyclePos == 2;
            }

            // Combo 41-70: spawn every 2 intervals (standard)
            if (combo <= 70)
                return true;

            // Combo 71-100: 2+1 pattern (spawn, wait, spawn, spawn, wait, spawn, wait, spawn, spawn...)
            // Every other spawn, spawn an extra one on the next interval
            // Cycle of 4: spawn at 0, spawn at 2, spawn at 3
            int cycle = _spawnCycleCounter % 4;
            return cycle == 0 || cycle == 2 || cycle == 3;
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

        public void ClearAllEntities()
        {
            // Destroy all alive enemies
            for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
            {
                if (_aliveEnemies[i] != null)
                {
                    _aliveEnemies[i].OnDestroyed -= EnemyDestroyed;
                    Destroy(_aliveEnemies[i].gameObject);
                }
            }
            _aliveEnemies.Clear();

            // Destroy all alive allies
            for (int i = _aliveAllies.Count - 1; i >= 0; i--)
            {
                if (_aliveAllies[i] != null)
                {
                    _aliveAllies[i].OnDestroyed -= EnemyDestroyed;
                    Destroy(_aliveAllies[i].gameObject);
                }
            }
            _aliveAllies.Clear();

            ResetProgression();
        }

        /// <summary>
        /// Destroys all entities with destruction particles but without updating combo.
        /// Used for rank-up screen clear effect.
        /// </summary>
        public void DestroyAllEntitiesWithParticles()
        {
            // Destroy all alive enemies with particles
            for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
            {
                if (_aliveEnemies[i] != null)
                {
                    _aliveEnemies[i].OnDestroyed -= EnemyDestroyed;
                    _aliveEnemies[i].Destroy(false);
                }
            }
            _aliveEnemies.Clear();

            // Destroy all alive allies with particles
            for (int i = _aliveAllies.Count - 1; i >= 0; i--)
            {
                if (_aliveAllies[i] != null)
                {
                    _aliveAllies[i].OnDestroyed -= EnemyDestroyed;
                    _aliveAllies[i].Destroy(false);
                }
            }
            _aliveAllies.Clear();
        }

        /// <summary>
        /// Push all current entities back by one step and reveal them permanently.
        /// Entities pushed beyond maxStep are destroyed.
        /// </summary>
        public void PushBackAndRevealAllEntities()
        {
            Debug.Log($"PushBackAndRevealAllEntities called. Enemies: {_aliveEnemies.Count}, Allies: {_aliveAllies.Count}");

            // Copy lists to avoid modification during iteration
            var enemiesToProcess = new List<Enemy>(_aliveEnemies);
            var alliesToProcess = new List<Ally>(_aliveAllies);

            // Push back and reveal all enemies
            foreach (var enemy in enemiesToProcess)
            {
                if (enemy != null)
                {
                    Debug.Log($"Processing enemy: {enemy.name}, step: {enemy.step}");
                    enemy.RevealPermanently(revealSortingOrder);
                    enemy.PushBack(maxStep);
                }
            }

            // Push back and reveal all allies
            foreach (var ally in alliesToProcess)
            {
                if (ally != null)
                {
                    Debug.Log($"Processing ally: {ally.name}, step: {ally.step}");
                    ally.RevealPermanently(revealSortingOrder);
                    ally.PushBack(maxStep);
                }
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

            int combo = GameManager.Instance.combo;
            int spawnInterval = GetSpawnIntervalForCombo(combo);

            if (_currentSpawnInterval < 1)
            {
                _currentSpawnInterval = spawnInterval;

                if (ShouldSpawnThisInterval(combo))
                {
                    var prefab = GetEntityPrefabForCombo(combo);
                    SpawnEntity(prefab);
                    _entityPatternIndex++;
                }

                _spawnCycleCounter++;
            }
            _currentSpawnInterval--;
        }

        public void ResetProgression()
        {
            _spawnCycleCounter = 0;
            _entityPatternIndex = 0;
            _currentSpawnInterval = 0;
        }
    }
}
