using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    [Header("Dependencies")]
    public LobbyManager lobbyManager;

    [System.Serializable]
    public struct StateCanvas
    {
        public LobbyState state;
        public CanvasGroup canvasGroup;
    }

    [Header("Settings")]
    public StateCanvas[] stateCanvases;

    private LobbyState lastState;

    void Start()
    {
        if (lobbyManager != null)
        {
            lastState = lobbyManager.CurrentState;
            UpdateCanvasState(lastState);
        }
    }

    void Update()
    {
        if (lobbyManager == null) return;

        if (lobbyManager.CurrentState != lastState)
        {
            lastState = lobbyManager.CurrentState;
            UpdateCanvasState(lastState);
        }
    }

    private void UpdateCanvasState(LobbyState targetState)
    {
        foreach (var item in stateCanvases)
        {
            if (item.canvasGroup == null) continue;

            bool isActive = item.state == targetState;

            item.canvasGroup.alpha = isActive ? 1f : 0f;
            item.canvasGroup.interactable = isActive;
            item.canvasGroup.blocksRaycasts = isActive;
        }
    }
}