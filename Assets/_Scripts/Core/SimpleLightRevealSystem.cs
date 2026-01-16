using UnityEngine;

namespace _Scripts.Core
{
    /// <summary>
    /// Simple and reliable light reveal system using a render texture approach.
    /// This bypasses Unity's problematic sprite mask system.
    /// </summary>
    public class SimpleLightRevealSystem : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Main camera (auto-detects if not set)")]
        public Camera mainCamera;

        [Header("Light Layer Setup")]
        [Tooltip("Layer mask for light revealer objects. Set your light sprites to this layer.")]
        public LayerMask lightLayer = 1 << 6; // Default to layer 6

        [Header("Blackout Settings")]
        [Tooltip("Color of the blackout area")]
        public Color blackoutColor = Color.black;

        [Tooltip("Sorting order for overlay (should be above everything)")]
        public int sortingOrder = 1000;

        [Tooltip("Edge softness (0 = hard, 1 = very soft)")]
        [Range(0f, 1f)]
        public float edgeSoftness = 0.5f;

        [Header("Debug")]
        [Tooltip("Show the mask texture in a corner for debugging")]
        public bool showDebugMask = false;

        private Camera _maskCamera;
        private RenderTexture _maskTexture;
        private GameObject _overlayObject;
        private SpriteRenderer _overlayRenderer;
        private Material _overlayMaterial;

        void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            CreateMaskCamera();
            CreateOverlay();
        }

        void LateUpdate()
        {
            SyncCameras();
            UpdateMaterial();
        }

        private void CreateMaskCamera()
        {
            // Create camera object
            GameObject camObj = new GameObject("Light Mask Camera");
            camObj.transform.SetParent(transform);

            _maskCamera = camObj.AddComponent<Camera>();

            // Copy settings from main camera
            _maskCamera.CopyFrom(mainCamera);

            // Only render the light layer
            _maskCamera.cullingMask = lightLayer;
            _maskCamera.clearFlags = CameraClearFlags.SolidColor;
            _maskCamera.backgroundColor = Color.clear; // Transparent where no lights

            // Render before main camera
            _maskCamera.depth = mainCamera.depth - 1;

            // Create render texture
            int width = Mathf.Max(Screen.width, 1);
            int height = Mathf.Max(Screen.height, 1);
            _maskTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            _maskTexture.name = "Light Mask Texture";
            _maskCamera.targetTexture = _maskTexture;

            Debug.Log($"SimpleLightRevealSystem: Mask camera created, rendering layer mask {lightLayer.value}");
        }

        private void CreateOverlay()
        {
            // Create overlay sprite object
            _overlayObject = new GameObject("Blackout Overlay (Shader)");
            _overlayObject.transform.SetParent(transform);

            // Create a 1x1 white sprite
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);

            // Setup sprite renderer
            _overlayRenderer = _overlayObject.AddComponent<SpriteRenderer>();
            _overlayRenderer.sprite = sprite;
            _overlayRenderer.sortingOrder = sortingOrder;

            // Create material with our shader
            _overlayMaterial = new Material(Shader.Find("Custom/AlphaMaskBlackout"));
            _overlayRenderer.material = _overlayMaterial;

            UpdateOverlayTransform();

            Debug.Log("SimpleLightRevealSystem: Overlay created");
        }

        private void SyncCameras()
        {
            if (_maskCamera == null || mainCamera == null) return;

            // Keep mask camera synced with main camera
            _maskCamera.transform.position = mainCamera.transform.position;
            _maskCamera.transform.rotation = mainCamera.transform.rotation;
            _maskCamera.orthographicSize = mainCamera.orthographicSize;
            _maskCamera.fieldOfView = mainCamera.fieldOfView;

            UpdateOverlayTransform();
        }

        private void UpdateOverlayTransform()
        {
            if (_overlayObject == null || mainCamera == null) return;

            if (mainCamera.orthographic)
            {
                float height = mainCamera.orthographicSize * 2f;
                float width = height * mainCamera.aspect;

                Vector3 camPos = mainCamera.transform.position;
                _overlayObject.transform.position = new Vector3(camPos.x, camPos.y, camPos.z + 0.5f);
                _overlayObject.transform.localScale = new Vector3(width, height, 1f);
            }
        }

        private void UpdateMaterial()
        {
            if (_overlayMaterial != null && _maskTexture != null)
            {
                _overlayMaterial.SetTexture("_MaskTex", _maskTexture);
                _overlayMaterial.SetColor("_BlackoutColor", blackoutColor);
                _overlayMaterial.SetFloat("_Softness", edgeSoftness);
            }
        }

        void OnGUI()
        {
            if (showDebugMask && _maskTexture != null)
            {
                // Show mask in bottom-right corner for debugging
                int size = 200;
                GUI.DrawTexture(new Rect(Screen.width - size - 10, Screen.height - size - 10, size, size), _maskTexture);
            }
        }

        void OnDestroy()
        {
            if (_maskTexture != null)
            {
                _maskTexture.Release();
                Destroy(_maskTexture);
            }

            if (_maskCamera != null)
            {
                Destroy(_maskCamera.gameObject);
            }

            if (_overlayObject != null)
            {
                Destroy(_overlayObject);
            }

            if (_overlayMaterial != null)
            {
                Destroy(_overlayMaterial);
            }
        }
    }
}
