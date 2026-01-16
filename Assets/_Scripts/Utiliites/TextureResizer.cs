using UnityEngine;
using UnityEditor;
using System.IO;

public class BatchTextureResizer : EditorWindow
{
    private string sourceFolder = "Assets/OriginalTextures";
    private string outputFolder = "Assets/Textures72x72";
    private int targetSize = 72;

    [MenuItem("Tools/Batch Resize Textures to 72x72")]
    public static void ShowWindow()
    {
        GetWindow<BatchTextureResizer>("Batch Texture Resizer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Resize Textures", EditorStyles.boldLabel);

        sourceFolder = EditorGUILayout.TextField("Source Folder", sourceFolder);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        targetSize = EditorGUILayout.IntField("Target Size", targetSize);

        if (GUILayout.Button("Resize All Textures"))
        {
            ResizeTextures();
        }
    }

    private void ResizeTextures()
    {
        if (!Directory.Exists(sourceFolder))
        {
            Debug.LogError("Source folder doesn't exist!");
            return;
        }

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        string[] files = Directory.GetFiles(sourceFolder, "*.png");

        foreach (string filePath in files)
        {
            string fileName = Path.GetFileName(filePath);
            string outputPath = Path.Combine(outputFolder, fileName);

            // Load original texture
            Texture2D original = new Texture2D(2, 2);
            byte[] fileData = File.ReadAllBytes(filePath);
            original.LoadImage(fileData);

            // Create resized texture
            RenderTexture rt = RenderTexture.GetTemporary(targetSize, targetSize);
            RenderTexture.active = rt;
            Graphics.Blit(original, rt);

            Texture2D resized = new Texture2D(targetSize, targetSize, TextureFormat.RGBA32, false);
            resized.ReadPixels(new Rect(0, 0, targetSize, targetSize), 0, 0);
            resized.Apply();

            // Save as PNG
            byte[] pngData = resized.EncodeToPNG();
            File.WriteAllBytes(outputPath, pngData);

            // Clean up
            RenderTexture.ReleaseTemporary(rt);
            RenderTexture.active = null;

            Debug.Log($"Resized: {fileName}");
        }

        AssetDatabase.Refresh();
        Debug.Log($" Resized {files.Length} textures to {targetSize}x{targetSize}px");
    }
}
