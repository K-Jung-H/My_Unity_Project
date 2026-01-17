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

    [Header("Gameplay Systems")]
    public PlayerManager playerManager;
    public DynamicChunkManager dynamicChunkManager;
    public EnemySpawnManager enemySpawnManager;

    void Start()
    {
        InitializeGameSequence();
    }

    private void InitializeGameSequence()
    {
        Debug.Log("--- [GameSceneBuilder] 게임 초기화 시작 ---");

        if (worldObjectDataManager != null) worldObjectDataManager.Initialize();
        if (effectManager != null) effectManager.Initialize();
        if (scoreManager != null) scoreManager.Initialize();

   
        if (difficultyManager != null) difficultyManager.Initialize();
        if (playerManager != null) playerManager.Initialize();
        if (dynamicChunkManager != null) dynamicChunkManager.Initialize();
        if (enemySpawnManager != null) enemySpawnManager.Initialize();

        Debug.Log("--- [GameSceneBuilder] 게임 초기화 완료 ---");
    }
}