using UnityEngine;

public class ChunkController : MonoBehaviour
{
    [Header("Optimization Targets")]
    [Tooltip("정적 물체의 콜라이더 그룹 (멀어지면 끔)")]
    public GameObject staticCollidersRoot;

    [Tooltip("동적 소품들의 부모 (멀어지면 끔)")]
    public GameObject propsRoot;

    [SerializeField] private DestructibleProp[] props;

    private Vector2Int currentCoord;

    void Reset()
    {
        if (propsRoot != null)
        {
            props = propsRoot.GetComponentsInChildren<DestructibleProp>(true);
        }
    }

    public void Setup(Vector2Int coord)
    {
        currentCoord = coord;

        if (props != null)
        {
            for (int i = 0; i < props.Length; i++)
            {

                props[i].InitProp(currentCoord, i);

                if (WorldObjectDataManager.Instance.IsPropDestroyed(currentCoord, i))
                {
                    props[i].SetDestroyedState();
                }
                else
                {
                    props[i].ResetState();
                }
            }
        }
    }

    public void SetPhysicsState(bool enablePhysics)
    {
        if (staticCollidersRoot != null && staticCollidersRoot.activeSelf != enablePhysics)
        {
            staticCollidersRoot.SetActive(enablePhysics);
        }

        if (propsRoot != null && propsRoot.activeSelf != enablePhysics)
        {
            propsRoot.SetActive(enablePhysics);
        }
    }
}