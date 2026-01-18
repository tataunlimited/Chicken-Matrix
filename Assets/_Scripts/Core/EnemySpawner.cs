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

        [Header("Cluster Spawning")]
        [Tooltip("Angular spacing between entities in a cluster (in degrees)")]
        [SerializeField] private float clusterAngleSpacing = 8f;
        [Tooltip("Maximum entities per cluster")]
        [SerializeField] private int maxClusterSize = 3;

        private int _entityPatternIndex;

        // Track interval within current 32-interval segment (0-31)
        // Each segment = 16 seconds = 10 combo points
        private int _segmentIntervalCounter;

        // Spawn schedules per tier - each must sum to 10 combo over 32 intervals
        // Format: array of intervals within the 32-interval segment when spawns occur

        // Tier 1-10: 5 spawns × +2 combo = 10 (very relaxed)
        private static readonly int[] SpawnScheduleTier1 = { 0, 6, 12, 18, 24 };
        // Tier 11-20: 5 spawns × +2 combo = 10 (still easy)
        private static readonly int[] SpawnScheduleTier2 = { 0, 6, 12, 18, 24 };
        // Tier 21-30: 7 spawns (5×+1, 2×+2 = 9, need adjustment) → use 5×+2 for simplicity
        private static readonly int[] SpawnScheduleTier3 = { 0, 5, 10, 15, 20 };
        // Tier 31-40: 7 spawns mixed timing
        private static readonly int[] SpawnScheduleTier4 = { 0, 4, 9, 13, 18, 22, 27 };
        // Tier 41-50: 10 spawns × +1 combo = 10 (standard difficulty)
        private static readonly int[] SpawnScheduleTier5 = { 0, 2, 5, 7, 10, 13, 15, 18, 20, 23 };
        // Tier 51-60: Neutral barrage (handled separately - spawns every interval)
        // Tier 61-70: 10 spawns × +1 combo = 10
        private static readonly int[] SpawnScheduleTier7 = { 0, 2, 5, 7, 10, 13, 15, 18, 20, 23 };
        // Tier 71+: 12 spawns (10×+1 from main, extras are neutrals that don't count)
        private static readonly int[] SpawnScheduleTier8 = { 0, 2, 4, 7, 9, 11, 14, 16, 18, 21, 23, 26 };


        private readonly List<Enemy> _aliveEnemies = new List<Enemy>();
        private readonly List<Ally> _aliveAllies = new List<Ally>();
        private readonly List<Neutral> _aliveNeutrals = new List<Neutral>();

        private bool _spawningEnabled = true;

        public static EnemySpawner Instance;
        
        void Awake()
        {
            Instance = this;
        }

        MovableEntitiy SpawnEntityAtAngle(MovableEntitiy prefab, float angle, int comboValue)
        {
            float x = Mathf.Cos(angle) * spawnRadius;
            float y = Mathf.Sin(angle) * spawnRadius;

            Vector3 spawnPosition = new Vector3(x, y, 0) + transform.position;

            var newEntity = Instantiate(prefab, spawnPosition, Quaternion.identity);

            newEntity.Init(offset, stepSize);
            newEntity.comboValue = comboValue;

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

            return newEntity;
        }

        /// <summary>
        /// Spawns a cluster of entities in a tight arc. Only the first entity gets combo value,
        /// the rest get 0 (but still cause setback if missed).
        /// </summary>
        void SpawnClusterAtAngle(MovableEntitiy prefab, float centerAngle, int clusterSize, int comboValue)
        {
            // Calculate starting angle to center the cluster
            float totalArc = (clusterSize - 1) * clusterAngleSpacing * Mathf.Deg2Rad;
            float startAngle = centerAngle - totalArc / 2f;

            for (int i = 0; i < clusterSize; i++)
            {
                float angle = startAngle + (i * clusterAngleSpacing * Mathf.Deg2Rad);
                // Only first entity in cluster gets combo value
                int entityComboValue = (i == 0) ? comboValue : 0;
                SpawnEntityAtAngle(prefab, angle, entityComboValue);
            }
        }

        /// <summary>
        /// Returns a random cluster size for the current combo tier.
        /// Higher tiers allow larger clusters, but size is randomized each spawn.
        /// </summary>
        private int GetClusterSizeForCombo(int combo)
        {
            int maxForTier;
            if (combo <= 30) maxForTier = 1;       // No clustering for early game
            else if (combo <= 50) maxForTier = 2;  // Up to pairs at mid game
            else if (combo <= 60) maxForTier = 1;  // Neutral barrage stays single
            else if (combo <= 70) maxForTier = 2;  // Up to pairs
            else maxForTier = maxClusterSize;      // Up to full clusters (3) at 71+

            // Random from 1 to max for this tier
            return Random.Range(1, maxForTier + 1);
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

            // Combo 51-60: Neutral barrage - random 1-4 per spawn
            if (combo <= 60)
            {
                count = Random.Range(1, 5); // 1-4 neutrals
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

        /// <summary>
        /// Returns the chance (0-1) to spawn a neutral alongside regular entities.
        /// 0% for combo 1-10, 10% for 11-20, 20% for 21-30, 30% for 31-40, 40% for 41-50.
        /// Returns -1 for combo 51-60 (neutral-only phase, handled separately).
        /// </summary>
        private float GetNeutralSpawnChance(int combo)
        {
            if (combo <= 10) return 0f;
            if (combo <= 20) return 1f;
            if (combo <= 30) return 0.1f;
            if (combo <= 40) return 0.15f;
            if (combo <= 50) return 0.15f;
            if (combo <= 60) return -1f; // Neutral-only phase
            if (combo <= 70) return 0.2f;
            if (combo <= 80) return 0.2f;
            if (combo <= 90) return 0.3f;
            return 0.5f; // 91+
        }

        /// <summary>
        /// Get the spawn schedule for the current combo tier.
        /// </summary>
        private int[] GetSpawnScheduleForCombo(int combo)
        {
            if (combo <= 10) return SpawnScheduleTier1;
            if (combo <= 20) return SpawnScheduleTier2;
            if (combo <= 30) return SpawnScheduleTier3;
            if (combo <= 40) return SpawnScheduleTier4;
            if (combo <= 50) return SpawnScheduleTier5;
            if (combo <= 60) return null; // Neutral barrage - spawns every interval
            if (combo <= 70) return SpawnScheduleTier7;
            return SpawnScheduleTier8;
        }

        /// <summary>
        /// Get the combo value for entities spawned at current combo tier.
        /// Uses spawn index to vary combo values within a tier when needed.
        /// </summary>
        private int GetComboValueForTier(int combo)
        {
            if (combo <= 10) return 2;  // 5 spawns × +2 = 10
            if (combo <= 20) return 2;  // 5 spawns × +2 = 10
            if (combo <= 30) return 2;  // 5 spawns × +2 = 10
            if (combo <= 40)            // 7 spawns: 3×+2 + 4×+1 = 10
            {
                // Pattern: +2, +1, +2, +1, +1, +2, +1
                int[] values = { 2, 1, 2, 1, 1, 2, 1 };
                return values[_entityPatternIndex % values.Length];
            }
            if (combo <= 50) return 1;  // 10 spawns × +1 = 10
            if (combo <= 60) return 0;  // Neutral barrage - auto-combo via timer
            if (combo <= 70) return 1;  // 10 spawns × +1 = 10
            return 1;                   // 12 spawns but neutrals mixed in (neutrals = 0)
        }

        /// <summary>
        /// Check if we should spawn this interval based on the segment schedule.
        /// </summary>
        private bool ShouldSpawnThisInterval(int combo)
        {
            // Neutral barrage phase - spawn every interval for maximum chaos
            if (combo >= 51 && combo <= 60)
                return true;

            var schedule = GetSpawnScheduleForCombo(combo);
            if (schedule == null) return true;

            return System.Array.IndexOf(schedule, _segmentIntervalCounter) >= 0;
        }

        private void EnemyDestroyed(MovableEntitiy entity)
        {
            // All entities affect combo - missing any entity causes setback
            GameManager.Instance.UpdateCombo(entity.Detected, entity.comboValue);

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
            Debug.Log($"RevealAllEntities called. Enemies: {_aliveEnemies.Count}, Allies: {_aliveAllies.Count}, Neutrals: {_aliveNeutrals.Count}");

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

            // Reveal all neutrals
            foreach (var neutral in _aliveNeutrals)
            {
                if (neutral != null)
                {
                    neutral.RevealPermanently(revealSortingOrder);
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


        /// <summary>
        /// Stop all spawning (used for victory sequence)
        /// </summary>
        public void StopSpawning()
        {
            _spawningEnabled = false;
        }

        /// <summary>
        /// Resume spawning
        /// </summary>
        public void ResumeSpawning()
        {
            _spawningEnabled = true;
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

            // Don't spawn if spawning is disabled
            if (!_spawningEnabled) return;

            int combo = GameManager.Instance.combo;

            // Check if we should spawn this interval based on the segment schedule
            if (ShouldSpawnThisInterval(combo))
            {
                var prefab = GetEntityPrefabForCombo(combo, out int count);
                float neutralChance = GetNeutralSpawnChance(combo);
                int comboValue = GetComboValueForTier(combo);
                int clusterSize = GetClusterSizeForCombo(combo);

                for (int i = 0; i < count; i++)
                {
                    // Spawn the main entity (or cluster) and get its angle
                    float spawnAngle = Random.Range(0f, Mathf.PI * 2);

                    if (clusterSize > 1)
                    {
                        // Spawn a cluster - only first entity gets combo value
                        SpawnClusterAtAngle(prefab, spawnAngle, clusterSize, comboValue);
                    }
                    else
                    {
                        SpawnEntityAtAngle(prefab, spawnAngle, comboValue);
                    }

                    // If not in neutral-only phase (51-60), check for bonus neutral spawn
                    if (neutralChance >= 0f && Random.value < neutralChance)
                    {
                        // Spawn neutral on opposite tangent (180 degrees / PI radians offset)
                        float oppositeAngle = spawnAngle + Mathf.PI;
                        SpawnEntityAtAngle(neutralPrefab, oppositeAngle, 0); // Neutrals give 0 combo
                    }
                }
                _entityPatternIndex++;
            }

            // Advance segment counter (wraps every 32 intervals = 16 seconds)
            _segmentIntervalCounter = (_segmentIntervalCounter + 1) % 32;
        }

        public void ResetProgression()
        {
            _segmentIntervalCounter = 0;
            _entityPatternIndex = 0;
        }

        /// <summary>
        /// Sync the spawner to a specific combo value.
        /// Called when combo resets to align spawn timing with music.
        /// </summary>
        public void SyncToCombo(int combo)
        {
            int comboInSegment = (combo - 1) % 10; // 0-9
            var schedule = GetSpawnScheduleForCombo(combo);

            if (schedule != null && comboInSegment < schedule.Length)
            {
                _segmentIntervalCounter = schedule[comboInSegment];
            }
            else
            {
                _segmentIntervalCounter = 0;
            }
            _entityPatternIndex = 0;
        }
    }
}
