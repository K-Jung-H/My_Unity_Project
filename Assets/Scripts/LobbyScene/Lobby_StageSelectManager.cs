using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class Lobby_StageSelectManager : MonoBehaviour
{
    [Header("Model: Data Management")]
    public List<ChunkData> allChunkDataList;

    private HashSet<int> selectedChunkIndices = new HashSet<int>();
    private int currentViewingIndex = 0;

    [Header("View: UI Connections")]
    public TMP_Text chunkNameText;
    public Image chunkPreviewImage;
    public Toggle selectionToggle;
    public Button leftArrowButton;
    public Button rightArrowButton;
    public Button SelectCompleteButton;

    public void Initialize()
    {
        InitializeData();
        BindUIEvents();
        RefreshView();

        Debug.Log("Lobby_StageSelectManager Initialized");
    }

    private void InitializeData()
    {
        for (int i = 0; i < allChunkDataList.Count; i++)
        {
            if (allChunkDataList[i].isMandatory)
            {
                selectedChunkIndices.Add(i);
            }
        }
    }

    private void BindUIEvents()
    {
        leftArrowButton.onClick.AddListener(() => NavigateChunk(-1));
        rightArrowButton.onClick.AddListener(() => NavigateChunk(1));
        
        selectionToggle.onValueChanged.AddListener(OnToggleValueChanged);

        if (SelectCompleteButton != null)
            SelectCompleteButton.onClick.AddListener(OnSelectCompleteClicked);
    }

    private void NavigateChunk(int direction)
    {
        currentViewingIndex += direction;

        if (currentViewingIndex < 0)
            currentViewingIndex = allChunkDataList.Count - 1;
        else if (currentViewingIndex >= allChunkDataList.Count)
            currentViewingIndex = 0;

        RefreshView();
    }

    private void RefreshView()
    {
        if (allChunkDataList.Count == 0) return;

        ChunkData currentData = allChunkDataList[currentViewingIndex];

        chunkNameText.text = currentData.chunkName;
        chunkPreviewImage.sprite = currentData.icon;

        selectionToggle.onValueChanged.RemoveListener(OnToggleValueChanged);

        if (currentData.isMandatory)
        {
            selectionToggle.isOn = true; 
            selectionToggle.interactable = false;
        }
        else
        {
            bool isSelected = selectedChunkIndices.Contains(currentViewingIndex);
            selectionToggle.isOn = isSelected;
            selectionToggle.interactable = true;
        }

        selectionToggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        if (allChunkDataList[currentViewingIndex].isMandatory)
        {
            if (!isOn) 
            {
                selectionToggle.SetIsOnWithoutNotify(true);
            }
            return;
        }

        if (isOn)
            selectedChunkIndices.Add(currentViewingIndex);
        else
            selectedChunkIndices.Remove(currentViewingIndex);
    }

    private void OnSelectCompleteClicked()
    {
        if (GameData.selectedChunks == null)
            GameData.selectedChunks = new List<ChunkData>();

        GameData.selectedChunks.Clear();

        foreach (int index in selectedChunkIndices)
        {
            GameData.selectedChunks.Add(allChunkDataList[index]);
        }

        Debug.Log($"Game Start! Selected Chunks: {GameData.selectedChunks.Count}");
    }
}