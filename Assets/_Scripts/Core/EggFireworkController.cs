using System.Collections;
using UnityEngine;

namespace _Scripts.Core
{
    /// <summary>
    /// Controls the spawning and launching of egg fireworks from screen edges toward the center.
    /// </summary>
    public class EggFireworkController : MonoBehaviour
    {
        [Header("Firework Settings")]
        [Tooltip("The egg firework prefab to instantiate")]
        [SerializeField] private GameObject fireworkPrefab;
        [Tooltip("The explosion particle prefab to assign to each firework")]
        [SerializeField] private GameObject explosionPrefab;

        [Header("Spawn Settings")]
        [Tooltip("Total number of fireworks to launch")]
        [SerializeField] private int fireworkCount = 20;
        [Tooltip("Total duration over which all fireworks will be launched")]
        [SerializeField] private float launchDuration = 6f;
        [Tooltip("Randomize timing between launches for a more organic feel")]
        [SerializeField] private bool randomizeTiming = true;
        [Tooltip("Variance in timing (0 = even spacing, 1 = fully random within duration)")]
        [Range(0f, 1f)]
        [SerializeField] private float timingVariance = 0.5f;

        [Header("Launch Parameters")]
        [Tooltip("Base speed for launched fireworks")]
        [SerializeField] private float launchSpeed = 10f;
        [Tooltip("Random variance added to launch speed")]
        [SerializeField] private float speedVariance = 3f;

        [Header("Spawn Point")]
        [Tooltip("Origin point where all fireworks spawn from")]
        [SerializeField] private Vector3 spawnPoint = Vector3.zero;

        private bool _isLaunching;

        [Header("Debug")]
        [SerializeField] private bool enableDebugInput = true;

        private void Update()
        {
            if (enableDebugInput && Input.GetKeyDown(KeyCode.Space))
            {
                StartFireworks();
            }
        }

        /// <summary>
        /// Start the firework launch sequence
        /// </summary>
        public void StartFireworks()
        {
            if (_isLaunching) return;
            StartCoroutine(LaunchSequence());
        }

        /// <summary>
        /// Start fireworks with custom count and duration
        /// </summary>
        public void StartFireworks(int count, float duration)
        {
            if (_isLaunching) return;
            fireworkCount = count;
            launchDuration = duration;
            StartCoroutine(LaunchSequence());
        }

        /// <summary>
        /// Stop the firework sequence
        /// </summary>
        public void StopFireworks()
        {
            _isLaunching = false;
            StopAllCoroutines();
        }

        private IEnumerator LaunchSequence()
        {
            _isLaunching = true;

            if (randomizeTiming)
            {
                // Generate randomized launch times
                float[] launchTimes = GenerateRandomLaunchTimes();

                float elapsed = 0f;
                int launchedCount = 0;

                while (launchedCount < fireworkCount && _isLaunching)
                {
                    elapsed += Time.deltaTime;

                    // Launch any fireworks whose time has come
                    while (launchedCount < fireworkCount && launchTimes[launchedCount] <= elapsed)
                    {
                        LaunchSingleFirework();
                        launchedCount++;
                    }

                    yield return null;
                }
            }
            else
            {
                // Even spacing between launches
                float interval = launchDuration / fireworkCount;

                for (int i = 0; i < fireworkCount && _isLaunching; i++)
                {
                    LaunchSingleFirework();
                    yield return new WaitForSeconds(interval);
                }
            }

            _isLaunching = false;
        }

        private float[] GenerateRandomLaunchTimes()
        {
            float[] times = new float[fireworkCount];
            float baseInterval = launchDuration / fireworkCount;

            for (int i = 0; i < fireworkCount; i++)
            {
                float baseTime = i * baseInterval;
                float variance = Random.Range(-baseInterval * timingVariance, baseInterval * timingVariance);
                times[i] = Mathf.Clamp(baseTime + variance, 0f, launchDuration);
            }

            // Sort to ensure proper order
            System.Array.Sort(times);

            return times;
        }

        private void LaunchSingleFirework()
        {
            if (fireworkPrefab == null) return;

            // Get random launch direction (full 360 degrees)
            Vector2 launchDir = GetRandomLaunchDirection();

            // Instantiate firework at spawn point
            GameObject firework = Instantiate(fireworkPrefab, spawnPoint, Quaternion.identity);

            EggFirework eggFirework = firework.GetComponent<EggFirework>();
            if (eggFirework != null)
            {
                // Assign explosion prefab
                if (explosionPrefab != null)
                {
                    eggFirework.SetExplosionPrefab(explosionPrefab);
                }

                // Launch with randomized speed
                float speed = launchSpeed + Random.Range(-speedVariance, speedVariance);
                eggFirework.Launch(speed, launchDir);
            }
        }

        private Vector2 GetRandomLaunchDirection()
        {
            // Random angle in full 360 degrees
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        /// <summary>
        /// Check if fireworks are currently being launched
        /// </summary>
        public bool IsLaunching => _isLaunching;
    }
}
