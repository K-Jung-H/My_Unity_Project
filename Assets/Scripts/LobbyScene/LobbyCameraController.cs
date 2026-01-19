using System.Collections;
using UnityEngine;

public class LobbyCameraController : MonoBehaviour
{
    [Header("Dependencies")]
    public LobbyManager lobbyManager;

    [System.Serializable]
    public struct StateView
    {
        public LobbyState state;
        public Transform targetTransform;
    }

    [Header("Settings")]
    public StateView[] stateViews;
    public float moveDuration = 1.0f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine currentMoveCoroutine;

    public void Initialize()    
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnStateChanged += OnStateChanged;
            SnapToState(lobbyManager.CurrentState);
        }

        Debug.Log("LobbyCameraController Initialized");
    }

    void OnDestroy()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnStateChanged -= OnStateChanged;
        }
    }

    private void OnStateChanged(LobbyState newState)
    {
        MoveToState(newState);
    }

    public void MoveToState(LobbyState targetState)
    {
        if (currentMoveCoroutine != null)
        {
            StopCoroutine(currentMoveCoroutine);
        }
        currentMoveCoroutine = StartCoroutine(MoveCameraRoutine(targetState));
    }

    private IEnumerator MoveCameraRoutine(LobbyState targetState)
    {
        Transform targetInfo = GetTargetTransform(targetState);

        if (targetInfo == null)
        {
            currentMoveCoroutine = null;
            yield break;
        }

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 endPos = targetInfo.position;
        Quaternion endRot = targetInfo.rotation;

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            float curveValue = moveCurve.Evaluate(t);

            transform.position = Vector3.Lerp(startPos, endPos, curveValue);
            transform.rotation = Quaternion.Slerp(startRot, endRot, curveValue);

            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;

        currentMoveCoroutine = null;
    }

    private void SnapToState(LobbyState state)
    {
        Transform targetInfo = GetTargetTransform(state);
        if (targetInfo != null)
        {
            transform.position = targetInfo.position;
            transform.rotation = targetInfo.rotation;
        }
    }

    private Transform GetTargetTransform(LobbyState state)
    {
        foreach (var view in stateViews)
        {
            if (view.state == state)
            {
                return view.targetTransform;
            }
        }
        return null;
    }
}