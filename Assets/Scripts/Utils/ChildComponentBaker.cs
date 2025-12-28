using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ChildComponentBaker : MonoBehaviour
{
    [Header("Settings")]
    public bool recursive = true;

    [Header("Components to Bake")]
    public List<Component> componentsToPropagate = new List<Component>();

    private void Awake()
    {
        Destroy(this);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ChildComponentBaker))]
public class ChildComponentBakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ChildComponentBaker baker = (ChildComponentBaker)target;

        GUILayout.Space(20);

        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("Bake Components to Children", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Bake Confirmation",
                "Apply settings to all child objects?", "Yes", "No"))
            {
                BakeComponents(baker);
            }
        }
    }

    private void BakeComponents(ChildComponentBaker baker)
    {
        if (baker.componentsToPropagate == null || baker.componentsToPropagate.Count == 0) return;

        Transform[] children;
        if (baker.recursive)
            children = baker.GetComponentsInChildren<Transform>(true);
        else
        {
            children = new Transform[baker.transform.childCount];
            for (int i = 0; i < baker.transform.childCount; i++)
                children[i] = baker.transform.GetChild(i);
        }

        int count = 0;

        foreach (Transform child in children)
        {
            if (child == baker.transform) continue;

            foreach (Component source in baker.componentsToPropagate)
            {
                if (source == null) continue;

                System.Type type = source.GetType();
                Component dest = child.GetComponent(type);

                if (dest == null) dest = Undo.AddComponent(child.gameObject, type);

                Undo.RecordObject(dest, "Propagate Values");
                EditorUtility.CopySerialized(source, dest);
                count++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(baker.gameObject.scene);
        Debug.Log($"Bake Completed. Updated {count} components.");
    }
}
#endif