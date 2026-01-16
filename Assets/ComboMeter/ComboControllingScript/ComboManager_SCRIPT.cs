using UnityEngine;

public class ComboManager_SCRIPT : MonoBehaviour
{
    [SerializeField] private ComboLetterVFX_SCRIPT comboVFX;

    [Header("Combo Settings")]
    [SerializeField] private float comboDecayRate = 0.5f; // Decay per second

    private int comboCount = 0;
    private float comboDecayTimer = 0f;
    private ComboLetterVFX_SCRIPT.ComboRank currentRank = ComboLetterVFX_SCRIPT.ComboRank.D;
    private bool isComboActive = false;

    private void Start()
    {
        if (comboVFX == null)
            comboVFX = FindObjectOfType<ComboLetterVFX_SCRIPT>();

        Debug.Log(" ComboManager initialized!");
    }

    private void Update()
    {
        // Only decay if combo is active
        if (isComboActive)
        {
            comboDecayTimer += Time.deltaTime;
            if (comboDecayTimer >= 1f / comboDecayRate)
            {
                comboCount = Mathf.Max(0, comboCount - 1);
                comboDecayTimer = 0f;

                if (comboCount <= 0)
                {
                    isComboActive = false;
                    comboCount = 0;
                }

                UpdateRank();
            }
        }
    }

    /// <summary>
    /// Called when player lands a hit
    /// </summary>
    public void OnComboHit(int pointsGained = 1)
    {
        comboCount += pointsGained;
        comboDecayTimer = 0f; // Reset decay timer
        isComboActive = true;
        UpdateRank();

        Debug.Log($"Combo: {comboCount} (Rank: {currentRank})");
    }

    /// <summary>
    /// Reset combo (called on player damage)
    /// </summary>
    public void ResetCombo()
    {
        comboCount = 0;
        isComboActive = false;
        UpdateRank();

        Debug.Log(" Combo reset");
    }

    private void UpdateRank()
    {
        ComboLetterVFX_SCRIPT.ComboRank newRank = CalculateRank(comboCount);

        if (newRank != currentRank)
        {
            currentRank = newRank;
            comboVFX.UpdateComboRank(newRank);
        }
    }

    private ComboLetterVFX_SCRIPT.ComboRank CalculateRank(int combo)
    {
        return combo switch
        {
            < 5 => ComboLetterVFX_SCRIPT.ComboRank.D,
            < 15 => ComboLetterVFX_SCRIPT.ComboRank.C,
            < 30 => ComboLetterVFX_SCRIPT.ComboRank.B,
            < 50 => ComboLetterVFX_SCRIPT.ComboRank.A,
            < 75 => ComboLetterVFX_SCRIPT.ComboRank.S,
            < 100 => ComboLetterVFX_SCRIPT.ComboRank.SS,
            _ => ComboLetterVFX_SCRIPT.ComboRank.SSS
        };
    }

    // Public getters for UI
    public int GetComboCount() => comboCount;
    public ComboLetterVFX_SCRIPT.ComboRank GetCurrentRank() => currentRank;
    public bool IsComboActive() => isComboActive;
}
