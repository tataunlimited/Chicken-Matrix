using UnityEngine;

namespace _Scripts.Core
{
    public class PlayerController : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            // 1. Get the mouse position in World Space
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // 2. Calculate the direction vector from the triangle to the mouse
            Vector2 direction = mousePosition - transform.position;

            // 3. Calculate the angle in degrees
            // Mathf.Atan2 returns radians, so we convert to degrees
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // 4. Apply the rotation
            // If your triangle "tip" points Up by default, subtract 90 from the angle
            transform.rotation = Quaternion.Euler(0, 0, angle);
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
