using UnityEngine;

namespace _Scripts.Core
{
    /// <summary>
    /// Alternative light reveal system using render textures.
    /// This approach is more reliable than sprite masks.
    /// </summary>
    public class LightRevealSystem_V2 : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("The main camera rendering your game")]
        public Camera mainCamera;

        [Header("Mask Camera Setup")]
        [Tooltip("Layer for light sprites (create a new layer called 'LightMask')")]
        public LayerMask lightMaskLayer;

        [Header("Blackout Settings")]
        [Tooltip("Color of the blackout")]
        public Color blackoutColor = Color.black;

        [Tooltip("Sorting order for the blackout overlay")]
        public int blackoutSortingOrder = 1000;

        [Tooltip("Feather/blur amount for light edges (0 = hard, higher = softer)")]
        [Range(0f, 10f)]
        public float edgeBlur = 2f;

        private Camera _maskCamera;
        private RenderTexture _maskRenderTexture;
        private GameObject _blackoutOverlay;
        private SpriteRenderer _blackoutRenderer;
        private Material _blackoutMaterial;

        void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                Debug.LogError("LightRevealSystem_V2: No main camera found!");
                return;
            }

            SetupMaskCamera();
            SetupBlackoutOverlay();
        }

        void LateUpdate()
        {
            UpdateCameraSettings();
        }

        private void SetupMaskCamera()
        {
            // Create a camera that renders only the light sprites
            GameObject maskCameraObj = new GameObject("Light Mask Camera");
            maskCameraObj.transform.SetParent(transform);

            _maskCamera = maskCameraObj.AddComponent<Camera>();
            _maskCamera.CopyFrom(mainCamera);

            // Camera only renders the light mask layer
            _maskCamera.cullingMask = lightMaskLayer;
            _maskCamera.clearFlags = CameraClearFlags.SolidColor;
            _maskCamera.backgroundColor = Color.black;
            _maskCamera.depth = mainCamera.depth - 1; // Render before main camera

            // Create render texture for the mask
            _maskRenderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _maskRenderTexture.name = "Light Mask RT";
            _maskCamera.targetTexture = _maskRenderTexture;

            Debug.Log("LightRevealSystem_V2: Mask camera created");
        }

        private void SetupBlackoutOverlay()
        {
            // Create the blackout overlay sprite
            _blackoutOverlay = new GameObject("Blackout Overlay V2");
            _blackoutOverlay.transform.SetParent(transform);

            // Create a simple white sprite
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);

            _blackoutRenderer = _blackoutOverlay.AddComponent<SpriteRenderer>();
            _blackoutRenderer.sprite = sprite;
            _blackoutRenderer.sortingOrder = blackoutSortingOrder;

            // Create material that uses the mask texture
            _blackoutMaterial = new Material(Shader.Find("Sprites/Default"));
            _blackoutRenderer.material = _blackoutMaterial;
            _blackoutRenderer.color = blackoutColor;

            UpdateOverlaySize();

            Debug.Log("LightRevealSystem_V2: Blackout overlay created");
        }

        private void UpdateCameraSettings()
        {
            if (_maskCamera != null && mainCamera != null)
            {
                _maskCamera.transform.position = mainCamera.transform.position;
                _maskCamera.transform.rotation = mainCamera.transform.rotation;
                _maskCamera.orthographicSize = mainCamera.orthographicSize;
                _maskCamera.orthographic = mainCamera.orthographic;
            }

            UpdateOverlaySize();
        }

        private void UpdateOverlaySize()
        {
            if (_blackoutOverlay == null || mainCamera == null) return;

            if (mainCamera.orthographic)
            {
                float height = mainCamera.orthographicSize * 2f;
                float width = height * mainCamera.aspect;

                Vector3 camPos = mainCamera.transform.position;
                _blackoutOverlay.transform.position = new Vector3(camPos.x, camPos.y, camPos.z + 1f);
                _blackoutOverlay.transform.localScale = new Vector3(width, height, 1f);
            }
        }

        void OnDestroy()
        {
            if (_maskRenderTexture != null)
            {
                _maskRenderTexture.Release();
                Destroy(_maskRenderTexture);
            }

            if (_blackoutOverlay != null)
            {
                Destroy(_blackoutOverlay);
            }

            if (_maskCamera != null)
            {
                Destroy(_maskCamera.gameObject);
            }
        }
    }
}
