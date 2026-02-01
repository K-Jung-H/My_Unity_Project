using UnityEngine;

public enum ChunkObjectType
{
    None,
    Track,
    Building,
    Nature,
    Prop,
    Logic,
    Ignore
}

[DisallowMultipleComponent]
public class ChunkObj : MonoBehaviour
{
    public ChunkObjectType type = ChunkObjectType.None;
}