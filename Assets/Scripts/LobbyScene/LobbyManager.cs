using System.Collections;
using UnityEngine;

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

    [Header("Buttons")]
    public Button_UI Press_Window;

    public Button_UI Change_L_Car_Button;
    public Button_UI Change_R_Car_Button;
    public Button_UI Select_Car_Button;

    public Button_UI Change_L_Stage_Button;
    public Button_UI Change_R_Stage_Button;
    public Button_UI Select_Stage_Button;

    public Button_UI Change_L_Level_Button;
    public Button_UI Change_R_Level_Button;
    public Button_UI Select_Level_Button;

    public Button_UI Game_Start_Button;

    private bool isGameStarting = false;
    private bool isCarSelected = false;
    private bool isLevelSelected = false;

    void Start()
    {
        Reset();
    }

    private void Reset()
    {
        lobbyState = LobbyState.Waiting;

        isGameStarting = false;
        isCarSelected = false;

        GameData.Reset();
    }

    void Update()
    {

        switch (lobbyState)
        {
            case LobbyState.Waiting:
                HandleWaitingState();
                break;

            case LobbyState.Selection_Car:
                HandleCarSelection();
                break;

            case LobbyState.Selection_Stage:
                HandleStageSelection();
                break;

                case LobbyState.Selection_Level:
                HandleLevelSelection();
                break;

            case LobbyState.ReadyToStart:
                HandleReadyToStart();
                break;
        }
    }

    void HandleWaitingState()
    {
        if (Press_Window != null && Press_Window.GetButtonState())
        {
            lobbyState = LobbyState.Selection_Car;
            Debug.Log("Transitioning to Car Selection");
        }
    }

    void HandleCarSelection()
    {
        if (Game_Start_Button != null && Game_Start_Button.GetButtonState())
        {
            lobbyState = LobbyState.Selection_Stage;
            Debug.Log("Transitioning to Stage Selection");
        }
    }

    void HandleStageSelection()
    {
        if (Game_Start_Button != null && Game_Start_Button.GetButtonState())
        {
            lobbyState = LobbyState.Selection_Level;
            Debug.Log("Transitioning to Level Selection");
        }
    }
    
    void HandleLevelSelection()
    {
        if (Game_Start_Button != null && Game_Start_Button.GetButtonState())
        {
            lobbyState = LobbyState.ReadyToStart;
            Debug.Log("Transitioning to ReadyToStart");
        }
    }

    void HandleReadyToStart()
    {

    }
}