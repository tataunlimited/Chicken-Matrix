using UnityEngine;
using System.Collections.Generic;

public class ComboLetterVFX_SCRIPT : MonoBehaviour
{
    public enum ComboRank { D, C, B, A, S, SS, SSS }

    [SerializeField] private ParticleSystem particleSystem;
    [SerializeField] private float particleSpacing = 4.0f; // Distance between particles in world units
    [SerializeField] private float particleLifetime = 4.625f; // How long each particle lives

    [SerializeField] private Color[] rankColors = new Color[7];

    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ComboRank currentRank = ComboRank.D;
    private bool isTransitioning = false;

    private Vector2[] currentLetterPositions;
    private Queue<Vector2> positionsToEmit = new Queue<Vector2>();

    private void Start()
    {
        if (particleSystem == null)
            particleSystem = GetComponent<ParticleSystem>();

        mainModule = particleSystem.main;
        emissionModule = particleSystem.emission;

        mainModule.startLifetime = particleLifetime;
        mainModule.gravityModifier = 0f;  

        // Initialize default rank colors (D - SSS)
        rankColors = new Color[]
        {
            new Color(0.8f, 0.4f, 1.0f),   // D - Purple
            new Color(0.3f, 0.6f, 1.0f),   // C - Blue
            new Color(0.2f, 0.9f, 0.4f),   // B - Green
            new Color(1.0f, 0.8f, 0.2f),   // A - Yellow
            new Color(1.0f, 0.4f, 0.2f),   // S - Orange
            new Color(1.0f, 0.2f, 0.2f),   // SS - Red
            new Color(1.0f, 1.0f, 1.0f),   // SSS - White
        };

        currentLetterPositions = LetterOutLineScript.Instance.GetLetterPositions((int)currentRank);

        Debug.Log(" ComboLetterVFX initialized!");
        Invoke("CycleRanks", 1f);
    }

    private void Update()
    {
        if (isTransitioning && positionsToEmit.Count > 0)
        {
            // Emit ALL particles at once (instant letter appearance)
            while (positionsToEmit.Count > 0)
            {
                EmitParticleAtPosition(positionsToEmit.Dequeue());
            }
            isTransitioning = false;
        }
    }

    private void CycleRanks()
    {
        int nextRank = ((int)currentRank + 1) % 7;
        UpdateComboRank((ComboRank)nextRank);
        Invoke("CycleRanks", 2f); // Repeat every 2 seconds
    }

    /// <summary>
    /// Call this to change the combo rank and update letter
    /// </summary>
    public void UpdateComboRank(ComboRank newRank)
    {
        if (newRank == currentRank)
            return;

        currentRank = newRank;
        currentLetterPositions = LetterOutLineScript.Instance.GetLetterPositions((int)newRank);

        particleSystem.Clear();

        // Queue all positions WITH interpolation for filled outline
        positionsToEmit.Clear();
        for (int i = 0; i < currentLetterPositions.Length; i++)
        {
            Vector2 currentPos = currentLetterPositions[i];
            positionsToEmit.Enqueue(currentPos);

            // Fill gaps between points for solid letter outline
            if (i < currentLetterPositions.Length - 1)
            {
                Vector2 nextPos = currentLetterPositions[i + 1];
                Vector2 direction = (nextPos - currentPos).normalized;
                float distance = Vector2.Distance(nextPos, currentPos);

                float step = 0.02f;
                for (float t = step; t < distance; t += step)
                {
                    Vector2 interpolated = currentPos + direction * t;
                    positionsToEmit.Enqueue(interpolated);
                }
            }
        }

        // Enable size over lifetime for shrink effect
        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;

        AnimationCurve shrinkCurve = new AnimationCurve();
        shrinkCurve.AddKey(0f, 1f);      // Start at full size
        shrinkCurve.AddKey(0.5f, 0.8f);  // Mid-life slightly smaller
        shrinkCurve.AddKey(1f, 0f);      // Fade to zero at end

        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, shrinkCurve);

        UpdateParticleColor(newRank);
        isTransitioning = true;

        Debug.Log($" Rank {newRank}! Emitting {positionsToEmit.Count} particles");
    }



    private void EmitParticleAtPosition(Vector2 position)
    {
        var emitParams = new ParticleSystem.EmitParams();

        // 2D world position - NO Z-AXIS
        Vector3 worldPos = transform.position + new Vector3(
            position.x * particleSpacing,
            position.y * particleSpacing,
            0  //  CRITICAL: Always 0 for 2D
        );
        emitParams.position = worldPos;

        // ZERO VELOCITY - particles stay in place for lette
        emitParams.velocity = Vector3.zero;  

        // Slight size variation
        emitParams.startSize = Random.Range(0.065f, 0.115f);

        // CORRECT: Use particleSystem, not emissionModule
        particleSystem.Emit(emitParams, 1);
    }


    private void UpdateParticleColor(ComboRank rank)
    {
        var colorOverLifetime = particleSystem.colorOverLifetime;
        Color rankColor = rankColors[(int)rank];

        // Enable color over lifetime
        colorOverLifetime.enabled = true;

        // Create gradient: start with rank color, fade to transparent
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
            new GradientColorKey(rankColor, 0f),
            new GradientColorKey(rankColor, 0.6f),
            new GradientColorKey(rankColor, 1f)
            },
            new GradientAlphaKey[]
            {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(0.8f, 0.5f),
            new GradientAlphaKey(0f, 1f)
            }
        );

        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        Debug.Log($" Color set to {rankColor} for rank {rank}");
    }


    /// <summary>
    /// For testing - press button to cycle through ranks
    /// </summary>
    private void OnGUI()
    {
        if (GUILayout.Button("Rank Up", GUILayout.Height(50)))
        {
            int nextRank = ((int)currentRank + 1) % 7;
            UpdateComboRank((ComboRank)nextRank);
        }
    }
}
