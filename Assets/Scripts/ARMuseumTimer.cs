using UnityEngine;
using UnityEngine.XR.ARFoundation;
using TMPro; // Використовуємо TextMeshPro
using System.Collections.Generic;

public class ARMuseumTimer : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText;

    private ARTrackedImageManager imageManager;
    private float timeElapsed = 0f;
    private bool isTracking = false;
    private string currentObjectName = "";

    private Dictionary<string, string> markerNames = new Dictionary<string, string>()
    {
        { "marker_01", "Будда, що проповідує" },
        { "marker_02", "Фрагмент статуї богині Ісіди-Серкет" },
        { "marker_03", "Колосальна голова фараона Аменемхета III" },
        { "marker_04", "Хоа Хакананайя" },
        { "marker_05", "Погруддя Нефертіті" },
        { "marker_06", "Колісцевий замок" },
        { "marker_07", "Юлій Цезар" }
    };

    void Awake()
    {
        imageManager = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable() => imageManager.trackedImagesChanged += OnChanged;
    void OnDisable() => imageManager.trackedImagesChanged -= OnChanged;

    void Update()
    {
        if (isTracking)
        {
            timeElapsed += Time.deltaTime;
            timerText.text = $"Час огляду: {timeElapsed:F1} сек";
        }
    }

    void OnChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.updated)
        {
            if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            {
                isTracking = true;
                currentObjectName = trackedImage.referenceImage.name;

                if (markerNames.ContainsKey(currentObjectName))
                    currentObjectName = markerNames[currentObjectName];
                statusText.text = $"Експонат: {currentObjectName}";
            }
            else
            {
                isTracking = false;
                statusText.text = "Шукаю маркер...";
            }
        }
    }
}