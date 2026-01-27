using UnityEngine;

[DefaultExecutionOrder(-100)] 

public class GameSceneBuilder : MonoBehaviour
{
    public static GameSceneBuilder Instance { get; private set; }

    [Header("Core Systems")]
    public WorldObjectDataManager worldObjectDataManager;
    public EffectManager effectManager;
    public ScoreManager scoreManager;
    public DifficultyManager difficultyManager;
    public GameFlowManager gameFlowManager;
    public InGame_CanvasManager canvasManager;
    public PlayerUIManager playerUIManager;

    [Header("Gameplay Systems")]
    public DynamicChunkManager dynamicChunkManager;
    public PlayerManager playerManager;
    public EnemySpawnManager enemySpawnManager;

    [Header("PostProcess Systems")]
    public SpeedBlurManager speedBlurManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitializeGameSequence();
    }

    private void InitializeGameSequence()
    {
        if (InGame_CanvasManager.Instance != null) InGame_CanvasManager.Instance.Initialize();
        else if (canvasManager != null) canvasManager.Initialize();

        if (PlayerUIManager.Instance != null) PlayerUIManager.Instance.Initialize();
        else if (playerUIManager != null) playerUIManager.Initialize();

        if (WorldObjectDataManager.Instance != null) WorldObjectDataManager.Instance.Initialize();
        else if (worldObjectDataManager != null) worldObjectDataManager.Initialize();

        if (DynamicChunkManager.Instance != null) DynamicChunkManager.Instance.Initialize();
        else if (dynamicChunkManager != null) dynamicChunkManager.Initialize();

        if (ScoreManager.Instance != null) ScoreManager.Instance.Initialize();
        else if (scoreManager != null) scoreManager.Initialize();


        if (PlayerManager.Instance != null) PlayerManager.Instance.Initialize();
        else if (playerManager != null) playerManager.Initialize();

        if (DifficultyManager.Instance != null) DifficultyManager.Instance.Initialize();
        else if (difficultyManager != null) difficultyManager.Initialize();

        if (EnemySpawnManager.Instance != null) EnemySpawnManager.Instance.Initialize();
        else if (enemySpawnManager != null) enemySpawnManager.Initialize();

        if (EffectManager.Instance != null) EffectManager.Instance.Initialize();
        else if (effectManager != null) effectManager.Initialize();

        if(SpeedBlurManager.Instance != null) SpeedBlurManager.Instance.Initialize();
        else if (speedBlurManager != null) speedBlurManager.Initialize();

        CarController localCar = PlayerManager.Instance != null ? PlayerManager.Instance.LocalPlayer : null;
        if (localCar != null && PlayerUIManager.Instance != null)
        {
            PlayerUIManager.Instance.SetupPlayerUI(localCar);
        }

        if (localCar != null && SpeedBlurManager.Instance != null)
        {
            SpeedBlurManager.Instance.SetTargetCar(localCar);
            Debug.Log("GameSceneBuilder: SpeedBlurManager Target Connected.");
        }


        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.Initialize();
            GameFlowManager.Instance.StartGameSequence();
        }
        
        if (InGame_CanvasManager.Instance != null)
        {
            InGame_CanvasManager.Instance.ShowPanel(UIPanelType.Game);
        }
    }
}