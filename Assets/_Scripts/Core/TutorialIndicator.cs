using UnityEngine;

namespace _Scripts.Core
{
    /// <summary>
    /// Tutorial indicator that follows an entity, appears above it, and blinks (fades in/out).
    /// Automatically destroys itself when the target entity is destroyed.
    /// Always maintains upward orientation regardless of camera or entity rotation.
    /// </summary>
    public class TutorialIndicator : MonoBehaviour
    {
        [Header("Follow Settings")]
        [Tooltip("Vertical offset above the target entity (in world space)")]
        [SerializeField] private float verticalOffset = 1.5f;

        [Header("Blink Settings")]
        [Tooltip("Duration of one fade cycle (in + out)")]
        [SerializeField] private float blinkCycleDuration = 1f;
        [Tooltip("Minimum alpha when faded out")]
        [SerializeField] private float minAlpha = 0.2f;
        [Tooltip("Maximum alpha when faded in")]
        [SerializeField] private float maxAlpha = 1f;

        private MovableEntitiy _targetEntity;
        private SpriteRenderer _spriteRenderer;
        private float _blinkTimer;
        private Camera _mainCamera;

        /// <summary>
        /// Initialize the indicator to follow a specific entity.
        /// </summary>
        public void Initialize(MovableEntitiy targetEntity)
        {
            _targetEntity = targetEntity;
            _mainCamera = Camera.main;
            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_targetEntity != null)
            {
                // Subscribe to the entity's destruction event
                _targetEntity.OnDestroyed += OnEntityDestroyed;

                // Set initial position and rotation
                UpdatePosition();
            }
        }

        private void Update()
        {
            if (_targetEntity == null)
            {
                // Target was destroyed, clean up
                Destroy(gameObject);
                return;
            }

            UpdatePosition();
            UpdateBlink();
        }

        private void UpdatePosition()
        {
            if (_targetEntity != null)
            {
                Vector3 targetPos = _targetEntity.transform.position;
                transform.position = targetPos + Vector3.up * verticalOffset;

                // Counter the camera's Z rotation so the sprite always appears upright on screen
                if (_mainCamera != null)
                {
                    float cameraZ = _mainCamera.transform.eulerAngles.z;
                    transform.rotation = Quaternion.Euler(0f, 0f, -cameraZ);
                }
            }
        }

        private void UpdateBlink()
        {
            if (_spriteRenderer == null) return;

            _blinkTimer += Time.deltaTime;

            // Use a sine wave for smooth fading
            float t = (_blinkTimer / blinkCycleDuration) * Mathf.PI * 2f;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(t) + 1f) / 2f);

            Color color = _spriteRenderer.color;
            color.a = alpha;
            _spriteRenderer.color = color;
        }

        private void OnEntityDestroyed(MovableEntitiy entity)
        {
            // Unsubscribe and destroy
            if (_targetEntity != null)
            {
                _targetEntity.OnDestroyed -= OnEntityDestroyed;
            }
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // Clean up event subscription if still connected
            if (_targetEntity != null)
            {
                _targetEntity.OnDestroyed -= OnEntityDestroyed;
            }
        }
    }
}
