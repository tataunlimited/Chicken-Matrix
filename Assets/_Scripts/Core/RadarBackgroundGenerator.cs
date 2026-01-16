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

        [Header("Rendering")]
        [SerializeField] private Material lineMaterial;
        [SerializeField] private int sortingOrder = 1001;

        [Header("Beat Pulse Animation")]
        [SerializeField] private float pulseScaleAmount = 0.1f;
        [SerializeField] private float pulseFadeAmount = 0.3f;
        [SerializeField] private float pulseDuration = 0.2f;
        [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Alpha Breathing Animation")]
        [SerializeField] private bool enableAlphaBreathing = true;
        [SerializeField] [Range(0f, 1f)] private float minAlpha = 0.3f;
        [SerializeField] [Range(0f, 1f)] private float maxAlpha = 1f;

        private GameObject ringsContainer;
        private GameObject gridContainer;
        private bool isGenerating = false;
        private LineRenderer[] ringRenderers;
        private LineRenderer[] gridRenderers;
        private float[] ringBaseRadii;
        private float[] gridBaseRadii;
        private float pulseTimer = 0f;
        private bool isPulsing = false;

        // Alpha breathing state
        private float breathingTimer = 0f;
        private float breathingDuration = 1f;
        private float breathingStartAlpha;
        private float breathingTargetAlpha;
        private bool breathingToMax = true;

        // Current pulse scale multiplier (exposed for radar line sync)
        private float currentScaleMultiplier = 1f;
        public float CurrentScaleMultiplier => currentScaleMultiplier;

        public static RadarBackgroundGenerator Instance;

        private void Awake()
        {
            Instance = this;
        }

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

        private void Update()
        {
            if (!Application.isPlaying) return;

            // Handle beat pulse
            float beatIntensity = 0f;
            if (isPulsing)
            {
                pulseTimer += Time.deltaTime;
                float t = pulseTimer / pulseDuration;

                if (t >= 1f)
                {
                    isPulsing = false;
                }
                else
                {
                    beatIntensity = pulseCurve.Evaluate(t);
                }
            }

            // Calculate breathing alpha (lerps between pulses)
            float breathingAlpha = 1f;
            if (enableAlphaBreathing && breathingDuration > 0f)
            {
                breathingTimer += Time.deltaTime;
                float t = Mathf.Clamp01(breathingTimer / breathingDuration);
                breathingAlpha = Mathf.Lerp(breathingStartAlpha, breathingTargetAlpha, t);
            }

            ApplyVisuals(beatIntensity, breathingAlpha);
        }

        public void Pulse()
        {
            Pulse(GameManager.Instance != null ? GameManager.Instance.interval : 1f);
        }

        public void Pulse(float interval)
        {
            if (!Application.isPlaying) return;

            // Beat pulse
            pulseTimer = 0f;
            isPulsing = true;

            // Alpha breathing - swap direction each pulse
            if (enableAlphaBreathing)
            {
                breathingTimer = 0f;
                breathingDuration = interval;
                breathingStartAlpha = breathingToMax ? minAlpha : maxAlpha;
                breathingTargetAlpha = breathingToMax ? maxAlpha : minAlpha;
                breathingToMax = !breathingToMax;
            }
        }

        private void ApplyVisuals(float beatIntensity, float breathingAlpha)
        {
            float scaleMultiplier = 1f + (pulseScaleAmount * beatIntensity);
            currentScaleMultiplier = scaleMultiplier;
            float fadeMultiplier = 1f + (pulseFadeAmount * beatIntensity);

            // Apply to rings
            if (ringRenderers != null)
            {
                for (int i = 0; i < ringRenderers.Length; i++)
                {
                    if (ringRenderers[i] == null) continue;

                    // Update scale by recalculating positions
                    float baseRadius = ringBaseRadii[i];
                    float currentRadius = baseRadius * scaleMultiplier;

                    for (int j = 0; j <= ringSegments; j++)
                    {
                        float angle = (j / (float)ringSegments) * Mathf.PI * 2f;
                        float x = Mathf.Cos(angle) * currentRadius;
                        float y = Mathf.Sin(angle) * currentRadius;
                        ringRenderers[i].SetPosition(j, new Vector3(x, y, 0));
                    }

                    // Update color/brightness with breathing alpha
                    Color pulsedColor = glowColor * glowIntensity * fadeMultiplier;
                    pulsedColor.a = glowColor.a * breathingAlpha;
                    ringRenderers[i].startColor = pulsedColor;
                    ringRenderers[i].endColor = pulsedColor;
                }
            }

            // Apply to grid
            if (gridRenderers != null)
            {
                for (int i = 0; i < gridRenderers.Length; i++)
                {
                    if (gridRenderers[i] == null) continue;

                    Color pulsedGridColor = gridColor * glowIntensity * 0.5f * fadeMultiplier;
                    pulsedGridColor.a = gridColor.a * breathingAlpha;
                    gridRenderers[i].startColor = pulsedGridColor;
                    gridRenderers[i].endColor = pulsedGridColor;
                }
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

            ringRenderers = new LineRenderer[ringCount];
            ringBaseRadii = new float[ringCount];

            for (int i = 1; i <= ringCount; i++)
            {
                float radius = maxRadius * (i / (float)ringCount);
                ringBaseRadii[i - 1] = radius;
                ringRenderers[i - 1] = CreateRing(radius, i);
            }
        }

        private LineRenderer CreateRing(float radius, int index)
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

            return lineRenderer;
        }

        private void CreateGrid()
        {
            gridContainer = new GameObject("Grid");
            gridContainer.transform.SetParent(transform);
            gridContainer.transform.localPosition = Vector3.zero;

            int totalGridLines = radialLines + circularGridLines;
            gridRenderers = new LineRenderer[totalGridLines];
            gridBaseRadii = new float[circularGridLines];

            int rendererIndex = 0;

            // Create radial lines (spokes)
            for (int i = 0; i < radialLines; i++)
            {
                float angle = (i / (float)radialLines) * Mathf.PI * 2f;
                gridRenderers[rendererIndex++] = CreateRadialLine(angle, i);
            }

            // Create circular grid lines (concentric circles)
            for (int i = 1; i <= circularGridLines; i++)
            {
                float radius = maxRadius * (i / (float)circularGridLines);
                gridBaseRadii[i - 1] = radius;
                gridRenderers[rendererIndex++] = CreateCircularGridLine(radius, i);
            }
        }

        private LineRenderer CreateRadialLine(float angle, int index)
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

            return lineRenderer;
        }

        private LineRenderer CreateCircularGridLine(float radius, int index)
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

            return lineRenderer;
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
            lr.sortingOrder = sortingOrder;
        }

        private Material CreateDefaultMaterial()
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            return mat;
        }
    }
}
