using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject gameUIPanel;
    [SerializeField] private GameObject optionUIPanel;

    private bool isPaused = false;

    void Start()
    {
        isPaused = false; 
        
        if(gameUIPanel != null) gameUIPanel.SetActive(true);
        if(optionUIPanel != null) optionUIPanel.SetActive(false);
    }

    void Update()
    {
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

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        
        if(gameUIPanel != null) gameUIPanel.SetActive(false);
        if(optionUIPanel != null) optionUIPanel.SetActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        if(gameUIPanel != null) gameUIPanel.SetActive(true);
        if(optionUIPanel != null) optionUIPanel.SetActive(false);
    }

    public void OnClickReset()
    {
        ResumeGame();
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