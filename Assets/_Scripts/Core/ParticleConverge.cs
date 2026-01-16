using UnityEngine;

namespace _Scripts.Core
{
    public class ParticleConverge : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform targetTransform;

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
        }

        private void Update()
        {
            if (_particleSystem == null) return;

            _timer += Time.deltaTime;

            // Only converge if we should (entity was detected, not a combo reset)
            if (_shouldConverge && targetTransform != null && _timer >= convergeDelay)
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

        private void ConvergeParticles()
        {
            int particleCount = _particleSystem.GetParticles(_particles);

            Vector3 targetPos = targetTransform.position;

            for (int i = 0; i < particleCount; i++)
            {
                // Convert particle position to world space if using local simulation
                Vector3 particleWorldPos = _particleSystem.main.simulationSpace == ParticleSystemSimulationSpace.Local
                    ? transform.TransformPoint(_particles[i].position)
                    : _particles[i].position;

                float distance = Vector3.Distance(particleWorldPos, targetPos);

                // Check if particle should be destroyed
                if (distance <= destroyDistance)
                {
                    _particles[i].remainingLifetime = 0f;
                }
                else
                {
                    // Lerp toward target with minimum speed to ensure progress
                    Vector3 lerpedPos = Vector3.Lerp(particleWorldPos, targetPos, lerpSpeed * Time.deltaTime);

                    // Calculate if lerp is too slow, use minimum speed instead
                    float lerpDistance = Vector3.Distance(particleWorldPos, lerpedPos);
                    float minDistance = minSpeed * Time.deltaTime;

                    Vector3 newWorldPos;
                    if (lerpDistance < minDistance)
                    {
                        // Use minimum speed
                        Vector3 direction = (targetPos - particleWorldPos).normalized;
                        newWorldPos = particleWorldPos + direction * minDistance;
                    }
                    else
                    {
                        newWorldPos = lerpedPos;
                    }

                    // Convert back to local space if needed
                    if (_particleSystem.main.simulationSpace == ParticleSystemSimulationSpace.Local)
                    {
                        _particles[i].position = transform.InverseTransformPoint(newWorldPos);
                    }
                    else
                    {
                        _particles[i].position = newWorldPos;
                    }

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
    }
}
