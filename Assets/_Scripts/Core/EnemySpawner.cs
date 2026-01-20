using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Scripts.Core
{
    /// <summary>
    /// Defines the entity type pattern for a spawn section.
    /// </summary>
    public enum EntityPattern
    {
        FriendlyOnly,
        EnemyOnly,
        AlternateFriendEnemy,
        FriendFriendEnemy,
        EnemyEnemyFriend,
        NeutralOnly,
        ComplexPattern,
        Random
    }

    /// <summary>
    /// Configuration for a single combo section (10 combo points, 16 seconds).
    /// </summary>
    [Serializable]
    public class SpawnSectionConfig
    {
        [Tooltip("Number of entities to spawn in this 16-second section")]
        [Range(1, 32)]
        public int spawnCount = 5;

        [Tooltip("Entity type pattern for this section")]
        public EntityPattern entityPattern = EntityPattern.FriendlyOnly;

        [Tooltip("Custom pattern (used when entityPattern is ComplexPattern). 0=Friendly, 1=Enemy, 2=Neutral")]
        public int[] customPattern;

        [Tooltip("Chance (0-1) to spawn a neutral alongside the main entity")]
        [Range(0f, 1f)]
        public float neutralSpawnChance = 0f;

        [Tooltip("Maximum cluster size (1 = no clustering)")]
        [Range(1, 5)]
        public int maxClusterSize = 1;

        [Tooltip("If true, spawn every interval (for barrage sections)")]
        public bool spawnEveryInterval = false;

        // Calculated at runtime - not serialized
        [HideInInspector]
        public int[] spawnIntervals;
    }

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

        [Header("Tutorial Indicators")]
        [Tooltip("Tutorial indicator prefab for allies (right mouse button)")]
        [SerializeField] private TutorialIndicator allyTutorialIndicatorPrefab;
        [Tooltip("Tutorial indicator prefab for enemies (left mouse button)")]
        [SerializeField] private TutorialIndicator enemyTutorialIndicatorPrefab;
        [Tooltip("Tutorial indicator prefab for neutrals (no mouse button)")]
        [SerializeField] private TutorialIndicator neutralTutorialIndicatorPrefab;

        [Header("Spawn Section Configuration")]
        [Tooltip("Configuration for each of the 10 combo sections (1-10, 11-20, ..., 91-100)")]
        [SerializeField]
        private SpawnSectionConfig[] sectionConfigs = new SpawnSectionConfig[10];

        private int _entityPatternIndex;

        // Track interval within current 32-interval segment (0-31)
        // Each segment = 16 seconds = 10 combo points
        private int _segmentIntervalCounter;


        private readonly List<Enemy> _aliveEnemies = new List<Enemy>();
        private readonly List<Ally> _aliveAllies = new List<Ally>();
        private readonly List<Neutral> _aliveNeutrals = new List<Neutral>();

        private bool _spawningEnabled = true;
        private bool _spawnedThisPulse;

        /// <summary>
        /// Returns true if any entities were spawned during the last UpdateEnemies call
        /// </summary>
        public bool SpawnedThisPulse => _spawnedThisPulse;

        public static EnemySpawner Instance;
        
        void Awake()
        {
            Instance = this;
            InitializeSectionConfigs();
        }

        MovableEntitiy SpawnEntityAtAngle(MovableEntitiy prefab, float angle)
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
                TrySpawnEnemyTutorial(enemy);
            }
            else if (newEntity is Ally ally)
            {
                _aliveAllies.Add(ally);
                TrySpawnAllyTutorial(ally);
            }
            else if (newEntity is Neutral neutral)
            {
                _aliveNeutrals.Add(neutral);
                TrySpawnNeutralTutorial(neutral);
            }

            return newEntity;
        }

        /// <summary>
        /// Spawns a cluster of entities in a tight arc.
        /// </summary>
        void SpawnClusterAtAngle(MovableEntitiy prefab, float centerAngle, int clusterSize)
        {
            // Calculate starting angle to center the cluster
            float totalArc = (clusterSize - 1) * clusterAngleSpacing * Mathf.Deg2Rad;
            float startAngle = centerAngle - totalArc / 2f;

            for (int i = 0; i < clusterSize; i++)
            {
                float angle = startAngle + (i * clusterAngleSpacing * Mathf.Deg2Rad);
                SpawnEntityAtAngle(prefab, angle);
            }
        }

        /// <summary>
        /// Get the section index (0-9) for a given combo value.
        /// </summary>
        private int GetSectionIndex(int combo)
        {
            return Mathf.Clamp((combo - 1) / 10, 0, 9);
        }

        /// <summary>
        /// Get the config for the current combo section.
        /// </summary>
        private SpawnSectionConfig GetCurrentSectionConfig(int combo)
        {
            int index = GetSectionIndex(combo);
            if (sectionConfigs == null || index >= sectionConfigs.Length || sectionConfigs[index] == null)
            {
                return GetDefaultSectionConfig(index);
            }
            return sectionConfigs[index];
        }

        /// <summary>
        /// Returns default config for a section if not configured.
        /// </summary>
        private SpawnSectionConfig GetDefaultSectionConfig(int sectionIndex)
        {
            var config = new SpawnSectionConfig();
            switch (sectionIndex)
            {
                case 0: // 1-10
                    config.spawnCount = 5;
                    config.entityPattern = EntityPattern.FriendlyOnly;
                    config.neutralSpawnChance = 0f;
                    config.maxClusterSize = 1;
                    break;
                case 1: // 11-20
                    config.spawnCount = 5;
                    config.entityPattern = EntityPattern.EnemyOnly;
                    config.neutralSpawnChance = 0.1f;
                    config.maxClusterSize = 1;
                    break;
                case 2: // 21-30
                    config.spawnCount = 5;
                    config.entityPattern = EntityPattern.AlternateFriendEnemy;
                    config.neutralSpawnChance = 0.1f;
                    config.maxClusterSize = 1;
                    break;
                case 3: // 31-40
                    config.spawnCount = 7;
                    config.entityPattern = EntityPattern.FriendFriendEnemy;
                    config.neutralSpawnChance = 0.15f;
                    config.maxClusterSize = 2;
                    break;
                case 4: // 41-50
                    config.spawnCount = 10;
                    config.entityPattern = EntityPattern.EnemyEnemyFriend;
                    config.neutralSpawnChance = 0.15f;
                    config.maxClusterSize = 2;
                    break;
                case 5: // 51-60
                    config.spawnCount = 32;
                    config.entityPattern = EntityPattern.NeutralOnly;
                    config.neutralSpawnChance = 0f;
                    config.maxClusterSize = 1;
                    config.spawnEveryInterval = true;
                    break;
                case 6: // 61-70
                    config.spawnCount = 10;
                    config.entityPattern = EntityPattern.ComplexPattern;
                    config.customPattern = new[] { 0, 0, 0, 1, 1, 0, 0, 1, 1, 1, 0, 0, 1, 1, 0, 0, 0 };
                    config.neutralSpawnChance = 0.2f;
                    config.maxClusterSize = 2;
                    break;
                case 7: // 71-80
                    config.spawnCount = 12;
                    config.entityPattern = EntityPattern.Random;
                    config.neutralSpawnChance = 0.2f;
                    config.maxClusterSize = 3;
                    break;
                case 8: // 81-90
                    config.spawnCount = 12;
                    config.entityPattern = EntityPattern.Random;
                    config.neutralSpawnChance = 0.3f;
                    config.maxClusterSize = 3;
                    break;
                case 9: // 91-100
                    config.spawnCount = 12;
                    config.entityPattern = EntityPattern.Random;
                    config.neutralSpawnChance = 0.5f;
                    config.maxClusterSize = 3;
                    break;
            }
            CalculateSpawnIntervals(config);
            return config;
        }

        /// <summary>
        /// Calculate evenly distributed spawn intervals for a section config.
        /// </summary>
        private void CalculateSpawnIntervals(SpawnSectionConfig config)
        {
            if (config.spawnEveryInterval)
            {
                config.spawnIntervals = null; // null means spawn every interval
                return;
            }

            config.spawnIntervals = new int[config.spawnCount];
            float spacing = 32f / config.spawnCount;
            for (int i = 0; i < config.spawnCount; i++)
            {
                config.spawnIntervals[i] = Mathf.RoundToInt(i * spacing);
            }
        }

        /// <summary>
        /// Returns a random cluster size based on the section config.
        /// </summary>
        private int GetClusterSizeForCombo(int combo)
        {
            var config = GetCurrentSectionConfig(combo);
            if (config.maxClusterSize <= 1) return 1;
            return Random.Range(1, config.maxClusterSize + 1);
        }

        /// <summary>
        /// Get the entity prefab based on the section's pattern configuration.
        /// </summary>
        private MovableEntitiy GetEntityPrefabForCombo(int combo, out int count)
        {
            count = 1;
            var config = GetCurrentSectionConfig(combo);

            switch (config.entityPattern)
            {
                case EntityPattern.FriendlyOnly:
                    return friendlyPrefab;

                case EntityPattern.EnemyOnly:
                    return enemyPrefab;

                case EntityPattern.NeutralOnly:
                    count = Random.Range(1, 5); // 1-4 neutrals for barrage
                    return neutralPrefab;

                case EntityPattern.AlternateFriendEnemy:
                    return (_entityPatternIndex % 2) == 0 ? friendlyPrefab : enemyPrefab;

                case EntityPattern.FriendFriendEnemy:
                    return (_entityPatternIndex % 3) < 2 ? friendlyPrefab : enemyPrefab;

                case EntityPattern.EnemyEnemyFriend:
                    return (_entityPatternIndex % 3) < 2 ? enemyPrefab : friendlyPrefab;

                case EntityPattern.ComplexPattern:
                    if (config.customPattern != null && config.customPattern.Length > 0)
                    {
                        int patternValue = config.customPattern[_entityPatternIndex % config.customPattern.Length];
                        if (patternValue == 2) return neutralPrefab;
                        return patternValue == 0 ? friendlyPrefab : enemyPrefab;
                    }
                    return friendlyPrefab;

                case EntityPattern.Random:
                    return Random.value < 0.5f ? friendlyPrefab : enemyPrefab;

                default:
                    return friendlyPrefab;
            }
        }

        /// <summary>
        /// Returns the chance (0-1) to spawn a neutral alongside regular entities.
        /// Returns -1 for neutral-only sections (handled separately).
        /// </summary>
        private float GetNeutralSpawnChance(int combo)
        {
            var config = GetCurrentSectionConfig(combo);
            if (config.entityPattern == EntityPattern.NeutralOnly)
                return -1f;
            return config.neutralSpawnChance;
        }

        /// <summary>
        /// Get the spawn schedule for the current combo tier.
        /// </summary>
        private int[] GetSpawnScheduleForCombo(int combo)
        {
            var config = GetCurrentSectionConfig(combo);
            if (config.spawnIntervals == null && !config.spawnEveryInterval)
            {
                CalculateSpawnIntervals(config);
            }
            return config.spawnIntervals;
        }

        /// <summary>
        /// Check if we should spawn this interval based on the segment schedule.
        /// </summary>
        private bool ShouldSpawnThisInterval(int combo)
        {
            var config = GetCurrentSectionConfig(combo);
            if (config.spawnEveryInterval)
                return true;

            var schedule = GetSpawnScheduleForCombo(combo);
            if (schedule == null || schedule.Length == 0) return true;

            bool shouldSpawn = Array.IndexOf(schedule, _segmentIntervalCounter) >= 0;
            Debug.Log($"[Spawner] Combo:{combo} Interval:{_segmentIntervalCounter} Schedule:[{string.Join(",", schedule)}] ShouldSpawn:{shouldSpawn}");
            return shouldSpawn;
        }

        /// <summary>
        /// Initialize section configs on Awake if they haven't been set in the inspector.
        /// </summary>
        private void InitializeSectionConfigs()
        {
            if (sectionConfigs == null || sectionConfigs.Length != 10)
            {
                sectionConfigs = new SpawnSectionConfig[10];
            }

            for (int i = 0; i < 10; i++)
            {
                // If config is null or has invalid spawnCount, use defaults
                if (sectionConfigs[i] == null || sectionConfigs[i].spawnCount <= 0)
                {
                    sectionConfigs[i] = GetDefaultSectionConfig(i);
                }
                else
                {
                    // Always recalculate spawn intervals at runtime (they're not serialized)
                    CalculateSpawnIntervals(sectionConfigs[i]);
                }
            }
        }

        private void EnemyDestroyed(MovableEntitiy entity)
        {
            // Missing any entity causes combo reset; detecting increments enemiesDestroyed
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

            // Get lightning color for spin attack - lightning always originates from center
            Color lightningColor = LightningEffect.Instance != null
                ? LightningEffect.Instance.SpinAttackColor
                : Color.magenta;
            Vector3 centerPos = Vector3.zero;

            // Check and destroy enemies within radius
            foreach (var enemy in enemiesToCheck)
            {
                if (enemy != null)
                {
                    float distSqr = (enemy.transform.position - worldPosition).sqrMagnitude;
                    if (distSqr <= radiusSqr)
                    {
                        Vector3 entityPos = enemy.transform.position;
                        destroyedPositions?.Add(entityPos);
                        enemy.Destroy(true); // detected = true for combo increase
                        destroyedCount++;

                        // Spawn lightning from center to destroyed entity
                        if (LightningEffect.Instance != null)
                            LightningEffect.Instance.SpawnLightning(centerPos, entityPos, lightningColor);
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
                        Vector3 entityPos = ally.transform.position;
                        destroyedPositions?.Add(entityPos);
                        ally.Destroy(true); // detected = true for combo increase
                        destroyedCount++;

                        // Spawn lightning from center to destroyed entity
                        if (LightningEffect.Instance != null)
                            LightningEffect.Instance.SpawnLightning(centerPos, entityPos, lightningColor);
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
                        Vector3 entityPos = neutral.transform.position;
                        destroyedPositions?.Add(entityPos);
                        neutral.Destroy(true); // detected = true for combo increase
                        destroyedCount++;

                        // Spawn lightning from center to destroyed entity
                        if (LightningEffect.Instance != null)
                            LightningEffect.Instance.SpawnLightning(centerPos, entityPos, lightningColor);
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
            // Reset spawn flag at start of each pulse
            _spawnedThisPulse = false;

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
                _spawnedThisPulse = true;
                var prefab = GetEntityPrefabForCombo(combo, out int count);
                float neutralChance = GetNeutralSpawnChance(combo);
                int clusterSize = GetClusterSizeForCombo(combo);

                for (int i = 0; i < count; i++)
                {
                    // Spawn the main entity (or cluster) and get its angle
                    float spawnAngle = Random.Range(0f, Mathf.PI * 2);

                    if (clusterSize > 1)
                    {
                        SpawnClusterAtAngle(prefab, spawnAngle, clusterSize);
                    }
                    else
                    {
                        SpawnEntityAtAngle(prefab, spawnAngle);
                    }

                    // If not in neutral-only phase, check for bonus neutral spawn
                    if (neutralChance >= 0f && Random.value < neutralChance)
                    {
                        // Spawn neutral on opposite tangent (180 degrees / PI radians offset)
                        float oppositeAngle = spawnAngle + Mathf.PI;
                        SpawnEntityAtAngle(neutralPrefab, oppositeAngle);
                    }
                }
                _entityPatternIndex++;
            }

            // Advance segment counter (wraps every 32 intervals = 16 seconds)
            _segmentIntervalCounter = (_segmentIntervalCounter + 1) % 32;
        }

        private void TrySpawnAllyTutorial(Ally ally)
        {
            // Show ally tutorial while combo is exactly 1, respawn if previous indicator was destroyed
            if (allyTutorialIndicatorPrefab != null && GameManager.Instance.combo == 1)
            {
                var indicator = Instantiate(allyTutorialIndicatorPrefab, ally.transform.position, Quaternion.identity);
                indicator.Initialize(ally);
            }
        }

        private void TrySpawnEnemyTutorial(Enemy enemy)
        {
            // Show enemy tutorial at combo 11 (first enemy spawn), respawn if previous was destroyed
            if (enemyTutorialIndicatorPrefab != null && GameManager.Instance.combo == 11)
            {
                var indicator = Instantiate(enemyTutorialIndicatorPrefab, enemy.transform.position, Quaternion.identity);
                indicator.Initialize(enemy);
            }
        }

        private void TrySpawnNeutralTutorial(Neutral neutral)
        {
            // Show neutral tutorial at combo 11 (first neutral spawn), respawn if previous was destroyed
            if (neutralTutorialIndicatorPrefab != null && GameManager.Instance.combo == 11)
            {
                var indicator = Instantiate(neutralTutorialIndicatorPrefab, neutral.transform.position, Quaternion.identity);
                indicator.Initialize(neutral);
            }
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
