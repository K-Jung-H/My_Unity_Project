using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor; 
#endif

[CreateAssetMenu(fileName = "GlobalChunkTable", menuName = "Game/Global Chunk Table")]
public class ChunkDataTable : ScriptableObject
{
    [Header("Data List")]
    public List<ChunkData> chunkList = new List<ChunkData>();

    private Dictionary<string, ChunkData> _chunkMap;

    public void Initialize()
    {
        _chunkMap = new Dictionary<string, ChunkData>();
        foreach (var chunk in chunkList)
        {
            if (chunk != null && !_chunkMap.ContainsKey(chunk.chunkName))
            {
                _chunkMap.Add(chunk.chunkName, chunk);
            }
        }
    }

    public ChunkData GetChunk(string name)
    {
        if (_chunkMap == null) Initialize();
        return _chunkMap.TryGetValue(name, out var data) ? data : null;
    }

#if UNITY_EDITOR
    [ContextMenu("Load All ChunkData from Resources")]
    public void LoadAllChunkData()
    {
        chunkList.Clear();
        string[] guids = AssetDatabase.FindAssets("t:ChunkData"); 
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ChunkData asset = AssetDatabase.LoadAssetAtPath<ChunkData>(path);
            if (asset != null)
            {
                chunkList.Add(asset);
            }
        }
        
        Debug.Log($"총 {chunkList.Count}개의 ChunkData를 로드했습니다.");
        EditorUtility.SetDirty(this); 
    }
#endif
}