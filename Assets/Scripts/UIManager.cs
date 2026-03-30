using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public enum InteractionMode { Build, Throw, Select }

    [Header("Prefabs List")]
    [SerializeField] private List<GameObject> objectPrefabs; // Закинь сюди всі префаби: куби, сфери, ragdolls

    [Header("UI Elements")]
    [SerializeField] private TMP_Dropdown objectDropdown; // Випадаючий список
    [SerializeField] private TextMeshProUGUI modeButtonText;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("References")]
    [SerializeField] private ObjectSelectorManager selector;

    private InteractionMode currentMode = InteractionMode.Build;
    private GameObject selectedPrefab;

    void Start()
    {
        // Налаштовуємо Dropdown
        objectDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var prefab in objectPrefabs)
        {
            options.Add(prefab.name);
        }
        objectDropdown.AddOptions(options);
        objectDropdown.onValueChanged.AddListener(OnDropdownValueChanged);

        SelectPrefab(0);
        UpdateModeUI();
    }

    private void OnDropdownValueChanged(int index)
    {
        SelectPrefab(index);
    }

    private void SelectPrefab(int index)
    {
        if (index >= 0 && index < objectPrefabs.Count)
        {
            selectedPrefab = objectPrefabs[index];
            UpdateStatus($"Selected: {selectedPrefab.name}");
        }
    }

    public void CycleMode()
    {
        // Перемикаємо між трьома режимами по колу
        currentMode = (InteractionMode)(((int)currentMode + 1) % 3);
        UpdateModeUI();
    }

    public InteractionMode GetCurrentMode() => currentMode;
    public GameObject GetSelectedPrefab() => selectedPrefab;

    private void UpdateModeUI()
    {
        if (modeButtonText != null)
            modeButtonText.text = $"Mode: {currentMode}";
        UpdateStatus($"Switched to {currentMode} mode");
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}