using UnityEngine;
using System.Collections;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("Dependencies")]
    public PlayerManager playerManager;
    public UIManager uiManager;
    public ScoreManager scoreManager;

    public bool IsGameRunning { get; private set; } = false;
    private bool isLocalPlayerDead = false;

    public void Initialize()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (playerManager != null)
        {
            playerManager.OnLocalPlayerDeath += OnLocalPlayerDeathLogics;
        }

        IsGameRunning = false;
        isLocalPlayerDead = false;
        Debug.Log("GameFlowManager Initialized");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (playerManager != null)
        {
            playerManager.OnLocalPlayerDeath -= OnLocalPlayerDeathLogics;
        }
    }

    public void StartGameSequence()
    {
        IsGameRunning = true;
        Debug.Log("--- Game Start Sequence Activated ---");
    }

    public void OnLocalPlayerDeathLogics()
    {
        if (isLocalPlayerDead) return;

        isLocalPlayerDead = true;
        IsGameRunning = false;

        if (scoreManager != null)
        {
            scoreManager.SaveGameResult();
        }
        else if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SaveGameResult();
        }

        StartCoroutine(ShowDeathSequence());
    }

    private IEnumerator ShowDeathSequence()
    {
        yield return new WaitForSeconds(1.0f);

        if (uiManager != null)
        {
            uiManager.ShowDeathPanel();
        }
        else if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDeathPanel();
        }
    }
}