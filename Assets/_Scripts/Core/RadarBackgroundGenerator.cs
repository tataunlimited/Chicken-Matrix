using UnityEngine;

namespace _Scripts.Core
{
    [ExecuteInEditMode]
    public class RadarBackgroundGenerator : MonoBehaviour
    {
        [Header("Ring Settings")]
        [SerializeField] private int ringCount = 5;
        [SerializeField] private float maxRadius = 10f;
        [SerializeField] private float ringThickness = 0.05f;
        [SerializeField] private int ringSegments = 64;

        [Header("Grid Settings")]
        [SerializeField] private int radialLines = 12;
        [SerializeField] private int circularGridLines = 10;
        [SerializeField] private float gridLineThickness = 0.02f;

        [Header("Visual Settings")]
        [SerializeField] private Color glowColor = new Color(0f, 1f, 0f, 1f);
        [SerializeField] private float glowIntensity = 2f;
        [SerializeField] private Color gridColor = new Color(0f, 1f, 0f, 0.3f);

        [Header("Material")]
        [SerializeField] private Material lineMaterial;

        private GameObject ringsContainer;
        private GameObject gridContainer;
        private bool isGenerating = false;

        private void OnValidate()
        {
            if (!Application.isPlaying && !isGenerating)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && !isGenerating)
                    {
                        GenerateRadar();
                    }
                };
#endif
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                GenerateRadar();
            }
        }

        [ContextMenu("Generate Radar")]
        public void GenerateRadar()
        {
            if (isGenerating) return;

            isGenerating = true;
            ClearRadar();
            CreateRings();
            CreateGrid();
            isGenerating = false;
        }

        [ContextMenu("Clear Radar")]
        public void ClearRadar()
        {
            // Clear all children regardless of stored references
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            ringsContainer = null;
            gridContainer = null;
        }

        private void CreateRings()
        {
            ringsContainer = new GameObject("Rings");
            ringsContainer.transform.SetParent(transform);
            ringsContainer.transform.localPosition = Vector3.zero;

            for (int i = 1; i <= ringCount; i++)
            {
                float radius = maxRadius * (i / (float)ringCount);
                CreateRing(radius, i);
            }
        }

        private void CreateRing(float radius, int index)
        {
            GameObject ringObj = new GameObject($"Ring_{index}");
            ringObj.transform.SetParent(ringsContainer.transform);
            ringObj.transform.localPosition = Vector3.zero;

            LineRenderer lineRenderer = ringObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(lineRenderer, ringThickness, glowColor, glowIntensity);

            lineRenderer.positionCount = ringSegments + 1;
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;

            for (int i = 0; i <= ringSegments; i++)
            {
                float angle = (i / (float)ringSegments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                lineRenderer.SetPosition(i, new Vector3(x, y, 0));
            }
        }

        private void CreateGrid()
        {
            gridContainer = new GameObject("Grid");
            gridContainer.transform.SetParent(transform);
            gridContainer.transform.localPosition = Vector3.zero;

            // Create radial lines (spokes)
            for (int i = 0; i < radialLines; i++)
            {
                float angle = (i / (float)radialLines) * Mathf.PI * 2f;
                CreateRadialLine(angle, i);
            }

            // Create circular grid lines (concentric circles)
            for (int i = 1; i <= circularGridLines; i++)
            {
                float radius = maxRadius * (i / (float)circularGridLines);
                CreateCircularGridLine(radius, i);
            }
        }

        private void CreateRadialLine(float angle, int index)
        {
            GameObject lineObj = new GameObject($"RadialLine_{index}");
            lineObj.transform.SetParent(gridContainer.transform);
            lineObj.transform.localPosition = Vector3.zero;

            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(lineRenderer, gridLineThickness, gridColor, glowIntensity * 0.5f);

            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = false;

            float x = Mathf.Cos(angle) * maxRadius;
            float y = Mathf.Sin(angle) * maxRadius;

            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, new Vector3(x, y, 0));
        }

        private void CreateCircularGridLine(float radius, int index)
        {
            GameObject circleObj = new GameObject($"CircularGrid_{index}");
            circleObj.transform.SetParent(gridContainer.transform);
            circleObj.transform.localPosition = Vector3.zero;

            LineRenderer lineRenderer = circleObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(lineRenderer, gridLineThickness, gridColor, glowIntensity * 0.5f);

            lineRenderer.positionCount = ringSegments + 1;
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;

            for (int i = 0; i <= ringSegments; i++)
            {
                float angle = (i / (float)ringSegments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                lineRenderer.SetPosition(i, new Vector3(x, y, 0));
            }
        }

        private void ConfigureLineRenderer(LineRenderer lr, float width, Color color, float intensity)
        {
            lr.startWidth = width;
            lr.endWidth = width;
            lr.material = lineMaterial != null ? lineMaterial : CreateDefaultMaterial();
            lr.startColor = color * intensity;
            lr.endColor = color * intensity;
            lr.numCapVertices = 2;
            lr.numCornerVertices = 2;
        }

        private Material CreateDefaultMaterial()
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            return mat;
        }
    }
}
