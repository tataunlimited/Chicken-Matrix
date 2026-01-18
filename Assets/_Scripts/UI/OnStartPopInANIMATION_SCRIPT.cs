using UnityEngine;
using DG.Tweening;

/// <summary>
/// Pop animation for RectTransform - scales up from zero with a bounce effect
/// Perfect for UI canvas elements appearing with style
/// Plays automatically on Start() and only once
/// Only affects the RectTransform on this GameObject
/// Animation sequence: 0 -> 1.65 -> 1
/// Ensures proper cleanup and handles edge cases
/// After animation completes, disables itself to allow other scripts to control scale
/// </summary>
public class OnStartPopInANIMATION_SCRIPT : MonoBehaviour
{
    #region VAR ZONE
    [Header("Pop In Settings")]
    [SerializeField] private float popDuration = 0.6f;             // How long the pop animation takes
    [SerializeField] private Vector3 startScale = Vector3.zero;    // Starting scale (0 = invisible)
    [SerializeField] private Vector3 endScale = Vector3.one;       // Ending scale (1 = normal size)
    [SerializeField] private Ease popEase = Ease.OutBack;          // Bouncy ease for pop effect
    [SerializeField] private float overshoot = 1.65f;              // Peak scale (0 -> 1.65 -> 1)

    [Header("Delay")]
    [SerializeField] private float popDelay = 0.2f;                // Delay before pop starts (slight delay to see pop effect)

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    [SerializeField] private RectTransform rectTransform;
    private Tween popTween;
    private Tween disableDelayTween;
    private bool isDestroyed = false;
    private bool hasPlayedAnimation = false;  // Ensures animation only plays once
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeReferences();
    }

    private void Start()
    {
        // Start pop animation on startup (only once)
        if (CanPlayAnimation() && !hasPlayedAnimation)
        {
            PlayPopIn();
            hasPlayedAnimation = true;

            // After pop animation completes, disable this script to let other scripts control the scale
            disableDelayTween = DOVirtual.DelayedCall(popDelay + popDuration, () =>
            {
                if (debugMode)
                {
                    Debug.Log($"OnStartPopInANIMATION_SCRIPT on {gameObject.name}: Pop animation complete. Disabling script to allow other animations to take over.");
                }
                enabled = false;
            });
        }
    }

    private void OnEnable()
    {
        isDestroyed = false;
    }

    private void OnDisable()
    {
        KillPopTween();
        KillDisableDelayTween();
    }

    private void OnDestroy()
    {
        isDestroyed = true;
        KillPopTween();
        KillDisableDelayTween();
    }
    #endregion

    /// <summary>
    /// Initialize RectTransform reference
    /// </summary>
    private void InitializeReferences()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();

            if (rectTransform == null)
            {
                Debug.LogError($"OnStartPopInANIMATION_SCRIPT on {gameObject.name}: RectTransform component not found!", gameObject);
                enabled = false;
                return;
            }
        }
    }

    #region Pop Animation
    /// <summary>
    /// Play the pop-in animation with two stages: 0 -> 1.65 -> 1
    /// Includes slight delay so the pop effect is visible
    /// </summary>
    public void PlayPopIn()
    {
        if (!CanPlayAnimation()) return;

        KillPopTween();

        // Set initial scale to invisible
        rectTransform.localScale = startScale;

        // Create a sequence for the pop animation
        Sequence popSequence = DOTween.Sequence();

        // First stage: scale up to overshoot (0 -> 1.65)
        popSequence.Append(rectTransform
            .DOScale(Vector3.one * overshoot, popDuration * 0.65f)
            .SetEase(popEase));

        // Second stage: settle back to normal (1.65 -> 1)
        popSequence.Append(rectTransform
            .DOScale(endScale, popDuration * 0.35f)
            .SetEase(Ease.OutQuad));

        // Apply delay and update settings
        popSequence.SetDelay(popDelay)
            .SetUpdate(true)
            .OnKill(() => { popTween = null; });

        popTween = popSequence;

        if (debugMode)
        {
            Debug.Log($"OnStartPopInANIMATION_SCRIPT on {gameObject.name}: Pop animation started. Delay: {popDelay}s, Duration: {popDuration}s, Peak Overshoot: {overshoot}");
        }
    }

    /// <summary>
    /// Pop out (reverse animation)
    /// </summary>
    public void PlayPopOut(System.Action onComplete = null)
    {
        if (!CanPlayAnimation()) return;

        KillPopTween();

        // Pop out animation (scale to zero)
        popTween = rectTransform
            .DOScale(startScale, popDuration)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                try
                {
                    onComplete?.Invoke();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error in PlayPopOut callback: {ex.Message}");
                }
            })
            .OnKill(() => { popTween = null; });

        if (debugMode)
        {
            Debug.Log($"OnStartPopInANIMATION_SCRIPT on {gameObject.name}: Pop out animation started.");
        }
    }

    /// <summary>
    /// Kill the pop tween safely with proper cleanup
    /// </summary>
    private void KillPopTween()
    {
        try
        {
            if (popTween != null)
            {
                // Complete the tween (important for cleanup)
                if (popTween.IsPlaying() || popTween.IsActive())
                {
                    popTween.Kill(true);
                }
                else
                {
                    popTween.Kill(false);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error killing pop tween: {ex.Message}");
        }
        finally
        {
            // Always ensure tween reference is null
            popTween = null;
        }
    }

    /// <summary>
    /// Kill the disable delay tween safely
    /// </summary>
    private void KillDisableDelayTween()
    {
        try
        {
            if (disableDelayTween != null)
            {
                if (disableDelayTween.IsPlaying() || disableDelayTween.IsActive())
                {
                    disableDelayTween.Kill(false);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error killing disable delay tween: {ex.Message}");
        }
        finally
        {
            disableDelayTween = null;
        }
    }
    #endregion

    #region Safety Checks
    /// <summary>
    /// Check if animation can play
    /// </summary>
    private bool CanPlayAnimation()
    {
        if (isDestroyed) return false;
        if (rectTransform == null) return false;
        if (!gameObject.activeInHierarchy) return false;
        return true;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Change pop duration at runtime
    /// </summary>
    public void SetPopDuration(float newDuration)
    {
        popDuration = Mathf.Max(0.1f, newDuration);  // Prevent zero or negative duration
    }

    /// <summary>
    /// Change overshoot (peak scale) at runtime
    /// </summary>
    public void SetOvershoot(float newOvershoot)
    {
        overshoot = Mathf.Max(1f, newOvershoot);  // Prevent values less than 1
    }

    /// <summary>
    /// Change pop ease at runtime
    /// </summary>
    public void SetPopEase(Ease newEase)
    {
        popEase = newEase;
    }

    /// <summary>
    /// Change animation delay at runtime
    /// </summary>
    public void SetPopDelay(float newDelay)
    {
        popDelay = Mathf.Max(0f, newDelay);  // Prevent negative delay
    }

    /// <summary>
    /// Reset the animation flag to allow playing again
    /// </summary>
    public void ResetAnimationFlag()
    {
        hasPlayedAnimation = false;
    }

    /// <summary>
    /// Instantly reset to start scale without animation
    /// </summary>
    public void InstantReset()
    {
        KillPopTween();

        if (rectTransform != null)
        {
            rectTransform.localScale = startScale;

            if (debugMode)
            {
                Debug.Log($"OnStartPopInANIMATION_SCRIPT on {gameObject.name}: Instant reset to start scale.");
            }
        }
    }

    /// <summary>
    /// Instantly set to end scale without animation
    /// </summary>
    public void InstantShow()
    {
        KillPopTween();

        if (rectTransform != null)
        {
            rectTransform.localScale = endScale;

            if (debugMode)
            {
                Debug.Log($"OnStartPopInANIMATION_SCRIPT on {gameObject.name}: Instant show at end scale.");
            }
        }
    }
    #endregion
}