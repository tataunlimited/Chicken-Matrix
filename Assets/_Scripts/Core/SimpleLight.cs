using UnityEngine;

namespace _Scripts.Core
{
    /// <summary>
    /// Simple light revealer component. Add this to any sprite to make it reveal areas.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SimpleLight : MonoBehaviour
    {
        [Header("Light Control")]
        [Tooltip("Is the light currently on?")]
        public bool isLightOn = true;

        private SpriteRenderer _spriteRenderer;

        void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            UpdateLightState();
        }

        void Update()
        {
            // Keep light state synced
            if (_spriteRenderer.enabled != isLightOn)
            {
                UpdateLightState();
            }
        }

        public void TurnOn()
        {
            isLightOn = true;
            UpdateLightState();
        }

        public void TurnOff()
        {
            isLightOn = false;
            UpdateLightState();
        }

        public void Toggle()
        {
            isLightOn = !isLightOn;
            UpdateLightState();
        }

        private void UpdateLightState()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = isLightOn;
            }
        }
    }
}
