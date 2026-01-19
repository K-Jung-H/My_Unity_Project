using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject gameUIPanel;
    [SerializeField] private GameObject optionUIPanel;
    [SerializeField] private GameObject deathUIPanel;

    private bool isPaused = false;
    private bool isGameOver = false;

    public void Initialize()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        isPaused = false;
        isGameOver = false;
        Time.timeScale = 1f;

        if (gameUIPanel != null) gameUIPanel.SetActive(true);
        if (optionUIPanel != null) optionUIPanel.SetActive(false);
        if (deathUIPanel != null) deathUIPanel.SetActive(false);

        Debug.Log("UIManager Initialized");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (isGameOver) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void ShowDeathPanel()
    {
        isGameOver = true;

        if (gameUIPanel != null) gameUIPanel.SetActive(false);
        if (optionUIPanel != null) optionUIPanel.SetActive(false);
        
        if (deathUIPanel != null) 
        {
            deathUIPanel.SetActive(true);
        }
    }

    public void PauseGame()
    {
        if (isGameOver) return;

        isPaused = true;
        Time.timeScale = 0f;
        
        if (gameUIPanel != null) gameUIPanel.SetActive(false);
        if (optionUIPanel != null) optionUIPanel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (isGameOver) return;

        isPaused = false;
        Time.timeScale = 1f;
        
        if (gameUIPanel != null) gameUIPanel.SetActive(true);
        if (optionUIPanel != null) optionUIPanel.SetActive(false);
    }

    public void OnClickReset()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnClickLobby()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Lobby_Scene");
    }

    public void OnClickSettings()
    {
        Debug.Log("Open Settings Popup");
    }

    public void OnClickExitGame()
    {
        Application.Quit();
    }
}