using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class LightEstimation : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] private ARCameraManager cameraManager;

    [Header("Scene Light (Global)")]
    [SerializeField] private Light mainLight;

    [Header("AR Object Point Light")]
    [Tooltip("Point Light, який буде слідувати за об'єктом")]
    [SerializeField] private Light pointLight;
    [Tooltip("Об'єкт, за яким має слідувати світло")]
    [SerializeField] private Transform targetARObject;
    [Tooltip("Зміщення світла (наприклад, трохи вище об'єкта)")]
    [SerializeField] private Vector3 lightOffset = new Vector3(0f, 0.5f, 0f);
    [Tooltip("Множник яскравості (Point Light потребує більших значень ніж Directional)")]
    [SerializeField] private float pointLightMultiplier = 5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private float smoothBrightness = 1f;
    private float smoothTemperature = 6500f;
    private int frameCount = 0;

    void OnEnable()
    {
        if (cameraManager == null || mainLight == null)
        {
            Debug.LogError("[LIGHT] Camera Manager або Main Light не призначені в Inspector!");
            enabled = false;
            return;
        }
        cameraManager.frameReceived += OnFrameReceived;
        Debug.Log("[LIGHT] ✓ LightEstimation підписався на frameReceived");
    }

    void OnDisable()
    {
        if (cameraManager != null)
            cameraManager.frameReceived -= OnFrameReceived;
        Debug.Log("[LIGHT] ✗ LightEstimation відписався від frameReceived");
    }

    void Update()
    {
        if (pointLight != null && targetARObject != null)
        {
            Vector3 targetPosition = targetARObject.position + lightOffset;
            pointLight.transform.position = Vector3.Lerp(pointLight.transform.position, targetPosition, Time.deltaTime * 10f);
        }
    }

    private void OnFrameReceived(ARCameraFrameEventArgs args)
    {
        frameCount++;
        var est = args.lightEstimation;

        if (showDebugInfo && frameCount % 60 == 1)
        {
            Debug.Log($"[LIGHT] === Кадр #{frameCount} === Доступні поля: mainLumens={est.mainLightIntensityLumens.HasValue}");
        }

        if (est.mainLightIntensityLumens.HasValue)
        {
            float lumens = est.mainLightIntensityLumens.Value;
            float target = Mathf.Clamp(lumens / 1000f, 0.1f, 3f);
            smoothBrightness = Mathf.Lerp(smoothBrightness, target, Time.deltaTime * 5f);
        }
        else if (est.averageIntensityInLumens.HasValue)
        {
            float lumens = est.averageIntensityInLumens.Value;
            float target = Mathf.Clamp(lumens / 1000f, 0.1f, 3f);
            smoothBrightness = Mathf.Lerp(smoothBrightness, target, Time.deltaTime * 5f);
        }
        else if (est.averageBrightness.HasValue)
        {
            smoothBrightness = Mathf.Lerp(smoothBrightness, est.averageBrightness.Value, Time.deltaTime * 5f);
        }

        mainLight.intensity = smoothBrightness;
        if (pointLight != null)
        {
            pointLight.intensity = smoothBrightness * pointLightMultiplier; // Множимо, щоб Point Light було видно
        }

        if (est.mainLightDirection.HasValue)
            mainLight.transform.rotation = Quaternion.LookRotation(est.mainLightDirection.Value);

        if (est.mainLightColor.HasValue)
        {
            mainLight.color = est.mainLightColor.Value;
            if (pointLight != null) pointLight.color = est.mainLightColor.Value; // Передаємо колір і на Point Light
        }
        else if (est.colorCorrection.HasValue)
        {
            mainLight.color = est.colorCorrection.Value;
            mainLight.useColorTemperature = false;
            if (pointLight != null) pointLight.color = est.colorCorrection.Value;
        }

        if (est.averageColorTemperature.HasValue)
        {
            smoothTemperature = Mathf.Lerp(smoothTemperature, est.averageColorTemperature.Value, Time.deltaTime * 5f);
            mainLight.colorTemperature = smoothTemperature;
            mainLight.useColorTemperature = true;

            if (pointLight != null) 
            {
                pointLight.colorTemperature = smoothTemperature;
                pointLight.useColorTemperature = true;
            }
        }
    }
}