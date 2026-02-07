using UnityEngine;
using DG.Tweening;

public class CanvasPulse_SCRIPT : MonoBehaviour
{
    #region VAR ZONE
    [Header("Pulse Settings")]
    [SerializeField] private float pulseScale = 1.085f;
    [SerializeField] private float pulseDuration = 0.2f;
    [SerializeField] private int pulseLoops = -1;
    [SerializeField] private Ease pulseEase = Ease.InOutSine;

    [SerializeField] private RectTransform rectTransform;
    private Sequence pulseSequence;
    private bool isDestroyed = false;
    #endregion

    private void Awake()
    {
        InitializeReferences();
    }

    private void Start()
    {
        if (CanPlayAnimation())
        {
            CreatePulseAnimations();
        }
    }

    /// <summary>
    /// Initialize all required references safely
    /// </summary>
    private void InitializeReferences()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (rectTransform == null)
        {
            Debug.LogError($"CanvasPulse_SCRIPT on {gameObject.name}: RectTransform component not found!", gameObject);
            enabled = false;
        }
    }

    #region DOTWEEN PULSE METHODS
    /// <summary>
    /// Create pulse animations with comprehensive safety checks
    /// </summary>
    private void CreatePulseAnimations()
    {
        if (!CanPlayAnimation()) return;

        // Kill any existing sequence to avoid conflicts
        KillSequence();

        try
        {
            // Create a new sequence for the pulse effect
            pulseSequence = DOTween.Sequence();

            // Scale from 1 to pulseScale and back to 1
            pulseSequence.Append(rectTransform.DOScale(new Vector3(pulseScale, pulseScale, 1f),pulseDuration * 0.5f).SetEase(pulseEase));

            pulseSequence.Append(rectTransform.DOScale( Vector3.one,pulseDuration * 0.5f).SetEase(pulseEase));

            // Set the loop
            pulseSequence.SetLoops(pulseLoops, LoopType.Restart);

            // Add callback when sequence is killed to prevent null reference
            pulseSequence.OnKill(() => pulseSequence = null);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error creating pulse animation: {ex.Message}");
            pulseSequence = null;
        }
    }
    #endregion

    #region Safety Checks
    /// <summary>
    /// Comprehensive null-check before playing any animation
    /// Prevents DOTween from trying to modify destroyed objects
    /// </summary>
    private bool CanPlayAnimation()
    {
        // Check if object is marked as destroyed
        if (isDestroyed)
        {
            return false;
        }

        // Check if RectTransform still exists
        if (rectTransform == null)
        {
            Debug.LogWarning($"CanvasPulse_SCRIPT on {gameObject.name}: RectTransform is null, cannot play animation");
            isDestroyed = true;
            return false;
        }

        // Check if GameObject is still valid and active
        if (!gameObject.activeInHierarchy)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Safe sequence cleanup - handles null sequences gracefully
    /// </summary>
    private void KillSequence()
    {
        try
        {
            if (pulseSequence != null && pulseSequence.active)
            {
                pulseSequence.Kill();
            }
            pulseSequence = null;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error killing sequence: {ex.Message}");
            pulseSequence = null;
        }
    }

    /// <summary>
    /// Master cleanup function - prevents null reference warnings
    /// </summary>
    private void CleanupAllTweens()
    {
        try
        {
            isDestroyed = true;
            KillSequence();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error during CanvasPulse_SCRIPT cleanup: {ex.Message}");
        }
    }
    #endregion

    #region Cleanup Lifecycle
    private void OnDisable()
    {
        CleanupAllTweens();
    }

    private void OnDestroy()
    {
        CleanupAllTweens();
    }
    #endregion
}
