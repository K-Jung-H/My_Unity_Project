using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class Lobby_LevelSelectManager : MonoBehaviour
{
    [Header("Model: Data Management")]
    public DifficultyDataTable difficultyDataTable;

    private List<DifficultyProfile> allProfileList;
    private int currentViewingIndex = 0;

    [Header("View: UI Connections")]
    public TMP_Text levelNameText;
    public Image levelPreviewImage;
    public Button leftArrowButton;
    public Button rightArrowButton;
    public Button selectCompleteButton;

    public void Initialize()
    {
        InitializeData();

        if (allProfileList == null || allProfileList.Count == 0)
        {
            Debug.LogError("DifficultyProfileList is empty or Table is not assigned!");
            return;
        }

        BindUIEvents();
        RefreshView();

        Debug.Log("Lobby_LevelSelectManager Initialized");
    }

    private void InitializeData()
    {
        if (difficultyDataTable != null)
        {
            allProfileList = difficultyDataTable.profiles;
        }
        else
        {
            Debug.LogError("DifficultyDataTable is missing in Inspector!");
            allProfileList = new List<DifficultyProfile>();
        }

        currentViewingIndex = GameData.DifficultyIndex;
        if (currentViewingIndex < 0 || currentViewingIndex >= allProfileList.Count)
        {
            currentViewingIndex = 0;
        }
    }

    private void BindUIEvents()
    {
        leftArrowButton.onClick.RemoveAllListeners();
        rightArrowButton.onClick.RemoveAllListeners();
        if (selectCompleteButton != null) selectCompleteButton.onClick.RemoveAllListeners();

        leftArrowButton.onClick.AddListener(() => NavigateLevel(-1));
        rightArrowButton.onClick.AddListener(() => NavigateLevel(1));

        if (selectCompleteButton != null)
            selectCompleteButton.onClick.AddListener(OnSelectCompleteClicked);
    }

    private void NavigateLevel(int direction)
    {
        currentViewingIndex += direction;

        if (currentViewingIndex < 0)
            currentViewingIndex = allProfileList.Count - 1;
        else if (currentViewingIndex >= allProfileList.Count)
            currentViewingIndex = 0;

        RefreshView();
    }

    private void RefreshView()
    {
        if (allProfileList == null || allProfileList.Count == 0) return;

        DifficultyProfile currentProfile = allProfileList[currentViewingIndex];

        if (string.IsNullOrEmpty(currentProfile.difficultyName))
        {
            levelNameText.text = currentProfile.name;
        }
        else
        {
            levelNameText.text = currentProfile.difficultyName;
        }
        
        if (currentProfile.icon != null)
        {
            levelPreviewImage.sprite = currentProfile.icon;
            levelPreviewImage.enabled = true;
        }
        else
        {
            levelPreviewImage.enabled = false;
        }
    }

    private void OnSelectCompleteClicked()
    {
        GameData.DifficultyIndex = currentViewingIndex;

        string selectedName = string.IsNullOrEmpty(allProfileList[currentViewingIndex].difficultyName) 
            ? allProfileList[currentViewingIndex].name 
            : allProfileList[currentViewingIndex].difficultyName;

        Debug.Log($"Difficulty Selected: {selectedName} (Index: {currentViewingIndex})");
    }
}