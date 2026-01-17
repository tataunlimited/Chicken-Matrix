using UnityEngine;

namespace _Scripts.Core
{
    public class DecisionTriangleParticles : MonoBehaviour
    {
        [Header("Particle Settings")]
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private int burstCount = 10;

        private ParticleSystem.MainModule mainModule;
        private ParticleSystem.TrailModule trailModule;

        private void Awake()
        {
            if (particles == null)
                particles = GetComponent<ParticleSystem>();

            if (particles != null)
            {
                mainModule = particles.main;
                trailModule = particles.trails;
            }
        }

        private void Start()
        {
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.OnPulse += BurstParticles;
            }
        }

        private void OnDestroy()
        {
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.OnPulse -= BurstParticles;
            }
        }

        /// <summary>
        /// Call this to burst particles with the current radar line color
        /// </summary>
        public void BurstParticles()
        {
            if (particles == null) return;

            Color currentColor = GetCurrentRadarColor();
            SetParticleColor(currentColor);

            particles.Emit(burstCount);
        }

        private Color GetCurrentRadarColor()
        {
            if (SCRIPT_RadarLineController.Instance != null)
            {
                var radarRenderer = SCRIPT_RadarLineController.Instance.GetComponent<SpriteRenderer>();
                if (radarRenderer != null)
                {
                    return radarRenderer.color;
                }
            }
            return Color.white;
        }

        private void SetParticleColor(Color color)
        {
            if (particles == null) return;

            // Set main particle color
            mainModule.startColor = color;

            // Set trail color if trails are enabled
            if (trailModule.enabled)
            {
                trailModule.colorOverLifetime = color;
            }
        }
    }
}
