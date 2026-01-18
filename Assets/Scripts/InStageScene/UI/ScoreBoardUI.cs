using UnityEngine;
using TMPro;

public class ScoreBoardManager : MonoBehaviour
{
    [Header("UI Reference")]
    public TMP_Text scoreText;

    void Start()
    {
        if (ScoreManager.Instance != null)
        {
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