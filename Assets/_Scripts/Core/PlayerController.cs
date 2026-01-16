using System.Collections;
using UnityEngine;

namespace _Scripts.Core
{
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance;
        public Collider2D detectionCollider;
        public SpriteRenderer spriteRenderer;
        
        public Color detectionColor;
        public Color defaultColor;
        
        public float detectionInterval = 0.1f;

        private Enemy _detectedEnemy;
        
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
        }

        public void UpdateInterval()
        {
            StartCoroutine(DetectionCoroutine());
        }

        private IEnumerator DetectionCoroutine()
        {
            detectionCollider.enabled = true;
            spriteRenderer.color = detectionColor;
            yield return new WaitForSeconds(detectionInterval);
            spriteRenderer.color = defaultColor;
            detectionCollider.enabled = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Enemy enemy))
            {
                enemy.DestroyEnemy();
                
            }
        }
    }
}
