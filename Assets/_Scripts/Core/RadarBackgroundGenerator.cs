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
        [SerializeField] private Color[] comboGridColors = new Color[10];

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
        private LineRenderer[] radialLineRenderers;
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
            ApplyVisuals();
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

            //set the number of radial lines to a random number between 4 and 16
            //int numRadialLines = Random.Range(4, 16);
            //SetNumRadialLines(numRadialLines);
        }

        private void OscilateNumCircularGridLines()
        {
            //oscillate between 4 and 12 lines based on a sine wave and time
            float time = Time.time;
            float sineValue = Mathf.Sin(time * 2f); // Adjust frequency as needed
            int numLines = Mathf.RoundToInt(Mathf.Lerp(0, 4, (sineValue + 1f) / 2f));
            SetNumCircularGridLines(numLines); 
        }

        private void OscilateNumRadialLines()
        {
            //oscillate between 4 and 16 lines based on the pulse time
            float time = Time.time;
            float sineValue = Mathf.Sin(time * 2f); // Adjust frequency as needed
            int numLines = Mathf.RoundToInt(Mathf.Lerp(4, 8, (sineValue + 1f) / 2f));
            SetNumRadialLines(numLines);
        }

        public void SetNumRadialLines(int count)
        {
            if (gridContainer == null) return;

            // Clamp count to reasonable values
            count = Mathf.Clamp(count, 1, 32);

            // Destroy existing radial lines
            for (int i = 0; i < radialLines; i++)
            {
                Transform child = gridContainer.transform.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            radialLines = count;
            radialLineRenderers = new LineRenderer[radialLines];

            // Create new radial lines
            for (int i = 0; i < radialLines; i++)
            {
                float angle = (i / (float)radialLines) * Mathf.PI * 2f;
                radialLineRenderers[i] = CreateRadialLine(angle, i);
                radialLineRenderers[i].transform.SetSiblingIndex(i);
            }
        }

        public void SetNumCircularGridLines(int count)
        {
            if (gridContainer == null) return;

            // Clamp count to reasonable values
            count = Mathf.Clamp(count, 1, 20);

            // Destroy existing circular grid lines
            for (int i = radialLines; i < gridContainer.transform.childCount; i++)
            {
                Transform child = gridContainer.transform.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            // Create new circular grid lines
            for (int i = 1; i <= count; i++)
            {
                float radius = maxRadius * (i / (float)count);
                CreateCircularGridLine(radius, i);
            }
        }

        private void PulseGrid()
        {
            // Calculate scale and fade multipliers
            // Handle beat pulse
            float beatIntensity = 0f;
            bool needsPositionUpdate = isPulsing;

            if (isPulsing)
            {
                pulseTimer += Time.deltaTime;
                float t = pulseTimer / pulseDuration;

                if (t >= 1f)
                {
                    isPulsing = false;
                    // One final update to reset positions
                    needsPositionUpdate = true;
                }
                else
                {
                    beatIntensity = pulseCurve.Evaluate(t);
                }
            }

            // Calculate breathing alpha (lerps between pulses)
            float breathingAlpha = 1f;
            bool needsColorUpdate = false;
            if (enableAlphaBreathing && breathingDuration > 0f)
            {
                float prevAlpha = Mathf.Lerp(breathingStartAlpha, breathingTargetAlpha, Mathf.Clamp01(breathingTimer / breathingDuration));
                breathingTimer += Time.deltaTime;
                float t = Mathf.Clamp01(breathingTimer / breathingDuration);
                breathingAlpha = Mathf.Lerp(breathingStartAlpha, breathingTargetAlpha, t);
                // Only update colors if alpha changed noticeably
                needsColorUpdate = Mathf.Abs(breathingAlpha - prevAlpha) > 0.001f;
            }

            // Skip all updates if nothing changed
            if (!needsPositionUpdate && !needsColorUpdate)
            {
                return;
            }

            float scaleMultiplier = 1f + (pulseScaleAmount * beatIntensity);
            currentScaleMultiplier = scaleMultiplier;
            float fadeMultiplier = 1f + (pulseFadeAmount * beatIntensity);

            Color ringBaseColor = glowColor;
            Color gridBaseColor = gridColor;
            if (TryGetComboGridColor(out Color comboColor))
            {
                ringBaseColor = new Color(comboColor.r, comboColor.g, comboColor.b, glowColor.a);
                gridBaseColor = new Color(comboColor.r, comboColor.g, comboColor.b, gridColor.a);
            }

            // Apply to rings
            if (ringRenderers != null)
            {
                for (int i = 0; i < ringRenderers.Length; i++)
                {
                    if (ringRenderers[i] == null) continue;

                    // Only update positions when pulsing (scale is changing)
                    if (needsPositionUpdate)
                    {
                        float baseRadius = ringBaseRadii[i];
                        float currentRadius = baseRadius * scaleMultiplier;

                        for (int j = 0; j <= ringSegments; j++)
                        {
                            float angle = (j / (float)ringSegments) * Mathf.PI * 2f;
                            float x = Mathf.Cos(angle) * currentRadius;
                            float y = Mathf.Sin(angle) * currentRadius;
                            ringRenderers[i].SetPosition(j, new Vector3(x, y, 0));
                        }
                    }

                    // Update color/brightness with breathing alpha
                    Color pulsedColor = ringBaseColor * glowIntensity * fadeMultiplier;
                    pulsedColor.a = ringBaseColor.a * breathingAlpha;
                    ringRenderers[i].startColor = pulsedColor;
                    ringRenderers[i].endColor = pulsedColor;
                }
            }

            //ensure all radial lines are updated and match the size of the ring growth
            int radialLineCount = radialLineRenderers != null ? radialLineRenderers.Length : 0;
            for (int i = 0; i < radialLineCount; i++)
            {
                if (radialLineRenderers[i] == null) continue;

                Color pulsedGridColor = gridBaseColor * glowIntensity * 0.5f * fadeMultiplier;
                pulsedGridColor.a = gridBaseColor.a * breathingAlpha;
                radialLineRenderers[i].startColor = pulsedGridColor;
                radialLineRenderers[i].endColor = pulsedGridColor;

                // Only update positions when pulsing
                if (needsPositionUpdate)
                {
                    float angle = (i / (float)radialLineCount) * Mathf.PI * 2f;
                    float x = Mathf.Cos(angle) * maxRadius * scaleMultiplier;
                    float y = Mathf.Sin(angle) * maxRadius * scaleMultiplier;
                    radialLineRenderers[i].SetPosition(1, new Vector3(x, y, 0));
                }
            }

            // Apply to grid
            if (gridRenderers != null)
            {
                for (int i = 0; i < gridRenderers.Length; i++)
                {
                    if (gridRenderers[i] == null) continue;

                    Color pulsedGridColor = gridBaseColor * glowIntensity * 0.5f * fadeMultiplier;
                    pulsedGridColor.a = gridBaseColor.a * breathingAlpha;
                    gridRenderers[i].startColor = pulsedGridColor;
                    gridRenderers[i].endColor = pulsedGridColor;
                }
            }
        }

        private bool TryGetComboGridColor(out Color comboColor)
        {
            comboColor = gridColor;
            if (comboGridColors == null || comboGridColors.Length < 10) return false;
            if (GameManager.Instance == null) return false;

            int comboValue = Mathf.Max(1, GameManager.Instance.combo);
            int colorIndex = Mathf.Clamp((comboValue - 1) / 10, 0, 9);
            comboColor = comboGridColors[colorIndex];
            return true;
        }

        private void ApplyVisuals()
        {
            //OscilateNumCircularGridLines();
            //OscilateNumRadialLines();
            PulseGrid();
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
            radialLineRenderers = new LineRenderer[radialLines];

            int rendererIndex = 0;

            // Create radial lines (spokes)
            for (int i = 0; i < radialLines; i++)
            {
                float angle = (i / (float)radialLines) * Mathf.PI * 2f;
                LineRenderer radialRenderer = CreateRadialLine(angle, i);
                gridRenderers[rendererIndex++] = radialRenderer;
                radialLineRenderers[i] = radialRenderer;
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
