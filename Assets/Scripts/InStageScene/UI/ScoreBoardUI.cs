using UnityEngine;
using TMPro;

public class ScoreBoardUI : MonoBehaviour
{
    [Header("UI Reference")]
    public TMP_Text scoreText;

    public void Initialize()
    {
        if (ScoreManager.Instance != null)
        {            
            ScoreManager.Instance.OnScoreChanged -= UpdateUI;
            ScoreManager.Instance.OnScoreChanged += UpdateUI;
            UpdateUI(GameData.TotalScore);
        }
    }

    void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateUI;
        }
    }

    private void UpdateUI(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"{newScore}";
        }
    }
}