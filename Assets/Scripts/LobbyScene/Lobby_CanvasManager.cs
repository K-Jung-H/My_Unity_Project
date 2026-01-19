using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    [Header("Dependencies")]
    public LobbyManager lobbyManager;

    [Header("UI Elements")]
    public GameObject returnButtonObject;


    [System.Serializable]
    public struct StateCanvas
    {
        public LobbyState state;
        public CanvasGroup canvasGroup;
    }

    [Header("Settings")]
    public StateCanvas[] stateCanvases;

    public void Initialize()    
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnStateChanged -= UpdateCanvasState;
            lobbyManager.OnStateChanged += UpdateCanvasState;
        }
        Debug.Log("CanvasManager Initialized");
    }

    void OnDestroy()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnStateChanged -= UpdateCanvasState;
        }
    }

    private void UpdateCanvasState(LobbyState targetState)
    {
        foreach (var item in stateCanvases)
        {
            if (item.canvasGroup == null) continue;

            bool isActive = item.state == targetState;

            if (item.canvasGroup.gameObject.activeSelf != isActive)
            {
                item.canvasGroup.gameObject.SetActive(isActive);
            }

            if (isActive)
            {
                item.canvasGroup.alpha = 1f;
                item.canvasGroup.interactable = true;
                item.canvasGroup.blocksRaycasts = true;
            }
        }

        if (returnButtonObject != null)
        {
            bool isReturnActive = targetState != LobbyState.Waiting;

            if (returnButtonObject.activeSelf != isReturnActive)
            {
                returnButtonObject.SetActive(isReturnActive);
            }
        }
    }
}