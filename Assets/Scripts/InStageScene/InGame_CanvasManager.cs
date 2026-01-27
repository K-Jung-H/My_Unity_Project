using UnityEngine;

public enum UIPanelType
{
    None,
    Game,
    Pause,
    Option,
    Reset,
    Death,
}

public class InGame_CanvasManager : MonoBehaviour
{
    public static InGame_CanvasManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject gameUIPanel;
    [SerializeField] private GameObject pauseUIPanel;
    [SerializeField] private GameObject optionUIPanel;
    [SerializeField] private GameObject resetUIPanel;
    [SerializeField] private GameObject deathUIPanel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Initialize()
    {
        ShowPanel(UIPanelType.Game);
    }

    public void ShowPanel(UIPanelType type)
    {
        gameUIPanel?.SetActive(type == UIPanelType.Game);
        pauseUIPanel?.SetActive(type == UIPanelType.Pause);
        optionUIPanel?.SetActive(type == UIPanelType.Option);
        resetUIPanel?.SetActive(type == UIPanelType.Reset);
        deathUIPanel?.SetActive(type == UIPanelType.Death);
    }

    public void ShowGamePanel() => ShowPanel(UIPanelType.Game);
    public void ShowPausePanel() => ShowPanel(UIPanelType.Pause);
    public void ShowOptionPanel() => ShowPanel(UIPanelType.Option);
    public void ShowResetPanel() => ShowPanel(UIPanelType.Reset);
    public void ShowDeathPanel() => ShowPanel(UIPanelType.Death); 
}