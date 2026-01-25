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
    private Transform poolContainer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize()
    {
        InitializePool();
        Debug.Log("EffectManager Initialized");
    }
    
    private void InitializePool()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        GameObject container = new GameObject("EffectPoolContainer");
        poolContainer = container.transform;
        poolContainer.SetParent(this.transform);

        foreach (var data in effectList)
        {
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
        int randomIndex = Random.Range(0, effectList.Count);
        EffectData randomData = effectList[randomIndex];
        PlayEffect(randomData.key, position, Quaternion.identity);
        Debug.Log("PlayRandomEffect: " + randomData.key);
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

            EffectData data = effectList.Find(x => x.key == key);
            return CreateNewObject(data);
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