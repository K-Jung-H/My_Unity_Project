using UnityEngine;
using System.Collections;

[DefaultExecutionOrder(-100)]
public class GameSceneBuilder : MonoBehaviour
{
    [Header("Core Systems")]
    public WorldObjectDataManager worldObjectDataManager;
    public EffectManager effectManager;
    public ScoreManager scoreManager;
    public DifficultyManager difficultyManager;
    public GameFlowManager gameFlowManager;
    public UIManager uiManager;

    [Header("Gameplay Systems")]
    public DynamicChunkManager dynamicChunkManager;
    public PlayerManager playerManager;
    public EnemySpawnManager enemySpawnManager;

    private void Awake()
    {
        InitializeGameSequence();
    }

    private void InitializeGameSequence()
    {
        Debug.Log("--- [GameSceneBuilder] Sequence Start ---");

        if (worldObjectDataManager != null) worldObjectDataManager.Initialize();
        if (effectManager != null) effectManager.Initialize();

        if (dynamicChunkManager != null) dynamicChunkManager.Initialize();

        if (playerManager != null) playerManager.Initialize();
        if (uiManager != null) uiManager.Initialize();

        if (scoreManager != null) scoreManager.Initialize();

        if (difficultyManager != null)
        {
            difficultyManager.Initialize(scoreManager);
        }

        if (enemySpawnManager != null) enemySpawnManager.Initialize();

        if (gameFlowManager != null) 
        {
            if (gameFlowManager.scoreManager == null) gameFlowManager.scoreManager = scoreManager;
            if (gameFlowManager.uiManager == null) gameFlowManager.uiManager = uiManager;
            
            gameFlowManager.Initialize();
        }


        if (gameFlowManager != null)
        {
            gameFlowManager.StartGameSequence();
        }

        Debug.Log("--- [GameSceneBuilder] Sequence Complete ---");
    }
}