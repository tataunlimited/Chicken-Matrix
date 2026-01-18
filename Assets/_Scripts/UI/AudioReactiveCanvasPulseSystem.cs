using UnityEngine;
using DG.Tweening;
public class AudioReactiveCanvasPulseSystem : MonoBehaviour
{
    #region VAR ZONE
    [Header("Base Breathing Tween (Always Running)")]
    [SerializeField] private float breatheMinScale = 1.0f;          // Bottom of breathing pulse
    [SerializeField] private float breatheMaxScale = 1.04f;         // Top of breathing pulse
    [SerializeField] private float breatheDuration = 3f;            // Full cycle time (slow, zen)
    [SerializeField] private Ease breatheEase = Ease.InOutSine;

    [Header("Audio Reactive Tween (Responds to Music)")]
    [SerializeField] private float audioTweenDuration = 0.08f;      // How fast audio bumps happen
    [SerializeField] private Ease audioEase = Ease.OutQuad;         // Punchy, snappy reaction
    [SerializeField] private float audioAmplitudeMultiplier = 0.08f; // How much extra scale from audio
    [SerializeField] private float minAmplitudeDelta = 0.01f;       // Ignore tiny amplitude changes

    [Header("Frequency Band Selection")]
    [SerializeField] private bool useAverageBass = true;            // Bands 0-3
    [SerializeField] private bool useMidrange = false;              // Bands 4-10
    [SerializeField] private bool useTreble = false;                // Bands 11-15
    [SerializeField] private int customBandIndex = -1;              // Specific band, -1 = off

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    [SerializeField] private RectTransform canvasRectTransform;

    private Sequence baseBreathingSequence;  // The infinite breathing loop
    private Tween audioReactiveTween;        // The reactive bump on top
    private bool isDestroyed = false;

    [SerializeField] private AudioSpectrumVisualizer audioVisualizer;
    private float lastAudioScale = 0f;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeReferences();
    }

    private void Start()
    {
        FindAudioVisualizer();
        if (CanPlayAnimation())
        {
            CreateBaseBreathingSequence();
        }
    }

    private void Update()
    {
        if (!CanPlayAnimation()) return;

        if (audioVisualizer == null)
        {
            FindAudioVisualizer();
            return;
        }

        float amplitude = GetCurrentAmplitude();
        float audioScale = amplitude * audioAmplitudeMultiplier;

        // Only trigger a new audio tween if amplitude changed enough
        if (Mathf.Abs(audioScale - lastAudioScale) > minAmplitudeDelta)
        {
            StartAudioReactiveTween(audioScale);
            lastAudioScale = audioScale;

            if (debugMode)
            {
                Debug.Log($"AudioSyncedCanvasPulse_DualLayer: amplitude={amplitude:F3}, audioScale={audioScale:F3}");
            }
        }
    }

    private void OnEnable()
    {
        // Allow animations to run again when the object is re-enabled
        isDestroyed = false;

        // Optionally recreate breathing if needed
        if (CanPlayAnimation() && baseBreathingSequence == null)
        {
            CreateBaseBreathingSequence();
        }
    }

    #endregion

    #region Init & References
    private void InitializeReferences()
    {
        if (canvasRectTransform == null)
            canvasRectTransform = GetComponent<RectTransform>();

        if (canvasRectTransform == null)
        {
            Debug.LogError($"AudioSyncedCanvasPulse_DualLayer on {gameObject.name}: RectTransform not found!", gameObject);
            enabled = false;
        }
    }

    private void FindAudioVisualizer()
    {
        if (audioVisualizer == null)
        {
            audioVisualizer = FindObjectOfType<AudioSpectrumVisualizer>();
            if (audioVisualizer == null && debugMode)
            {
                Debug.LogWarning("AudioSyncedCanvasPulse_DualLayer: AudioSpectrumVisualizer not found.");
            }
        }
    }
    #endregion

    #region Base Breathing Sequence
    /// <summary>
    /// Create the infinite base breathing animation.
    /// This loops forever, creating a gentle pulse even with no audio.
    /// </summary>
    private void CreateBaseBreathingSequence()
    {
        KillBaseBreathingSequence();

        baseBreathingSequence = DOTween.Sequence();

        // Scale up from min to max
        baseBreathingSequence.Append(canvasRectTransform.DOScale(Vector3.one * breatheMaxScale, breatheDuration * 0.5f).SetEase(breatheEase)).SetUpdate(true);

        // Scale down from max to min
        baseBreathingSequence.Append(canvasRectTransform.DOScale(Vector3.one * breatheMinScale, breatheDuration * 0.5f).SetEase(breatheEase)).SetUpdate(true);

        // Loop infinitely
        baseBreathingSequence.SetLoops(-1, LoopType.Restart);
        baseBreathingSequence.OnKill(() => baseBreathingSequence = null);

        if (debugMode)
            Debug.Log("AudioSyncedCanvasPulse_DualLayer: Base breathing sequence created.");
    }

    private void KillBaseBreathingSequence()
    {
        try
        {
            if (baseBreathingSequence != null && baseBreathingSequence.active)
                baseBreathingSequence.Kill();
        }
        finally
        {
            baseBreathingSequence = null;
        }
    }
    #endregion

    #region Audio Reactive Tween
    /// <summary>
    /// Start a short audio-reactive tween that adds scale on top of the breathing base.
    /// When this tween completes, the breathing base takes over again.
    /// </summary>
    private void StartAudioReactiveTween(float extraAudioScale)
    {
        if (!CanPlayAnimation()) return;

        // Kill previous audio tween
        KillAudioReactiveTween();

        // Target scale = base breathing range + audio bump
        float baselineScale = (breatheMinScale + breatheMaxScale) / 2f;
        float targetScale = baselineScale + extraAudioScale;

        // Clamp to reasonable bounds
        targetScale = Mathf.Clamp(targetScale, breatheMinScale, breatheMaxScale + 0.1f);

        audioReactiveTween = canvasRectTransform.DOScale(Vector3.one * targetScale, audioTweenDuration).SetEase(audioEase).SetUpdate(true).OnKill(() => audioReactiveTween = null);
    }

    private void KillAudioReactiveTween()
    {
        try
        {
            if (audioReactiveTween != null && audioReactiveTween.active)
                audioReactiveTween.Kill();
        }
        finally
        {
            audioReactiveTween = null;
        }
    }
    #endregion

    #region Amplitude Sampling
    /// <summary>
    /// Get the current amplitude from the audio visualizer based on selected bands
    /// </summary>
    private float GetCurrentAmplitude()
    {
        if (audioVisualizer == null)
            return 0f;

        // Use custom band if set
        if (customBandIndex >= 0)
            return audioVisualizer.GetBandAmplitude(customBandIndex);

        float sum = 0f;
        int count = 0;

        if (useAverageBass)
        {
            for (int i = 0; i < 4; i++)
            {
                sum += audioVisualizer.GetBandAmplitude(i);
                count++;
            }
        }

        if (useMidrange)
        {
            for (int i = 4; i < 11; i++)
            {
                sum += audioVisualizer.GetBandAmplitude(i);
                count++;
            }
        }

        if (useTreble)
        {
            for (int i = 11; i < 16; i++)
            {
                sum += audioVisualizer.GetBandAmplitude(i);
                count++;
            }
        }

        if (count == 0)
            return audioVisualizer.GetAverageAmplitude();

        return sum / count;
    }
    #endregion

    #region Safety & Cleanup
    private bool CanPlayAnimation()
    {
        if (isDestroyed) return false;
        if (canvasRectTransform == null)
        {
            isDestroyed = true;
            return false;
        }
        if (!gameObject.activeInHierarchy) return false;
        return true;
    }

    private void CleanupAllTweens()
    {
        isDestroyed = true;
        KillBaseBreathingSequence();
        KillAudioReactiveTween();
    }

    private void OnDisable()
    {
        CleanupAllTweens();
    }

    private void OnDestroy()
    {
        CleanupAllTweens();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Adjust the breathing parameters at runtime
    /// </summary>
    public void SetBreathingParams(float minScale, float maxScale, float duration)
    {
        breatheMinScale = minScale;
        breatheMaxScale = maxScale;
        breatheDuration = duration;
        CreateBaseBreathingSequence();
    }

    /// <summary>
    /// Adjust audio reactivity at runtime
    /// </summary>
    public void SetAudioReactivity(float tweenDuration, float amplitudeMultiplier)
    {
        audioTweenDuration = tweenDuration;
        audioAmplitudeMultiplier = amplitudeMultiplier;
    }

    /// <summary>
    /// Set which frequency bands to use
    /// </summary>
    public void SetFrequencyBands(bool bass, bool midrange, bool treble)
    {
        useAverageBass = bass;
        useMidrange = midrange;
        useTreble = treble;
        customBandIndex = -1;
    }
    #endregion
}
