using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    public enum InteractionMode { Spawn, Select, TestForce } // Змінив порядок, Select тепер другий

    [Header("Prefabs List")]
    [SerializeField] private List<GameObject> objectPrefabs;

    [Header("UI Elements - General")]
    [SerializeField] private TMP_Dropdown objectDropdown;
    [SerializeField] private TextMeshProUGUI modeButtonText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("UI Elements - Physics Panel")]
    [SerializeField] private GameObject physicsPanel;
    [SerializeField] private Slider massSlider;
    [SerializeField] private TextMeshProUGUI massText;
    [SerializeField] private Slider bounceSlider;
    [SerializeField] private TextMeshProUGUI bounceText;
    [SerializeField] private Slider frictionSlider;
    [SerializeField] private TextMeshProUGUI frictionText;
    [SerializeField] private Slider forceSlider;
    [SerializeField] private TextMeshProUGUI forceText;

    [Header("References")]
    [SerializeField] private ObjectSelectorController selector;


    private InteractionMode currentMode = InteractionMode.Spawn;
    private GameObject selectedPrefab;

    // Публічні властивості, щоб PhysicsLabController міг брати ці значення при спавні
    public float CurrentSpawnMass => massSlider.value;
    public float CurrentSpawnBounce => bounceSlider.value;
    public float CurrentSpawnFriction => frictionSlider.value;
    public float CurrentTestForce => forceSlider != null ? forceSlider.value : 10f;
    void Start()
    {
        SetupDropdown();
        UpdateModeUI();

        // Панель тепер завжди увімкнена (можеш вимикати її тільки в TestForce за бажанням)
        physicsPanel.SetActive(true);

        massSlider.onValueChanged.AddListener(OnPhysicsSliderChanged);
        bounceSlider.onValueChanged.AddListener(OnPhysicsSliderChanged);
        if (frictionSlider != null) frictionSlider.onValueChanged.AddListener(OnPhysicsSliderChanged);
        if (forceSlider != null) forceSlider.onValueChanged.AddListener(OnPhysicsSliderChanged);
        UpdatePhysicsTexts();
    }

    private void SetupDropdown()
    {
        objectDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var prefab in objectPrefabs) options.Add(prefab.name);
        objectDropdown.AddOptions(options);
        objectDropdown.onValueChanged.AddListener(index =>
        {
            selectedPrefab = objectPrefabs[index];
            UpdateStatus($"Обрано префаб: {selectedPrefab.name}");
        });
        selectedPrefab = objectPrefabs[0];
    }

    public void CycleMode()
    {
        currentMode = (InteractionMode)(((int)currentMode + 1) % 3);
        UpdateModeUI();

        // Якщо вийшли з режиму Select, просто знімаємо виділення з об'єкта
        if (currentMode != InteractionMode.Select)
        {
            selector.DeselectCurrent();
        }
    }

    public InteractionMode GetCurrentMode() => currentMode;
    public GameObject GetSelectedPrefab() => selectedPrefab;

    private void UpdateModeUI()
    {
        modeButtonText.text = $"Режим: {currentMode}";
        UpdateStatus($"Поточний режим: {currentMode}");
    }

    private void UpdateStatus(string message) { if (statusText != null) statusText.text = message; }

    // Викликається, коли ми самі тягнемо повзунок
    private void OnPhysicsSliderChanged(float value)
    {
        UpdatePhysicsTexts();
        // Якщо в цей момент у нас вибраний якийсь об'єкт на сцені — оновлюємо і його
        if (selector.HasSelectedObject())
        {
            selector.UpdateSelectedObjectPhysics(massSlider.value, bounceSlider.value, frictionSlider.value);
        }
    }

    // Викликається з ObjectSelectorController, коли ми клікаємо на об'єкт на сцені
    // SetValueWithoutNotify змінює повзунок, але не викликає OnPhysicsSliderChanged (щоб не було нескінченного циклу)
    public void SyncSlidersWithObject(float objMass, float objBounce, float objFriction)
    {
        massSlider.SetValueWithoutNotify(objMass);
        bounceSlider.SetValueWithoutNotify(objBounce);
        frictionSlider.SetValueWithoutNotify(objFriction); // Синхронізуємо тертя
        UpdatePhysicsTexts();
    }
    private void UpdatePhysicsTexts()
    {
        if (massText != null) massText.text = $"Маса: {massSlider.value:F1} кг";
        if (bounceText != null) bounceText.text = $"Пружність: {bounceSlider.value:F2}";
        if (frictionText != null) frictionText.text = $"Тертя: {frictionSlider.value:F2}";
        
        if (forceText != null) forceText.text = $"Сила: {forceSlider.value:F0}";
    }
}