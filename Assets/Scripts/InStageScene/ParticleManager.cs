using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance;

    [System.Serializable]
    public struct ParticleData
    {
        public string key;
        public GameObject prefab;
        public int poolSize;
    }

    public List<ParticleData> particleList;
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Transform poolContainer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePool()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        poolContainer = new GameObject("ParticlePoolContainer").transform;
        poolContainer.SetParent(this.transform);

        foreach (var data in particleList)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < data.poolSize; i++)
            {
                GameObject obj = Instantiate(data.prefab, poolContainer);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(data.key, objectPool);
        }
    }

    public void PlayParticle(string key, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(key)) return;

        GameObject obj = GetObjectFromPool(key);

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            StartCoroutine(ReturnToPoolAfterDelay(key, obj, ps.main.duration));
        }
        else
        {
            StartCoroutine(ReturnToPoolAfterDelay(key, obj, 2.0f));
        }
    }

    private GameObject GetObjectFromPool(string key)
    {
        Queue<GameObject> pool = poolDictionary[key];

        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            if (!obj.activeInHierarchy)
            {
                return obj;
            }
            else
            {
                pool.Enqueue(obj);
                return CreateNewObject(key);
            }
        }
        else
        {
            return CreateNewObject(key);
        }
    }

    private GameObject CreateNewObject(string key)
    {
        ParticleData data = particleList.Find(x => x.key == key);
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