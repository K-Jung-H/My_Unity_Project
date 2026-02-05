using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class Lobby_StageSelectManager : MonoBehaviour
{
    [Header("Model: Data Management")]
    public ChunkDataTable chunkDataTable;

    private List<BiomeType> availableBiomes = new List<BiomeType>();
    private Dictionary<BiomeType, ChunkData> biomeRepresentative = new Dictionary<BiomeType, ChunkData>();
    
    private HashSet<BiomeType> selectedBiomes = new HashSet<BiomeType>();
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
        if (chunkDataTable == null || chunkDataTable.chunkList.Count == 0)
        {
            Debug.LogError("ChunkDataTable is missing or empty!");
            return;
        }

        InitializeData();
        BindUIEvents();
        RefreshView();

        Debug.Log("Lobby_StageSelectManager Initialized (Biome Mode)");
    }

    private void InitializeData()
    {
        availableBiomes.Clear();
        biomeRepresentative.Clear();
        selectedBiomes.Clear();

        foreach (var chunk in chunkDataTable.chunkList)
        {
            if (!availableBiomes.Contains(chunk.biomeType))
            {
                availableBiomes.Add(chunk.biomeType);
                biomeRepresentative[chunk.biomeType] = chunk;
            }
        }

        if (availableBiomes.Count > 0)
        {
            selectedBiomes.Add(availableBiomes[0]);
        }
    }

    private void BindUIEvents()
    {
        leftArrowButton.onClick.RemoveAllListeners();
        rightArrowButton.onClick.RemoveAllListeners();
        selectionToggle.onValueChanged.RemoveAllListeners();
        if (SelectCompleteButton != null) SelectCompleteButton.onClick.RemoveAllListeners();

        leftArrowButton.onClick.AddListener(() => Navigate(-1));
        rightArrowButton.onClick.AddListener(() => Navigate(1));
        selectionToggle.onValueChanged.AddListener(OnToggleValueChanged);

        if (SelectCompleteButton != null)
            SelectCompleteButton.onClick.AddListener(OnSelectCompleteClicked);
    }

    private void Navigate(int direction)
    {
        if (availableBiomes.Count == 0) return;

        currentViewingIndex += direction;

        if (currentViewingIndex < 0)
            currentViewingIndex = availableBiomes.Count - 1;
        else if (currentViewingIndex >= availableBiomes.Count)
            currentViewingIndex = 0;

        RefreshView();
    }

    private void RefreshView()
    {
        if (availableBiomes.Count == 0) return;

        BiomeType currentBiome = availableBiomes[currentViewingIndex];
        ChunkData representative = biomeRepresentative[currentBiome];

        chunkNameText.text = currentBiome.ToString();
        if (representative != null)
        {
            chunkPreviewImage.sprite = representative.icon;
        }

        selectionToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        selectionToggle.isOn = selectedBiomes.Contains(currentBiome);
        selectionToggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        BiomeType currentBiome = availableBiomes[currentViewingIndex];

        if (isOn)
            selectedBiomes.Add(currentBiome);
        else
            selectedBiomes.Remove(currentBiome);
    }

    private void OnSelectCompleteClicked()
    {
        if (selectedBiomes.Count == 0)
        {
            Debug.LogWarning("No Biome Selected! Selecting the first one by default.");
            if (availableBiomes.Count > 0) selectedBiomes.Add(availableBiomes[0]);
        }

        if (GameData.activeBiomes == null) GameData.activeBiomes = new List<BiomeType>();
        
        GameData.activeBiomes.Clear();
        foreach (var biome in selectedBiomes)
        {
            GameData.activeBiomes.Add(biome);
        }

        Debug.Log($"Selected Biomes: {string.Join(", ", GameData.activeBiomes)}");
    }
}