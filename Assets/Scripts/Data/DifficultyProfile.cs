using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemySpawnConfig
{
    public string enemyName;
    public GameObject prefab;
    
    [Tooltip("이 적이 등장하기 시작하는 난이도 수치")]
    public float unlockThreshold = 0f;
    
    [Tooltip("난이도에 따른 스폰 확률 가중치 그래프")]
    public AnimationCurve spawnWeightCurve; 

    [Tooltip("월드에 동시에 존재할 수 있는 이 타입의 최대 개수 (-1은 무제한)")]
    public int maxInstanceCount = -1;
}

[CreateAssetMenu(fileName = "DifficultyProfile", menuName = "Game/Difficulty Profile")]
public class DifficultyProfile : ScriptableObject
{
    [Header("UI Display")]
    public string difficultyName;
    public Sprite icon;
    
    [Header("Global Settings")]
    [Tooltip("점수를 난이도 수치로 변환하는 비율 (예: 0.01이면 1000점 -> 난이도 10)")]
    public float difficultyScaler = 0.01f;

    [Tooltip("X축: 현재 난이도 (Score * Scaler), Y축: 월드 전체 허용 가능한 최대 적 개수")]
    public AnimationCurve globalMaxEnemyCurve;

    [Header("Enemy Roster")]
    public List<EnemySpawnConfig> enemyConfigs;
}