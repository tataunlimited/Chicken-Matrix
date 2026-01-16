using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Automatically loads letter shapes from texture images or falls back to grid data
/// Integrates seamlessly with ComboLetterVFX_SCRIPT
/// </summary>
public class LetterOutLineScript : MonoBehaviour
{
    [System.Serializable]
    public class LetterShape
    {
        public string letterName;
        public Vector2[] particlePositions;
    }

    public static LetterOutLineScript Instance { get; private set; }

    private LetterShape[] letterShapes = new LetterShape[7];

    // Configuration
    private const float PARTICLE_SCALE = 0.01f;
    private const float LETTER_WIDTH = 1.0f;
    private const float LETTER_HEIGHT = 1.7f;

    // Inspector references for automatic generation
    [Header("Texture-Based Letter Generation")]
    [SerializeField] private Texture2D letterA_Texture;
    [SerializeField] private Texture2D letterB_Texture;
    [SerializeField] private Texture2D letterC_Texture;
    [SerializeField] private Texture2D letterD_Texture;
    [SerializeField] private Texture2D letterS_Texture;
    [SerializeField] private Texture2D letterSS_Texture;
    [SerializeField] private Texture2D letterSSS_Texture;

    [Header("Texture Processing Settings")]
    [SerializeField] private float alphaThreshold = 0.5f;
    [SerializeField] private bool useFallbackGrids = true;
    [SerializeField] private bool debugLogging = true;

    private bool texturesLoaded = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeLetterOutlines();

        if (debugLogging)
        {
            for (int i = 0; i < letterShapes.Length; i++)
            {
                if (letterShapes[i] != null)
                    Debug.Log("Letter " + letterShapes[i].letterName + ": " + letterShapes[i].particlePositions.Length + " particles loaded");
            }
        }
    }

    private void InitializeLetterOutlines()
    {
        // Initialize array
        for (int i = 0; i < letterShapes.Length; i++)
        {
            letterShapes[i] = new LetterShape();
        }

        // Try to load from textures first
        texturesLoaded = LoadLettersFromTextures();

        // If any textures failed or missing, use fallback grid data
        if (!texturesLoaded && useFallbackGrids)
        {
            if (debugLogging)
                Debug.Log("Using fallback grid data for missing letters...");
            LoadLettersFromGrids();
        }

        Debug.Log("Letter outline system initialized! (Textures: " + (texturesLoaded ? "YES" : "NO") + ")");
    }

    /// <summary>
    /// Load letter shapes from texture images
    /// Scans for non-transparent pixels and converts to particle positions
    /// </summary>
    private bool LoadLettersFromTextures()
    {
        try
        {
            bool anyTextureLoaded = false;

            if (letterA_Texture != null)
            {
                letterShapes[3] = new LetterShape
                {
                    letterName = "A",
                    particlePositions = TextureToParticles(letterA_Texture, "A")
                };
                anyTextureLoaded = true;
            }

            if (letterB_Texture != null)
            {
                letterShapes[2] = new LetterShape
                {
                    letterName = "B",
                    particlePositions = TextureToParticles(letterB_Texture, "B")
                };
                anyTextureLoaded = true;
            }

            if (letterC_Texture != null)
            {
                letterShapes[1] = new LetterShape
                {
                    letterName = "C",
                    particlePositions = TextureToParticles(letterC_Texture, "C")
                };
                anyTextureLoaded = true;
            }

            if (letterD_Texture != null)
            {
                letterShapes[0] = new LetterShape
                {
                    letterName = "D",
                    particlePositions = TextureToParticles(letterD_Texture, "D")
                };
                anyTextureLoaded = true;
            }

            if (letterS_Texture != null)
            {
                letterShapes[4] = new LetterShape
                {
                    letterName = "S",
                    particlePositions = TextureToParticles(letterS_Texture, "S")
                };
                anyTextureLoaded = true;
            }

            if (letterSS_Texture != null)
            {
                letterShapes[5] = new LetterShape
                {
                    letterName = "SS",
                    particlePositions = TextureToParticles(letterSS_Texture, "SS")
                };
                anyTextureLoaded = true;
            }

            if (letterSSS_Texture != null)
            {
                letterShapes[6] = new LetterShape
                {
                    letterName = "SSS",
                    particlePositions = TextureToParticles(letterSSS_Texture, "SSS")
                };
                anyTextureLoaded = true;
            }

            if (anyTextureLoaded && debugLogging)
                Debug.Log("Successfully loaded letter shapes from textures!");

            return anyTextureLoaded;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error loading textures: " + e.Message + "\n" + e.StackTrace);
            return false;
        }
    }

    /// <summary>
    /// Convert a texture to particle positions by scanning for non-transparent pixels
    /// Ensures proper aspect ratio and scaling
    /// </summary>
    private Vector2[] TextureToParticles(Texture2D texture, string letterName)
    {
        List<Vector2> particles = new List<Vector2>();

        if (texture == null)
        {
            Debug.LogWarning("Texture for letter '" + letterName + "' is null");
            return particles.ToArray();
        }

        // Verify texture is readable
        if (!texture.isReadable)
        {
            Debug.LogError("Texture '" + texture.name + "' is NOT readable!\n" +
                          "FIX: Select the texture in Project, go to Inspector > Texture Type: Sprite (2D and UI) " +
                          "> Check 'Read/Write Enabled' > Apply");
            return particles.ToArray();
        }

        int width = texture.width;
        int height = texture.height;

        if (debugLogging)
            Debug.Log("Processing texture '" + texture.name + "' for letter " + letterName + " (" + width + "x" + height + ")");

        // Read the texture pixels
        Color[] pixels;
        try
        {
            pixels = texture.GetPixels();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to read pixels from texture '" + texture.name + "': " + e.Message);
            return particles.ToArray();
        }

        if (pixels == null || pixels.Length == 0)
        {
            Debug.LogError("Failed to read pixels from texture '" + texture.name + "'");
            return particles.ToArray();
        }

        // Scan for solid pixels (non-transparent, not white background)
        int pixelsFound = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = y * width + x;

                if (pixelIndex >= pixels.Length)
                    continue;

                Color pixel = pixels[pixelIndex];

                // Detect solid pixels: high alpha AND dark color (black letter on transparent bg)
                // OR transparency detected (PNG with alpha channel)
                bool isSolid = (pixel.a >= alphaThreshold) &&
                              (pixel.r < 0.9f || pixel.g < 0.9f || pixel.b < 0.9f);

                if (isSolid)
                {
                    // Convert pixel coordinates to world space
                    // Flip Y axis: image top (y=0) to world top (positive)
                    float worldX = (x - width * 0.5f) * PARTICLE_SCALE;
                    float worldY = (height * 0.5f - y) * PARTICLE_SCALE;

                    particles.Add(new Vector2(worldX, worldY));
                    pixelsFound++;
                }
            }
        }

        if (debugLogging)
            Debug.Log("Letter '" + letterName + "': Found " + pixelsFound + " solid pixels -> " + particles.Count + " particles");

        return particles.ToArray();
    }

    /// <summary>
    /// Fallback: Load letters from hardcoded grid data
    /// Used when textures aren't available
    /// </summary>
    private void LoadLettersFromGrids()
    {
        letterShapes[0] = new LetterShape { letterName = "D", particlePositions = CreateLetterD() };
        letterShapes[1] = new LetterShape { letterName = "C", particlePositions = CreateLetterC() };
        letterShapes[2] = new LetterShape { letterName = "B", particlePositions = CreateLetterB() };
        letterShapes[3] = new LetterShape { letterName = "A", particlePositions = CreateLetterA() };
        letterShapes[4] = new LetterShape { letterName = "S", particlePositions = CreateLetterS() };
        letterShapes[5] = new LetterShape { letterName = "SS", particlePositions = CreateLetterSS() };
        letterShapes[6] = new LetterShape { letterName = "SSS", particlePositions = CreateLetterSSS() };

        if (debugLogging)
            Debug.Log("Loaded letter shapes from fallback grid data");
    }

    /// <summary>
    /// Convert bitmap grid to world positions (fallback method)
    /// </summary>
    private Vector2[] GridToParticles(int[,] grid, float offsetX = 0f, float offsetY = 0f)
    {
        List<Vector2> particles = new List<Vector2>();
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (grid[row, col] == 1)
                {
                    float x = offsetX + (col - cols * 0.5f) * PARTICLE_SCALE;
                    float y = offsetY + (rows * 0.5f - row) * PARTICLE_SCALE;
                    particles.Add(new Vector2(x, y));
                }
            }
        }

        return particles.ToArray();
    }

    //======== FALLBACK GRID DATA ========
    private Vector2[] CreateLetterD()
    {
        int[,] grid = new int[,]
        {
            {1,1,1,1,1,1,1,1,0,0},
            {1,1,1,1,1,1,1,1,1,0},
            {1,1,0,0,0,0,1,1,1,0},
            {1,1,0,0,0,0,0,1,1,1},
            {1,1,0,0,0,0,0,0,1,1},
            {1,1,0,0,0,0,0,0,1,1},
            {1,1,0,0,0,0,0,0,1,1},
            {1,1,0,0,0,0,0,0,0,1},
            {1,1,0,0,0,0,0,0,0,1},
            {1,1,0,0,0,0,0,0,1,1},
            {1,1,0,0,0,0,0,0,1,1},
            {1,1,0,0,0,0,0,1,1,1},
            {1,1,0,0,0,0,1,1,1,0},
            {1,1,1,1,1,1,1,1,1,0},
            {1,1,1,1,1,1,1,1,0,0},
        };
        return GridToParticles(grid, 0f, 0f);
    }

    private Vector2[] CreateLetterC()
    {
        int[,] grid = new int[,]
        {
            {0,1,1,1,1,1,1,1,1,0},
            {1,1,1,1,1,1,1,1,1,1},
            {1,1,0,0,0,0,0,1,1,1},
            {1,1,0,0,0,0,0,0,0,0},
            {1,1,0,0,0,0,0,0,0,0},
            {1,1,0,0,0,0,0,0,0,0},
            {1,1,0,0,0,0,0,0,0,0},
            {1,1,0,0,0,0,0,0,0,0},
            {1,1,0,0,0,0,0,0,0,0},
            {1,1,0,0,0,0,0,0,0,0},
            {1,1,0,0,0,0,0,0,0,0},
            {1,1,0,0,0,0,0,0,0,0},
            {1,1,0,0,0,0,0,1,1,1},
            {1,1,1,1,1,1,1,1,1,1},
            {0,1,1,1,1,1,1,1,1,0},
        };
        return GridToParticles(grid, 0f, 0f);
    }

    private Vector2[] CreateLetterB()
    {
        int[,] grid = new int[,]
        {
            {1,1,1,1,1,1,1,1,0,0},
            {1,1,1,1,1,1,1,1,1,0},
            {1,1,0,0,0,0,1,1,1,0},
            {1,1,0,0,0,0,0,1,1,1},
            {1,1,0,0,0,0,0,0,1,1},
            {1,1,0,0,0,0,0,1,1,0},
            {1,1,0,0,0,0,1,1,1,0},
            {1,1,1,1,1,1,1,1,0,0},
            {1,1,1,1,1,1,1,1,0,0},
            {1,1,1,1,1,1,1,1,1,0},
            {1,1,0,0,0,0,1,1,1,0},
            {1,1,0,0,0,0,0,1,1,1},
            {1,1,0,0,0,0,0,0,1,1},
            {1,1,0,0,0,0,0,1,1,1},
            {1,1,1,1,1,1,1,1,1,0},
            {1,1,1,1,1,1,1,1,0,0},
        };
        return GridToParticles(grid, 0f, 0f);
    }

    private Vector2[] CreateLetterA()
    {
        int[,] grid = new int[,]
        {
            {0,0,0,1,1,1,1,0,0,0},
            {0,0,1,1,1,1,1,1,0,0},
            {0,0,1,1,0,0,1,1,0,0},
            {0,1,1,0,0,0,0,1,1,0},
            {0,1,1,0,0,0,0,1,1,0},
            {1,1,0,0,0,0,0,0,1,1},
            {1,1,0,0,0,0,0,0,1,1},
            {1,1,1,1,1,1,1,1,1,1},
            {1,1,1,1,1,1,1,1,1,1},
            {1,1,1,1,1,1,1,1,1,1},
            {1,1,0,0,0,0,0,0,1,1},
            {1,1,0,0,0,0,0,0,1,1},
            {1,1,0,0,0,0,0,0,1,1},
            {1,1,0,0,0,0,0,0,1,1},
            {1,1,0,0,0,0,0,0,1,1},
        };
        return GridToParticles(grid, 0f, 0f);
    }

    private Vector2[] CreateLetterS()
    {
        int[,] grid = new int[,]
        {
            {0,1,1,1,1,1,1,1,1,0},
            {1,1,1,1,1,1,1,1,1,1},
            {1,1,0,0,0,0,0,1,1,1},
            {1,1,0,0,0,0,0,0,0,0},
            {1,1,0,0,0,0,0,0,0,0},
            {1,1,1,0,0,0,0,0,0,0},
            {0,1,1,1,1,1,0,0,0,0},
            {0,0,1,1,1,1,1,1,0,0},
            {0,0,0,0,0,1,1,1,1,1},
            {0,0,0,0,0,0,0,1,1,1},
            {0,0,0,0,0,0,0,0,1,1},
            {0,0,0,0,0,0,0,0,1,1},
            {1,1,0,0,0,0,0,0,1,1},
            {1,1,1,1,1,1,1,1,1,1},
            {0,1,1,1,1,1,1,1,1,0},
        };
        return GridToParticles(grid, 0f, 0f);
    }

    private Vector2[] CreateLetterSS()
    {
        List<Vector2> positions = new List<Vector2>();
        int[,] sGrid = GetSGrid();

        Vector2[] s1 = GridToParticles(sGrid, -0.005f, 0f);
        positions.AddRange(s1);

        Vector2[] s2 = GridToParticles(sGrid, 0.005f, 0f);
        positions.AddRange(s2);

        return positions.ToArray();
    }

    private Vector2[] CreateLetterSSS()
    {
        List<Vector2> positions = new List<Vector2>();
        int[,] sGrid = GetSGrid();

        Vector2[] s1 = GridToParticles(sGrid, -0.010f, 0f);
        positions.AddRange(s1);

        Vector2[] s2 = GridToParticles(sGrid, 0f, 0f);
        positions.AddRange(s2);

        Vector2[] s3 = GridToParticles(sGrid, 0.010f, 0f);
        positions.AddRange(s3);

        return positions.ToArray();
    }

    private int[,] GetSGrid()
    {
        return new int[,]
        {
            {0,1,1,1,1,1,1,1,1,0},
            {1,1,1,1,1,1,1,1,1,1},
            {1,1,0,0,0,0,0,1,1,1},
            {1,1,0,0,0,0,0,0,0,0},
            {1,1,0,0,0,0,0,0,0,0},
            {1,1,1,0,0,0,0,0,0,0},
            {0,1,1,1,1,1,0,0,0,0},
            {0,0,1,1,1,1,1,1,0,0},
            {0,0,0,0,0,1,1,1,1,1},
            {0,0,0,0,0,0,0,1,1,1},
            {0,0,0,0,0,0,0,0,1,1},
            {0,0,0,0,0,0,0,0,1,1},
            {1,1,0,0,0,0,0,0,1,1},
            {1,1,1,1,1,1,1,1,1,1},
            {0,1,1,1,1,1,1,1,1,0},
        };
    }

    /// <summary>
    /// GET letter particle positions - used by ComboLetterVFX_SCRIPT
    /// </summary>
    public Vector2[] GetLetterPositions(int rankIndex)
    {
        if (rankIndex < 0 || rankIndex >= letterShapes.Length)
        {
            Debug.LogError("Invalid rank index: " + rankIndex);
            return new Vector2[0];
        }

        if (letterShapes[rankIndex] == null || letterShapes[rankIndex].particlePositions == null)
        {
            Debug.LogError("Letter at index " + rankIndex + " has no particle data");
            return new Vector2[0];
        }

        return letterShapes[rankIndex].particlePositions;
    }

    /// <summary>
    /// GET letter name by rank index
    /// </summary>
    public string GetLetterName(int rankIndex)
    {
        if (rankIndex < 0 || rankIndex >= letterShapes.Length)
            return "Unknown";
        return letterShapes[rankIndex].letterName;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor context menu: Right-click component to Reload Letters From Textures
    /// </summary>
    [ContextMenu("Reload Letters From Textures")]
    public void ReloadLettersFromTextures()
    {
        Debug.Log("Reloading letter shapes from textures...");
        InitializeLetterOutlines();
        EditorUtility.SetDirty(this);
        Debug.Log("Letter shapes reloaded!");
    }

    /// <summary>
    /// Editor context menu: Validate texture settings
    /// </summary>
    [ContextMenu("Check Texture Settings")]
    public void CheckTextureSettings()
    {
        Debug.Log("=== TEXTURE VALIDATION ===");

        Texture2D[] textures = { letterA_Texture, letterB_Texture, letterC_Texture,
                               letterD_Texture, letterS_Texture, letterSS_Texture, letterSSS_Texture };
        string[] names = { "A", "B", "C", "D", "S", "SS", "SSS" };

        for (int i = 0; i < textures.Length; i++)
        {
            if (textures[i] == null)
            {
                Debug.LogWarning("Letter " + names[i] + ": NOT ASSIGNED");
                continue;
            }

            bool isReadable = textures[i].isReadable;
            string status = isReadable ? "OK" : "READ/WRITE DISABLED";
            Debug.Log(names[i] + ": " + textures[i].name + " (" + textures[i].width + "x" + textures[i].height + ") - " + status);

            if (!isReadable)
            {
                Debug.LogError("FIX: Select texture '" + textures[i].name + "' > Inspector > Check 'Read/Write Enabled' > Apply");
            }
        }
    }
#endif
}