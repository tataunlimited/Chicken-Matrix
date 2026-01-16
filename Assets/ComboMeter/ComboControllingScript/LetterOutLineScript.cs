using UnityEngine;

public class LetterOutLineScript : MonoBehaviour
{
    [System.Serializable]
    public class LetterShape
    {
        public string letterName;
        public Vector2[] particlePositions; // Where each particle should be
    }

    public static LetterOutLineScript Instance { get; private set; }

    private LetterShape[] letterShapes = new LetterShape[7];

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        InitializeLetterOutlines();

        for (int i = 0; i < letterShapes.Length; i++)
        {
            Debug.Log($"Letter {letterShapes[i].letterName}: {letterShapes[i].particlePositions.Length} particles");
        }
    }

    private void InitializeLetterOutlines()
    {
        // Letter D - Outlined vertical line with curve on right
        letterShapes[0] = new LetterShape
        {
            letterName = "D",
            particlePositions = new Vector2[]
            {
                // Vertical line (left side)
                new Vector2(-0.4f, -0.5f), new Vector2(-0.4f, -0.3f), new Vector2(-0.4f, -0.1f),
                new Vector2(-0.4f, 0.1f), new Vector2(-0.4f, 0.3f), new Vector2(-0.4f, 0.5f),
                // Top curve
                new Vector2(-0.3f, 0.5f), new Vector2(-0.1f, 0.55f), new Vector2(0.1f, 0.5f),
                new Vector2(0.3f, 0.4f),
                // Bottom curve
                new Vector2(0.3f, -0.4f), new Vector2(0.1f, -0.55f), new Vector2(-0.1f, -0.5f),
                new Vector2(-0.3f, -0.5f),
                // Inner connections
                new Vector2(-0.2f, 0.2f), new Vector2(0.2f, 0.2f),
                new Vector2(-0.2f, -0.2f), new Vector2(0.2f, -0.2f),
            }
        };

        // Letter C - Open curved line
        letterShapes[1] = new LetterShape
        {
            letterName = "C",
            particlePositions = new Vector2[]
            {
                // Top line (horizontal)
                new Vector2(-0.4f, 0.5f), new Vector2(-0.2f, 0.55f), new Vector2(0f, 0.55f),
                new Vector2(0.2f, 0.5f),
                // Right curve (top)
                new Vector2(0.4f, 0.3f), new Vector2(0.45f, 0.1f),
                // Right curve (bottom)
                new Vector2(0.45f, -0.1f), new Vector2(0.4f, -0.3f),
                // Bottom line (horizontal)
                new Vector2(0.2f, -0.5f), new Vector2(0f, -0.55f), new Vector2(-0.2f, -0.55f),
                new Vector2(-0.4f, -0.5f),
                // Left curve
                new Vector2(-0.45f, -0.3f), new Vector2(-0.48f, -0.1f), new Vector2(-0.48f, 0.1f),
                new Vector2(-0.45f, 0.3f),
            }
        };

        // Letter B - Two bumps
        letterShapes[2] = new LetterShape
        {
            letterName = "B",
            particlePositions = new Vector2[]
            {
                // Vertical line (left)
                new Vector2(-0.4f, -0.5f), new Vector2(-0.4f, -0.2f), new Vector2(-0.4f, 0f),
                new Vector2(-0.4f, 0.2f), new Vector2(-0.4f, 0.5f),
                // Top right bump
                new Vector2(-0.3f, 0.5f), new Vector2(0f, 0.52f), new Vector2(0.3f, 0.4f),
                new Vector2(0.35f, 0.25f), new Vector2(0.3f, 0.15f), new Vector2(0f, 0.1f),
                // Bottom right bump
                new Vector2(0f, 0.05f), new Vector2(0.3f, -0.15f), new Vector2(0.35f, -0.25f),
                new Vector2(0.3f, -0.4f), new Vector2(0f, -0.52f), new Vector2(-0.3f, -0.5f),
                // Middle line
                new Vector2(-0.3f, 0.05f), new Vector2(0.2f, 0.05f), new Vector2(0.2f, 0f),
            }
        };

        // Letter A - Triangle with crossbar
        letterShapes[3] = new LetterShape
        {
            letterName = "A",
            particlePositions = new Vector2[]
            {
                // Left diagonal
                new Vector2(-0.45f, -0.5f), new Vector2(-0.3f, -0.2f), new Vector2(-0.1f, 0.2f),
                new Vector2(0f, 0.5f),
                // Right diagonal
                new Vector2(0.45f, -0.5f), new Vector2(0.3f, -0.2f), new Vector2(0.1f, 0.2f),
                // Top point
                new Vector2(0f, 0.55f),
                // Crossbar
                new Vector2(-0.3f, -0.1f), new Vector2(-0.1f, -0.05f), new Vector2(0.1f, -0.05f),
                new Vector2(0.3f, -0.1f),
                // Base
                new Vector2(-0.4f, -0.5f), new Vector2(-0.2f, -0.52f), new Vector2(0.2f, -0.52f),
                new Vector2(0.4f, -0.5f),
            }
        };

        // Letter S - Curved
        letterShapes[4] = new LetterShape
        {
            letterName = "S",
            particlePositions = new Vector2[]
            {
                // Top curve
                new Vector2(-0.35f, 0.5f), new Vector2(0f, 0.55f), new Vector2(0.35f, 0.5f),
                new Vector2(0.4f, 0.35f), new Vector2(0.35f, 0.25f), new Vector2(0f, 0.2f),
                new Vector2(-0.35f, 0.15f),
                // Middle
                new Vector2(-0.4f, 0.05f), new Vector2(-0.35f, -0.05f), new Vector2(0f, 0f),
                new Vector2(0.35f, -0.05f), new Vector2(0.4f, -0.05f),
                // Bottom curve
                new Vector2(0.35f, -0.15f), new Vector2(0f, -0.2f), new Vector2(-0.35f, -0.25f),
                new Vector2(-0.4f, -0.35f), new Vector2(-0.35f, -0.5f), new Vector2(0f, -0.55f),
                new Vector2(0.35f, -0.5f),
            }
        };

        // Letter SS - Two S side by side
        letterShapes[5] = new LetterShape
        {
            letterName = "SS",
            particlePositions = new Vector2[]
            {
                // First S (left)
                new Vector2(-0.65f, 0.5f), new Vector2(-0.4f, 0.55f), new Vector2(-0.15f, 0.5f),
                new Vector2(-0.1f, 0.35f), new Vector2(-0.15f, 0.25f), new Vector2(-0.4f, 0.2f),
                new Vector2(-0.65f, 0.15f), new Vector2(-0.7f, 0.05f), new Vector2(-0.65f, -0.05f),
                new Vector2(-0.4f, 0f), new Vector2(-0.15f, -0.05f), new Vector2(-0.1f, -0.05f),
                new Vector2(-0.15f, -0.15f), new Vector2(-0.4f, -0.2f), new Vector2(-0.65f, -0.25f),
                new Vector2(-0.7f, -0.35f), new Vector2(-0.65f, -0.5f), new Vector2(-0.4f, -0.55f),
                new Vector2(-0.15f, -0.5f),
                // Second S (right)
                new Vector2(0.15f, 0.5f), new Vector2(0.4f, 0.55f), new Vector2(0.65f, 0.5f),
                new Vector2(0.7f, 0.35f), new Vector2(0.65f, 0.25f), new Vector2(0.4f, 0.2f),
                new Vector2(0.15f, 0.15f), new Vector2(0.1f, 0.05f), new Vector2(0.15f, -0.05f),
                new Vector2(0.4f, 0f), new Vector2(0.65f, -0.05f), new Vector2(0.7f, -0.05f),
                new Vector2(0.65f, -0.15f), new Vector2(0.4f, -0.2f), new Vector2(0.15f, -0.25f),
                new Vector2(0.1f, -0.35f), new Vector2(0.15f, -0.5f), new Vector2(0.4f, -0.55f),
                new Vector2(0.65f, -0.5f),
            }
        };

        // Letter SSS - Three S in a row (compact)
        letterShapes[6] = new LetterShape
        {
            letterName = "SSS",
            particlePositions = new Vector2[]
            {
                // First S (far left)
                new Vector2(-0.9f, 0.4f), new Vector2(-0.7f, 0.43f), new Vector2(-0.5f, 0.4f),
                new Vector2(-0.45f, 0.3f), new Vector2(-0.5f, 0.23f), new Vector2(-0.7f, 0.2f),
                new Vector2(-0.9f, 0.17f), new Vector2(-0.95f, 0.08f), new Vector2(-0.9f, 0f),
                new Vector2(-0.7f, -0.02f), new Vector2(-0.5f, 0f), new Vector2(-0.45f, 0f),
                new Vector2(-0.5f, -0.1f), new Vector2(-0.7f, -0.13f), new Vector2(-0.9f, -0.15f),
                new Vector2(-0.95f, -0.25f), new Vector2(-0.9f, -0.4f), new Vector2(-0.7f, -0.43f),
                new Vector2(-0.5f, -0.4f),
                // Second S (middle)
                new Vector2(-0.15f, 0.4f), new Vector2(0.05f, 0.43f), new Vector2(0.25f, 0.4f),
                new Vector2(0.3f, 0.3f), new Vector2(0.25f, 0.23f), new Vector2(0.05f, 0.2f),
                new Vector2(-0.15f, 0.17f), new Vector2(-0.2f, 0.08f), new Vector2(-0.15f, 0f),
                new Vector2(0.05f, -0.02f), new Vector2(0.25f, 0f), new Vector2(0.3f, 0f),
                new Vector2(0.25f, -0.1f), new Vector2(0.05f, -0.13f), new Vector2(-0.15f, -0.15f),
                new Vector2(-0.2f, -0.25f), new Vector2(-0.15f, -0.4f), new Vector2(0.05f, -0.43f),
                new Vector2(0.25f, -0.4f),
                // Third S (far right)
                new Vector2(0.6f, 0.4f), new Vector2(0.8f, 0.43f), new Vector2(1f, 0.4f),
                new Vector2(1.05f, 0.3f), new Vector2(1f, 0.23f), new Vector2(0.8f, 0.2f),
                new Vector2(0.6f, 0.17f), new Vector2(0.55f, 0.08f), new Vector2(0.6f, 0f),
                new Vector2(0.8f, -0.02f), new Vector2(1f, 0f), new Vector2(1.05f, 0f),
                new Vector2(1f, -0.1f), new Vector2(0.8f, -0.13f), new Vector2(0.6f, -0.15f),
                new Vector2(0.55f, -0.25f), new Vector2(0.6f, -0.4f), new Vector2(0.8f, -0.43f),
                new Vector2(1f, -0.4f),
            }
        };
    }

    public Vector2[] GetLetterPositions(int rankIndex)
    {
        if (rankIndex < 0 || rankIndex >= letterShapes.Length)
            return new Vector2[0];
        return letterShapes[rankIndex].particlePositions;
    }
}
