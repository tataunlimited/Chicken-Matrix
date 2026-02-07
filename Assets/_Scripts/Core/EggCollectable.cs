using System;
using System.Collections;
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

        [Header("Growth Animation")]
        [Tooltip("Starting scale multiplier when egg spawns (0.1 = 10%)")]
        [SerializeField] private float spawnScaleMultiplier = 0.1f;
        [Tooltip("Scale multiplier at end of growth phase before collection window (0.5 = 50%)")]
        [SerializeField] private float preCollectionScaleMultiplier = 0.5f;
        [Tooltip("Duration of the quick scale-up when collection window starts")]
        [SerializeField] private float collectionReadyScaleDuration = 0.15f;

        [Header("Destruction Animation")]
        [Tooltip("Duration of the grow and fade animation")]
        [SerializeField] private float destroyAnimDuration = 0.25f;
        [Tooltip("Scale multiplier at the end of the animation")]
        [SerializeField] private float destroyScaleMultiplier = 2.5f;

        private float _angleRadians;
        private bool _isDestroyed;
        private Vector3 _originalScale;
        private float _growthDuration;
        private float _growthTimer;
        private bool _isGrowing;
        private bool _isCollectionReady;
        private Coroutine _scaleUpCoroutine;

        public event Action<EggCollectable, bool> OnEggDestroyed;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        /// <summary>
        /// Initialize the egg at a specific angle on a radar ring
        /// </summary>
        /// <param name="angleRadians">Angle in radians where the egg spawns</param>
        /// <param name="ringRadius">The base radius of the ring this egg sits on</param>
        public void Init(float angleRadians, float ringRadius)
        {
            _angleRadians = angleRadians;
            baseRadius = ringRadius;
            _originalScale = transform.localScale;
            UpdatePositionForPulse(1f);

            // Start at spawn scale and begin growing
            transform.localScale = _originalScale * spawnScaleMultiplier;
            _isGrowing = true;
            _growthTimer = 0f;

            // Growth duration is the interval minus beat window time
            float interval = GameManager.Instance != null ? GameManager.Instance.interval : 0.5f;
            float beatWindow = EggManager.Instance != null ? EggManager.Instance.BeatWindowTime : 0.15f;
            _growthDuration = interval - beatWindow;
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

            // Handle growth animation (10% to 50% over the interval)
            if (_isGrowing && !_isCollectionReady)
            {
                _growthTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_growthTimer / _growthDuration);

                // Smooth interpolation from spawn scale to pre-collection scale
                float currentScaleMultiplier = Mathf.Lerp(spawnScaleMultiplier, preCollectionScaleMultiplier, t);
                transform.localScale = _originalScale * currentScaleMultiplier;
            }
        }

        /// <summary>
        /// Called when the collection window starts - quickly scale up to full size
        /// </summary>
        public void OnCollectionWindowStart()
        {
            if (_isDestroyed || _isCollectionReady) return;

            _isGrowing = false;
            _isCollectionReady = true;
            _scaleUpCoroutine = StartCoroutine(ScaleUpToFullSize());
        }

        /// <summary>
        /// Smoothly scale up from current size to full size
        /// </summary>
        private IEnumerator ScaleUpToFullSize()
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < collectionReadyScaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / collectionReadyScaleDuration;

                // Ease out for snappy feel
                float easedT = 1f - (1f - t) * (1f - t);

                transform.localScale = Vector3.Lerp(startScale, _originalScale, easedT);
                yield return null;
            }

            transform.localScale = _originalScale;
            _scaleUpCoroutine = null;
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
            StartCoroutine(DestroyAnimation());
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
        /// Animate the egg growing rapidly and fading out
        /// </summary>
        private IEnumerator DestroyAnimation()
        {
            if (spriteRenderer == null)
            {
                Destroy(gameObject);
                yield break;
            }

            float elapsed = 0f;
            Color startColor = spriteRenderer.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            Vector3 targetScale = _originalScale * destroyScaleMultiplier;

            while (elapsed < destroyAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / destroyAnimDuration;

                // Ease out for snappy growth
                float easedT = 1f - (1f - t) * (1f - t);

                // Scale up
                transform.localScale = Vector3.Lerp(_originalScale, targetScale, easedT);

                // Fade out
                spriteRenderer.color = Color.Lerp(startColor, endColor, easedT);

                yield return null;
            }

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
