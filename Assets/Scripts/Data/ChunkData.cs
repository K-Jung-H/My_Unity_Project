using UnityEngine;

[CreateAssetMenu(fileName = "NewChunk", menuName = "Game/ChunkData")]
public class ChunkData : ScriptableObject
{
    public string chunkName;
    public GameObject chunkPrefab;
    public Sprite icon;
    public bool isMandatory;

    [Range(0f, 100f)]
    public float spawnWeight = 10f;
}
