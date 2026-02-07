using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Fully procedural letter shape generator
/// All letter shapes defined in code using Vector2[] arrays
/// Integrates seamlessly with ComboLetterVFX_SCRIPT
/// </summary>
public class LetterOutLineScript : MonoBehaviour
{
//    [System.Serializable]
//    public class LetterShape
//    {
//        public string letterName;
//        public Vector2[] particlePositions;
//    }
//
//    public static LetterOutLineScript Instance { get; private set; }
//
//    private LetterShape[] letterShapes = new LetterShape[7];
//
//    // Shape definition settings
//    [Header("Shape Generation Settings")]
//    [SerializeField] private float particleSpacing = 0.05f;
//    [SerializeField] private float shapeScale = 1.0f;
//    [SerializeField] private bool debugLogging = true;
//    [SerializeField] private bool visualizeShapes = false;
//
//    // Shape customization
//    [Header("Letter Customization")]
//    [SerializeField] private bool useThickStrokes = true;
//    [SerializeField] private float strokeThickness = 2.0f;
//    [SerializeField] private bool useSmoothedCorners = true;
//    [SerializeField] private int cornerSmoothingIterations = 2;
//
//    private void Awake()
//    {
//        if (Instance == null)
//        {
//            Instance = this;
//        }
//        else
//        {
//            Destroy(gameObject);
//            return;
//        }
//
//        InitializeLetterOutlines();
//
//        if (debugLogging)
//        {
//            for (int i = 0; i < letterShapes.Length; i++)
//            {
//                if (letterShapes[i] != null)
//                    Debug.Log($"Letter {letterShapes[i].letterName}: {letterShapes[i].particlePositions.Length} particles loaded");
//            }
//        }
//    }
//
//    private void InitializeLetterOutlines()
//    {
//        // Initialize array
//        for (int i = 0; i < letterShapes.Length; i++)
//        {
//            letterShapes[i] = new LetterShape();
//        }
//
//        // Generate all letters from procedural code
//        letterShapes[0] = CreateLetterD_Procedural();
//        letterShapes[1] = CreateLetterC_Procedural();
//        letterShapes[2] = CreateLetterB_Procedural();
//        letterShapes[3] = CreateLetterA_Procedural();
//        letterShapes[4] = CreateLetterS_Procedural();
//        letterShapes[5] = CreateLetterSS_Procedural();
//        letterShapes[6] = CreateLetterSSS_Procedural();
//
//        Debug.Log("Letter outline system initialized with fully procedural shapes!");
//    }
//
//    /// <summary>
//    /// Convert grid array to Vector2 particle positions
//    /// </summary>
//    private Vector2[] GridToParticles(int[,] grid, float offsetX = 0f, float offsetY = 0f)
//    {
//        List<Vector2> particles = new List<Vector2>();
//        int rows = grid.GetLength(0);
//        int cols = grid.GetLength(1);
//
//        for (int row = 0; row < rows; row++)
//        {
//            for (int col = 0; col < cols; col++)
//            {
//                if (grid[row, col] == 1)
//                {
//                    float x = offsetX + (col - cols * 0.5f) * particleSpacing * shapeScale;
//                    float y = offsetY + (rows * 0.5f - row) * particleSpacing * shapeScale;
//                    particles.Add(new Vector2(x, y));
//                }
//            }
//        }
//
//        return particles.ToArray();
//    }
//
//    /// <summary>
//    /// Create letter from outline points with optional stroke thickness
//    /// </summary>
//    private Vector2[] CreateLetterFromOutline(List<Vector2> outlinePoints, float thickness = 0f)
//    {
//        List<Vector2> allParticles = new List<Vector2>(outlinePoints);
//
//        if (useThickStrokes && thickness > 0)
//        {
//            // Create parallel outline for thickness
//            foreach (Vector2 point in outlinePoints)
//            {
//                Vector2 offsetPoint = point + Vector2.one * thickness * 0.01f;
//                allParticles.Add(offsetPoint);
//            }
//        }
//
//        return allParticles.ToArray();
//    }
//
//    /// <summary>
//    /// Draw a line using Bresenham's algorithm for discrete particles
//    /// </summary>
//    private List<Vector2> DrawLine(Vector2 start, Vector2 end, float step = 0.05f)
//    {
//        List<Vector2> linePoints = new List<Vector2>();
//        float distance = Vector2.Distance(start, end);
//        int pointCount = (int)(distance / step) + 1;
//
//        for (int i = 0; i <= pointCount; i++)
//        {
//            float t = pointCount > 0 ? (float)i / pointCount : 0;
//            Vector2 point = Vector2.Lerp(start, end, t);
//            linePoints.Add(point * shapeScale);
//        }
//
//        return linePoints;
//    }
//
//    /// <summary>
//    /// Draw a circular arc
//    /// </summary>
//    private List<Vector2> DrawArc(Vector2 center, float radius, float startAngle, float endAngle, float step = 0.1f)
//    {
//        List<Vector2> arcPoints = new List<Vector2>();
//        float angleRange = endAngle - startAngle;
//        int pointCount = (int)(Mathf.Abs(angleRange) / step) + 1;
//
//        for (int i = 0; i <= pointCount; i++)
//        {
//            float t = pointCount > 0 ? (float)i / pointCount : 0;
//            float angle = startAngle + (angleRange * t);
//            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
//            arcPoints.Add(point * shapeScale);
//        }
//
//        return arcPoints;
//    }
//
//    /// <summary>
//    /// Draw a rectangle outline
//    /// </summary>
//    private List<Vector2> DrawRectangle(Vector2 center, float width, float height)
//    {
//        List<Vector2> rectPoints = new List<Vector2>();
//        float halfWidth = width * 0.5f;
//        float halfHeight = height * 0.5f;
//
//        Vector2 topLeft = center + new Vector2(-halfWidth, halfHeight);
//        Vector2 topRight = center + new Vector2(halfWidth, halfHeight);
//        Vector2 bottomRight = center + new Vector2(halfWidth, -halfHeight);
//        Vector2 bottomLeft = center + new Vector2(-halfWidth, -halfHeight);
//
//        // Top edge
//        rectPoints.AddRange(DrawLine(topLeft, topRight, particleSpacing));
//        // Right edge
//        rectPoints.AddRange(DrawLine(topRight, bottomRight, particleSpacing));
//        // Bottom edge
//        rectPoints.AddRange(DrawLine(bottomRight, bottomLeft, particleSpacing));
//        // Left edge
//        rectPoints.AddRange(DrawLine(bottomLeft, topLeft, particleSpacing));
//
//        return rectPoints;
//    }
//
//    // ========== PROCEDURAL LETTER GENERATORS ==========
//
//    private LetterShape CreateLetterA_Procedural()
//    {
//        List<Vector2> points = new List<Vector2>();
//
//        // Left diagonal
//        points.AddRange(DrawLine(new Vector2(-0.4f, -0.85f), new Vector2(-0.1f, 0.85f), particleSpacing));
//
//        // Right diagonal
//        points.AddRange(DrawLine(new Vector2(0.4f, -0.85f), new Vector2(0.1f, 0.85f), particleSpacing));
//
//        // Horizontal bar (middle)
//        points.AddRange(DrawLine(new Vector2(-0.35f, 0f), new Vector2(0.35f, 0f), particleSpacing));
//
//        // Top peak
//        points.AddRange(DrawLine(new Vector2(-0.1f, 0.85f), new Vector2(0.1f, 0.85f), particleSpacing));
//
//        return new LetterShape { letterName = "A", particlePositions = points.ToArray() };
//    }
//
//    private LetterShape CreateLetterB_Procedural()
//    {
//        List<Vector2> points = new List<Vector2>();
//
//        // Left spine
//        points.AddRange(DrawLine(new Vector2(-0.4f, -0.85f), new Vector2(-0.4f, 0.85f), particleSpacing));
//
//        // Top curve
//        points.AddRange(DrawArc(new Vector2(-0.15f, 0.7f), 0.25f, Mathf.PI, Mathf.PI * 2, 0.05f));
//
//        // Bottom curve
//        points.AddRange(DrawArc(new Vector2(-0.15f, -0.15f), 0.3f, Mathf.PI, Mathf.PI * 2, 0.05f));
//
//        // Middle divider
//        points.AddRange(DrawLine(new Vector2(-0.4f, -0.15f), new Vector2(0.15f, -0.15f), particleSpacing));
//
//        // Closing lines
//        points.AddRange(DrawLine(new Vector2(0.15f, 0.85f), new Vector2(-0.4f, 0.85f), particleSpacing));
//        points.AddRange(DrawLine(new Vector2(0.2f, -0.85f), new Vector2(-0.4f, -0.85f), particleSpacing));
//
//        return new LetterShape { letterName = "B", particlePositions = points.ToArray() };
//    }
//
//    private LetterShape CreateLetterC_Procedural()
//    {
//        List<Vector2> points = new List<Vector2>();
//
//        // Main arc - left curve of C
//        float arcRadius = 0.5f;
//        Vector2 arcCenter = new Vector2(0.1f, 0f);
//
//        // Left arc (main body)
//        points.AddRange(DrawArc(arcCenter, arcRadius, -Mathf.PI * 0.4f, Mathf.PI * 0.4f, 0.05f));
//
//        // Top right extension
//        points.AddRange(DrawLine(
//            arcCenter + new Vector2(arcRadius * Mathf.Cos(-Mathf.PI * 0.4f), arcRadius * Mathf.Sin(-Mathf.PI * 0.4f)),
//            new Vector2(0.4f, 0.75f),
//            particleSpacing
//        ));
//
//        // Bottom right extension
//        points.AddRange(DrawLine(
//            arcCenter + new Vector2(arcRadius * Mathf.Cos(Mathf.PI * 0.4f), arcRadius * Mathf.Sin(Mathf.PI * 0.4f)),
//            new Vector2(0.4f, -0.75f),
//            particleSpacing
//        ));
//
//        return new LetterShape { letterName = "C", particlePositions = points.ToArray() };
//    }
//
//    private LetterShape CreateLetterD_Procedural()
//    {
//        List<Vector2> points = new List<Vector2>();
//
//        // Left spine
//        points.AddRange(DrawLine(new Vector2(-0.4f, -0.85f), new Vector2(-0.4f, 0.85f), particleSpacing));
//
//        // Right curve (semicircle)
//        Vector2 curveCenter = new Vector2(0.1f, 0f);
//        float curveRadius = 0.85f;
//
//        // Arc from top to bottom
//        points.AddRange(DrawArc(curveCenter, curveRadius, -Mathf.PI * 0.5f, Mathf.PI * 0.5f, 0.05f));
//
//        // Top connector
//        points.AddRange(DrawLine(new Vector2(-0.4f, 0.85f), curveCenter + new Vector2(0, curveRadius), particleSpacing));
//
//        // Bottom connector
//        points.AddRange(DrawLine(new Vector2(-0.4f, -0.85f), curveCenter + new Vector2(0, -curveRadius), particleSpacing));
//
//        return new LetterShape { letterName = "D", particlePositions = points.ToArray() };
//    }
//
//    private LetterShape CreateLetterS_Procedural()
//    {
//        List<Vector2> points = new List<Vector2>();
//
//        // Top curve (starts right, curves left)
//        points.AddRange(DrawArc(new Vector2(-0.1f, 0.6f), 0.35f, 0, Mathf.PI, 0.05f));
//
//        // Middle transition
//        points.AddRange(DrawLine(new Vector2(-0.45f, 0.6f), new Vector2(0.25f, 0f), particleSpacing));
//
//        // Bottom curve (starts left, curves right)
//        points.AddRange(DrawArc(new Vector2(0.1f, -0.6f), 0.35f, Mathf.PI, Mathf.PI * 2, 0.05f));
//
//        // Connecting line to middle
//        points.AddRange(DrawLine(new Vector2(0.45f, -0.6f), new Vector2(-0.25f, 0f), particleSpacing));
//
//        return new LetterShape { letterName = "S", particlePositions = points.ToArray() };
//    }
//
//    private LetterShape CreateLetterSS_Procedural()
//    {
//        List<Vector2> sShapePoints = GetProceduralSShape();
//        List<Vector2> allPoints = new List<Vector2>();
//
//        // First S offset to the left
//        foreach (var point in sShapePoints)
//        {
//            allPoints.Add(point + Vector2.left * 0.3f);
//        }
//
//        // Second S offset to the right
//        foreach (var point in sShapePoints)
//        {
//            allPoints.Add(point + Vector2.right * 0.3f);
//        }
//
//        return new LetterShape { letterName = "SS", particlePositions = allPoints.ToArray() };
//    }
//
//    private LetterShape CreateLetterSSS_Procedural()
//    {
//        List<Vector2> sShapePoints = GetProceduralSShape();
//        List<Vector2> allPoints = new List<Vector2>();
//
//        // First S offset to the far left
//        foreach (var point in sShapePoints)
//        {
//            allPoints.Add(point + Vector2.left * 0.6f);
//        }
//
//        // Second S in the middle
//        foreach (var point in sShapePoints)
//        {
//            allPoints.Add(point);
//        }
//
//        // Third S offset to the right
//        foreach (var point in sShapePoints)
//        {
//            allPoints.Add(point + Vector2.right * 0.6f);
//        }
//
//        return new LetterShape { letterName = "SSS", particlePositions = allPoints.ToArray() };
//    }
//
//    private List<Vector2> GetProceduralSShape()
//    {
//        List<Vector2> points = new List<Vector2>();
//
//        // Top curve (starts right, curves left)
//        points.AddRange(DrawArc(new Vector2(-0.1f, 0.6f), 0.35f, 0, Mathf.PI, 0.05f));
//
//        // Middle transition
//        points.AddRange(DrawLine(new Vector2(-0.45f, 0.6f), new Vector2(0.25f, 0f), particleSpacing));
//
//        // Bottom curve (starts left, curves right)
//        points.AddRange(DrawArc(new Vector2(0.1f, -0.6f), 0.35f, Mathf.PI, Mathf.PI * 2, 0.05f));
//
//        // Connecting line to middle
//        points.AddRange(DrawLine(new Vector2(0.45f, -0.6f), new Vector2(-0.25f, 0f), particleSpacing));
//
//        return points;
//    }
//
//    // ========== PUBLIC INTERFACE ==========
//
//    /// <summary>
//    /// GET letter particle positions - used by ComboLetterVFX_SCRIPT
//    /// </summary>
//    public Vector2[] GetLetterPositions(int rankIndex)
//    {
//        if (rankIndex < 0 || rankIndex >= letterShapes.Length)
//        {
//            Debug.LogError($"Invalid rank index: {rankIndex}");
//            return new Vector2[0];
//        }
//
//        if (letterShapes[rankIndex] == null || letterShapes[rankIndex].particlePositions == null)
//        {
//            Debug.LogError($"Letter at index {rankIndex} has no particle data");
//            return new Vector2[0];
//        }
//
//        return letterShapes[rankIndex].particlePositions;
//    }
//
//    /// <summary>
//    /// GET letter name by rank index
//    /// </summary>
//    public string GetLetterName(int rankIndex)
//    {
//        if (rankIndex < 0 || rankIndex >= letterShapes.Length)
//            return "Unknown";
//        return letterShapes[rankIndex].letterName;
//    }
//
//    /// <summary>
//    /// Regenerate all letter shapes with current settings
//    /// </summary>
//    public void RegenerateAllShapes()
//    {
//        InitializeLetterOutlines();
//        Debug.Log("All letter shapes regenerated!");
//    }
//
//#if UNITY_EDITOR
//    /// <summary>
//    /// Editor context menu: Regenerate all letters
//    /// </summary>
//    [ContextMenu("Regenerate Letters")]
//    public void RegenerateLetters()
//    {
//        Debug.Log("Regenerating letter shapes from procedural code...");
//        InitializeLetterOutlines();
//        EditorUtility.SetDirty(this);
//        Debug.Log("Letter shapes regenerated!");
//    }
//
//    /// <summary>
//    /// Editor context menu: Export shape data to debug log
//    /// </summary>
//    [ContextMenu("Export Shape Data")]
//    public void ExportShapeData()
//    {
//        Debug.Log("=== LETTER SHAPE DATA ===");
//        for (int i = 0; i < letterShapes.Length; i++)
//        {
//            if (letterShapes[i] != null)
//            {
//                Debug.Log($"{letterShapes[i].letterName}: {letterShapes[i].particlePositions.Length} particles");
//            }
//        }
//    }
//
//    /// <summary>
//    /// Visualize shapes in the scene for debugging
//    /// </summary>
//    private void OnDrawGizmos()
//    {
//        if (!visualizeShapes || letterShapes == null || letterShapes.Length == 0)
//            return;
//
//        for (int i = 0; i < letterShapes.Length; i++)
//        {
//            if (letterShapes[i]?.particlePositions == null)
//                continue;
//
//            // Different color for each letter
//            Color letterColor = GetColorForLetter(i);
//            Gizmos.color = letterColor;
//
//            foreach (var particle in letterShapes[i].particlePositions)
//            {
//                Vector3 worldPos = transform.position + (Vector3)particle;
//                Gizmos.DrawSphere(worldPos, 0.02f);
//            }
//        }
//    }
//
//    private Color GetColorForLetter(int index)
//    {
//        Color[] colors = {
//            Color.red,
//            Color.green,
//            Color.blue,
//            Color.yellow,
//            Color.cyan,
//            Color.magenta,
//            Color.white
//        };
//        return colors[index % colors.Length];
//    }
//
//#endif
}