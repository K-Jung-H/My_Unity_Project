using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum LobbyState
{
    Waiting,
    Selection_Car,
    Selection_Stage,
    Selection_Level,
    ReadyToStart
}

public class LobbyManager : MonoBehaviour
{
    [SerializeField]
    private LobbyState lobbyState;

    public LobbyState CurrentState => lobbyState;

    private bool isGameStarting = false;

    void Start()
    {
        Reset();
    }

    public void Reset()
    {
        lobbyState = LobbyState.Waiting;
        isGameStarting = false;

        GameData.Reset();
    }

    public void ChangeState(LobbyState newState)
    {
        if (lobbyState == newState) return;

        lobbyState = newState;
        Debug.Log($"[LobbyManager] State Changed to: {lobbyState}");
    }

    public void OnClick_ToWaiting()
    {
        ChangeState(LobbyState.Waiting);
    }

    public void OnClick_ToCarSelection()
    {
        ChangeState(LobbyState.Selection_Car);
    }

    public void OnClick_ToStageSelection()
    {
        ChangeState(LobbyState.Selection_Stage);
    }

    public void OnClick_ToLevelSelection()
    {
        ChangeState(LobbyState.Selection_Level);
    }

    public void OnClick_ToReady()
    {
        ChangeState(LobbyState.ReadyToStart);
    }

    public void OnClick_GameStart()
    {
        if (lobbyState == LobbyState.ReadyToStart)
        {
            isGameStarting = true;
            SceneManager.LoadScene("Game_Stage");
        }
    }
}