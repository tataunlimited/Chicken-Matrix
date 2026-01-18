using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace _Scripts.Core
{
    public class SpinChargeManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image chargeBarFill;
        [SerializeField] private TMP_Text spinCountText;

        [Header("Charge Settings")]
        [Tooltip("Base fill percentage per detection (at 0 stored spins)")]
        [SerializeField] private float baseChargePerDetection = 0.25f;

        [Header("Animation Settings")]
        [Tooltip("Duration to animate each particle's charge increment")]
        [SerializeField] private float particleChargeDuration = 0.05f;
        [Tooltip("Scale punch amount when spin count increases")]
        [SerializeField] private float spinCountPunchScale = 1.5f;
        [Tooltip("Duration of spin count punch animation")]
        [SerializeField] private float spinCountPunchDuration = 0.2f;

        private float _currentCharge = 0f;
        private float _targetCharge = 0f;
        private int _storedSpins = 0;
        private Coroutine _spinPunchCoroutine;
        private Vector3 _spinTextOriginalScale;

        public static SpinChargeManager Instance { get; private set; }

        /// <summary>
        /// Current number of stored spins available
        /// </summary>
        public int StoredSpins => _storedSpins;

        /// <summary>
        /// Returns true if at least one spin charge is available
        /// </summary>
        public bool HasCharge => _storedSpins >= 1;

        private void Awake()
        {
            Instance = this;
            if (spinCountText != null)
            {
                _spinTextOriginalScale = spinCountText.transform.localScale;
            }
            
        }

        private void Start()
        {
            UpdateUI();
        }

        private void Update()
        {
            // Smoothly animate the bar fill toward target
            if (chargeBarFill != null && !Mathf.Approximately(chargeBarFill.fillAmount, _targetCharge))
            {
                float newFill = Mathf.MoveTowards(chargeBarFill.fillAmount, _targetCharge, Time.deltaTime / particleChargeDuration);
                chargeBarFill.fillAmount = newFill;
            }
        }

        /// <summary>
        /// Returns the total charge amount for a detection based on current stored spins.
        /// Called by ParticleConverge to calculate per-particle charge.
        /// </summary>
        public float GetChargeAmountForDetection()
        {
            // 50% at 0 spins, 25% at 1 spin, 12.5% at 2 spins, etc.
            return baseChargePerDetection / Mathf.Pow(2f, _storedSpins);
        }

        /// <summary>
        /// Called when a single particle converges. Adds the specified charge amount.
        /// </summary>
        public void OnParticleConverged(float chargeAmount)
        {
            _currentCharge += chargeAmount;
            _targetCharge = _currentCharge;
            Debug.Log($"[SpinChargeManager] OnParticleConverged: +{chargeAmount:F4}, total now: {_currentCharge:F4}, storedSpins: {_storedSpins}");

            // Check if bar is full
            if (_currentCharge >= 1f)
            {
                // Overflow carries to next charge cycle
                float overflow = _currentCharge - 1f;
                _currentCharge = overflow;
                _targetCharge = overflow;
                _storedSpins++;

                // Reset bar to overflow amount
                if (chargeBarFill != null)
                {
                    chargeBarFill.fillAmount = overflow;
                }

                AnimateSpinCountPunch();
                UpdateSpinCountText();
            }
        }

        /// <summary>
        /// Consumes one stored spin charge. Returns true if successful.
        /// </summary>
        public bool ConsumeCharge()
        {
            if (_storedSpins >= 1)
            {
                _storedSpins--;
                UpdateSpinCountText();
                AnimateSpinCountPunch();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets all charges (e.g., on game over or restart)
        /// </summary>
        public void ResetCharges()
        {
            _currentCharge = 0f;
            _targetCharge = 0f;
            _storedSpins = 0;
            UpdateUI();
        }

        private void AnimateSpinCountPunch()
        {
            if (spinCountText == null) return;

            if (_spinPunchCoroutine != null)
            {
                StopCoroutine(_spinPunchCoroutine);
            }
            _spinPunchCoroutine = StartCoroutine(AnimateSpinCountPunchCoroutine());
        }

        private IEnumerator AnimateSpinCountPunchCoroutine()
        {
            float elapsed = 0f;
            Vector3 punchedScale = _spinTextOriginalScale * spinCountPunchScale;

            // Scale up
            while (elapsed < spinCountPunchDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (spinCountPunchDuration * 0.5f);
                spinCountText.transform.localScale = Vector3.Lerp(_spinTextOriginalScale, punchedScale, t);
                yield return null;
            }

            // Scale back down
            elapsed = 0f;
            while (elapsed < spinCountPunchDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (spinCountPunchDuration * 0.5f);
                spinCountText.transform.localScale = Vector3.Lerp(punchedScale, _spinTextOriginalScale, t);
                yield return null;
            }

            spinCountText.transform.localScale = _spinTextOriginalScale;
            _spinPunchCoroutine = null;
        }

        private void UpdateUI()
        {
            if (chargeBarFill != null)
            {
                chargeBarFill.fillAmount = _currentCharge;
            }
            _targetCharge = _currentCharge;
            UpdateSpinCountText();
        }

        private void UpdateSpinCountText()
        {
            if (spinCountText != null)
            {
                spinCountText.text = _storedSpins.ToString();
            }
        }
    }
}
