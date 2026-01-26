using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject gameUIPanel;
    [SerializeField] private GameObject optionUIPanel;
    [SerializeField] private GameObject playerSettingUIPanel;
    [SerializeField] private GameObject playerResetUIPanel;

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
        if (playerSettingUIPanel != null) playerSettingUIPanel.SetActive(false);
        if (playerResetUIPanel != null) playerResetUIPanel.SetActive(false);

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
        if (playerSettingUIPanel != null) playerSettingUIPanel.SetActive(false);
        if (playerResetUIPanel != null) playerResetUIPanel.SetActive(false);

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

    public void OnClickReset_Soft()
    {
        if (playerSettingUIPanel != null) playerSettingUIPanel.SetActive(false);
        if (playerResetUIPanel != null) playerResetUIPanel.SetActive(true);
    }

    public void OnClickReset_Hard()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


    private void Process_SoftReset(float scoreRatio, float healthRatio, float fuelRatio)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player not found!");
            return;
        }

        DynamicChunkManager chunkManager = FindFirstObjectByType<DynamicChunkManager>(); 
        var carController = player.GetComponent<CarController>();
        var healthSystem = player.GetComponent<HealthSystem>();
        
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ApplyScorePenalty(scoreRatio);
        }

        if (healthSystem != null)
        {
            healthSystem.MultiplyCurrentHealth(healthRatio);
        }

        if (carController != null)
        {
            Transform safeSpawnPoint = null;
            if (chunkManager != null)
            {
                safeSpawnPoint = chunkManager.GetRandomActiveSpawnPoint();
            }
            carController.Revive(fuelRatio, safeSpawnPoint);
        }

        isGameOver = false;
        isPaused = false;

        if (playerResetUIPanel != null) playerResetUIPanel.SetActive(false);
        if (deathUIPanel != null) deathUIPanel.SetActive(false);
        if (gameUIPanel != null) gameUIPanel.SetActive(true);

        Debug.Log($"Revived! Ratios -> Score:{scoreRatio}, HP:{healthRatio}, Fuel:{fuelRatio}");
    }

    public void OnClickReset_Soft_Score()
    {
        Process_SoftReset(0.5f, -1f, -1f);
    }

    public void OnClickReset_Soft_Health()
    {
        Process_SoftReset(-1f, 0.5f, -1f);
    }

    public void OnClickReset_Soft_Fuel()
    {
        Process_SoftReset(-1f, -1f, 0.5f);
    }

    public void OnClickLobby()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Lobby_Scene");
    }

    public void OnClickSettings()
    {
        if (playerSettingUIPanel != null) playerSettingUIPanel.SetActive(true);
        if (optionUIPanel != null) optionUIPanel.SetActive(false);
    }

    public void OnClickExitGame()
    {
        Application.Quit();
    }
}