using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum LobbyState
{
    Waiting,
    Selection_GameMode,
    Selection_Car,
    Selection_Stage,
    ReadyToStart
}

public class LobbyManager : MonoBehaviour
{
    public event Action<LobbyState> OnStateChanged;

    [SerializeField]
    private LobbyState lobbyState;
    public LobbyState CurrentState => lobbyState;

    private Stack<LobbyState> stateHistory = new Stack<LobbyState>();
    private bool isGameStarting = false;

    void Start()
    {
        Reset();
    }

    public void Reset()
    {
        stateHistory.Clear();
        lobbyState = LobbyState.Waiting;
        isGameStarting = false;
        
        OnStateChanged?.Invoke(lobbyState);
        GameData.Reset();
    }

    private void MoveToState(LobbyState nextState)
    {
        if (lobbyState == nextState) return;

        stateHistory.Push(lobbyState);
        lobbyState = nextState;
        
        OnStateChanged?.Invoke(lobbyState);
    }

    public void ForceChangeState(LobbyState targetState)
    {
        if (lobbyState == targetState) return;

        stateHistory.Push(lobbyState);
        lobbyState = targetState;

        OnStateChanged?.Invoke(lobbyState);
    }

    public void OnClick_NextStep()
    {
        if (isGameStarting) return;

        switch (lobbyState)
        {
            case LobbyState.Waiting:
                MoveToState(LobbyState.Selection_GameMode);
                break;

            case LobbyState.Selection_GameMode:
                MoveToState(LobbyState.Selection_Car);
                break;

            case LobbyState.Selection_Car:
                if (GameData.gameMode == GameMode.Default)
                {
                    MoveToState(LobbyState.ReadyToStart);
                }
                else
                {
                    MoveToState(LobbyState.Selection_Stage);
                }
                break;

            case LobbyState.Selection_Stage:
                MoveToState(LobbyState.ReadyToStart);
                break;

            case LobbyState.ReadyToStart:
                TryStartGame();
                break;
        }
    }

    public void OnClick_BackStep()
    {
        if (isGameStarting) return;
        if (stateHistory.Count <= 0) return;

        lobbyState = stateHistory.Pop();
        OnStateChanged?.Invoke(lobbyState);
    }

    private void TryStartGame()
    {
        isGameStarting = true;
        SceneManager.LoadScene("Game_Stage");
    }
}