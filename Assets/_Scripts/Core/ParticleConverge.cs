using UnityEngine;

namespace _Scripts.Core
{
    public class ParticleConverge : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform targetTransform;

        [Header("Screen Space Target (Camera Rotation Safe)")]
        [Tooltip("Use a fixed screen position instead of a transform")]
        [SerializeField] private bool useScreenPosition = false;
        [Tooltip("Screen position (0-1 normalized). 0.5, 0.5 = center")]
        [SerializeField] private Vector2 screenPosition = new Vector2(0.5f, 0.5f);
        [Tooltip("Distance from camera for the target point")]
        [SerializeField] private float targetDepth = 10f;

        [Header("Timing")]
        [Tooltip("Delay before particles start converging")]
        [SerializeField] private float convergeDelay = 0.5f;

        [Header("Movement")]
        [Tooltip("Lerp speed for convergence (higher = faster)")]
        [SerializeField] private float lerpSpeed = 5f;
        [Tooltip("Minimum speed to ensure particles always make progress")]
        [SerializeField] private float minSpeed = 2f;
        [Tooltip("Distance at which particles are destroyed")]
        [SerializeField] private float destroyDistance = 0.2f;

        private ParticleSystem _particleSystem;
        private ParticleSystem.Particle[] _particles;
        private float _timer;
        private bool _isConverging;
        private bool _shouldConverge = true;
        private bool _isLocalSpace;
        private Camera _mainCamera;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            if (_particleSystem == null)
            {
                Debug.LogError("ParticleConverge requires a ParticleSystem component!");
                enabled = false;
                return;
            }

            _particles = new ParticleSystem.Particle[_particleSystem.main.maxParticles];
            _isLocalSpace = _particleSystem.main.simulationSpace == ParticleSystemSimulationSpace.Local;
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (_particleSystem == null) return;

            // If not converging, disable this script and let normal particle lifecycle handle cleanup
            if (!_shouldConverge)
            {
                enabled = false;
                return;
            }

            _timer += Time.deltaTime;

            // Only converge if we should (entity was detected, not a combo reset)
            bool hasValidTarget = useScreenPosition ? (_mainCamera != null) : (targetTransform != null);
            if (hasValidTarget && _timer >= convergeDelay)
            {
                _isConverging = true;
                ConvergeParticles();
            }

            // Destroy when all particles are gone
            if (_isConverging && _particleSystem.particleCount == 0)
            {
                Destroy(gameObject);
            }
        }

        private Vector3 GetTargetPosition()
        {
            if (useScreenPosition && _mainCamera != null)
            {
                // Convert normalized screen position to world position
                // This stays fixed on screen regardless of camera rotation
                Vector3 screenPoint = new Vector3(
                    screenPosition.x * Screen.width,
                    screenPosition.y * Screen.height,
                    targetDepth
                );
                return _mainCamera.ScreenToWorldPoint(screenPoint);
            }
            return targetTransform != null ? targetTransform.position : Vector3.zero;
        }

        private void ConvergeParticles()
        {
            int particleCount = _particleSystem.GetParticles(_particles);
            if (particleCount == 0) return;

            Vector3 targetPos = GetTargetPosition();
            float deltaTime = Time.deltaTime;
            float lerpFactor = lerpSpeed * deltaTime;
            float minDistance = minSpeed * deltaTime;
            float destroyDistSqr = destroyDistance * destroyDistance;

            for (int i = 0; i < particleCount; i++)
            {
                // Convert particle position to world space if using local simulation
                Vector3 particleWorldPos = _isLocalSpace
                    ? transform.TransformPoint(_particles[i].position)
                    : _particles[i].position;

                // Use sqrMagnitude to avoid sqrt in distance calculation
                Vector3 toTarget = targetPos - particleWorldPos;
                float distanceSqr = toTarget.sqrMagnitude;

                // Check if particle should be destroyed
                if (distanceSqr <= destroyDistSqr)
                {
                    _particles[i].remainingLifetime = 0f;
                }
                else
                {
                    // Lerp toward target with minimum speed to ensure progress
                    Vector3 lerpedPos = Vector3.Lerp(particleWorldPos, targetPos, lerpFactor);

                    // Calculate if lerp is too slow, use minimum speed instead
                    Vector3 lerpDelta = lerpedPos - particleWorldPos;
                    float lerpDistSqr = lerpDelta.sqrMagnitude;
                    float minDistSqr = minDistance * minDistance;

                    Vector3 newWorldPos;
                    if (lerpDistSqr < minDistSqr)
                    {
                        // Use minimum speed - normalize direction manually to avoid extra sqrt
                        float distance = Mathf.Sqrt(distanceSqr);
                        Vector3 direction = toTarget / distance;
                        newWorldPos = particleWorldPos + direction * minDistance;
                    }
                    else
                    {
                        newWorldPos = lerpedPos;
                    }

                    // Convert back to local space if needed
                    _particles[i].position = _isLocalSpace
                        ? transform.InverseTransformPoint(newWorldPos)
                        : newWorldPos;

                    // Override velocity to prevent original movement
                    _particles[i].velocity = Vector3.zero;
                }
            }

            _particleSystem.SetParticles(_particles, particleCount);
        }

        /// <summary>
        /// Set the target transform at runtime
        /// </summary>
        public void SetTarget(Transform target)
        {
            targetTransform = target;
        }

        /// <summary>
        /// Set whether particles should converge (false if combo was reset)
        /// </summary>
        public void SetShouldConverge(bool shouldConverge)
        {
            _shouldConverge = shouldConverge;
        }

        /// <summary>
        /// Set a fixed screen position for convergence (camera rotation safe)
        /// </summary>
        /// <param name="normalizedScreenPos">Screen position (0-1 normalized)</param>
        /// <param name="depth">Distance from camera</param>
        public void SetScreenTarget(Vector2 normalizedScreenPos, float depth = 10f)
        {
            useScreenPosition = true;
            screenPosition = normalizedScreenPos;
            targetDepth = depth;
        }
    }
}
