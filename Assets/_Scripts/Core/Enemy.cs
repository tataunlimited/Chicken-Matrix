using System;
using UnityEngine;

namespace _Scripts.Core
{
    public class Enemy : MonoBehaviour
    {

        public int step = 5;

        private float _stepSize = 20;

        private float _offset = 0.5f;

        private Vector3 _sourcePosition = Vector3.zero;

        public event Action<Enemy> OnDestroyed;

        public void Init(float offset, float stepSize)
        {
            _offset = offset;
            _stepSize = stepSize;
        }

        public void UpdatePosition()
        {
            if (step < 1)
                return;
            step--;

            // 1. Get the direction moving AWAY from the center/source
            // (Ensure _sourcePosition is set to the center of your circle)
            Vector3 direction = (transform.position - _sourcePosition).normalized;

            // 2. Calculate the new distance: (Base Step Distance) + Offset
            float distance = (_stepSize * step) + _offset;

            // 3. Set the new position
            transform.position = _sourcePosition + (direction * distance);
            
            if(step == 0) DestroyEnemy();
        }

        private void DestroyEnemy()
        {
            OnDestroyed?.Invoke(this);

            Destroy(gameObject,0.2f);

        }
    }
}