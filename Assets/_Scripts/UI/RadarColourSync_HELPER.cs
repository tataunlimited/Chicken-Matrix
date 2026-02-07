using UnityEngine;
using UnityEngine.UI;
using _Scripts.Core;

public class RadarRankImageSync_Script : MonoBehaviour
{
    #region VAR ZONE
    [Header("UI References")]
    [SerializeField] private Image targetImage;

    [Header("Rank Sprites")]
    [SerializeField] private Sprite defaultImage;
    [SerializeField] private Sprite[] comboTxtImages = new Sprite[7]; 
    [Tooltip("Index 0=D, 1=C, 2=B, 3=A, 4=S, 5=SS, 6=SSS")]

    private GameManager gameManager;
    private int currentRankIndex = -1; // -1 = no rank, 0-6 = rank indices

    #endregion

    private void Start()
    {
        gameManager = GameManager.Instance;

        if (targetImage == null)
        {
            Debug.LogError("RadarRankImageSync: Target Image not assigned!");
            return;
        }

        if (gameManager == null)
        {
            Debug.LogWarning("RadarRankImageSync: GameManager instance not found!");
            return;
        }

        // Set default image initially
        if (defaultImage != null)
        {
            targetImage.sprite = defaultImage;
        }
    }

    private void Update()
    {
        if (gameManager == null || targetImage == null) return;

        // Get current rank based on combo
        int newRankIndex = GetRankIndex(gameManager.combo);

        // Only update image if rank index changed
        if (newRankIndex != currentRankIndex)
        {
            currentRankIndex = newRankIndex;
            UpdateImageSprite();
        }
    }

    private void UpdateImageSprite()
    {
        // ===== NO RANK STATE =====
        if (currentRankIndex == -1)
        {
            if (defaultImage != null)
            {
                targetImage.sprite = defaultImage;
            }
            Debug.Log("RadarRankImageSync: No rank - displaying default image");
            return;
        }

        // ===== RANKED STATE (D-SSS) =====
        if (comboTxtImages == null || comboTxtImages.Length < 7)
        {
            Debug.LogWarning("RadarRankImageSync: rankImages array not properly configured! Need 7 sprites.");
            if (defaultImage != null)
            {
                targetImage.sprite = defaultImage;
            }
            return;
        }

        Sprite newSprite = comboTxtImages[currentRankIndex];

        if (newSprite != null)
        {
            targetImage.sprite = newSprite;
       
        }
        else
        {
            Debug.LogWarning($"RadarRankImageSync: No sprite assigned for rank index {currentRankIndex}");
            if (defaultImage != null)
            {
                targetImage.sprite = defaultImage;
            }
        }
    }

    private int GetRankIndex(int comboValue)
    {
        if (comboValue <= 10) return -1;       // No rank display
        if (comboValue <= 20) return 0;        // D Rank
        if (comboValue <= 30) return 1;        // C Rank
        if (comboValue <= 50) return 2;        // B Rank
        if (comboValue <= 60) return 3;        // A Rank
        if (comboValue <= 70) return 4;        // S Rank
        if (comboValue <= 89) return 5;        // SS Rank
        return 6;                              // SSS Rank (90+)
    }

    /// <summary>
    /// Manually set which rank image to display (0-6, or -1 for default)
    /// </summary>
    public void SetImageByRankIndex(int index)
    {
        if (index == -1)
        {
            currentRankIndex = -1;
        }
        else
        {
            index = Mathf.Clamp(index, 0, 6);
            currentRankIndex = index;
        }
        UpdateImageSprite();
    }

    /// <summary>
    /// Force update the image immediately
    /// </summary>
    public void ForceUpdate()
    {
        int newRankIndex = GetRankIndex(gameManager.combo);
        if (newRankIndex != currentRankIndex)
        {
            currentRankIndex = newRankIndex;
            UpdateImageSprite();
        }
    }
}
