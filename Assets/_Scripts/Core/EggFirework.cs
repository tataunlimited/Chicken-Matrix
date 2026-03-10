using System.Collections;
using UnityEngine;

namespace _Scripts.Core
{
    /// <summary>
    /// Handles an egg firework that arcs upward with randomized trajectory and explodes at peak height.
    /// Spawns an explosion particle system prefab on detonation.
    /// </summary>
    public class EggFirework : MonoBehaviour
    {
        [Header("Launch Settings")]
        [Tooltip("Base upward speed of the firework")]
        [SerializeField] private float launchSpeed = 8f;
        [Tooltip("Random variance added to launch speed")]
        [SerializeField] private float launchSpeedVariance = 2f;
        [Tooltip("Horizontal drift range (random value between -drift and +drift)")]
        [SerializeField] private float horizontalDrift = 3f;
        [Tooltip("Gravity applied to the firework")]
        [SerializeField] private float gravity = 12f;

        [Header("Trajectory Wobble")]
        [Tooltip("Amount of random wobble during flight")]
        [SerializeField] private float wobbleIntensity = 0.5f;
        [Tooltip("Speed of the wobble oscillation")]
        [SerializeField] private float wobbleFrequency = 8f;

        [Header("Explosion Settings")]
        [Tooltip("Prefab to spawn when the firework explodes")]
        [SerializeField] private GameObject explosionPrefab;
        [Tooltip("Minimum distance traveled before the firework can explode")]
        [SerializeField] private float minExplodeDistance = 2f;
        [Tooltip("Time after reaching peak velocity before explosion (adds anticipation)")]
        [SerializeField] private float explodeDelay = 0.1f;

        [Header("Visual Trail")]
        [Tooltip("Optional trail renderer for the firework")]
        [SerializeField] private TrailRenderer trailRenderer;
        [Tooltip("Time to wait for trail to fade before destroying")]
        [SerializeField] private float trailFadeTime = 0.3f;

        private Vector3 _velocity;
        private Vector3 _launchDirection;
        private float _wobbleOffset;
        private float _flightTime;
        private bool _hasExploded;
        private bool _reachedPeak;
        private Vector3 _startPosition;
        private bool _launched;
        private float _initialSpeed;

        private void Start()
        {
            // Only use default trajectory if Launch() wasn't called
            if (!_launched)
            {
                InitializeTrajectory();
            }
        }

        /// <summary>
        /// Initialize the firework with randomized trajectory values (default behavior)
        /// </summary>
        private void InitializeTrajectory()
        {
            _startPosition = transform.position;
            _wobbleOffset = Random.Range(0f, Mathf.PI * 2f);

            // Randomize launch parameters - random direction
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            _launchDirection = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            float actualLaunchSpeed = launchSpeed + Random.Range(-launchSpeedVariance, launchSpeedVariance);
            _initialSpeed = actualLaunchSpeed;
            _velocity = _launchDirection * actualLaunchSpeed;
        }

        /// <summary>
        /// Launch the firework with custom parameters
        /// </summary>
        /// <param name="speed">Override launch speed</param>
        /// <param name="direction">Override launch direction (will be normalized and scaled by speed)</param>
        public void Launch(float speed, Vector2 direction)
        {
            _launched = true;
            _startPosition = transform.position;
            _wobbleOffset = Random.Range(0f, Mathf.PI * 2f);

            _launchDirection = new Vector3(direction.x, direction.y, 0f).normalized;
            _initialSpeed = speed;
            _velocity = _launchDirection * speed;
        }

        /// <summary>
        /// Set the explosion prefab at runtime
        /// </summary>
        public void SetExplosionPrefab(GameObject prefab)
        {
            explosionPrefab = prefab;
        }

        private void Update()
        {
            if (_hasExploded) return;

            _flightTime += Time.deltaTime;

            // Apply gravity opposite to launch direction (decelerate then reverse)
            _velocity -= _launchDirection * gravity * Time.deltaTime;

            // Calculate wobble perpendicular to launch direction
            Vector3 wobblePerpendicular = new Vector3(-_launchDirection.y, _launchDirection.x, 0f);
            float wobble = Mathf.Sin(_flightTime * wobbleFrequency + _wobbleOffset) * wobbleIntensity;
            Vector3 wobbleOffset = wobblePerpendicular * wobble * Time.deltaTime;

            // Move the firework
            transform.position += (_velocity * Time.deltaTime) + wobbleOffset;

            // Check if we've reached the peak (velocity reversing direction)
            float dotWithLaunch = Vector3.Dot(_velocity.normalized, _launchDirection);
            if (!_reachedPeak && dotWithLaunch <= 0f)
            {
                _reachedPeak = true;
                StartCoroutine(ExplodeAfterDelay());
            }

            // Safety check: explode if returned past start position
            Vector3 toStart = _startPosition - transform.position;
            float distanceAlongLaunch = Vector3.Dot(toStart, _launchDirection);
            if (distanceAlongLaunch > 0f && _flightTime > 0.5f)
            {
                Explode();
            }
        }

        private IEnumerator ExplodeAfterDelay()
        {
            // Wait for the delay to add anticipation at the peak
            yield return new WaitForSeconds(explodeDelay);

            // Only explode if we've traveled minimum distance along launch direction
            float distanceTraveled = Vector3.Distance(transform.position, _startPosition);
            if (distanceTraveled >= minExplodeDistance)
            {
                Explode();
            }
            else
            {
                // If not far enough, wait until we fall back to start
                _reachedPeak = false;
            }
        }

        private void Explode()
        {
            if (_hasExploded) return;
            _hasExploded = true;

            // Spawn explosion effect
            if (explosionPrefab != null)
            {
                GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);

                // Auto-destroy the explosion after its particle system finishes
                ParticleSystem ps = explosion.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                    Destroy(explosion, duration);
                }
                else
                {
                    // Fallback destroy time if no particle system found
                    Destroy(explosion, 3f);
                }
            }

            // Handle trail fade-out before destroying
            if (trailRenderer != null)
            {
                StartCoroutine(FadeOutAndDestroy());
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private IEnumerator FadeOutAndDestroy()
        {
            // Disable the sprite renderer if present
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = false;
            }

            // Wait for trail to fade
            yield return new WaitForSeconds(trailFadeTime);

            Destroy(gameObject);
        }

        /// <summary>
        /// Force the firework to explode immediately
        /// </summary>
        public void ForceExplode()
        {
            Explode();
        }
    }
}
