using UnityEngine;
using System.Collections;
using _Scripts.Core;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// Creates an equalizer bar visualization that reacts to the currently playing music.
/// Analyzes audio spectrum data from the active AudioSource and drives visual bars.
/// </summary>
public class AudioSpectrumVisualizer : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern bool WebGLAudioSpectrum_Initialize(int fftSize);

    [DllImport("__Internal")]
    private static extern void WebGLAudioSpectrum_GetSpectrumData(float[] data, int dataLength);

    [DllImport("__Internal")]
    private static extern int WebGLAudioSpectrum_IsInitialized();

    [DllImport("__Internal")]
    private static extern void WebGLAudioSpectrum_Reconnect();

    private bool webGLInitialized = false;
    private float webGLReconnectTimer = 0f;
    private const float WEBGL_RECONNECT_INTERVAL = 2f;
#endif

    [Header("Audio Analysis")]
    [Tooltip("Number of spectrum samples (must be power of 2: 64, 128, 256, 512, 1024)")]
    [SerializeField] private int spectrumSamples = 512;

    [Tooltip("FFT window type for spectrum analysis")]
    [SerializeField] private FFTWindow fftWindow = FFTWindow.Blackman;

    [Header("Bar Configuration")]
    [Tooltip("Number of equalizer bars to display")]
    [SerializeField] private int barCount = 16;

    [Tooltip("Prefab for individual bars (should have SpriteRenderer or LineRenderer)")]
    [SerializeField] private GameObject barPrefab;

    [Tooltip("Total width of the equalizer display")]
    [SerializeField] private float totalWidth = 8f;

    [Tooltip("Maximum height bars can reach")]
    [SerializeField] private float maxBarHeight = 4f;

    [Tooltip("Minimum height when silent")]
    [SerializeField] private float minBarHeight = 0.1f;

    [Tooltip("Width of each bar (spacing is calculated automatically)")]
    [SerializeField] private float barWidth = 0.3f;

    [Tooltip("Z distance from camera when parented to camera")]
    [SerializeField] private float cameraZOffset = 10f;

    [Tooltip("Flip bars to grow downward instead of upward")]
    [SerializeField] private bool flipVertical = false;

    [Header("Animation")]
    [Tooltip("How quickly bars respond to audio (higher = snappier)")]
    [SerializeField] private float responseSpeed = 15f;

    [Tooltip("How quickly bars fall back down")]
    [SerializeField] private float fallSpeed = 8f;

    [Tooltip("Multiplier for spectrum amplitude")]
    [SerializeField] private float amplitudeMultiplier = 5f;

    [Tooltip("Smooth the spectrum across neighboring frequencies")]
    [SerializeField] private bool smoothSpectrum = true;

    [Tooltip("Number of neighbors to average when smoothing")]
    [SerializeField] private int smoothingWindow = 2;

    [Header("Visual Style")]
    [Tooltip("Sorting order for bars (peaks are +1)")]
    [SerializeField] private int sortingOrder = 1100;

    [Tooltip("Use gradient coloring based on bar height")]
    [SerializeField] private bool useGradient = true;

    [Tooltip("Color at minimum height")]
    [SerializeField] private Color lowColor = new Color(0.2f, 0.8f, 0.2f, 1f);

    [Tooltip("Color at maximum height")]
    [SerializeField] private Color highColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Tooltip("Sync bar colors with radar/combo color system")]
    [SerializeField] private bool syncWithRadarColor = false;

    [Tooltip("Add glow/emission to bars")]
    [SerializeField] private bool enableGlow = true;

    [Tooltip("Glow intensity multiplier")]
    [SerializeField] private float glowIntensity = 2f;

    [Header("Debug")]
    [Tooltip("Log audio source status and spectrum data")]
    [SerializeField] private bool debugMode = false;

    [Header("Peak Indicators")]
    [Tooltip("Show peak indicator caps on bars")]
    [SerializeField] private bool showPeakIndicators = true;

    [Tooltip("How long peaks hold before falling")]
    [SerializeField] private float peakHoldTime = 0.3f;

    [Tooltip("How fast peaks fall after hold time")]
    [SerializeField] private float peakFallSpeed = 3f;

    // Runtime data
    private float[] spectrumData;
    private float[] barHeights;
    private float[] targetHeights;
    private float[] peakHeights;
    private float[] peakHoldTimers;
    private GameObject[] bars;
    private Transform[] barTransforms;
    private SpriteRenderer[] barRenderers;
    private GameObject[] peakIndicators;
    private SpriteRenderer[] peakRenderers;

    // Cached references
    private AudioSource activeAudioSource;
    private SoundController soundController;

    private void Start()
    {
        InitializeArrays();
        CreateBars();

        // Get reference to SoundController
        soundController = SoundController.Instance;
        if (soundController == null)
        {
            Debug.LogWarning("AudioSpectrumVisualizer: SoundController not found. Will retry on Update.");
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // Initialize WebGL audio spectrum analysis
        // Use fftSize of spectrumSamples * 2 because frequencyBinCount = fftSize / 2
        StartCoroutine(InitializeWebGLAudioDelayed());
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private IEnumerator InitializeWebGLAudioDelayed()
    {
        // Wait a bit for Unity's audio system to initialize
        yield return new WaitForSeconds(0.5f);

        int fftSize = spectrumSamples * 2;
        webGLInitialized = WebGLAudioSpectrum_Initialize(fftSize);

        if (webGLInitialized)
        {
            Debug.Log("AudioSpectrumVisualizer: WebGL audio spectrum initialized successfully.");
        }
        else
        {
            Debug.LogWarning("AudioSpectrumVisualizer: WebGL audio spectrum initialization failed. Will retry periodically.");
        }
    }
#endif

    private void InitializeArrays()
    {
        spectrumData = new float[spectrumSamples]; 
        barHeights = new float[barCount];
        targetHeights = new float[barCount];
        peakHeights = new float[barCount];
        peakHoldTimers = new float[barCount];
        bars = new GameObject[barCount];
        barTransforms = new Transform[barCount];
        barRenderers = new SpriteRenderer[barCount];

        if (showPeakIndicators)
        {
            peakIndicators = new GameObject[barCount];
            peakRenderers = new SpriteRenderer[barCount];
        }
    }

    private void CreateBars()
    {
        float spacing = totalWidth / barCount;
        float startX = -totalWidth / 2f + spacing / 2f;

        // Check if we're parented to a camera - if so, position bars in front of it
        bool isChildOfCamera = GetComponentInParent<Camera>() != null;
        float zPos = isChildOfCamera ? cameraZOffset : 0f;

        for (int i = 0; i < barCount; i++)
        {
            // Create bar
            GameObject bar;
            if (barPrefab != null)
            {
                bar = Instantiate(barPrefab, transform);
            }
            else
            {
                // Create default bar with SpriteRenderer
                bar = CreateDefaultBar();
            }

            bar.name = $"EQ_Bar_{i}";
            bar.transform.localPosition = new Vector3(startX + i * spacing, 0f, zPos);
            bar.transform.localScale = new Vector3(barWidth, minBarHeight, 1f);

            bars[i] = bar;
            barTransforms[i] = bar.transform;
            barRenderers[i] = bar.GetComponent<SpriteRenderer>();

            if (barRenderers[i] != null)
            {
                barRenderers[i].color = lowColor;

                if (enableGlow && barRenderers[i].material != null)
                {
                    barRenderers[i].material.EnableKeyword("_EMISSION");
                }
            }

            // Create peak indicator
            if (showPeakIndicators)
            {
                GameObject peak = CreatePeakIndicator();
                peak.name = $"EQ_Peak_{i}";
                peak.transform.SetParent(transform);
                peak.transform.localPosition = new Vector3(startX + i * spacing, minBarHeight, zPos - 0.1f);
                peak.transform.localScale = new Vector3(barWidth * 1.1f, 0.05f, 1f);

                peakIndicators[i] = peak;
                peakRenderers[i] = peak.GetComponent<SpriteRenderer>();

                if (peakRenderers[i] != null)
                {
                    peakRenderers[i].color = Color.white;
                }
            }

            barHeights[i] = minBarHeight;
            peakHeights[i] = minBarHeight;
        }
    }

    private Sprite cachedBarSprite;

    private GameObject CreateDefaultBar()
    {
        GameObject bar = new GameObject("Bar");
        bar.transform.SetParent(transform);
        SpriteRenderer sr = bar.AddComponent<SpriteRenderer>();
        sr.sprite = GetOrCreateBarSprite();
        sr.sortingOrder = sortingOrder;
        return bar;
    }

    private GameObject CreatePeakIndicator()
    {
        GameObject peak = new GameObject("Peak");
        peak.transform.SetParent(transform);
        SpriteRenderer sr = peak.AddComponent<SpriteRenderer>();
        sr.sprite = GetOrCreateBarSprite();
        sr.sortingOrder = sortingOrder + 1;
        return peak;
    }

    private Sprite GetOrCreateBarSprite()
    {
        if (cachedBarSprite == null)
        {
            // Create a simple 1x1 white texture for the bar
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            // Pivot at bottom-center (0.5, 0) so bars grow upward from their position
            cachedBarSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0f), 1f);
        }
        return cachedBarSprite;
    }

    private void Update()
    {
        // Try to get audio source if we don't have one
        UpdateAudioSourceReference();

#if UNITY_WEBGL && !UNITY_EDITOR
        // Periodically try to reconnect to new audio sources in WebGL
        webGLReconnectTimer += Time.deltaTime;
        if (webGLReconnectTimer >= WEBGL_RECONNECT_INTERVAL)
        {
            webGLReconnectTimer = 0f;
            if (webGLInitialized)
            {
                WebGLAudioSpectrum_Reconnect();
            }
            else
            {
                // Try to initialize again if it failed before
                int fftSize = spectrumSamples * 2;
                webGLInitialized = WebGLAudioSpectrum_Initialize(fftSize);
            }
        }
#endif

        if (activeAudioSource == null || !activeAudioSource.isPlaying)
        {
            if (debugMode && Time.frameCount % 60 == 0)
            {
                Debug.Log($"AudioSpectrumVisualizer: No active audio source or not playing. Source: {activeAudioSource}, SoundController: {soundController}");
            }

            // Gradually lower bars when no audio
            for (int i = 0; i < barCount; i++)
            {
                targetHeights[i] = minBarHeight;
            }
        }
        else
        {
            // Get spectrum data from the active audio source
            GetSpectrumDataCrossPlatform();
            CalculateBarHeights();

            if (debugMode && Time.frameCount % 60 == 0)
            {
                float maxSpectrum = 0f;
                for (int i = 0; i < spectrumSamples; i++)
                {
                    if (spectrumData[i] > maxSpectrum) maxSpectrum = spectrumData[i];
                }
                Debug.Log($"AudioSpectrumVisualizer: Max spectrum value: {maxSpectrum}, Target height[0]: {targetHeights[0]}");
            }
        }

        // Animate bars
        AnimateBars();

        // Update peak indicators
        if (showPeakIndicators)
        {
            UpdatePeakIndicators();
        }

        // Update colors
        UpdateBarColors();
    }

    private void UpdateAudioSourceReference()
    {
        // Re-check for SoundController if we don't have it
        if (soundController == null)
        {
            soundController = SoundController.Instance;
        }

        if (soundController != null)
        {
            // Get the currently active audio source from SoundController
            activeAudioSource = soundController.GetActiveAudioSource();
        }
    }

    private void GetSpectrumDataCrossPlatform()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Use JavaScript plugin to get spectrum data on WebGL
        if (webGLInitialized)
        {
            WebGLAudioSpectrum_GetSpectrumData(spectrumData, spectrumSamples);
        }
        else
        {
            // Fallback: try Unity's native method (may return zeros on WebGL)
            activeAudioSource.GetSpectrumData(spectrumData, 0, fftWindow);
        }
#else
        // Use Unity's native spectrum analysis on other platforms
        activeAudioSource.GetSpectrumData(spectrumData, 0, fftWindow);
#endif
    }

    private void CalculateBarHeights()
    {
        for (int i = 0; i < barCount; i++)
        {
            // Use logarithmic distribution - more resolution in bass, less in treble
            // This matches how we perceive sound (octaves, not linear Hz)
            float t0 = (float)i / barCount;
            float t1 = (float)(i + 1) / barCount;

            // Map to spectrum indices using exponential curve
            // Start from index 1 to skip DC offset, use up to 1/4 of spectrum (lower frequencies)
            int maxIndex = spectrumSamples / 4;
            int startIndex = Mathf.Max(1, (int)(Mathf.Pow(t0, 2f) * maxIndex));
            int endIndex = Mathf.Max(startIndex + 1, (int)(Mathf.Pow(t1, 2f) * maxIndex));

            float sum = 0f;
            int count = 0;

            for (int j = startIndex; j < endIndex && j < spectrumSamples; j++)
            {
                sum += spectrumData[j];
                count++;
            }

            float average = count > 0 ? sum / count : 0f;

            // Apply per-band scaling to normalize energy across frequencies
            // Bass has way more energy than treble, so we attenuate bass heavily
            // Use exponential curve - first bars get heavily reduced
            float t = (float)i / barCount;
            float bandScale = Mathf.Pow(t, 1.5f) * 0.95f + 0.05f; // Range: 0.05 to 1.0, exponential curve
            average *= bandScale;

            // Use logarithmic scaling for better dynamic range
            // This prevents loud sounds from instantly maxing out
            float db = average > 0.0001f ? Mathf.Log10(average / 0.0001f) / 4f : 0f;
            db = Mathf.Clamp01(db);

            float height = Mathf.Lerp(minBarHeight, maxBarHeight, db * amplitudeMultiplier);
            height = Mathf.Clamp(height, minBarHeight, maxBarHeight);

            targetHeights[i] = height;
        }
    }

    private void AnimateBars()
    {
        for (int i = 0; i < barCount; i++)
        {
            // Smooth animation - rise fast, fall slow
            if (targetHeights[i] > barHeights[i])
            {
                barHeights[i] = Mathf.Lerp(barHeights[i], targetHeights[i], Time.deltaTime * responseSpeed);
            }
            else
            {
                barHeights[i] = Mathf.Lerp(barHeights[i], targetHeights[i], Time.deltaTime * fallSpeed);
            }

            // Update bar scale (Y axis for height)
            if (barTransforms[i] != null)
            {
                Vector3 scale = barTransforms[i].localScale;
                scale.y = flipVertical ? -barHeights[i] : barHeights[i];
                barTransforms[i].localScale = scale;
            }
        }
    }

    private void UpdatePeakIndicators()
    {
        for (int i = 0; i < barCount; i++)
        {
            // Check if current height exceeds peak
            if (barHeights[i] >= peakHeights[i])
            {
                peakHeights[i] = barHeights[i];
                peakHoldTimers[i] = peakHoldTime;
            }
            else
            {
                // Count down hold timer
                peakHoldTimers[i] -= Time.deltaTime;

                // After hold time, start falling
                if (peakHoldTimers[i] <= 0f)
                {
                    peakHeights[i] -= peakFallSpeed * Time.deltaTime;
                    peakHeights[i] = Mathf.Max(peakHeights[i], barHeights[i], minBarHeight);
                }
            }

            // Update peak indicator position
            if (peakIndicators[i] != null)
            {
                Vector3 pos = peakIndicators[i].transform.localPosition;
                pos.y = flipVertical ? -peakHeights[i] : peakHeights[i];
                peakIndicators[i].transform.localPosition = pos;
            }
        }
    }

    private void UpdateBarColors()
    {
        Color baseColor = lowColor;

        // Sync with radar color if enabled
        if (syncWithRadarColor && RadarBackgroundGenerator.Instance != null)
        {
            baseColor = RadarBackgroundGenerator.Instance.GetCurrentComboColor();
        }

        for (int i = 0; i < barCount; i++)
        {
            if (barRenderers[i] == null) continue;

            Color barColor;

            if (useGradient)
            {
                // Lerp between low and high color based on height
                float t = Mathf.InverseLerp(minBarHeight, maxBarHeight, barHeights[i]);
                barColor = Color.Lerp(baseColor, highColor, t);
            }
            else
            {
                barColor = baseColor;
            }

            barRenderers[i].color = barColor;

            // Apply glow
            if (enableGlow && barRenderers[i].material != null)
            {
                float intensity = Mathf.Lerp(0.5f, glowIntensity, barHeights[i] / maxBarHeight);
                barRenderers[i].material.SetColor("_EmissionColor", barColor * intensity);
            }
        }
    }

    /// <summary>
    /// Manually trigger a pulse effect on the bars (e.g., on beat detection)
    /// </summary>
    public void Pulse(float intensity = 1f)
    {
        StartCoroutine(PulseCoroutine(intensity));
    }

    private IEnumerator PulseCoroutine(float intensity)
    {
        // Briefly boost all bars
        for (int i = 0; i < barCount; i++)
        {
            targetHeights[i] = Mathf.Min(targetHeights[i] + intensity * maxBarHeight * 0.3f, maxBarHeight);
        }

        yield return new WaitForSeconds(0.05f);
    }

    /// <summary>
    /// Set the color scheme for the equalizer
    /// </summary>
    public void SetColors(Color low, Color high)
    {
        lowColor = low;
        highColor = high;
    }

    /// <summary>
    /// Enable or disable radar color sync
    /// </summary>
    public void SetRadarColorSync(bool enabled)
    {
        syncWithRadarColor = enabled;
    }

    /// <summary>
    /// Get the current average amplitude across all bars
    /// </summary>
    public float GetAverageAmplitude()
    {
        float sum = 0f;
        for (int i = 0; i < barCount; i++)
        {
            sum += barHeights[i];
        }
        return sum / barCount;
    }

    /// <summary>
    /// Get amplitude for a specific frequency band (0 = bass, barCount-1 = treble)
    /// </summary>
    public float GetBandAmplitude(int band)
    {
        if (band >= 0 && band < barCount)
        {
            return barHeights[band];
        }
        return 0f;
    }

}
