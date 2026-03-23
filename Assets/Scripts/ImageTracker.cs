using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTracker : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] private ARTrackedImageManager imageManager;
    [SerializeField] private ARAnchorManager anchorManager;

    [Header("Content Prefabs")]
    [Tooltip("Prefab для кожного зображення. Element 0 = marker_01...")]
    [SerializeField] private GameObject[] contentPrefabs;

    [Header("Settings")]
    [SerializeField] private float animationDuration = 0.5f;

    // Словник для зберігання вже створених об'єктів (запобігає дублікатам)
    private Dictionary<string, GameObject> spawnedContent = new Dictionary<string, GameObject>();

    void OnEnable()
    {
        if (imageManager == null || anchorManager == null)
        {
            Debug.LogError("ImageTracker: Призначте Image Manager та Anchor Manager в Inspector!");
            return;
        }
        imageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        if (imageManager != null)
            imageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        // Обробляємо тільки нові зображення
        foreach (var trackedImage in args.added) HandleAddedImage(trackedImage);
        
        // Оновлюємо позицію, тільки якщо зображення активно трекається (для точності)
        foreach (var trackedImage in args.updated) HandleUpdatedImage(trackedImage);
    }

    private void HandleAddedImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        string key = imageName ?? trackedImage.referenceImage.guid.ToString();

        if (spawnedContent.ContainsKey(key)) return;

        int index = GetImageIndex(imageName);
        if (index < 0 || index >= contentPrefabs.Length) return;

        // --- ВИПРАВЛЕННЯ ПОМИЛКИ AddAnchor ---
        // Створюємо новий порожній GameObject для якоря
        GameObject anchorGO = new GameObject($"Anchor_{imageName}");
        anchorGO.transform.position = trackedImage.transform.position;
        anchorGO.transform.rotation = trackedImage.transform.rotation;

        // Додаємо компонент ARAnchor. Менеджер сам його зареєструє.
        ARAnchor anchor = anchorGO.AddComponent<ARAnchor>();

        if (anchor == null)
        {
            Debug.LogError("ImageTracker: Не вдалося створити ARAnchor!");
            return;
        }

        // Створюємо контент як дитину об'єкта-якоря
        GameObject content = Instantiate(contentPrefabs[index], anchor.transform);
        content.name = $"Content_{imageName}";

        spawnedContent[key] = content;

        StartCoroutine(AppearAnimation(content));
        Debug.Log($"[ImageTracker] Persistent anchor created for: {imageName}");
    }

    private void HandleUpdatedImage(ARTrackedImage trackedImage)
    {
        // Корекція якоря, поки маркер у полі зору
        if (trackedImage.trackingState == TrackingState.Tracking)
        {
            string key = trackedImage.referenceImage.name ?? trackedImage.referenceImage.guid.ToString();
            
            if (spawnedContent.TryGetValue(key, out GameObject content))
            {
                // Отримуємо об'єкт якоря (батько нашого контенту)
                Transform anchorTransform = content.transform.parent;
                
                if (anchorTransform != null)
                {
                    // Плавно підтягуємо якір до реального маркера
                    anchorTransform.position = Vector3.Lerp(anchorTransform.position, trackedImage.transform.position, Time.deltaTime * 5f);
                    anchorTransform.rotation = Quaternion.Lerp(anchorTransform.rotation, trackedImage.transform.rotation, Time.deltaTime * 5f);
                }
            }
        }
    }

    private IEnumerator AppearAnimation(GameObject target)
    {
        Vector3 targetScale = target.transform.localScale;
        target.transform.localScale = Vector3.zero;
        float time = 0f;

        while (time < animationDuration)
        {
            time += Time.deltaTime;
            target.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, time / animationDuration);
            yield return null;
        }
        target.transform.localScale = targetScale;
    }

    private int GetImageIndex(string imageName)
    {
        if (string.IsNullOrEmpty(imageName)) return 0;
        if (imageName.StartsWith("marker_"))
        {
            string numberStr = imageName.Replace("marker_", "");
            if (int.TryParse(numberStr, out int number)) return number - 1;
        }
        return 0;
    }
}