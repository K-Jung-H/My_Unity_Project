using UnityEngine;

public class PlayerUIManager : MonoBehaviour
{
    public static PlayerUIManager Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private ScoreBoardUI scoreBoardUI;
    [SerializeField] private FuelGaugeUI fuelGaugeUI;
    [SerializeField] private SpeedBoardUI speedBoardUI;
    [SerializeField] private GearBoxUI gearBoxUI;
    [SerializeField] private CarCameraUIManager carCameraUI;

    public void Initialize()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (scoreBoardUI != null) scoreBoardUI.Initialize();
        if (speedBoardUI != null) speedBoardUI.Initialize();

        if (gearBoxUI != null) gearBoxUI.Initialize();
        if (carCameraUI != null) carCameraUI.Initialize();
    }

    public void SetupPlayerUI(CarController localPlayer)
    {
        if (localPlayer == null) return;

        fuelGaugeUI?.SetTarget(localPlayer);
        gearBoxUI?.SetTarget(localPlayer);
        speedBoardUI?.SetTarget(localPlayer);
        carCameraUI?.SetTarget(localPlayer);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SetTarget(localPlayer);
        }
    }

    public void OnClickReset_Soft_Score() => Process_SoftReset(0.5f, -1f, -1f);
    public void OnClickReset_Soft_Health() => Process_SoftReset(-1f, 0.5f, -1f);
    public void OnClickReset_Soft_Fuel() => Process_SoftReset(-1f, -1f, 0.5f);

    private void Process_SoftReset(float scoreRatio, float healthRatio, float fuelRatio)
    {
        CarController player = PlayerManager.Instance.LocalPlayer;
        if (player == null) return;

        if (ScoreManager.Instance != null) ScoreManager.Instance.ApplyScorePenalty(scoreRatio);

        var healthSystem = player.GetComponent<HealthSystem>();
        if (healthSystem != null && healthRatio > 0) healthSystem.MultiplyCurrentHealth(healthRatio);

        DynamicChunkManager chunkManager = DynamicChunkManager.Instance;
        Transform safeSpawnPoint = (chunkManager != null) ? chunkManager.GetRandomActiveSpawnPoint() : null;
        
        player.Revive(fuelRatio, safeSpawnPoint);
        GameFlowManager.Instance.ResumeGame();
    }
}