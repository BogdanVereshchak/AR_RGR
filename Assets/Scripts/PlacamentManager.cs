using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class PlacementManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private Camera arCamera; // Перетягни сюди AR Camera

    [Header("Settings")]
    [SerializeField] private int maxObjects = 30;
    [SerializeField] private float dropHeight = 0.5f; // Висота, з якої падає об'єкт при будівництві
    [SerializeField] private float throwForce = 15f;  // Сила кидка
    [SerializeField] private float offsetParam = 0.05f;  // Сила кидка

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI counterText;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private List<GameObject> placedObjects = new List<GameObject>();

    private bool isSlowMo = false;
    private bool isFrozen = false;

    void Start()
    {
        if (arCamera == null) arCamera = Camera.main;
        UpdateCounterUI();
    }

    void Update()
    {
        if (uiManager.GetCurrentMode() == UIManager.InteractionMode.Select) return; // Select обробляється в іншому скрипті

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began) return;
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;

            HandleInteraction(touch.position);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            HandleInteraction(Input.mousePosition);
        }
    }

    private void HandleInteraction(Vector2 screenPosition)
    {
        GameObject prefabToSpawn = uiManager.GetSelectedPrefab();
        if (prefabToSpawn == null) return;

        if (uiManager.GetCurrentMode() == UIManager.InteractionMode.Build)
        {
            TryBuildObject(screenPosition, prefabToSpawn);
        }
        else if (uiManager.GetCurrentMode() == UIManager.InteractionMode.Throw)
        {
            ThrowObject(prefabToSpawn);
        }
    }

    private void TryBuildObject(Vector2 screenPosition, GameObject prefab)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit physicsHit))
        {
            Vector3 offset = physicsHit.normal * offsetParam;
            Vector3 spawnPos = physicsHit.point + offset + (Vector3.up * dropHeight);

            SpawnObject(prefab, spawnPos, Quaternion.identity);
            return;
        }

        if (arRaycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            Vector3 spawnPos = hitPose.position + (Vector3.up * dropHeight);
            SpawnObject(prefab, spawnPos, hitPose.rotation);
        }
    }

    private void ThrowObject(GameObject prefab)
    {
        Vector3 spawnPos = arCamera.transform.position + arCamera.transform.forward * 0.5f;
        GameObject spawnedObj = SpawnObject(prefab, spawnPos, arCamera.transform.rotation);

        Rigidbody rb = spawnedObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(arCamera.transform.forward * throwForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
        }
        else
        {
            Debug.LogWarning("Prefab missing Rigidbody! Can't throw.");
        }
    }

    private GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        ManageObjectLimit();

        GameObject newObj = Instantiate(prefab, position, rotation);
        Rigidbody rb = newObj.GetComponent<Rigidbody>();
        if (rb != null && isFrozen) rb.isKinematic = true;
        placedObjects.Add(newObj);
        UpdateCounterUI();

        return newObj;
    }

    private void ManageObjectLimit()
    {
        if (placedObjects.Count >= maxObjects)
        {
            if (placedObjects[0] != null) Destroy(placedObjects[0]);
            placedObjects.RemoveAt(0);
        }
    }

    private void UpdateCounterUI()
    {
        if (counterText != null)
            counterText.text = $"Objects: {placedObjects.Count}/{maxObjects}";
    }

    public void ClearAllObjects()
    {
        foreach (var obj in placedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        placedObjects.Clear();
        UpdateCounterUI();
    }

    public void ToggleSlowMo()
    {
        isSlowMo = !isSlowMo;
        Time.timeScale = isSlowMo ? 0.2f : 1.0f;

        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }
    public void Undo()
    {
        if (placedObjects.Count > 0)
        {
            int lastIndex = placedObjects.Count - 1;
            GameObject lastObj = placedObjects[lastIndex];

            if (lastObj != null) Destroy(lastObj);

            placedObjects.RemoveAt(lastIndex);
            UpdateCounterUI();
        }
    }
    public void ToggleFreeze()
    {
        isFrozen = !isFrozen;
        foreach (GameObject obj in placedObjects)
        {
            if (obj == null) continue;
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = isFrozen;
            }
        }
    }
}