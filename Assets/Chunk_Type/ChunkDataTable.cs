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
        
        Dictionary<string, int> nameFrequency = new Dictionary<string, int>();
        int duplicateCount = 0;

        foreach (var chunk in chunkList)
        {
            if (chunk == null) continue;

            if (string.IsNullOrEmpty(chunk.chunkName))
            {
                Debug.LogError($"[ChunkDataTable] 이름이 없는 청크 데이터가 있습니다! 파일명: {chunk.name}");
                continue;
            }

            string finalKey = chunk.chunkName;

            if (_chunkMap.ContainsKey(finalKey))
            {
                duplicateCount++;

                if (!nameFrequency.ContainsKey(chunk.chunkName))
                {
                    nameFrequency[chunk.chunkName] = 1;
                }
                
                int count = ++nameFrequency[chunk.chunkName];
                finalKey = $"{chunk.chunkName}_{count}";

                while (_chunkMap.ContainsKey(finalKey))
                {
                    count++;
                    finalKey = $"{chunk.chunkName}_{count}";
                }
            }
            else
            {
                nameFrequency[chunk.chunkName] = 1;
            }

            _chunkMap.Add(finalKey, chunk);
        }
        
        if (duplicateCount > 0)
        {
            Debug.LogWarning($"[ChunkDataTable] {duplicateCount}개의 중복된 ChunkName이 감지되어 이름을 자동 변형하여 로드했습니다.");
        }
        else
        {
            Debug.Log($"[ChunkDataTable] 초기화 완료. 총 {_chunkMap.Count}개 로드됨.");
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
        
        HashSet<string> nameChecker = new HashSet<string>();
        int duplicateCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ChunkData asset = AssetDatabase.LoadAssetAtPath<ChunkData>(path);
            
            if (asset != null)
            {
                if (nameChecker.Contains(asset.chunkName))
                {
                    duplicateCount++;
                }
                else
                {
                    nameChecker.Add(asset.chunkName);
                }

                chunkList.Add(asset);
            }
        }
        
        if (duplicateCount > 0)
        {
            Debug.LogWarning($"[LoadAllChunkData] 총 {chunkList.Count}개 로드됨. ( 중복된 이름 {duplicateCount}개 존재)");
        }
        else
        {
            Debug.Log($"[LoadAllChunkData] 총 {chunkList.Count}개 로드 완료. (중복 없음)");
        }
        
        EditorUtility.SetDirty(this); 
    }
#endif
}