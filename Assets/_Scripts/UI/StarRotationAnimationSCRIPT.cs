using UnityEngine;
using DG.Tweening;

/// <summary>
/// Rotation direction enum for easy control
/// </summary>
public enum RotationDirection
{
    Clockwise,         // Positive Z rotation (right/down)
    CounterClockwise  // Negative Z rotation (left/up)
}

/// <summary>
/// Gently rotates a RectTransform on the Z-axis using DOTween
/// Includes multiple rotation patterns with directional control
/// </summary>
public class StarRotationAnimationSCRIPT : MonoBehaviour
{
    #region VAR ZONE
    [Header("Rotation Settings")]
    [SerializeField] private float rotationDuration = 4f;          // Duration of one full rotation
    [SerializeField] private float rotationAmount = 360f;          // Degrees to rotate
    [SerializeField] private Ease rotationEase = Ease.Linear;      // Easing function (Linear for smooth spinning)
    [SerializeField] private RotationDirection rotationDirection = RotationDirection.Clockwise;  // Direction of rotation

    [Header("Rotation Mode")]
    [SerializeField] private RotateMode rotateMode = RotateMode.FastBeyond360;  // Handles full 360° rotations

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    [SerializeField] private RectTransform rectTransform;
    private Tween rotationTween;
    private bool isDestroyed = false;
    #endregion

    #region Unity Lifecycle Methods
    private void Awake()
    {
        InitializeReferences();
    }

    private void OnEnable()
    {
        isDestroyed = false;

        if (CanPlayAnimation())
        {
            StartRotation();
        }
    }

    private void OnDisable()
    {
        KillRotationTween();
    }

    private void OnDestroy()
    {
        isDestroyed = true;
        KillRotationTween();
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
        }

        if (rectTransform == null)
        {
            Debug.LogError($"StarRotationAnimationSCRIPT on {gameObject.name}: RectTransform component not found!", gameObject);
            enabled = false;
        }
    }

    #region Main DOTween Logic
    /// <summary>
    /// Start the continuous rotation animation
    /// </summary>
    private void StartRotation()
    {
        KillRotationTween();

        // Calculate final rotation amount based on direction
        float finalRotationAmount = rotationDirection == RotationDirection.Clockwise ? rotationAmount : -rotationAmount;

        // Rotate on Z-axis only
        rotationTween = rectTransform.DOLocalRotate(new Vector3(0, 0, finalRotationAmount), rotationDuration, rotateMode).SetEase(rotationEase).SetLoops(-1, LoopType.Restart).SetRelative()
            .SetUpdate(true);

        if (debugMode)
        {
            string direction = rotationDirection == RotationDirection.Clockwise ? "Clockwise" : "Counter-Clockwise";
            Debug.Log($"StarRotationAnimationSCRIPT on {gameObject.name}: Rotation started. Duration: {rotationDuration}s, Amount: {rotationAmount}°, Direction: {direction}");
        }
    }

    /// <summary>
    /// Kill the rotation tween safely
    /// </summary>
    private void KillRotationTween()
    {
        try
        {
            if (rotationTween != null && rotationTween.active)
            {
                rotationTween.Kill();
            }
            rotationTween = null;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error killing rotation tween: {ex.Message}");
            rotationTween = null;
        }
    }

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
    /// Change rotation speed at runtime
    /// </summary>
    public void SetRotationSpeed(float newDuration)
    {
        rotationDuration = newDuration;
        StartRotation();
    }

    /// <summary>
    /// Change rotation amount at runtime
    /// </summary>
    public void SetRotationAmount(float newAmount)
    {
        rotationAmount = newAmount;
        StartRotation();
    }

    /// <summary>
    /// Change rotation direction at runtime
    /// </summary>
    public void SetRotationDirection(RotationDirection newDirection)
    {
        rotationDirection = newDirection;
        StartRotation();
    }

    /// <summary>
    /// Toggle rotation direction at runtime
    /// </summary>
    public void ToggleRotationDirection()
    {
        rotationDirection = rotationDirection == RotationDirection.Clockwise
            ? RotationDirection.CounterClockwise
            : RotationDirection.Clockwise;
        StartRotation();
    }

    /// <summary>
    /// Pause rotation
    /// </summary>
    public void PauseRotation()
    {
        if (rotationTween != null && rotationTween.active)
        {
            rotationTween.Pause();
        }
    }

    /// <summary>
    /// Resume rotation
    /// </summary>
    public void ResumeRotation()
    {
        if (rotationTween != null && rotationTween.active)
        {
            rotationTween.Play();
        }
    }

    /// <summary>
    /// Stop and reset rotation to zero
    /// </summary>
    public void StopAndReset()
    {
        KillRotationTween();
        if (rectTransform != null)
        {
            rectTransform.localEulerAngles = Vector3.zero;
        }
    }
    #endregion
}