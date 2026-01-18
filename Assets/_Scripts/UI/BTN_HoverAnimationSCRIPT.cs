using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

public class BTN_HoverAnimationSCRIPT : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region VAR ZONE
    [Header("Hover Scale Settings")]
    [SerializeField] private float hoverScale = 1.1f;       // How much to scale up on hover (1.1 = 10% larger)
    [SerializeField] private float scaleUpDuration = 0.65f;  // Time to scale up
    [SerializeField] private float scaleDownDuration = 0.15f; // Time to scale back down
    [SerializeField] private Ease scaleUpEase = Ease.OutQuad;
    [SerializeField] private Ease scaleDownEase = Ease.InQuad;

    [SerializeField] private RectTransform BTNrectTransform;
    private Tween scaleTween;
    #endregion
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(BTNrectTransform == null)
        {
            BTNrectTransform = GetComponent<RectTransform>();
        }
    }


    #region Pointer Events
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Kill the previous tween if it's still running
        scaleTween?.Kill();

        // Scale up smoothly
        scaleTween = BTNrectTransform.DOScale(new Vector3(hoverScale, hoverScale, 1f), scaleUpDuration).SetEase(scaleUpEase);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Kill the previous tween if it's still running
        scaleTween?.Kill();

        // Scale back down quickly
        scaleTween = BTNrectTransform.DOScale(Vector3.one, scaleDownDuration).SetEase(scaleDownEase);
    }

    #endregion

    #region Cleanup
    private void OnDisable()
    {
        // Clean up the tween when the object is disabled
        scaleTween?.Kill();
    }
    #endregion
}
