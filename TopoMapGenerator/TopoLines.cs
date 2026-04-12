using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TopoLines : MonoBehaviour
{
    [Header("Terrain Settings")]
    [Tooltip("The terrain to generate contour lines from")]
    public Terrain terrain;

    [Header("Contour Settings")]
    [Tooltip("Number of elevation bands to display as contour lines")]
    public int numberOfBands = 12;

    [Tooltip("Color of the contour lines")]
    public Color bandColor = Color.white;

    [Tooltip("Background color for areas between contour lines")]
    public Color backgroundColor = Color.clear;

    [Header("Export Settings")]
    [Tooltip("Folder path where generated topo maps will be saved")]
    public string exportFolder = "TopoMaps";

    [Tooltip("Renderer to apply the generated topo map to")]
    public Renderer outputPlain;

    [Tooltip("Generated topographic map texture (automatically created)")]
    public Texture2D topoMap;

    // Constants
    private const string SHADER_TYPE = "HDRP/Lit";
    private const string TOPO_MAP_SUFFIX = "_TopoMap";
    private const float ALPHA_CUTOFF = 0.5f;

    /// Gets the current scene name, or returns a default value if the scene is unnamed.
    private string GetSceneNameOrDefault()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return string.IsNullOrEmpty(sceneName) ? "SceneTexture" : sceneName;
    }

    /// It makes sure that the export folder exists, creating it if necessary.
    private string EnsureSceneFolderExists(string sceneName)
    {
        string folderPath = Path.Combine("Assets", exportFolder);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", exportFolder);
        }

        string sceneFolderPath = Path.Combine(folderPath, sceneName);
        if (!AssetDatabase.IsValidFolder(sceneFolderPath))
        {
            AssetDatabase.CreateFolder(folderPath, sceneName);
        }

        return sceneFolderPath;
    }

    public void GenerateTopoMap()
    {
        topoMap = ContourMap.FromTerrain(terrain, numberOfBands, bandColor, backgroundColor);

        if (outputPlain)
        {
            outputPlain.material.mainTexture = topoMap;
        }

        ContourMap.SaveTextureToSceneFile(topoMap, exportFolder);

        CreateAndSaveMaterial(topoMap);
        SaveTextureToPNG(topoMap);
    }

    private void SaveTextureToPNG(Texture2D texture)
    {
        string sceneName = GetSceneNameOrDefault();
        string sceneFolderPath = EnsureSceneFolderExists(sceneName);

        string fileName = sceneName + TOPO_MAP_SUFFIX + ".png";
        string pngPath = Path.Combine(sceneFolderPath, fileName).Replace("\\", "/");

        byte[] bytes = texture.EncodeToPNG();
        string fullPath = Path.Combine(Application.dataPath, exportFolder, sceneName, fileName);
        System.IO.File.WriteAllBytes(fullPath, bytes);

        AssetDatabase.Refresh();

        // Configure PNG import settings
        TextureImporter importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.textureFormat = TextureImporterFormat.RGBA32;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.SaveAndReimport();
        }

        Debug.Log($"PNG saved: {pngPath}");
    }

    private void CreateAndSaveMaterial(Texture2D texture)
    {
        Debug.Log("Creating material for topo map");

        string sceneName = GetSceneNameOrDefault();
        string sceneFolderPath = EnsureSceneFolderExists(sceneName);
        string materialPath = Path.Combine(sceneFolderPath, sceneName + TOPO_MAP_SUFFIX + ".mat")
            .Replace("\\", "/");
        Debug.Log($"Material path: {materialPath}");

        Shader standardShader = Shader.Find(SHADER_TYPE);
        if (standardShader == null)
        {
            Debug.LogError("HDRP/Lit shader not found!");
            return;
        }

        Material newMaterial = new Material(standardShader);
        newMaterial.mainTexture = texture;

        // Enable alpha clipping
        newMaterial.SetFloat("_AlphaCutoff", ALPHA_CUTOFF);
        newMaterial.SetInt("_AlphaCutoffEnable", 1);

        AssetDatabase.CreateAsset(newMaterial, materialPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Material created and saved: {materialPath}");

        if (outputPlain)
        {
            outputPlain.material = newMaterial;
            Debug.Log("Material assigned to renderer");
        }
        else
        {
            Debug.LogWarning("Output renderer is not assigned");
        }
    }
}
