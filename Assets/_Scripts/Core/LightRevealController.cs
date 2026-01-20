using UnityEngine;

namespace _Scripts.Core
{
    [RequireComponent(typeof(SpriteMask))]
    public class LightRevealController : MonoBehaviour
    {
        [Header("Light Control")]
        [Tooltip("Toggle the light on/off")]
        public bool isLightOn = true;

        [Header("Edge Softness")]
        [Range(0f, 1f)]
        [Tooltip("0 = Hard edge, 1 = Soft/feathered edge")]
        public float edgeSoftness = 0.5f;

        private SpriteMask _spriteMask;
        private SpriteRenderer _spriteRenderer;

        // Cached range values for softness to sprite mask range conversion
        private const float MinRange = 0.01f;
        private const float MaxRange = 1f;

        void Awake()
        {
            _spriteMask = GetComponent<SpriteMask>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            // Copy sprite from SpriteRenderer to SpriteMask if available
            if (_spriteRenderer != null && _spriteRenderer.sprite != null)
            {
                _spriteMask.sprite = _spriteRenderer.sprite;
                //Debug.Log($"LightRevealController: Copied sprite '{_spriteRenderer.sprite.name}' to SpriteMask");
            }
            else
            {
                Debug.LogWarning("LightRevealController: No SpriteRenderer or sprite found to copy!");
            }

            // Initialize the sprite mask
            _spriteMask.alphaCutoff = 0.01f;
            _spriteMask.isCustomRangeActive = true;

            // Set the sorting order range for the mask
            // The mask needs to affect sprites on the "Default" sorting layer
            // with sorting orders between back and front
            int defaultLayerID = SortingLayer.NameToID("Default");
            _spriteMask.frontSortingLayerID = defaultLayerID;
            _spriteMask.backSortingLayerID = defaultLayerID;
            _spriteMask.frontSortingOrder = 10000; // Very high to catch everything
            _spriteMask.backSortingOrder = 0; // Start from 0

            //Debug.Log($"LightRevealController: Initialized. Light is {(isLightOn ? "ON" : "OFF")}");
            //Debug.Log($"LightRevealController: SpriteMask range - Back: {_spriteMask.backSortingOrder}, Front: {_spriteMask.frontSortingOrder}");
        }

        void Start()
        {
            UpdateLightState();

            // Check sorting order vs blackout overlay
            if (_spriteRenderer != null)
            {
                //Debug.Log($"LightRevealController: Sprite sorting layer '{_spriteRenderer.sortingLayerName}', order {_spriteRenderer.sortingOrder}");
                //Debug.Log($"LightRevealController: SpriteMask should affect sorting orders {_spriteMask.backSortingOrder} to {_spriteMask.frontSortingOrder}");
                //Debug.Log($"LightRevealController: Triangle position: {transform.position}");
            }

            // Debug check - is the mask enabled?
            //Debug.Log($"LightRevealController: SpriteMask enabled: {_spriteMask.enabled}, has sprite: {_spriteMask.sprite != null}");
        }

        void Update()
        {
            UpdateSoftness();
        }

        /// <summary>
        /// Turn the light on
        /// </summary>
        public void TurnOn()
        {
            isLightOn = true;
            UpdateLightState();
        }

        /// <summary>
        /// Turn the light off
        /// </summary>
        public void TurnOff()
        {
            isLightOn = false;
            UpdateLightState();
        }

        /// <summary>
        /// Toggle the light on/off
        /// </summary>
        public void Toggle()
        {
            isLightOn = !isLightOn;
            UpdateLightState();
        }

        private void UpdateLightState()
        {
            _spriteMask.enabled = isLightOn;
        }

        private void UpdateSoftness()
        {
            if (_spriteMask == null) return;

            // Convert edgeSoftness (0-1) to sprite mask range
            // Higher softness = larger range = softer edge
            float range = Mathf.Lerp(MinRange, MaxRange, edgeSoftness);
            _spriteMask.frontSortingOrder = 1;
            _spriteMask.backSortingOrder = -1;

            // Use the sprite's alpha channel for soft edges
            // Range controls how much of the alpha gradient is used
            _spriteMask.alphaCutoff = Mathf.Lerp(0.5f, 0.01f, edgeSoftness);
        }

        /// <summary>
        /// Change the sprite used for the light mask
        /// </summary>
        public void SetSprite(Sprite newSprite)
        {
            if (_spriteMask != null)
            {
                _spriteMask.sprite = newSprite;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = newSprite;
            }
        }

        /// <summary>
        /// Get the current sprite
        /// </summary>
        public Sprite GetSprite()
        {
            return _spriteMask?.sprite;
        }
    }
}
