using UnityEngine;

namespace _Scripts.Core
{
    /// <summary>
    /// Controls a trail prefab that follows the mouse cursor at all times.
    /// Attach this script to an empty GameObject and assign your trail prefab.
    /// </summary>
    public class MouseTrailController : MonoBehaviour
    {
        [Header("Trail Settings")]
        [Tooltip("The trail prefab to instantiate and follow the mouse")]
        [SerializeField] private GameObject trailPrefab;

        [Tooltip("Z position for the trail (useful for layering)")]
        [SerializeField] private float zPosition = 0f;

        [Tooltip("If true, uses smooth interpolation to follow the mouse")]
        [SerializeField] private bool useSmoothFollow;

        [Tooltip("Smoothing speed when useSmoothFollow is enabled")]
        [SerializeField] private float smoothSpeed = 20f;

        private GameObject _trailInstance;
        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;

            if (trailPrefab != null)
            {
                _trailInstance = Instantiate(trailPrefab, GetMouseWorldPosition(), Quaternion.identity);
                _trailInstance.transform.SetParent(transform);
            }
        }

        private void Update()
        {
            if (_trailInstance == null || _mainCamera == null) return;

            Vector3 targetPosition = GetMouseWorldPosition();

            if (useSmoothFollow)
            {
                _trailInstance.transform.position = Vector3.Lerp(
                    _trailInstance.transform.position,
                    targetPosition,
                    smoothSpeed * Time.deltaTime
                );
            }
            else
            {
                _trailInstance.transform.position = targetPosition;
            }
        }

        private Vector3 GetMouseWorldPosition()
        {
            Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = zPosition;
            return mousePos;
        }

        /// <summary>
        /// Enables or disables the trail visibility
        /// </summary>
        public void SetTrailActive(bool active)
        {
            if (_trailInstance != null)
            {
                _trailInstance.SetActive(active);
            }
        }

        /// <summary>
        /// Replaces the current trail with a new prefab
        /// </summary>
        public void SetTrailPrefab(GameObject newPrefab)
        {
            if (_trailInstance != null)
            {
                Destroy(_trailInstance);
            }

            trailPrefab = newPrefab;

            if (trailPrefab != null)
            {
                _trailInstance = Instantiate(trailPrefab, GetMouseWorldPosition(), Quaternion.identity);
                _trailInstance.transform.SetParent(transform);
            }
        }

        private void OnDestroy()
        {
            if (_trailInstance != null)
            {
                Destroy(_trailInstance);
            }
        }
    }
}
