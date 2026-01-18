using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace _Scripts.Core
{
    public enum DetectionMode {None, Friendly, Aggressive}
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance;
        public Collider2D detectionCollider;
        public Collider2D mirrorDetectionCollider;
        public SpriteRenderer spriteRenderer;
        public SpriteRenderer mirrorSpriteRenderer;
        public Collider2D neutralDetector;
        public Collider2D mirrorNeutralDetector;
        public Transform rangeVisualIndicatorParent;
        public GameObject mirrorRangeVisualIndicator;
        public GameObject mirrorRadarLine;
        
        private bool _isMirroring;
        
        public List<float> rangeScaleMultipliers = new List<float>();

        public event Action OnPulse;
        
        public Color enemyDetectionColor;
        public Color allyDetectionColor;
        public Color enemyPulseColor;
        public Color allyPulseColor;
        public Color defaultColor;
        
        private bool _isPulsing;
        
        public float detectionInterval = 0.1f;

        private Enemy _detectedEnemy;
        private DetectionMode _detectionMode;
        
        private int _range = 0;
        void Awake()
        {
            Instance = this;
            detectionCollider.enabled = false;
            Color color = Color.black;
            color.a = 0;
            spriteRenderer.color = color;
            mirrorSpriteRenderer.color = color;

        }
    

        void Update()
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Vector2 direction = mousePosition - transform.position;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            // if (Input.GetKeyDown(KeyCode.A))
            // {
            //     UpdateRange(_range+1);
            // }
            if(_isPulsing)
                return;
            if (Input.GetMouseButton(0))
            {
                _detectionMode = DetectionMode.Aggressive;
                neutralDetector.gameObject.SetActive(false);
                if (_isMirroring)
                {
                    mirrorNeutralDetector.gameObject.SetActive(false);
                }

            } 
            else if (Input.GetMouseButton(1))
            {
                _detectionMode = DetectionMode.Friendly;
                neutralDetector.gameObject.SetActive(false);
                if (_isMirroring)
                {
                    mirrorNeutralDetector.gameObject.SetActive(false);
                }
            }
            else
            {
                _detectionMode = DetectionMode.None;
                neutralDetector.gameObject.SetActive(true);
                if (_isMirroring)
                {
                    mirrorNeutralDetector.gameObject.SetActive(true);
                }
            }
            //UpdateColor();
        }

        public void UpdateInterval()
        {
            StartCoroutine(DetectionCoroutine());
        }

        public void UpdateRange(int range)
        {
            
            if (range >= rangeScaleMultipliers.Count) return;
            _range = range;
            detectionCollider.transform.localScale = Vector3.one * rangeScaleMultipliers[range]; 
            mirrorDetectionCollider.transform.localScale = Vector3.one * rangeScaleMultipliers[range];
            rangeVisualIndicatorParent.localScale = Vector3.one * rangeScaleMultipliers[range];
            
        }

        public void EnableKonamiMode()
        {
            UpdateRange(3);
            mirrorSpriteRenderer.gameObject.SetActive(true);
            mirrorDetectionCollider.gameObject.SetActive(true);
            mirrorRangeVisualIndicator.SetActive(true);
            mirrorRadarLine.SetActive(true);;
            _isMirroring = true;
        }
        private IEnumerator DetectionCoroutine()
        {
            detectionCollider.enabled = true;
            neutralDetector.enabled = true;
            mirrorDetectionCollider.enabled = true;
            mirrorNeutralDetector.enabled = true;
            _isPulsing = true;
            OnPulse?.Invoke();
            var cloneSprite = Instantiate(spriteRenderer, spriteRenderer.transform.position, spriteRenderer.transform.rotation);
            UpdateColor(cloneSprite);
            var tweenColor = cloneSprite.color;
            tweenColor.a = 0;
            cloneSprite.DOColor(tweenColor, .5f).SetEase(Ease.Flash).onComplete += () => Destroy(cloneSprite.gameObject);
            if (_isMirroring)
            {
                var cloneMirrorSprite = Instantiate(mirrorSpriteRenderer, mirrorSpriteRenderer.transform.position, mirrorSpriteRenderer.transform.rotation);
                UpdateColor(cloneMirrorSprite);
                cloneMirrorSprite.DOColor(tweenColor, .5f).SetEase(Ease.Flash).onComplete += () => Destroy(cloneMirrorSprite.gameObject);
            }
            
            yield return new WaitForSeconds(detectionInterval);
            detectionCollider.enabled = false;
            neutralDetector.enabled = false;
            mirrorDetectionCollider.enabled = false;
            mirrorNeutralDetector.enabled = false;
            _isPulsing = false;
        }

        void UpdateColor(SpriteRenderer rendere)
        {
            if (!_isPulsing)
            {
                Color color = Color.black;
                color.a = 0;
                rendere.color = color;
                return;
            }
            switch (_detectionMode)
            {
                case DetectionMode.None:
                    rendere.color = defaultColor;
                    break;
                case DetectionMode.Friendly:
                    rendere.color = _isPulsing? allyPulseColor: allyDetectionColor;
                    break;
                case DetectionMode.Aggressive:
                    rendere.color = _isPulsing ? enemyPulseColor : enemyDetectionColor;
                    break;
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            switch (_detectionMode)
            {
                case DetectionMode.None:
                    if (other.TryGetComponent(out Neutral neutral))
                    {
                        neutral.Destroy(true);
                    }
                    break;
                case DetectionMode.Friendly:
                    if (other.TryGetComponent(out Ally ally))
                    {
                        ally.Destroy(true);
                    }
                    break;
                case DetectionMode.Aggressive:
                    if (other.TryGetComponent(out Enemy enemy))
                    {
                        enemy.Destroy(true);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
           
        }

    }
}
