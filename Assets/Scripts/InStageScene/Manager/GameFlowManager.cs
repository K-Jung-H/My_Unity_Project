using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

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

    private void HandleLocalPlayerDeath()
    {
        if (isGameOver) return;

        isGameOver = true;
        IsGameRunning = false;

        if (scoreManager != null) scoreManager.SaveGameResult();
        else if (ScoreManager.Instance != null) ScoreManager.Instance.SaveGameResult();

        StartCoroutine(ShowDeathSequence());
    }

    private IEnumerator ShowDeathSequence()
    {
        yield return new WaitForSeconds(1.0f);

        if (canvasManager != null) canvasManager.ShowDeathPanel();
        else if (InGame_CanvasManager.Instance != null) InGame_CanvasManager.Instance.ShowDeathPanel();
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