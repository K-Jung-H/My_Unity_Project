using UnityEngine;
using UnityEditor;

public class ComponentMover : Editor
{
    [MenuItem("GameObject/Move Components to Parent", false, 0)]
    static void MoveComponents()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null || selectedObject.transform.parent == null)
        {
            Debug.LogWarning("부모 객체가 있거나, 객체가 선택되어야 합니다.");
            return;
        }

        GameObject parentObject = selectedObject.transform.parent.gameObject;
        Component[] components = selectedObject.GetComponents<Component>();

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Move Components to Parent");

        foreach (Component component in components)
        {
            if (component is Transform) continue;

            if (UnityEditorInternal.ComponentUtility.CopyComponent(component))
            {
                if (UnityEditorInternal.ComponentUtility.PasteComponentAsNew(parentObject))
                {
                    Undo.RegisterCreatedObjectUndo(parentObject.GetComponents<Component>()[parentObject.GetComponents<Component>().Length - 1], "Paste Component");
                    Debug.Log($"{component.GetType().Name} 컴포넌트가 {parentObject.name}로 복사되었습니다.");
                }
            }
        }
        
        // foreach (Component component in components)
        // {
        //     if (!(component is Transform)) Undo.DestroyObjectImmediate(component);
        // }
        
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
    }
}