using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance;

    [System.Serializable]
    public struct EffectData
    {
        public string key;
        public GameObject prefab;
        public int poolSize;
    }

    public List<EffectData> effectList;
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, EffectData> effectDataMap; 
    private Transform poolContainer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    public void Initialize()
    {
        InitializePool();
        Debug.Log("EffectManager Initialized");
    }
    
    private void InitializePool()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        effectDataMap = new Dictionary<string, EffectData>();

        GameObject container = new GameObject("EffectPoolContainer");
        poolContainer = container.transform;
        poolContainer.SetParent(this.transform);

        foreach (var data in effectList)
        {
            if (!effectDataMap.ContainsKey(data.key))
            {
                effectDataMap.Add(data.key, data);
            }

            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < data.poolSize; i++)
            {
                GameObject obj = CreateNewObject(data);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(data.key, objectPool);
        }
    }

    public void PlayRandomEffect(Vector3 position)
    {
        if (effectList.Count == 0) return;
        int randomIndex = Random.Range(0, effectList.Count);
        EffectData randomData = effectList[randomIndex];
        PlayEffect(randomData.key, position, Quaternion.identity);
    }

    public void PlayEffect(string key, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogWarning($"Effect Key Not Found: {key}");
            return;
        }

        GameObject obj = GetObjectFromPool(key);
        if (obj == null) return;

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        EffectController controller = obj.GetComponent<EffectController>();
        if (controller != null)
        {
            controller.PlayEffect();
            StartCoroutine(ReturnToPoolAfterDelay(key, obj, controller.totalDuration));
        }
        else
        {
            StartCoroutine(ReturnToPoolAfterDelay(key, obj, 2.0f));
        }
    }

    private GameObject GetObjectFromPool(string key)
    {
        Queue<GameObject> pool = poolDictionary[key];
        GameObject obj = null;

        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }

        if (obj != null && !obj.activeInHierarchy)
        {
            return obj;
        }
        else
        {
            if (obj != null) pool.Enqueue(obj);

            if (effectDataMap.TryGetValue(key, out EffectData data))
            {
                return CreateNewObject(data);
            }
            return null;
        }
    }

    private GameObject CreateNewObject(EffectData data)
    {
        if (data.prefab == null) return null;

        GameObject newObj = Instantiate(data.prefab, poolContainer);
        newObj.SetActive(false);

        return newObj;
    }

    private IEnumerator ReturnToPoolAfterDelay(string key, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj.activeInHierarchy)
        {
            obj.SetActive(false);
            poolDictionary[key].Enqueue(obj);
        }
    }
}