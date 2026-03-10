using UnityEngine;

namespace _Scripts.Core
{
    public class ParticleConverge : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform targetTransform;

        [Header("Screen Space Target (Camera Rotation Safe)")]
        [Tooltip("Use a fixed screen position instead of a transform")]
        [SerializeField] private bool useScreenPosition = true;
        [Tooltip("Screen position (0-1 normalized). 0.5, 0.5 = center")]
        [SerializeField] private Vector2 screenPosition = new Vector2(0.5f, 0.5f);
        [Tooltip("Distance from camera for the target point")]
        [SerializeField] private float targetDepth = 10f;

        [Header("Timing")]
        [Tooltip("Delay before particles start converging")]
        [SerializeField] private float convergeDelay = 0.5f;

        [Header("Movement")]
        [Tooltip("Lerp speed for convergence (higher = faster)")]
        [SerializeField] private float lerpSpeed = 12f;
        [Tooltip("Minimum speed to ensure particles always make progress")]
        [SerializeField] private float minSpeed = 5f;
        [Tooltip("Distance at which particles are destroyed")]
        [SerializeField] private float destroyDistance = 0.5f;

        private ParticleSystem _particleSystem;
        private ParticleSystem.Particle[] _particles;
        private float _timer;
        private bool _isConverging;
        private bool _shouldConverge = true;
        private bool _isLocalSpace;
        private Camera _mainCamera;

        // Charge tracking
        private int _initialParticleCount = 0;
        private int _particlesConvergedThisFrame = 0;
        private float _chargePerParticle = 0f;
        private bool _chargeInitialized = false;

        // Cached target position (fixed at convergence start to avoid moving target)
        private Vector3 _cachedTargetPosition;
        private bool _targetCached = false;

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
                //Debug.Log($"[ParticleConverge] _shouldConverge is false, disabling. ParticleCount: {_particleSystem.particleCount}");
                enabled = false;
                return;
            }

            _timer += Time.deltaTime;

            // Only converge if we should (entity was detected, not a combo reset)
            bool hasValidTarget = useScreenPosition ? (_mainCamera != null) : (targetTransform != null);
            if (hasValidTarget && _timer >= convergeDelay)
            {
                // Initialize charge tracking on first convergence frame
                if (!_chargeInitialized)
                {
                    InitializeChargeTracking();
                }

                _isConverging = true;
                ConvergeParticles();

                // Notify charge manager for each particle that converged this frame
                if (_particlesConvergedThisFrame > 0 && SpinChargeManager.Instance != null)
                {
                    //Debug.Log($"[ParticleConverge] {_particlesConvergedThisFrame} particles converged, adding {_chargePerParticle * _particlesConvergedThisFrame:F4} charge");
                    for (int i = 0; i < _particlesConvergedThisFrame; i++)
                    {
                        SpinChargeManager.Instance.OnParticleConverged(_chargePerParticle);
                    }
                }
            }

            // Destroy when all particles are gone
            if (_isConverging && _particleSystem.particleCount == 0)
            {
                //Debug.Log("[ParticleConverge] All particles gone, destroying gameObject");
                Destroy(gameObject);
            }

            // Debug: Log particle status periodically
            if (_isConverging && Time.frameCount % 30 == 0)
            {
                Vector3 targetPos = _targetCached ? _cachedTargetPosition : GetTargetPosition();
                int pCount = _particleSystem.GetParticles(_particles);
                float avgDist = 0f;
                if (pCount > 0)
                {
                    for (int i = 0; i < pCount; i++)
                    {
                        Vector3 pPos = _isLocalSpace ? transform.TransformPoint(_particles[i].position) : _particles[i].position;
                        // Use 2D distance (ignore Z)
                        Vector2 pPos2D = new Vector2(pPos.x, pPos.y);
                        Vector2 targetPos2D = new Vector2(targetPos.x, targetPos.y);
                        avgDist += Vector2.Distance(pPos2D, targetPos2D);
                    }
                    avgDist /= pCount;
                }
                //Debug.Log($"[ParticleConverge] Status: particleCount={pCount}, convergedThisFrame={_particlesConvergedThisFrame}, cachedTarget={targetPos}, avgDist2D={avgDist:F2}, destroyDist={destroyDistance}");
            }
        }

        private void InitializeChargeTracking()
        {
            _initialParticleCount = _particleSystem.particleCount;

            // Cache the target position at start of convergence so it doesn't move with camera
            _cachedTargetPosition = GetTargetPosition();
            _targetCached = true;

            // Ask SpinChargeManager how much charge this detection is worth, then divide by particle count
            if (SpinChargeManager.Instance != null && _initialParticleCount > 0)
            {
                float totalCharge = SpinChargeManager.Instance.GetChargeAmountForDetection();
                _chargePerParticle = totalCharge / _initialParticleCount;
                //Debug.Log($"[ParticleConverge] Initialized: {_initialParticleCount} particles, {totalCharge:F3} total charge, {_chargePerParticle:F5} per particle, targetPos={_cachedTargetPosition}");
            }
            else
            {
                Debug.LogWarning($"[ParticleConverge] Could not initialize charge tracking. SpinChargeManager: {SpinChargeManager.Instance != null}, ParticleCount: {_initialParticleCount}");
            }

            _chargeInitialized = true;
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
            _particlesConvergedThisFrame = 0;
            int particleCount = _particleSystem.GetParticles(_particles);
            if (particleCount == 0) return;

            // Use cached target position to avoid chasing a moving target
            Vector3 targetPos = _targetCached ? _cachedTargetPosition : GetTargetPosition();
            float deltaTime = Time.deltaTime;
            float lerpFactor = lerpSpeed * deltaTime;
            float minDistance = minSpeed * deltaTime;
            float destroyDistSqr = destroyDistance * destroyDistance;

            int skippedDead = 0;
            int processed = 0;

            for (int i = 0; i < particleCount; i++)
            {
                // Skip particles that are already dead
                if (_particles[i].remainingLifetime <= 0f)
                {
                    skippedDead++;
                    continue;
                }
                processed++;

                // Convert particle position to world space if using local simulation
                Vector3 particleWorldPos = _isLocalSpace
                    ? transform.TransformPoint(_particles[i].position)
                    : _particles[i].position;

                // For 2D games, flatten Z to match target plane for distance calculations
                Vector3 particleWorldPos2D = new Vector3(particleWorldPos.x, particleWorldPos.y, targetPos.z);

                // Use sqrMagnitude to avoid sqrt in distance calculation
                Vector3 toTarget = targetPos - particleWorldPos2D;
                float distanceSqr = toTarget.sqrMagnitude;

                // Check if particle should be destroyed
                if (distanceSqr <= destroyDistSqr)
                {
                    _particles[i].remainingLifetime = 0f;
                    _particlesConvergedThisFrame++;
                }
                else
                {
                    // Keep particle alive while converging - extend lifetime
                    if (_particles[i].remainingLifetime < 1f)
                    {
                        _particles[i].remainingLifetime = 1f;
                    }

                    // Lerp toward target with minimum speed to ensure progress (using 2D position)
                    Vector3 lerpedPos = Vector3.Lerp(particleWorldPos2D, targetPos, lerpFactor);

                    // Calculate if lerp is too slow, use minimum speed instead
                    Vector3 lerpDelta = lerpedPos - particleWorldPos2D;
                    float lerpDistSqr = lerpDelta.sqrMagnitude;
                    float minDistSqr = minDistance * minDistance;

                    Vector3 newWorldPos;
                    if (lerpDistSqr < minDistSqr)
                    {
                        // Use minimum speed - normalize direction manually to avoid extra sqrt
                        float distance = Mathf.Sqrt(distanceSqr);
                        Vector3 direction = toTarget / distance;
                        newWorldPos = particleWorldPos2D + direction * minDistance;
                    }
                    else
                    {
                        newWorldPos = lerpedPos;
                    }

                    // Convert back to local space if needed
                    Vector3 finalPos = _isLocalSpace
                        ? transform.InverseTransformPoint(newWorldPos)
                        : newWorldPos;

                    // Debug first particle movement
                    //if (i == 0 && Time.frameCount % 30 == 0)
                    //{
                    //    Debug.Log($"[ParticleConverge] Particle0: oldPos={_particles[i].position}, newPos={finalPos}, worldOld={particleWorldPos}, worldNew={newWorldPos}, dist={Mathf.Sqrt(distanceSqr):F3}");
                    //}

                    _particles[i].position = finalPos;

                    // Override velocity to prevent original movement
                    _particles[i].velocity = Vector3.zero;
                }
            }

            _particleSystem.SetParticles(_particles, particleCount);

            // Debug: Log processing stats every 30 frames
            //if (Time.frameCount % 30 == 0)
            //{
            //    Debug.Log($"[ParticleConverge] ConvergeParticles: total={particleCount}, processed={processed}, skippedDead={skippedDead}, lerpFactor={lerpFactor:F4}, minDist={minDistance:F4}, isLocal={_isLocalSpace}");
            //}
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
