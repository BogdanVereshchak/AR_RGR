using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;
using TMPro;

public class PhysicsLabController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private UIController uiController;
    [SerializeField] private Camera arCamera;

    [Header("Lab Settings")]
    [SerializeField] private int maxObjects = 20;
    [SerializeField] private float dropHeight = 1.0f;
    [SerializeField] private float testForcePower = 10f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI counterText;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private List<GameObject> placedObjects = new List<GameObject>();

    private bool isSlowMo = false;
    private bool isFrozen = false;

    // СТРУКТУРА ДЛЯ ЗБЕРЕЖЕННЯ ІМПУЛЬСУ (ШВИДКОСТІ)
    private struct SavedPhysicsState
    {
        public Vector3 velocity;
        public Vector3 angularVelocity;
    }

    // Словник, що прив'язує кожен Rigidbody до його збереженої швидкості
    private Dictionary<Rigidbody, SavedPhysicsState> savedStates = new Dictionary<Rigidbody, SavedPhysicsState>();

    void Start()
    {
        if (arCamera == null) arCamera = Camera.main;
        UpdateCounterUI();
    }

    void Update()
    {
        if (uiController.GetCurrentMode() == UIController.InteractionMode.Select) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject() || (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)))
                return;

            Vector2 screenPos = Input.mousePosition;

            if (uiController.GetCurrentMode() == UIController.InteractionMode.Spawn)
            {
                SpawnTestSubject(screenPos);
            }
            else if (uiController.GetCurrentMode() == UIController.InteractionMode.TestForce)
            {
                ApplyTestForce(screenPos);
            }
        }
    }

    private void SpawnTestSubject(Vector2 screenPosition)
    {
        GameObject prefab = uiController.GetSelectedPrefab();
        if (prefab == null) return;

        if (arRaycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            Vector3 spawnPos = hitPose.position + (Vector3.up * dropHeight);

            ManageObjectLimit();
            GameObject newObj = Instantiate(prefab, spawnPos, Random.rotation);

            Rigidbody rb = newObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.mass = uiController.CurrentSpawnMass;
                rb.isKinematic = isFrozen;
            }

            Collider col = newObj.GetComponent<Collider>();
            if (col != null)
            {
                PhysicsMaterial pMat = new PhysicsMaterial("InstanceMaterial");
                pMat.bounciness = uiController.CurrentSpawnBounce;
                pMat.dynamicFriction = uiController.CurrentSpawnFriction;
                pMat.staticFriction = uiController.CurrentSpawnFriction;
                pMat.frictionCombine = PhysicsMaterialCombine.Maximum;
                pMat.bounceCombine = PhysicsMaterialCombine.Maximum;

                col.material = pMat;
            }

            placedObjects.Add(newObj);
            UpdateCounterUI();
        }
    }

    private void ApplyTestForce(Vector2 screenPosition)
    {
        if (isFrozen) return;

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                float currentForce = uiController.CurrentTestForce;

                Vector3 punchDirection = (ray.direction + Vector3.up * 0.5f).normalized;

                rb.AddForceAtPosition(punchDirection * currentForce, hit.point, ForceMode.Impulse);

                rb.AddTorque(Random.insideUnitSphere * currentForce * 0.05f, ForceMode.Impulse);
            }
        }
    }

    private void ManageObjectLimit()
    {
        if (placedObjects.Count >= maxObjects)
        {
            if (placedObjects[0] != null)
            {
                // Очищаємо пам'ять словника перед видаленням об'єкта
                Rigidbody rb = placedObjects[0].GetComponent<Rigidbody>();
                if (rb != null) savedStates.Remove(rb);

                Destroy(placedObjects[0]);
            }
            placedObjects.RemoveAt(0);
        }
    }

    private void UpdateCounterUI() { if (counterText != null) counterText.text = $"Об'єкти: {placedObjects.Count}/{maxObjects}"; }

    public void ClearAllObjects()
    {
        foreach (var obj in placedObjects) if (obj != null) Destroy(obj);
        placedObjects.Clear();
        savedStates.Clear(); // Очищаємо всі збережені стани
        UpdateCounterUI();
    }

    public void Undo()
    {
        if (placedObjects.Count > 0)
        {
            int lastIndex = placedObjects.Count - 1;
            if (placedObjects[lastIndex] != null)
            {
                Rigidbody rb = placedObjects[lastIndex].GetComponent<Rigidbody>();
                if (rb != null) savedStates.Remove(rb);

                Destroy(placedObjects[lastIndex]);
            }
            placedObjects.RemoveAt(lastIndex);
            UpdateCounterUI();
        }
    }

    public void SetSlowMo(bool isEnabled)
    {
        isSlowMo = isEnabled;
        Time.timeScale = isSlowMo ? 0.2f : 1.0f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    // ОНОВЛЕНИЙ МЕТОД FREEZE ЗІ ЗБЕРЕЖЕННЯМ ВЕКТОРУ ШВИДКОСТІ
    public void SetFreeze(bool isEnabled)
    {
        isFrozen = isEnabled;
        foreach (GameObject obj in placedObjects)
        {
            if (obj == null) continue;
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (isFrozen)
                {
                    // 1. Запам'ятовуємо швидкість ДО того, як зробимо об'єкт Kinematic
                    savedStates[rb] = new SavedPhysicsState
                    {
                        velocity = rb.linearVelocity,
                        angularVelocity = rb.angularVelocity
                    };

                    // 2. Тепер безпечно заморожуємо
                    rb.isKinematic = true;
                }
                else
                {
                    // 1. Розморожуємо
                    rb.isKinematic = false;
                    rb.WakeUp();

                    // 2. Повертаємо збережену швидкість
                    if (savedStates.TryGetValue(rb, out SavedPhysicsState state))
                    {
                        rb.linearVelocity = state.velocity;
                        rb.angularVelocity = state.angularVelocity;
                    }
                }
            }
        }
    }
}