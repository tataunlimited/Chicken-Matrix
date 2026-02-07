using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.Core
{
    /// <summary>
    /// Controls the credits display that appears after winning the game.
    /// The credits fade in, then scroll upward with text fading out at top and in at bottom.
    /// </summary>
    public class CreditsController : MonoBehaviour
    {
        public static CreditsController Instance { get; private set; }

        [Header("References")]
        [SerializeField] private TMP_Text creditsText;
        [SerializeField] private RectTransform creditsContainer;
        [SerializeField] private RectMask2D scrollMask;
        [SerializeField] private Image thankYouImage;

        [Header("Fade In Settings")]
        [SerializeField] private float delayBeforeFadeIn = 2f;
        [SerializeField] private float fadeInDuration = 2f;

        [Header("Scroll Settings")]
        [Tooltip("How much of the viewport height to use for fade zones at top and bottom")]
        [SerializeField] private float fadeZonePercent = 0.15f;

        private CanvasGroup _canvasGroup;
        private float _scrollDuration;
        private bool _isPlaying;
        private Coroutine _creditsCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Start invisible
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Starts the credits sequence. Call this when the player reaches combo 100.
        /// </summary>
        /// <param name="totalDuration">Total time available for the entire credits sequence (should end 3 seconds before track ends)</param>
        public void StartCredits(float totalDuration)
        {
            if (_isPlaying) return;

            gameObject.SetActive(true);

            // Calculate scroll duration: total time minus fade-in delay and fade-in duration
            _scrollDuration = totalDuration - delayBeforeFadeIn - fadeInDuration;

            if (_scrollDuration < 1f)
            {
                Debug.LogWarning("CreditsController: Not enough time for credits scroll. Adjusting durations.");
                _scrollDuration = Mathf.Max(1f, totalDuration * 0.5f);
            }

            _creditsCoroutine = StartCoroutine(PlayCreditsSequence());
        }

        /// <summary>
        /// Stops the credits sequence immediately.
        /// </summary>
        public void StopCredits()
        {
            if (_creditsCoroutine != null)
            {
                StopCoroutine(_creditsCoroutine);
                _creditsCoroutine = null;
            }
            _isPlaying = false;
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private IEnumerator PlayCreditsSequence()
        {
            _isPlaying = true;

            // Calculate total content height (thank you image + credits text)
            float imageHeight = 0f;
            if (thankYouImage != null)
            {
                imageHeight = thankYouImage.rectTransform.rect.height;
            }
            float textHeight = creditsText.preferredHeight;
            float totalContentHeight = imageHeight + textHeight;
            float viewportHeight = scrollMask != null ? scrollMask.rectTransform.rect.height : 800f;

            // Set initial position BEFORE fade-in (content starts below the viewport)
            creditsContainer.anchoredPosition = new Vector2(0, -viewportHeight);

            // Wait before starting fade in
            yield return new WaitForSeconds(delayBeforeFadeIn);

            // Fade in the credits (already positioned at bottom)
            _canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.InOutSine);
            yield return new WaitForSeconds(fadeInDuration);

            // Scroll the credits upward (scroll enough to move all content through)
            creditsContainer.DOAnchorPosY(totalContentHeight, _scrollDuration)
                .SetEase(Ease.Linear);

            yield return new WaitForSeconds(_scrollDuration);

            _isPlaying = false;
        }

        /// <summary>
        /// Sets the credits text content.
        /// </summary>
        public void SetCreditsText(string text)
        {
            if (creditsText != null)
            {
                creditsText.text = text;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
