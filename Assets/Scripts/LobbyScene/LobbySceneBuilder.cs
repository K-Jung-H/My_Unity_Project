using UnityEngine;

[DefaultExecutionOrder(-100)] 
public class LobbySceneBuilder : MonoBehaviour
{
    [Header("Core Managers")]
    public LobbyManager lobbyManager;

    [Header("Sub Systems")]
    public CanvasManager canvasManager;
    public LobbyCameraController cameraController;
    public Lobby_ModeSelectManager modeSelectManager;
    public Lobby_CarSelectManager carSelectManager;
    public Lobby_StageSelectManager stageSelectManager;
    public Lobby_LevelSelectManager levelSelectManager;

    [Header("Visual Effects")]
    public LobbyLightController lobbySpotLight;

    private void Awake()
    {
        InitializeGameData();
        InitializeLobbySequence();
    }

    private void InitializeGameData()
    {
        GameData.Reset();
        Debug.Log("[LobbySceneBuilder] GameData Initialized");
    }

    private void InitializeLobbySequence()
    {
        Debug.Log("--- [LobbySceneBuilder] Sequence Start ---");

        if (carSelectManager != null) carSelectManager.lobbyManager = lobbyManager;
        if (cameraController != null) cameraController.lobbyManager = lobbyManager;
        if (canvasManager != null) canvasManager.lobbyManager = lobbyManager;

        if (carSelectManager != null) carSelectManager.Initialize();
        if (stageSelectManager != null) stageSelectManager.Initialize();
        if (levelSelectManager!= null) levelSelectManager.Initialize();

        if (canvasManager != null) canvasManager.Initialize();
        if (cameraController != null) cameraController.Initialize();

        if (lobbyManager != null)
        {
            lobbyManager.Initialize();
        }

        if (lobbySpotLight != null) 
        {
            lobbySpotLight.targetCamera = Camera.main; 
            lobbySpotLight.Initialize();
        }

        Debug.Log("--- [LobbySceneBuilder] Sequence Complete ---");
    }
}