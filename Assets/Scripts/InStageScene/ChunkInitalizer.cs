using Unity.AI.Navigation;
using UnityEngine;

public class ChunkInitalizer : MonoBehaviour
{
    void Start()
    {
        var links = GetComponentsInChildren<NavMeshLink>();
        foreach (var link in links)
        {
            link.enabled = false;
            link.enabled = true;
        }
    }
}
