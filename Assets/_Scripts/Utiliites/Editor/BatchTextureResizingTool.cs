using System.IO;
using UnityEditor;
using UnityEngine;

public class BatchTextureResizingTool : EditorWindow
{
    private string sourceFolder = "Assets/OriginalTextures";
    private string outputFolder = "Assets/TexturesResized";
    private int targetWidth = 72;
    private int targetHeight = 72;
    private bool maintainAspectRatio = true;
    private FilterMode filterMode = FilterMode.Bilinear;

    [MenuItem("Tools/Batch Texture Resizer")]
    public static void ShowWindow()
    {
        GetWindow<BatchTextureResizingTool>("Batch Texture Resizer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Texture Resizer", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Folders
        GUILayout.Label("Folders", EditorStyles.boldLabel);
        sourceFolder = EditorGUILayout.TextField("Source Folder", sourceFolder);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        GUILayout.Space(10);

        // Target Size
        GUILayout.Label("Target Size", EditorStyles.boldLabel);
        targetWidth = EditorGUILayout.IntField("Width", targetWidth);
        targetHeight = EditorGUILayout.IntField("Height", targetHeight);

        // Quick presets
        GUILayout.BeginHorizontal();
        GUILayout.Label("Quick Presets:", EditorStyles.miniLabel);
        if (GUILayout.Button("64x64", GUILayout.Width(60)))
        {
            targetWidth = 64;
            targetHeight = 64;
        }
        if (GUILayout.Button("72x72", GUILayout.Width(60)))
        {
            targetWidth = 72;
            targetHeight = 72;
        }
        if (GUILayout.Button("128x128", GUILayout.Width(80)))
        {
            targetWidth = 128;
            targetHeight = 128;
        }
        if (GUILayout.Button("256x256", GUILayout.Width(80)))
        {
            targetWidth = 256;
            targetHeight = 256;
        }
        if (GUILayout.Button("512x512", GUILayout.Width(80)))
        {
            targetWidth = 512;
            targetHeight = 512;
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        // Options
        GUILayout.Label("Options", EditorStyles.boldLabel);
        maintainAspectRatio = EditorGUILayout.Toggle("Maintain Aspect Ratio", maintainAspectRatio);
        filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", filterMode);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Quality:", EditorStyles.miniLabel);
        if (GUILayout.Button("Point (Pixelated)", GUILayout.Width(120)))
        {
            filterMode = FilterMode.Point;
        }
        if (GUILayout.Button("Bilinear (Smooth)", GUILayout.Width(130)))
        {
            filterMode = FilterMode.Bilinear;
        }
        if (GUILayout.Button("Trilinear (Best)", GUILayout.Width(120)))
        {
            filterMode = FilterMode.Trilinear;
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(15);

        // Info box
        EditorGUILayout.HelpBox(
            "Source: " + sourceFolder + "\n" +
            "Output: " + outputFolder + "\n" +
            "Target: " + targetWidth + "x" + targetHeight + "px\n" +
            "Aspect Ratio: " + (maintainAspectRatio ? "Locked" : "Free"),
            MessageType.Info);
        GUILayout.Space(10);

        // Buttons
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Browse Source Folder", GUILayout.Height(30)))
        {
            string selectedFolder = EditorUtility.OpenFolderPanel("Select Source Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selectedFolder))
            {
                sourceFolder = "Assets" + selectedFolder.Substring(Application.dataPath.Length);
            }
        }

        if (GUILayout.Button("Browse Output Folder", GUILayout.Height(30)))
        {
            string selectedFolder = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selectedFolder))
            {
                outputFolder = "Assets" + selectedFolder.Substring(Application.dataPath.Length);
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        // Main resize button
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Resize All Textures", GUILayout.Height(40)))
        {
            ResizeTextures();
        }
        GUI.backgroundColor = Color.white;
    }

    private void ResizeTextures()
    {
        if (!Directory.Exists(sourceFolder))
        {
            EditorUtility.DisplayDialog("Error", "Source folder doesn't exist!\n\n" + sourceFolder, "OK");
            Debug.LogError("ERROR: Source folder doesn't exist: " + sourceFolder);
            return;
        }

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
            Debug.Log("SUCCESS: Created output folder: " + outputFolder);
        }

        string[] files = Directory.GetFiles(sourceFolder, "*.png");

        if (files.Length == 0)
        {
            EditorUtility.DisplayDialog("No Files Found", "No PNG files found in:\n\n" + sourceFolder, "OK");
            Debug.LogWarning("WARNING: No PNG files found in " + sourceFolder);
            return;
        }

        int successCount = 0;
        int failureCount = 0;

        for (int i = 0; i < files.Length; i++)
        {
            string filePath = files[i];
            string fileName = Path.GetFileName(filePath);
            string outputPath = Path.Combine(outputFolder, fileName);

            // Progress bar
            EditorUtility.DisplayProgressBar(
                "Resizing Textures",
                "Processing " + fileName + " (" + (i + 1) + "/" + files.Length + ")",
                (float)(i + 1) / files.Length);

            try
            {
                // Load original texture
                Texture2D original = new Texture2D(2, 2);
                byte[] fileData = File.ReadAllBytes(filePath);
                original.LoadImage(fileData);

                int newWidth = targetWidth;
                int newHeight = targetHeight;

                // Maintain aspect ratio if enabled
                if (maintainAspectRatio)
                {
                    float aspectRatio = (float)original.width / original.height;
                    float targetAspect = (float)targetWidth / targetHeight;

                    if (aspectRatio > targetAspect)
                    {
                        // Image is wider
                        newHeight = (int)(targetWidth / aspectRatio);
                    }
                    else
                    {
                        // Image is taller
                        newWidth = (int)(targetHeight * aspectRatio);
                    }
                }

                // Create resized texture using RenderTexture
                RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 0);
                rt.filterMode = filterMode;
                RenderTexture.active = rt;
                Graphics.Blit(original, rt);

                Texture2D resized = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
                resized.filterMode = filterMode;
                resized.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
                resized.Apply();

                // Save as PNG
                byte[] pngData = resized.EncodeToPNG();
                File.WriteAllBytes(outputPath, pngData);

                // Clean up
                RenderTexture.ReleaseTemporary(rt);
                RenderTexture.active = null;
                Object.DestroyImmediate(original);
                Object.DestroyImmediate(resized);

                Debug.Log("SUCCESS: Resized " + fileName + " (" + original.width + "x" + original.height + " -> " + newWidth + "x" + newHeight + "px)");
                successCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError("ERROR: Failed to resize " + fileName + ": " + e.Message);
                failureCount++;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();

        // Summary dialog
        string summary = "Resize Complete!\n\n" +
            "SUCCESS: " + successCount + "\n" +
            "FAILED: " + failureCount + "\n\n" +
            "Output folder: " + outputFolder;

        EditorUtility.DisplayDialog("Batch Resize Complete", summary, "OK");
        Debug.Log("\n=== RESIZE SUMMARY ===\n" +
            "Total processed: " + files.Length + "\n" +
            "Successful: " + successCount + "\n" +
            "Failed: " + failureCount + "\n" +
            "Target size: " + targetWidth + "x" + targetHeight + "px\n" +
            "Aspect ratio maintained: " + maintainAspectRatio + "\n" +
            "Filter mode: " + filterMode + "\n" +
            "Output folder: " + outputFolder + "\n");
    }
}