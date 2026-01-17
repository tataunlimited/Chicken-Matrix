using UnityEngine;
using _Scripts.Core;

public class CameraRotator : MonoBehaviour
{
    [Header("Base Rotation Settings")]
    [Tooltip("Base rotation speed in degrees per second (at combo 1)")]
    public float baseRotationSpeed = 30f;

    [Tooltip("Rotation direction: 1 = clockwise, -1 = counter-clockwise")]
    [Range(-1, 1)]
    public int direction = 1;

    [Header("Combo Scaling")]
    [Tooltip("Additional degrees per second added per combo point")]
    public float speedPerCombo = 0.5f;
    [Tooltip("Maximum additional speed from combo")]
    public float maxComboBonus = 60f;

    [Header("Boost Settings")]
    [Tooltip("Speed multiplier when boost is triggered")]
    public float boostMultiplier = 2.5f;
    [Tooltip("How long the boost takes to fade back to normal")]
    public float boostDecayDuration = 1f;

    private float _currentBoostMultiplier = 1f;
    private float _boostDecayTimer = 0f;
    private bool _isBoosting = false;

    public static CameraRotator Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Calculate combo-based speed (linear addition)
        float comboBonus = 0f;
        if (GameManager.Instance != null)
        {
            int combo = GameManager.Instance.combo;
            // Add speedPerCombo for each combo point above 1, capped at max
            comboBonus = Mathf.Min((combo - 1) * speedPerCombo, maxComboBonus);
        }

        // Handle boost decay
        if (_isBoosting)
        {
            _boostDecayTimer += Time.deltaTime;
            float t = _boostDecayTimer / boostDecayDuration;

            if (t >= 1f)
            {
                _currentBoostMultiplier = 1f;
                _isBoosting = false;
            }
            else
            {
                // Smooth ease-out decay
                _currentBoostMultiplier = Mathf.Lerp(boostMultiplier, 1f, t * t);
            }
        }

        float finalSpeed = (baseRotationSpeed + comboBonus) * _currentBoostMultiplier;
        float rotationAmount = finalSpeed * direction * Time.deltaTime;
        transform.Rotate(0f, 0f, rotationAmount);
    }

    /// <summary>
    /// Triggers a speed boost that smoothly decays over time
    /// </summary>
    public void TriggerBoost()
    {
        _currentBoostMultiplier = boostMultiplier;
        _boostDecayTimer = 0f;
        _isBoosting = true;
    }

    /// <summary>
    /// Flips the rotation direction and triggers a boost
    /// </summary>
    public void FlipDirectionWithBoost()
    {
        direction = -direction;
        TriggerBoost();
    }
}
