using System;
using System.Collections;
using UnityEngine;

namespace _Scripts.Core
{
    public class MovableEntitiy : MonoBehaviour
    {
        public int step = 4;

        private float _stepSize = 20;

        private float _offset = 0.5f;

        private Vector3 _sourcePosition = Vector3.zero;

        public event Action<MovableEntitiy> OnDestroyed;

        public bool Detected => _detected;

        private bool _detected;

        [SerializeField] private float lerpDuration = 0.15f;
        private Vector3 _direction;
        private bool _isLerping;

        public void Init(float offset, float stepSize)
        {
            _offset = offset;
            _stepSize = stepSize;
            _direction = (transform.position - _sourcePosition).normalized;
        }

        public void UpdatePosition()
        {
            if (step < 1)
                return;
            step--;

            // Calculate direction once if not set
            if (_direction == Vector3.zero)
                _direction = (transform.position - _sourcePosition).normalized;

            // Calculate the new distance: (Base Step Distance) + Offset
            float distance = (_stepSize * step) + _offset;

            // Calculate target position
            Vector3 targetPosition = _sourcePosition + (_direction * distance);

            // Start smooth lerp to target
            StartCoroutine(LerpToPosition(targetPosition, step == 0));
        }

        private IEnumerator LerpToPosition(Vector3 targetPosition, bool destroyAfter)
        {
            _isLerping = true;
            Vector3 startPosition = transform.position;
            float elapsed = 0f;

            while (elapsed < lerpDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lerpDuration;
                t = t * t * (3f - 2f * t); // Smoothstep for snappy feel
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            transform.position = targetPosition;
            _isLerping = false;

            if (destroyAfter)
                Destroy();
        }

        public void Destroy(bool detected = false)
        {
            _detected = detected;
            OnDestroyed?.Invoke(this);

            GetComponentInChildren<SpriteRenderer>().color = Color.ghostWhite;
            Destroy(gameObject,0.2f);

        }
    }
}
