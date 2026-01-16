using UnityEngine;

namespace _Scripts.Core
{
    /// <summary>
    /// Manages the fog of war/light reveal system.
    /// Attach this to a GameObject in your scene (only one instance needed).
    /// This sets up the rendering layers to show only lit areas.
    /// </summary>
    public class LightRevealManager : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("The main camera rendering your game")]
        public Camera mainCamera;

        [Header("Blackout Overlay")]
        [Tooltip("Enable/disable the blackout overlay for debugging")]
        public bool enableBlackout = true;

        [Tooltip("Color of the blackout (typically pure black)")]
        public Color blackoutColor = Color.black;

        [Tooltip("Sorting layer for the blackout overlay")]
        public string blackoutSortingLayer = "Default";

        [Tooltip("Order in layer for the blackout overlay (should be above everything)")]
        public int blackoutSortingOrder = 1000;

        private GameObject _blackoutOverlay;
        private SpriteRenderer _blackoutRenderer;
        private Sprite _blackoutSprite;

        void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                Debug.LogError("LightRevealManager: No main camera found! Please assign a camera.");
                return;
            }

            SetupBlackoutOverlay();
        }

        void LateUpdate()
        {
            UpdateBlackoutOverlaySize();

            // Toggle blackout visibility
            if (_blackoutRenderer != null && _blackoutRenderer.enabled != enableBlackout)
            {
                _blackoutRenderer.enabled = enableBlackout;
            }
        }

        private void SetupBlackoutOverlay()
        {
            // Create a sprite-based overlay
            _blackoutOverlay = new GameObject("Blackout Overlay");
            _blackoutOverlay.transform.SetParent(transform);

            // Create a white square sprite dynamically
            _blackoutSprite = CreateSquareSprite();

            // Add sprite renderer
            _blackoutRenderer = _blackoutOverlay.AddComponent<SpriteRenderer>();
            _blackoutRenderer.sprite = _blackoutSprite;
            _blackoutRenderer.color = blackoutColor;

            // CRITICAL: Set mask interaction to "Visible Outside Mask"
            // This means the sprite is hidden where the mask is, and visible everywhere else
            _blackoutRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;

            // Setup sorting
            _blackoutRenderer.sortingLayerName = blackoutSortingLayer;
            _blackoutRenderer.sortingOrder = blackoutSortingOrder;

            // Position it
            UpdateBlackoutOverlaySize();

            Debug.Log($"LightRevealManager: Blackout overlay created at sorting order {blackoutSortingOrder}");
            Debug.Log($"LightRevealManager: Mask interaction set to VisibleOutsideMask");
            Debug.Log($"LightRevealManager: Blackout position will be: {_blackoutOverlay.transform.position}");
        }

        private void UpdateBlackoutOverlaySize()
        {
            if (_blackoutOverlay == null || mainCamera == null) return;

            // For orthographic camera (typical for 2D games)
            if (mainCamera.orthographic)
            {
                float height = mainCamera.orthographicSize * 2f;
                float width = height * mainCamera.aspect;

                // Position at camera position (z slightly behind to ensure it renders)
                Vector3 cameraPos = mainCamera.transform.position;
                _blackoutOverlay.transform.position = new Vector3(cameraPos.x, cameraPos.y, cameraPos.z + 1f);
                _blackoutOverlay.transform.localScale = new Vector3(width, height, 1f);
            }
            else
            {
                // For perspective camera
                float distance = 10f;
                float height = 2f * distance * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float width = height * mainCamera.aspect;

                _blackoutOverlay.transform.position = mainCamera.transform.position + mainCamera.transform.forward * distance;
                _blackoutOverlay.transform.rotation = mainCamera.transform.rotation;
                _blackoutOverlay.transform.localScale = new Vector3(width, height, 1f);
            }
        }

        private Sprite CreateSquareSprite()
        {
            // Create a 1x1 white texture
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            // Create sprite from texture
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f),
                1f
            );
            sprite.name = "Blackout Square";

            return sprite;
        }

        /// <summary>
        /// Call this if you change the camera at runtime
        /// </summary>
        public void UpdateCamera(Camera newCamera)
        {
            mainCamera = newCamera;
        }

        /// <summary>
        /// Change the blackout color
        /// </summary>
        public void SetBlackoutColor(Color color)
        {
            blackoutColor = color;
            if (_blackoutRenderer != null)
            {
                _blackoutRenderer.color = color;
            }
        }

        void OnDestroy()
        {
            if (_blackoutOverlay != null)
            {
                Destroy(_blackoutOverlay);
            }

            if (_blackoutSprite != null)
            {
                Destroy(_blackoutSprite.texture);
                Destroy(_blackoutSprite);
            }
        }
    }
}
