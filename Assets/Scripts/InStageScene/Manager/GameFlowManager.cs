using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;
using System.IO;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("Dependencies")]
    public PlayerManager playerManager;
    public InGame_CanvasManager canvasManager;
    public ScoreManager scoreManager;

    [Header("State")]
    private bool isPaused = false;
    private bool isGameOver = false;
    public bool IsGameRunning { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    public void Initialize()
    {

        if (playerManager != null)
        {
            playerManager.OnLocalPlayerDeath += HandleLocalPlayerDeath;
        }

        isPaused = false;
        isGameOver = false;
        IsGameRunning = false;
        Time.timeScale = 1f;

        Debug.Log("GameFlowManager Initialized");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (playerManager != null)
        {
            playerManager.OnLocalPlayerDeath -= HandleLocalPlayerDeath;
        }
    }

    private void Update()
    {
        if (isGameOver) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void StartGameSequence()
    {
        isPaused = false;
        isGameOver = false;
        IsGameRunning = true;
        Time.timeScale = 1f;
        
        if (canvasManager != null) canvasManager.ShowGamePanel();
        else if (InGame_CanvasManager.Instance != null) InGame_CanvasManager.Instance.ShowGamePanel();
        
        Debug.Log("--- Game Start Sequence Activated ---");
    }

    public void PauseGame()
    {
        if (isGameOver || isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;
        
        if (canvasManager != null) canvasManager.ShowPausePanel();
        else if (InGame_CanvasManager.Instance != null) InGame_CanvasManager.Instance.ShowPausePanel();
    }

    public void ResumeGame()
    {
        if (isGameOver || !isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;
        
        if (canvasManager != null) canvasManager.ShowGamePanel();
        else if (InGame_CanvasManager.Instance != null) InGame_CanvasManager.Instance.ShowGamePanel();
    }

    private void SaveGameResult()
    {
        GameResult newResult = new GameResult(); 
        
        GameHistory history = LoadGameHistory();
        
        history.results.Add(newResult);

        string json = JsonUtility.ToJson(history, true);
        File.WriteAllText(GameHistory.FilePath, json);

        Debug.Log($"저장 경로: {Application.persistentDataPath}");
        Debug.Log($"[GameFlowManager] Game Result Saved. High Score: {history.GetHighScore()}");
    }

    private GameHistory LoadGameHistory()
    {
        if (File.Exists(GameHistory.FilePath))
        {
            try
            {
                string json = File.ReadAllText(GameHistory.FilePath);
                return JsonUtility.FromJson<GameHistory>(json) ?? new GameHistory();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameFlowManager] Failed to load history: {e.Message}");
            }
        }
        return new GameHistory();
    }

    public int GetHighScore()
    {
        return LoadGameHistory().GetHighScore();
    }


    private void HandleLocalPlayerDeath()
    {
        if (isGameOver) return;

        isGameOver = true;
        IsGameRunning = false;

        SaveGameResult();

        StartCoroutine(ShowDeathSequence());
    }

    private IEnumerator ShowDeathSequence()
    {
        yield return new WaitForSeconds(1.0f);

        int currentScore = GameData.TotalScore; 

        if (canvasManager != null) canvasManager.ShowDeathPanel(currentScore);
        else if (InGame_CanvasManager.Instance != null) InGame_CanvasManager.Instance.ShowDeathPanel(currentScore);
    }

    public void OnClickLobby()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Lobby_Scene");
    }

    public void OnClickRestart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}