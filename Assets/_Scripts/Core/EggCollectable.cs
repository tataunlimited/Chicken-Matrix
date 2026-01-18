using System;
using UnityEngine;

namespace _Scripts.Core
{
    /// <summary>
    /// Represents a collectable egg that spawns on radar rings and moves with the pulse.
    /// Player must click on the egg within the beat window to collect it.
    /// </summary>
    public class EggCollectable : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Pulse Sync")]
        [Tooltip("Base distance from center (matches a radar ring)")]
        [SerializeField] private float baseRadius = 4f;

        private float _angleRadians;
        private bool _isDestroyed;

        public event Action<EggCollectable, bool> OnEggDestroyed;

        /// <summary>
        /// Initialize the egg at a specific angle on a radar ring
        /// </summary>
        /// <param name="angleRadians">Angle in radians where the egg spawns</param>
        /// <param name="ringRadius">The base radius of the ring this egg sits on</param>
        public void Init(float angleRadians, float ringRadius)
        {
            _angleRadians = angleRadians;
            baseRadius = ringRadius;
            UpdatePositionForPulse(1f);
        }

        private void Update()
        {
            if (_isDestroyed) return;

            // Sync position with radar pulse scale
            float scaleMultiplier = 1f;
            if (RadarBackgroundGenerator.Instance != null)
            {
                scaleMultiplier = RadarBackgroundGenerator.Instance.CurrentScaleMultiplier;
            }

            UpdatePositionForPulse(scaleMultiplier);
        }

        /// <summary>
        /// Update position based on the current pulse scale multiplier
        /// </summary>
        private void UpdatePositionForPulse(float scaleMultiplier)
        {
            float currentRadius = baseRadius * scaleMultiplier;
            float x = Mathf.Cos(_angleRadians) * currentRadius;
            float y = Mathf.Sin(_angleRadians) * currentRadius;
            transform.position = new Vector3(x, y, 0);
        }

        /// <summary>
        /// Called when the egg is successfully collected (clicked on beat)
        /// </summary>
        public void Collect()
        {
            if (_isDestroyed) return;
            _isDestroyed = true;

            OnEggDestroyed?.Invoke(this, true);
            Destroy(gameObject);
        }

        /// <summary>
        /// Called when the egg is missed (pulse passed without collection)
        /// </summary>
        public void Miss()
        {
            if (_isDestroyed) return;
            _isDestroyed = true;

            OnEggDestroyed?.Invoke(this, false);
            Destroy(gameObject);
        }

        /// <summary>
        /// Get the angle this egg is positioned at
        /// </summary>
        public float AngleRadians => _angleRadians;

        /// <summary>
        /// Get the base radius this egg sits on
        /// </summary>
        public float BaseRadius => baseRadius;
    }
}
