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
    #endregion

    private void Awake()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CreatePulseAnimations();
    }


 
    #region DOTWEEN PULSE METHODS
    private void CreatePulseAnimations()
    {
        // Kill any existing sequence to avoid conflicts
        pulseSequence?.Kill();

        // Create a new sequence for the pulse effect
        pulseSequence = DOTween.Sequence();

        // Scale from 1 to pulseScale and back to 1
        pulseSequence.Append(rectTransform.DOScale(new Vector3(pulseScale, pulseScale, 1f), pulseDuration * 0.5f).SetEase(pulseEase));

        pulseSequence.Append(rectTransform.DOScale(Vector3.one, pulseDuration * 0.5f).SetEase(pulseEase));

        // Set the loop
        pulseSequence.SetLoops(pulseLoops, LoopType.Restart);
    }
    #endregion
}
