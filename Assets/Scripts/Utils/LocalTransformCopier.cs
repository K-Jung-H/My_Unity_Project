#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class LocalTransformCopier : MonoBehaviour
{

    private static Vector3? copiedPosition;
    private static Quaternion? copiedRotation;
    private static Vector3? copiedScale;

    [MenuItem("Tools/Copy Transform Values #1")]
    static void CopyTransform()
    {
        if (Selection.activeTransform == null) return;
        
        copiedPosition = Selection.activeTransform.localPosition;
        copiedRotation = Selection.activeTransform.localRotation;
        copiedScale = Selection.activeTransform.localScale;
        
        Debug.Log($"Transform Copied (Local): {Selection.activeGameObject.name}");
    }

    [MenuItem("Tools/Paste Transform Values #2")]
    static void PasteTransform()
    {
        if (Selection.activeTransform == null || copiedPosition == null) return;

        Undo.RecordObject(Selection.activeTransform, "Paste Transform Values");
        
        Selection.activeTransform.localPosition = copiedPosition.Value;
        Selection.activeTransform.localRotation = copiedRotation.Value;
        Selection.activeTransform.localScale = copiedScale.Value;
        
        Debug.Log($"Transform Pasted (Local) to: {Selection.activeGameObject.name}");
    }
}

#endif