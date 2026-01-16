using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Scripts.Core
{
    public enum DetectionMode {None, Friendly, Aggressive}
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance;
        public Collider2D detectionCollider;
        public SpriteRenderer spriteRenderer;
        
        public Color enemyDetectionColor;
        public Color allyDetectionColor;
        public Color enemyPulseColor;
        public Color allyPulseColor;
        public Color defaultColor;
        
        private bool _isPulsing;
        
        public float detectionInterval = 0.1f;

        private Enemy _detectedEnemy;
        private DetectionMode _detectionMode;
        void Awake()
        {
            Instance = this;
            detectionCollider.enabled = false;
            spriteRenderer.color = defaultColor;

        }
    

        void Update()
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Vector2 direction = mousePosition - transform.position;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            if(_isPulsing)
                return;
            if (Input.GetMouseButton(0))
            {
                _detectionMode = DetectionMode.Aggressive;
            } 
            else if (Input.GetMouseButton(1))
            {
                _detectionMode = DetectionMode.Friendly;
            }
            else
            {
                _detectionMode = DetectionMode.None;
            }
            UpdateColor();
        }

        public void UpdateInterval()
        {
            StartCoroutine(DetectionCoroutine());
        }

        private IEnumerator DetectionCoroutine()
        {
            detectionCollider.enabled = true;
            _isPulsing = true;
            UpdateColor();
            var cloneSprite = Instantiate(spriteRenderer, spriteRenderer.transform.position, spriteRenderer.transform.rotation);
            Destroy(cloneSprite.GetComponent<Collider2D>());
            var tweenColor = cloneSprite.color;
            tweenColor.a = 0;
            cloneSprite.DOColor(tweenColor, .5f).SetEase(Ease.Flash);

            yield return new WaitForSeconds(detectionInterval);
            detectionCollider.enabled = false;
            _isPulsing = false;
        }

        void UpdateColor()
        {
            switch (_detectionMode)
            {
                case DetectionMode.None:
                    spriteRenderer.color = defaultColor;
                    break;
                case DetectionMode.Friendly:
                    spriteRenderer.color = _isPulsing? allyPulseColor: allyDetectionColor;
                    break;
                case DetectionMode.Aggressive:
                    spriteRenderer.color = _isPulsing ? enemyPulseColor : enemyDetectionColor;
                    break;
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            switch (_detectionMode)
            {
                case DetectionMode.None:
                    break;
                case DetectionMode.Friendly:
                    if (other.TryGetComponent(out Ally ally))
                    {
                        ally.Destroy();
                    }
                    break;
                case DetectionMode.Aggressive:
                    if (other.TryGetComponent(out Enemy enemy))
                    {
                        enemy.Destroy();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
           
        }
    }
}
