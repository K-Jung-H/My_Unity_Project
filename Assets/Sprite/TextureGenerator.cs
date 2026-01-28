using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureGenerator : EditorWindow
{
    [MenuItem("Tools/Generate SpeedLine Sprite")]
    public static void GenerateTriangleTexture()
    {
        int width = 1024;
        int height = 128;
        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);

        // 모든 픽셀을 투명하게 초기화
        Color[] colors = new Color[width * height];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.clear;
        }
        texture.SetPixels(colors);

        // 삼각형 그리기 (왼쪽이 넓고 오른쪽으로 갈수록 좁아짐)
        for (int x = 0; x < width; x++)
        {
            // 0 ~ 1 사이의 진행도 (왼쪽 0, 오른쪽 1)
            float progress = (float)x / width;
            
            // 오른쪽으로 갈수록 높이가 줄어듦 (선형 보간)
            float currentHalfHeight = (height / 2.0f) * (1.0f - progress);

            int centerY = height / 2;
            int startY = Mathf.FloorToInt(centerY - currentHalfHeight);
            int endY = Mathf.CeilToInt(centerY + currentHalfHeight);

            for (int y = startY; y < endY; y++)
            {
                // 가장자리를 부드럽게 처리 (Anti-aliasing 흉내)
                float alpha = 1.0f;
                if (y == startY || y == endY - 1) alpha = 0.5f; 
                
                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }

        texture.Apply();

        // PNG 파일로 저장
        byte[] bytes = texture.EncodeToPNG();
        string path = Application.dataPath + "/1024x128_LongTriangle.png";
        File.WriteAllBytes(path, bytes);

        Debug.Log("이미지 생성 완료: " + path);
        AssetDatabase.Refresh(); // 에디터 새로고침하여 파일 인식
    }
}