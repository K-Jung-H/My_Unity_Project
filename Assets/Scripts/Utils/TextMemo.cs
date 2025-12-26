using UnityEngine;

public class TextMemo : MonoBehaviour
{
#if UNITY_EDITOR
    [TextArea(10, 30)]
    public string devNotes; // 에디터에서만 존재함
#endif
}