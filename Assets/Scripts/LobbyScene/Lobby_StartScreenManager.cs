using UnityEngine;
using TMPro;
using System.IO; 

public class Lobby_StartScreenManager : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TMP_Text highScoreText;

    [Header("Animation Settings")]
    [SerializeField] private float colorChangeSpeed = 1.0f;
    [SerializeField] private float scaleSpeed = 2.0f;
    
    [SerializeField] private float minScale = 0.5f; 
    [SerializeField] private float maxScale = 1.5f;

    private int currentBestScore = 0;
    private bool hasScore = false;

    private void OnEnable()
    {
        LoadHighScore();
    }

    private void Update()
    {
        if (!hasScore || highScoreText == null) return;

        
        float hue = Mathf.Repeat(Time.time * colorChangeSpeed, 1f);
        Color rainbowColor = Color.HSVToRGB(hue, 1f, 1f);
        string hexColor = ColorUtility.ToHtmlStringRGB(rainbowColor);

        highScoreText.text = $"High Score:\n<color=#{hexColor}>{currentBestScore}</color>";
        
        float range = (maxScale - minScale) / 2.0f;
        float mid = (maxScale + minScale) / 2.0f;

        float scaleVal = mid + (range * Mathf.Sin(Time.time * scaleSpeed));
        
        highScoreText.transform.localScale = Vector3.one * scaleVal;
    }

    private void LoadHighScore()
    {
        currentBestScore = 0;
        hasScore = false;

        if (File.Exists(GameHistory.FilePath))
        {
            try
            {
                string json = File.ReadAllText(GameHistory.FilePath);
                GameHistory history = JsonUtility.FromJson<GameHistory>(json);
                
                if (history != null)
                {
                    currentBestScore = history.GetHighScore();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Lobby] High Score 로드 실패: {e.Message}");
            }
        }

        if (currentBestScore > 0)
        {
            hasScore = true;
            highScoreText.gameObject.SetActive(true);
        }
        else
        {
            hasScore = false;
            highScoreText.text = "";
        }
    }
}