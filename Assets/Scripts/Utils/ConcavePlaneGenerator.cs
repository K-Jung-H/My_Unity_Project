using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode] // 에디터에서 즉시 실행되게 하는 핵심 속성
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ConcavePlaneGenerator : MonoBehaviour
{
    [Header("크기 설정")]
    [Tooltip("전체 평면의 크기 (가로/세로)")]
    public float size = 20f;
    [Tooltip("움푹 파이는 최대 깊이")]
    public float depth = 5f;
    [Tooltip("격자 해상도 (높을수록 부드러워짐)")]
    [Range(10, 200)] public int resolution = 50;

    [Header("모양 조절 (핵심)")]
    [Tooltip("웅덩이의 단면 모양을 결정합니다. (0: 중심, 1: 가장자리)")]
    // 기본값: U자 형태로 설정
    public AnimationCurve slopeCurve = new AnimationCurve(
        new Keyframe(0f, 0f),    // 중심 (깊음)
        new Keyframe(0.2f, 0f),  // 바닥 평평한 구간
        new Keyframe(1f, 1f)     // 가장자리 (높음)
    );

    [Header("재질 (없으면 안보임)")]
    public Material targetMaterial;

    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;
    private MeshRenderer _meshRenderer;

    private void OnEnable()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshCollider = GetComponent<MeshCollider>();
        _meshRenderer = GetComponent<MeshRenderer>();
        
        GenerateMesh(); 
    }

    private void OnValidate()
    {
        if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
        if (_meshRenderer != null && targetMaterial != null) _meshRenderer.sharedMaterial = targetMaterial;
        
        GenerateMesh();
    }

    public void GenerateMesh()
    {
        if (resolution < 2) return;
        if (size <= 0.001f) return;

        Mesh mesh = new Mesh();
        mesh.name = "CustomConcavePlane";

        int vCount = resolution + 1;
        Vector3[] vertices = new Vector3[vCount * vCount];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[resolution * resolution * 6];

        float halfSize = size * 0.5f;
        float step = size / resolution;

        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                float xPos = (x * step) - halfSize;
                float zPos = (z * step) - halfSize;

                
                float dist = Mathf.Sqrt(xPos * xPos + zPos * zPos);
                float normalizedDist = Mathf.Clamp01(dist / halfSize);

               
                float curveValue = slopeCurve.Evaluate(normalizedDist);
                float yPos = (curveValue - 1f) * depth; 

                int index = z * vCount + x;
                vertices[index] = new Vector3(xPos, yPos, zPos);
                uvs[index] = new Vector2((float)x / resolution, (float)z / resolution);
            }
        }

        int tIndex = 0;
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int bottomLeft = z * vCount + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = (z + 1) * vCount + x;
                int topRight = topLeft + 1;

                triangles[tIndex++] = bottomLeft;
                triangles[tIndex++] = topLeft;
                triangles[tIndex++] = bottomRight;

                triangles[tIndex++] = bottomRight;
                triangles[tIndex++] = topLeft;
                triangles[tIndex++] = topRight;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        
        mesh.RecalculateNormals(); 
        mesh.RecalculateBounds();

        if (_meshFilter != null) _meshFilter.sharedMesh = mesh;
        if (_meshCollider != null) _meshCollider.sharedMesh = mesh;
    }

#if UNITY_EDITOR
    // 에디터 전용: 만든 메쉬를 파일로 저장하는 함수
    public void SaveMeshAsAsset()
    {
        if (_meshFilter.sharedMesh == null) return;

        string path = EditorUtility.SaveFilePanel("Save Concave Mesh", "Assets/", "ConcavePlane", "asset");
        if (string.IsNullOrEmpty(path)) return;

        path = FileUtil.GetProjectRelativePath(path);

        Mesh meshToSave = Instantiate(_meshFilter.sharedMesh); // 복제해서 저장 (안전)
        AssetDatabase.CreateAsset(meshToSave, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"메쉬 저장 완료: {path}");
        
        // 저장된 파일로 갈아끼우기 (이제 프리팹 저장 가능)
        _meshFilter.sharedMesh = meshToSave;
        if(_meshCollider != null) _meshCollider.sharedMesh = meshToSave;
        
        // 이 컴포넌트 자동 제거 (더 이상 필요 없으므로)
        // DestroyImmediate(this); 
        Debug.Log("이제 이 오브젝트를 프리팹으로 만드셔도 됩니다.");
    }
#endif
}

#if UNITY_EDITOR
// 인스펙터에 버튼 추가하는 에디터 스크립트
[CustomEditor(typeof(ConcavePlaneGenerator))]
public class ConcavePlaneGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        ConcavePlaneGenerator script = (ConcavePlaneGenerator)target;

        GUILayout.Space(20);
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("메쉬 파일로 저장하기 (.asset)", GUILayout.Height(40)))
        {
            script.SaveMeshAsAsset();
        }
    }
}
#endif