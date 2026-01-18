using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

public class BTN_HoverAnimationSCRIPT : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region VAR ZONE
    [Header("Hover Scale Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float scaleUpDuration = 0.65f;
    [SerializeField] private float scaleDownDuration = 0.15f;
    [SerializeField] private Ease scaleUpEase = Ease.OutQuad;
    [SerializeField] private Ease scaleDownEase = Ease.InQuad;

    [SerializeField] private RectTransform BTNrectTransform;
    private Tween scaleTween;
    private bool isDestroyed = false;
    #endregion

    private void Awake()
    {
        InitializeReferences();
    }

    private void Start()
    {
        // Double-check references are valid
        if (BTNrectTransform == null)
        {
            BTNrectTransform = GetComponent<RectTransform>();
        }
    }

    /// <summary>
    /// Initialize all required references safely
    /// </summary>
    private void InitializeReferences()
    {
        BTNrectTransform = GetComponent<RectTransform>();

        if (BTNrectTransform == null)
        {
            Debug.LogError($"BTN_HoverAnimationSCRIPT on {gameObject.name}: RectTransform component not found!", gameObject);
            enabled = false;
        }
    }

    #region Pointer Events
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanPlayAnimation()) return;

        KillTween();

        // Scale up smoothly
        scaleTween = BTNrectTransform.DOScale(new Vector3(hoverScale, hoverScale, 1f), scaleUpDuration).SetEase(scaleUpEase).OnKill(() => scaleTween = null);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!CanPlayAnimation()) return;

        KillTween();

        // Scale back down quickly
        scaleTween = BTNrectTransform.DOScale(Vector3.one, scaleDownDuration).SetEase(scaleDownEase).OnKill(() => scaleTween = null);
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
        if (BTNrectTransform == null)
        {
            Debug.LogWarning($"BTN_HoverAnimationSCRIPT on {gameObject.name}: RectTransform is null, cannot play animation");
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
    /// Safe tween cleanup - handles null tweens gracefully
    /// </summary>
    private void KillTween()
    {
        try
        {
            if (scaleTween != null && scaleTween.active)
            {
                scaleTween.Kill();
            }
            scaleTween = null;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error killing tween: {ex.Message}");
            scaleTween = null;
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
            KillTween();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error during BTN_HoverAnimationSCRIPT cleanup: {ex.Message}");
        }
    }
    #endregion

    #region Cleanup
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
