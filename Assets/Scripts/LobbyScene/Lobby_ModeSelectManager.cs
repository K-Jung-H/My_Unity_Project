using UnityEngine;


public class Lobby_ModeSelectManager : MonoBehaviour
{

    public void Initialize()    
    {
        
    }

    public void OnClick_DefaultMode()
    {
        SaveModeToData(GameMode.Default);
    }

    public void OnClick_CustomMode()
    {
        SaveModeToData(GameMode.Custom);
    }

    private void SaveModeToData(GameMode mode)
    {
        GameData.gameMode = mode;
        Debug.Log($"[ModeSelect] GameMode Saved: {mode}");
    }
}