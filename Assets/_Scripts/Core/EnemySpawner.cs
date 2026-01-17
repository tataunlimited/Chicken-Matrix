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
        public MovableEntitiy neutralPrefab;

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
        private readonly List<Neutral> _aliveNeutrals = new List<Neutral>();

        public static EnemySpawner Instance;
        
        void Awake()
        {
            Instance = this;
        }

        float SpawnEntityAtAngle(MovableEntitiy prefab, float angle)
        {
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
            else if (newEntity is Neutral neutral)
            {
                _aliveNeutrals.Add(neutral);
            }

            return angle;
        }

        private MovableEntitiy GetEntityPrefabForCombo(int combo, out int count)
        {
            count = 1;
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

            // Combo 51-60: Neutral only, 2 per pulse
            if (combo <= 60)
            {
                count = 2;
                return neutralPrefab;
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

        /// <summary>
        /// Returns the chance (0-1) to spawn a neutral alongside regular entities.
        /// 0% for combo 1-10, 10% for 11-20, 20% for 21-30, 30% for 31-40, 40% for 41-50.
        /// Returns -1 for combo 51-60 (neutral-only phase, handled separately).
        /// </summary>
        private float GetNeutralSpawnChance(int combo)
        {
            if (combo <= 10) return 0f;
            if (combo <= 20) return 0.05f;
            if (combo <= 30) return 0.1f;
            if (combo <= 40) return 0.15f;
            if (combo <= 50) return 0f;
            if (combo <= 60) return -1f; // Neutral-only phase
            if (combo <= 70) return 0f;
            return 0.6f; // 71+
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
            else if (entity is Neutral neutral && _aliveNeutrals.Contains(neutral))
            {
                _aliveNeutrals.Remove(neutral);
                Debug.Log("neutral Destroyed"+ neutral.name);
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

            for (int i = _aliveNeutrals.Count - 1; i >= 0; i--)
            {
                if (_aliveNeutrals[i] != null)
                {
                    Destroy(_aliveNeutrals[i].gameObject);
                }
            }
            _aliveNeutrals.Clear();

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
        /// Reveal all current entities permanently (no push back).
        /// </summary>
        public void RevealAllEntities()
        {
            Debug.Log($"RevealAllEntities called. Enemies: {_aliveEnemies.Count}, Allies: {_aliveAllies.Count}");

            // Reveal all enemies
            foreach (var enemy in _aliveEnemies)
            {
                if (enemy != null)
                {
                    enemy.RevealPermanently(revealSortingOrder);
                }
            }

            // Reveal all allies
            foreach (var ally in _aliveAllies)
            {
                if (ally != null)
                {
                    ally.RevealPermanently(revealSortingOrder);
                }
            }
        }

        /// <summary>
        /// Destroy all entities within the specified radius of a world position.
        /// Destroyed entities count as detected (increase combo).
        /// </summary>
        /// <param name="worldPosition">Center point for the radius check</param>
        /// <param name="radius">Radius in world units</param>
        /// <param name="destroyedPositions">Optional list to receive positions of destroyed entities</param>
        /// <returns>Number of entities destroyed</returns>
        public int DestroyEntitiesInRadius(Vector3 worldPosition, float radius, List<Vector3> destroyedPositions = null)
        {
            int destroyedCount = 0;
            float radiusSqr = radius * radius;

            // Copy lists to avoid modification during iteration
            var enemiesToCheck = new List<Enemy>(_aliveEnemies);
            var alliesToCheck = new List<Ally>(_aliveAllies);
            var neutralsToCheck = new List<Neutral>(_aliveNeutrals);

            // Check and destroy enemies within radius
            foreach (var enemy in enemiesToCheck)
            {
                if (enemy != null)
                {
                    float distSqr = (enemy.transform.position - worldPosition).sqrMagnitude;
                    if (distSqr <= radiusSqr)
                    {
                        destroyedPositions?.Add(enemy.transform.position);
                        enemy.Destroy(true); // detected = true for combo increase
                        destroyedCount++;
                    }
                }
            }

            // Check and destroy allies within radius
            foreach (var ally in alliesToCheck)
            {
                if (ally != null)
                {
                    float distSqr = (ally.transform.position - worldPosition).sqrMagnitude;
                    if (distSqr <= radiusSqr)
                    {
                        destroyedPositions?.Add(ally.transform.position);
                        ally.Destroy(true); // detected = true for combo increase
                        destroyedCount++;
                    }
                }
            }

            // Check and destroy neutrals within radius
            foreach (var neutral in neutralsToCheck)
            {
                if (neutral != null)
                {
                    float distSqr = (neutral.transform.position - worldPosition).sqrMagnitude;
                    if (distSqr <= radiusSqr)
                    {
                        destroyedPositions?.Add(neutral.transform.position);
                        neutral.Destroy(true); // detected = true for combo increase
                        destroyedCount++;
                    }
                }
            }

            return destroyedCount;
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
            
            for (int i = _aliveNeutrals.Count - 1; i >= 0; i--)
            {
                _aliveNeutrals[i].UpdatePosition();
            }

            int combo = GameManager.Instance.combo;
            int spawnInterval = GetSpawnIntervalForCombo(combo);

            if (_currentSpawnInterval < 1)
            {
                _currentSpawnInterval = spawnInterval;

                if (ShouldSpawnThisInterval(combo))
                {
                    var prefab = GetEntityPrefabForCombo(combo, out int count);
                    float neutralChance = GetNeutralSpawnChance(combo);

                    for (int i = 0; i < count; i++)
                    {
                        // Spawn the main entity and get its angle
                        float spawnAngle = SpawnEntityAtAngle(prefab, Random.Range(0f, Mathf.PI * 2));

                        // If not in neutral-only phase (51-60), check for bonus neutral spawn
                        if (neutralChance >= 0f && Random.value < neutralChance)
                        {
                            // Spawn neutral on opposite tangent (180 degrees / PI radians offset)
                            float oppositeAngle = spawnAngle + Mathf.PI;
                            SpawnEntityAtAngle(neutralPrefab, oppositeAngle);
                        }
                    }
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
