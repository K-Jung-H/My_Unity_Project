using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.AI;
using System.Collections.Generic;
using Unity.AI.Navigation;

public class ChunkBaker : EditorWindow
{
    private GameObject sourcePrefab;
    private string outputPath = "Assets/Resources/Chunks - Baked";

    [MenuItem("Tools/Chunk Baker")]
    public static void ShowWindow()
    {
        GetWindow<ChunkBaker>("Chunk Baker");
    }

    private void OnGUI()
    {
        GUILayout.Label("Chunk Bake Settings", EditorStyles.boldLabel);

        sourcePrefab = (GameObject)EditorGUILayout.ObjectField("Source Prefab", sourcePrefab, typeof(GameObject), false);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);

        GUILayout.Space(10);

        if (GUILayout.Button("Bake Chunk"))
        {
            if (sourcePrefab != null)
            {
                BakeChunk();
            }
            else
            {
                Debug.LogError("Source Prefab is missing.");
            }
        }
    }

    private void BakeChunk()
    {
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        GameObject tempSource = Instantiate(sourcePrefab);
        GameObject bakedRoot = new GameObject(sourcePrefab.name + "_Baked");

        Transform staticGroup = new GameObject("Static_Group").transform;
        staticGroup.SetParent(bakedRoot.transform);

        Transform visualRoot = new GameObject("Visual_Root").transform;
        visualRoot.SetParent(staticGroup);

        Transform physicsRoot = new GameObject("Physics_Root").transform;
        physicsRoot.SetParent(staticGroup);

        Transform extractedLogicRoot = new GameObject("Extracted_Logic").transform;
        extractedLogicRoot.SetParent(bakedRoot.transform);

        Transform dynamicPropsRoot = new GameObject("Dynamic_Props_Root").transform;
        dynamicPropsRoot.SetParent(bakedRoot.transform);

        foreach (Transform child in tempSource.transform)
        {
            ProcessRecursive(child, ChunkObjectType.None, visualRoot, physicsRoot, extractedLogicRoot, dynamicPropsRoot, bakedRoot.transform);
        }

        if (extractedLogicRoot.childCount == 0) DestroyImmediate(extractedLogicRoot.gameObject);
        if (dynamicPropsRoot.childCount == 0) DestroyImmediate(dynamicPropsRoot.gameObject);

        AttachRootComponents(bakedRoot);

        string fileName = bakedRoot.name + ".prefab";
        string fullPath = Path.Combine(outputPath, fileName);

        PrefabUtility.SaveAsPrefabAsset(bakedRoot, fullPath);
        Debug.Log($"Chunk Baked Successfully: {fullPath}");

        DestroyImmediate(tempSource);
        DestroyImmediate(bakedRoot);
    }

    private void AttachRootComponents(GameObject root)
    {
        NavMeshSurface surface = root.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
    }

    private void ProcessRecursive(Transform currentSource, ChunkObjectType inheritedType, Transform visualRoot, Transform physicsRoot, Transform logicRoot, Transform dynamicRoot, Transform bakedRoot)
    {
        ChunkObjectType currentType = inheritedType;

        if (currentSource.TryGetComponent(out ChunkObj chunkObj))
        {
            currentType = chunkObj.type;
        }

        if (currentType == ChunkObjectType.Logic)
        {
            GameObject logicCopy = Instantiate(currentSource.gameObject);
            logicCopy.name = currentSource.name;

            if (currentSource.parent.parent == null)
            {
                logicCopy.transform.SetParent(bakedRoot);
            }
            else
            {
                logicCopy.transform.SetParent(logicRoot);
            }

            CleanupChunkObjRecursive(logicCopy.transform);
            return;
        }

        if (currentType == ChunkObjectType.Prop)
        {
            GameObject propCopy = Instantiate(currentSource.gameObject);
            propCopy.name = currentSource.name;
            propCopy.transform.SetParent(dynamicRoot);

            CleanupChunkObjRecursive(propCopy.transform);
            return;
        }

        if (currentType == ChunkObjectType.Ignore)
        {
            return;
        }

        SeparateAndCopy(currentSource, currentType, visualRoot, physicsRoot, logicRoot);

        foreach (Transform child in currentSource)
        {
            ProcessRecursive(child, currentType, visualRoot, physicsRoot, logicRoot, dynamicRoot, bakedRoot);
        }
    }

    private void CleanupChunkObjRecursive(Transform target)
    {
        var cObj = target.GetComponent<ChunkObj>();
        if (cObj != null)
        {
            DestroyImmediate(cObj);
        }

        foreach (Transform child in target)
        {
            CleanupChunkObjRecursive(child);
        }
    }

    private void SeparateAndCopy(Transform source, ChunkObjectType type, Transform visualRoot, Transform physicsRoot, Transform logicRoot)
    {
        if (HasLogicComponents(source.gameObject))
        {
            GameObject logicGo = Instantiate(source.gameObject, logicRoot);
            logicGo.name = source.name;

            var cObj = logicGo.GetComponent<ChunkObj>();
            if (cObj != null)
            {
                DestroyImmediate(cObj);
            }

            foreach (Transform child in logicGo.transform)
            {
                DestroyImmediate(child.gameObject);
            }
            return;
        }

        string category = type == ChunkObjectType.None ? "Uncategorized" : type.ToString();

        if (source.TryGetComponent(out MeshRenderer meshRenderer))
        {
            Transform targetParent = GetOrCreatePath(visualRoot, category);
            GameObject visualGo = Instantiate(source.gameObject, targetParent);
            visualGo.name = source.name;
            SetupVisualObject(visualGo);
        }

        if (source.GetComponent<Collider>() != null)
        {
            Transform targetParent = GetOrCreatePath(physicsRoot, category);
            GameObject physicsGo = Instantiate(source.gameObject, targetParent);
            physicsGo.name = source.name;
            SetupPhysicsObject(physicsGo);
        }
    }

    private bool HasLogicComponents(GameObject obj)
    {
        if (obj.GetComponent<NavMeshLink>() != null) return true;
        if (obj.GetComponent<NavMeshModifierVolume>() != null) return true;

        var scripts = obj.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script == null) continue;
            if (script is ChunkObj) continue;
            if (script is NavMeshModifier) continue;
            return true;
        }
        return false;
    }

    private void SetupVisualObject(GameObject obj)
    {
        var colliders = obj.GetComponents<Collider>();
        foreach (var col in colliders)
        {
            DestroyImmediate(col);
        }

        var chunkObj = obj.GetComponent<ChunkObj>();
        if (chunkObj != null)
        {
            DestroyImmediate(chunkObj);
        }

        var scripts = obj.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (!(script is ChunkObj))
            {
                DestroyImmediate(script);
            }
        }

        foreach (Transform child in obj.transform)
        {
            DestroyImmediate(child.gameObject);
        }
    }

    private void SetupPhysicsObject(GameObject obj)
    {
        var renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            DestroyImmediate(renderer);
        }

        var filter = obj.GetComponent<MeshFilter>();
        if (filter != null)
        {
            DestroyImmediate(filter);
        }

        var chunkObj = obj.GetComponent<ChunkObj>();
        if (chunkObj != null)
        {
            DestroyImmediate(chunkObj);
        }

        var scripts = obj.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (!(script is ChunkObj) && !(script is NavMeshModifier))
            {
                DestroyImmediate(script);
            }
        }

        foreach (Transform child in obj.transform)
        {
            DestroyImmediate(child.gameObject);
        }
    }

    private Transform GetOrCreatePath(Transform root, string subPath)
    {
        Transform current = root;
        string[] steps = subPath.Split('/');

        foreach (string step in steps)
        {
            Transform child = current.Find(step);
            if (child == null)
            {
                GameObject newGo = new GameObject(step);
                child = newGo.transform;
                child.SetParent(current);
            }
            current = child;
        }
        return current;
    }
}