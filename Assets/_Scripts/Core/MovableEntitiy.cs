using System;
using System.Collections;
using UnityEngine;

namespace _Scripts.Core
{
    public class MovableEntitiy : MonoBehaviour
    {
        public int currentStep = 4;

        private float _stepSize = 20;

        private float _offset = 0.5f;

        private Vector3 _sourcePosition = Vector3.zero;

        public event Action<MovableEntitiy> OnDestroyed;

        public bool Detected => _detected;

        private bool _detected;
        private bool _permanentlyRevealed;

        public bool IsPermanentlyRevealed => _permanentlyRevealed;

        [SerializeField] private float lerpDuration = 0.15f;
        private Vector3 _direction;
        private bool _isLerping;

        [Header("Destruction Particles")]
        [SerializeField] private ParticleSystem destructionParticlePrefab;
        [SerializeField] private Color particleColor = Color.white;
        [SerializeField] private int particleBurstCount = 15;

        public void Init(float offset, float stepSize)
        {
            _offset = offset;
            _stepSize = stepSize;
            _direction = (transform.position - _sourcePosition).normalized;
        }

        public void UpdatePosition()
        {
            if (currentStep < 1)
                return;
            currentStep --;

            // Calculate direction once if not set
            if (_direction == Vector3.zero)
                _direction = (transform.position - _sourcePosition).normalized;

            // Calculate the new distance: (Base Step Distance) + Offset
            float distance = (_stepSize * currentStep) + _offset;

            // Calculate target position
            Vector3 targetPosition = _sourcePosition + (_direction * distance);

            // Start smooth lerp to target
            StartCoroutine(LerpToPosition(targetPosition, currentStep == 0));
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

        /// <summary>
        /// Push the entity back by one step. Returns true if entity was destroyed (exceeded max step).
        /// </summary>
        public bool PushBack(int maxStep)
        {
            currentStep++;
            if (currentStep > maxStep)
            {
                Destroy(false);
                return true;
            }

            // Recalculate position moving outward
            float distance = (_stepSize * currentStep) + _offset;
            Vector3 targetPosition = _sourcePosition + (_direction * distance);
            StartCoroutine(LerpToPosition(targetPosition, false));
            return false;
        }

        /// <summary>
        /// Permanently reveal this entity by setting its sorting order
        /// </summary>
        public void RevealPermanently(int sortingOrder)
        {
            _permanentlyRevealed = true;
            var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = sortingOrder;
            }
        }

        public void Destroy(bool detected = false)
        {
            _detected = detected;
            OnDestroyed?.Invoke(this);

            SpawnDestructionParticles(detected);

            // Play death sound effect
            SoundController.Instance?.PlayDeathSound();

            GetComponentInChildren<SpriteRenderer>().color = Color.ghostWhite;
            Destroy(gameObject,0.2f);
        }

        private void SpawnDestructionParticles(bool wasDetected)
        {
            if (destructionParticlePrefab == null) return;

            var particles = Instantiate(destructionParticlePrefab, transform.position, Quaternion.identity);

            // Stop any auto-play and disable emission to prevent extra bursts
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.startColor = particleColor;
            main.playOnAwake = false;
            main.loop = false;

            // Configure convergence based on whether entity was detected
            var converge = particles.GetComponent<ParticleConverge>();
            if (converge != null)
            {
                converge.SetShouldConverge(wasDetected);
            }

            // Manually emit particles
            particles.Emit(particleBurstCount);

            // Only auto-destroy if not converging (converge script handles its own cleanup)
            if (converge == null || !wasDetected)
            {
                Destroy(particles.gameObject, main.duration + main.startLifetime.constantMax);
            }
        }
    }
}
