using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PrefabLightmapDataGenerator : EditorWindow
{
    private const string DefaultOutputFolder = "Assets/Resources/PrefabLightmaps";
    private const string Test1Scene = "Assets/Scenes/4.unity";
    private const string Test2Scene = "Assets/Scenes/4 1.unity";
    private const string Test1Prefab = "Assets/Source/test/test1.prefab";
    private const string Test2Prefab = "Assets/Source/test/test2.prefab";

    [SerializeField] private GameObject prefab;
    [SerializeField] private SceneAsset bakedScene;
    [SerializeField] private bool useCurrentScene = true;
    [SerializeField] private bool bindToPrefab = true;
    [SerializeField] private string outputFolder = DefaultOutputFolder;

    [MenuItem("Tools/Lightmaps/Prefab Lightmap Tool")]
    private static void OpenWindow()
    {
        GetWindow<PrefabLightmapDataGenerator>("Prefab Lightmap");
    }

    [MenuItem("Tools/Lightmaps/Generate Data From Current Scene")]
    private static void GenerateFromCurrentSceneMenu()
    {
        GameObject selectedPrefab = Selection.activeObject as GameObject;
        if (selectedPrefab == null || PrefabUtility.GetPrefabAssetType(selectedPrefab) == PrefabAssetType.NotAPrefab)
        {
            EditorUtility.DisplayDialog("Prefab Lightmap Data", "Select the baked prefab asset first.", "OK");
            return;
        }

        string prefabPath = AssetDatabase.GetAssetPath(selectedPrefab);
        string assetPath = GetDataAssetPath(DefaultOutputFolder, selectedPrefab.name);
        GenerateForOpenScene(prefabPath, assetPath, true);
    }

    [MenuItem("Tools/Lightmaps/Generate Test1 And Test2 Data")]
    private static void GenerateTestDataMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        GenerateFromScene(Test1Scene, Test1Prefab, GetDataAssetPath(DefaultOutputFolder, "test1"), true);
        GenerateFromScene(Test2Scene, Test2Prefab, GetDataAssetPath(DefaultOutputFolder, "test2"), true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Prefab Lightmap Data", "Generated and bound test1/test2 lightmap data.", "OK");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Prefab Lightmap Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);
        useCurrentScene = EditorGUILayout.Toggle("Use Current Scene", useCurrentScene);

        using (new EditorGUI.DisabledScope(useCurrentScene))
        {
            bakedScene = (SceneAsset)EditorGUILayout.ObjectField("Baked Scene", bakedScene, typeof(SceneAsset), false);
        }

        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        bindToPrefab = EditorGUILayout.Toggle("Bind To Prefab", bindToPrefab);

        EditorGUILayout.Space(10);
        using (new EditorGUI.DisabledScope(!CanGenerate()))
        {
            if (GUILayout.Button("Generate Lightmap Data", GUILayout.Height(32)))
                GenerateFromWindow();
        }

        EditorGUILayout.HelpBox(
            "Bake the prefab in the selected scene first. The baked scene object must match the prefab hierarchy.",
            MessageType.Info
        );
    }

    private bool CanGenerate()
    {
        if (prefab == null)
            return false;

        if (string.IsNullOrWhiteSpace(outputFolder))
            return false;

        return useCurrentScene || bakedScene != null;
    }

    private void GenerateFromWindow()
    {
        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            EditorUtility.DisplayDialog("Prefab Lightmap Data", "Select a prefab asset.", "OK");
            return;
        }

        string assetPath = GetDataAssetPath(outputFolder, prefab.name);
        if (useCurrentScene)
        {
            GenerateForOpenScene(prefabPath, assetPath, bindToPrefab);
        }
        else
        {
            string scenePath = AssetDatabase.GetAssetPath(bakedScene);
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            GenerateFromScene(scenePath, prefabPath, assetPath, bindToPrefab);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Prefab Lightmap Data", $"Generated {assetPath}.", "OK");
    }

    private static void GenerateFromScene(string scenePath, string prefabPath, string assetPath, bool bindToPrefab)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GenerateForOpenScene(prefabPath, assetPath, bindToPrefab);
    }

    private static void GenerateForOpenScene(string prefabPath, string assetPath, bool bindToPrefab)
    {
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null)
        {
            Debug.LogError($"PrefabLightmapDataGenerator: Prefab not found at {prefabPath}");
            return;
        }

        GameObject sceneRoot = FindSceneObjectForPrefab(prefabAsset);
        if (sceneRoot == null)
        {
            Debug.LogError($"PrefabLightmapDataGenerator: No scene object matches prefab '{prefabAsset.name}' in scene '{SceneManager.GetActiveScene().path}'.");
            return;
        }

        PrefabLightmapData data = LoadOrCreate(assetPath);
        data.lightmapColor = ExtractLightmapColors();
        data.lightmapDir = ExtractLightmapDirs();
        data.shadowMask = ExtractShadowMasks();
        data.renderers = CollectRendererInfo(sceneRoot);

        EditorUtility.SetDirty(data);

        if (bindToPrefab)
            BindDataToPrefab(prefabPath, data);

        Debug.Log($"PrefabLightmapDataGenerator: Generated {assetPath} from {SceneManager.GetActiveScene().path}");
    }

    private static void BindDataToPrefab(string prefabPath, PrefabLightmapData data)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            PrefabLightmapBinding binding = prefabRoot.GetComponent<PrefabLightmapBinding>();
            if (binding == null)
                binding = prefabRoot.AddComponent<PrefabLightmapBinding>();

            binding.lightmapData = data;
            binding.applyOnEnable = true;
            binding.applyOncePerInstance = true;

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static GameObject FindSceneObjectForPrefab(GameObject prefabAsset)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            GameObject match = FindInHierarchy(root.transform, prefabAsset);
            if (match != null)
                return match;
        }

        return null;
    }

    private static GameObject FindInHierarchy(Transform current, GameObject prefabAsset)
    {
        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(current.gameObject);
        if (source == prefabAsset)
            return current.gameObject;

        string expectedName = prefabAsset.name;
        if (current.name == expectedName || current.name == $"{expectedName} (1)")
            return current.gameObject;

        for (int i = 0; i < current.childCount; i++)
        {
            GameObject match = FindInHierarchy(current.GetChild(i), prefabAsset);
            if (match != null)
                return match;
        }

        return null;
    }

    private static PrefabLightmapData LoadOrCreate(string assetPath)
    {
        EnsureFolder(GetFolder(assetPath));

        PrefabLightmapData data = AssetDatabase.LoadAssetAtPath<PrefabLightmapData>(assetPath);
        if (data != null)
            return data;

        data = ScriptableObject.CreateInstance<PrefabLightmapData>();
        AssetDatabase.CreateAsset(data, assetPath);
        return data;
    }

    private static PrefabLightmapData.RendererLightmapInfo[] CollectRendererInfo(GameObject root)
    {
        var infos = new List<PrefabLightmapData.RendererLightmapInfo>();
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer.lightmapIndex < 0 && renderer.realtimeLightmapIndex < 0)
                continue;

            infos.Add(new PrefabLightmapData.RendererLightmapInfo
            {
                rendererPath = GetPath(root.transform, renderer.transform),
                lightmapIndex = renderer.lightmapIndex,
                lightmapScaleOffset = renderer.lightmapScaleOffset,
                realtimeLightmapIndex = renderer.realtimeLightmapIndex,
                realtimeLightmapScaleOffset = renderer.realtimeLightmapScaleOffset
            });
        }

        return infos.ToArray();
    }

    private static Texture2D[] ExtractLightmapColors()
    {
        return ExtractTextures(data => data.lightmapColor);
    }

    private static Texture2D[] ExtractLightmapDirs()
    {
        return ExtractTextures(data => data.lightmapDir);
    }

    private static Texture2D[] ExtractShadowMasks()
    {
        return ExtractTextures(data => data.shadowMask);
    }

    private static Texture2D[] ExtractTextures(Func<LightmapData, Texture2D> selector)
    {
        LightmapData[] lightmaps = LightmapSettings.lightmaps ?? Array.Empty<LightmapData>();
        var textures = new Texture2D[lightmaps.Length];

        for (int i = 0; i < lightmaps.Length; i++)
            textures[i] = selector(lightmaps[i]);

        return textures;
    }

    private static string GetPath(Transform root, Transform target)
    {
        if (root == target)
            return string.Empty;

        var parts = new Stack<string>();
        Transform current = target;

        while (current != null && current != root)
        {
            parts.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", parts.ToArray());
    }

    private static string GetDataAssetPath(string folder, string prefabName)
    {
        return $"{folder.TrimEnd('/')}/{prefabName}_LightmapData.asset";
    }

    private static string GetFolder(string assetPath)
    {
        int slashIndex = assetPath.LastIndexOf('/');
        return slashIndex > 0 ? assetPath.Substring(0, slashIndex) : "Assets";
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }
}
