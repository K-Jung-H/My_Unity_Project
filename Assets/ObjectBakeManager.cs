using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SingleLayerAttribute : PropertyAttribute { }

public class ObjectBakeManager : MonoBehaviour
{
    public enum ObjectType
    {
        StaticEnvironment,
        DynamicProp,
        Track
    }

    public enum ColliderType { Box, Mesh, Sphere, Capsule }

    [Header("Target Root")]
    public Transform targetRoot;
    public bool recursiveSearch = true;

    [Header("Bake Settings")]
    public ObjectType objectType;
    public ColliderType colliderType;

    [SingleLayer]
    public int layerIndex = 0;

    [Header("Dynamic Options (Only for DynamicProp)")]
    public float mass = 10f;
    public bool addDestructibleScript = true;
    public float pushPower = 2.0f;
    public float hitThreshold = 1.0f;

    public void BakeObjects()
    {
        if (targetRoot == null)
        {
            Debug.LogError("[ObjectBakeManager] Target Root is missing.");
            return;
        }

        int processedCount = 0;
        Transform[] targets;

        if (recursiveSearch)
        {
            targets = targetRoot.GetComponentsInChildren<Transform>(true);
        }
        else
        {
            var childList = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in targetRoot) childList.Add(child);
            targets = childList.ToArray();
        }

        foreach (Transform child in targets)
        {
            if (child == targetRoot) continue;

            GameObject go = child.gameObject;

            if (go.GetComponent<MeshRenderer>() == null && go.GetComponent<SkinnedMeshRenderer>() == null)
                continue;

            go.layer = layerIndex;

            SetupCollider(go);

            if (objectType == ObjectType.DynamicProp)
            {
                SetupDynamicComponents(go);
            }
            else
            {
                CleanUpDynamicComponents(go);

#if UNITY_EDITOR
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
#endif
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(go);
#endif
            processedCount++;
        }

        Debug.Log($"[ObjectBakeManager] Baked {processedCount} objects under '{targetRoot.name}' as {objectType}.");
    }

    private void SetupCollider(GameObject go)
    {
        Collider[] existingColliders = go.GetComponents<Collider>();
        foreach (Collider col in existingColliders)
        {
            DestroyImmediate(col);
        }

        switch (colliderType)
        {
            case ColliderType.Box: go.AddComponent<BoxCollider>(); break;
            case ColliderType.Sphere: go.AddComponent<SphereCollider>(); break;
            case ColliderType.Capsule: go.AddComponent<CapsuleCollider>(); break;
            case ColliderType.Mesh:
                MeshCollider mc = go.AddComponent<MeshCollider>();
                if (objectType == ObjectType.DynamicProp) mc.convex = true;
                break;
        }
    }

    private void SetupDynamicComponents(GameObject go)
    {
        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();

        rb.mass = mass;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        if (addDestructibleScript)
        {
            DestructibleProp prop = go.GetComponent<DestructibleProp>();
            if (prop == null) prop = go.AddComponent<DestructibleProp>();

            prop.pushPower = pushPower;
            prop.hitThreshold = hitThreshold;
        }
    }

    private void CleanUpDynamicComponents(GameObject go)
    {
        if (go.GetComponent<Rigidbody>()) DestroyImmediate(go.GetComponent<Rigidbody>());
        if (go.GetComponent<DestructibleProp>()) DestroyImmediate(go.GetComponent<DestructibleProp>());
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SingleLayerAttribute))]
public class SingleLayerDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        property.intValue = EditorGUI.LayerField(position, label, property.intValue);
        EditorGUI.EndProperty();
    }
}

[CustomEditor(typeof(ObjectBakeManager))]
public class ObjectBakeManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();

        ObjectBakeManager script = (ObjectBakeManager)target;

        if (GUILayout.Button("Bake This Group", GUILayout.Height(40)))
        {
            script.BakeObjects();
        }
    }
}
#endif