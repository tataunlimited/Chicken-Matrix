using UnityEngine;
using UnityEngine.UI;
using _Scripts.Core;
public class RadarColourSychComboIMG_Script : MonoBehaviour
{
    #region VAR ZONE
    [Header("UI References")]
    [SerializeField] private Image targetImage;

    [Header("Image Sprites")]
    [SerializeField] private Sprite[] colorImages = new Sprite[10];
    [Tooltip("Image to display when no combo color is active")]
    [SerializeField] private Sprite defaultImage;

    [Header("Settings")]
    [SerializeField] private float updateCheckInterval = 0.1f;

    private RadarBackgroundGenerator radarGenerator;
    private int currentColorIndex = -1;
    private float timeSinceLastCheck = 0f;

    #endregion
    private void Start()
    {
        radarGenerator = RadarBackgroundGenerator.Instance;

        if (targetImage == null)
        {
            Debug.LogError("RadarImageColorSync: Target Image not assigned!");
            return;
        }

        if (radarGenerator == null)
        {
            Debug.LogWarning("RadarImageColorSync: RadarBackgroundGenerator instance not found!");
            return;
        }

        // Set default image
        if (defaultImage != null)
        {
            targetImage.sprite = defaultImage;
        }
    }

    private void Update()
    {
        if (radarGenerator == null || targetImage == null) return;

        timeSinceLastCheck += Time.deltaTime;

        // Only check every interval to avoid performance issues
        if (timeSinceLastCheck < updateCheckInterval) return;
        timeSinceLastCheck = 0f;

        // Get current combo and calculate which color is active
        int combo = Mathf.Max(1, GameManager.Instance != null ? GameManager.Instance.combo : 1);
        int colorIndex = Mathf.Clamp((combo - 1) / 10, 0, 9);

        // Only update image if color index changed
        if (colorIndex != currentColorIndex)
        {
            currentColorIndex = colorIndex;
            UpdateImageSprite();
        }
    }

    private void UpdateImageSprite()
    {
        if (colorImages == null || colorImages.Length < 10)
        {
            Debug.LogWarning("RadarImageColorSync: colorImages array not properly configured!");
            return;
        }

        // Get sprite for current color index
        Sprite newSprite = colorImages[currentColorIndex];

        if (newSprite != null)
        {
            targetImage.sprite = newSprite;
        }
        else
        {
            Debug.LogWarning($"RadarImageColorSync: No sprite assigned for color index {currentColorIndex}");
            if (defaultImage != null)
            {
                targetImage.sprite = defaultImage;
            }
        }
    }

    /// <summary>
    /// Manually set which image to display by color index (0-9)
    /// </summary>
    public void SetImageByColorIndex(int index)
    {
        index = Mathf.Clamp(index, 0, 9);
        currentColorIndex = index;
        UpdateImageSprite();
    }

    /// <summary>
    /// Force update the image immediately
    /// </summary>
    public void ForceUpdate()
    {
        timeSinceLastCheck = updateCheckInterval;
    }
}
