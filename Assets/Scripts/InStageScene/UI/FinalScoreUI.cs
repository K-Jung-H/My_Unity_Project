using UnityEngine;
using TMPro;

public class FinalScoreUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text finalScoreText;

    public void SetScore(int score)
    {
        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {score}";
            
        }
        else
        {
            Debug.LogWarning("[FinalScoreUI] Final Score Text가 연결되지 않았습니다.");
        }
    }
}
