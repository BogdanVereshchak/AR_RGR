using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class GesturesController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ObjectSelectorController objectSelector;
    [SerializeField] private ARRaycastManager arRaycastManager;

    [Header("Scale Settings")]
    [SerializeField] private float minScale = 0.1f;
    [SerializeField] private float maxScale = 3.0f;
    [SerializeField] private float pinchScaleSpeed = 0.01f;
    [SerializeField] private float mouseScrollSpeed = 0.5f;

    [Header("Rotation Settings")]
    [SerializeField] private float twistRotationSpeed = 2.0f;

    private Rigidbody selectedRb;
    private bool wasKinematic;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    
    // ДОДАНО: Прапорець, який показує, чи ми дійсно "вхопили" об'єкт для перетягування
    private bool isDragging = false; 

    void Update()
    {
        if (objectSelector == null) return;

        GameObject selected = objectSelector.GetSelectedObject();
        if (selected == null) 
        {
            ResetPhysicsState(); 
            isDragging = false;
            return;
        }

        if (Application.isEditor)
        {
            HandleEditorMouse(selected);
        }
        else
        {
            HandleMobileTouch(selected);
        }
    }

    private void HandleEditorMouse(GameObject target)
    {
        // 1. Drag (Ліва кнопка)
        if (Input.GetMouseButtonDown(0))
        {
            // Перевіряємо, чи клікнули ми саме по виділеному об'єкту
            if (IsPointerOverTarget(Input.mousePosition, target))
            {
                isDragging = true;
                PrepareObjectForManipulation(target);
            }
        }
        else if (Input.GetMouseButton(0) && isDragging) // Рухаємо тільки якщо успішно "вхопили"
        {
            if (arRaycastManager != null && arRaycastManager.Raycast(Input.mousePosition, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                Vector3 newPosition = hitPose.position + Vector3.up * 0.05f; 
                if (selectedRb != null) selectedRb.MovePosition(newPosition);
                else target.transform.position = newPosition;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            ResetPhysicsState();
        }

        // 2. Rotate (Права кнопка)
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            target.transform.Rotate(Vector3.up, -mouseX * twistRotationSpeed * 3f, Space.World);
        }

        // 3. Scale (Коліщатко)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float newScaleX = Mathf.Clamp(target.transform.localScale.x + (scroll * mouseScrollSpeed), minScale, maxScale);
            target.transform.localScale = Vector3.one * newScaleX;
        }
    }

    private void HandleMobileTouch(GameObject target)
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                // Перевіряємо, чи тапнули ми саме по виділеному об'єкту
                if (IsPointerOverTarget(touch.position, target))
                {
                    isDragging = true;
                    PrepareObjectForManipulation(target);
                }
            }
            else if (touch.phase == TouchPhase.Moved && isDragging) // Рухаємо тільки якщо "вхопили"
            {
                if (arRaycastManager != null && arRaycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
                {
                    Pose hitPose = hits[0].pose;
                    Vector3 newPosition = hitPose.position + Vector3.up * 0.05f; 
                    if (selectedRb != null) selectedRb.MovePosition(newPosition);
                    else target.transform.position = newPosition;
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDragging = false;
                ResetPhysicsState();
            }
        }
        else if (Input.touchCount == 2)
        {
            isDragging = false; // Скасовуємо перетягування, якщо почали масштабувати/крутити
            ResetPhysicsState(); // Повертаємо фізику перед двома пальцями

            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began) PrepareObjectForManipulation(target);

            if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
            {
                // Масштаб
                float prevTouchDeltaMag = (touch0.position - touch0.deltaPosition - (touch1.position - touch1.deltaPosition)).magnitude;
                float touchDeltaMag = (touch0.position - touch1.position).magnitude;
                Vector3 newScale = target.transform.localScale - Vector3.one * ((prevTouchDeltaMag - touchDeltaMag) * pinchScaleSpeed);
                target.transform.localScale = Vector3.one * Mathf.Clamp(newScale.x, minScale, maxScale);

                // Обертання
                Vector2 prevTouch0 = touch0.position - touch0.deltaPosition;
                Vector2 prevTouch1 = touch1.position - touch1.deltaPosition;
                float anglePrev = Mathf.Atan2(prevTouch0.y - prevTouch1.y, prevTouch0.x - prevTouch1.x) * Mathf.Rad2Deg;
                float angleCurr = Mathf.Atan2(touch0.position.y - touch1.position.y, touch0.position.x - touch1.position.x) * Mathf.Rad2Deg;
                float angleDelta = Mathf.DeltaAngle(anglePrev, angleCurr);

                if (Mathf.Abs(angleDelta) > 0.5f) target.transform.Rotate(Vector3.up, angleDelta * twistRotationSpeed, Space.World);
            }

            if (touch0.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Ended) ResetPhysicsState();
        }
        else
        {
            isDragging = false;
            ResetPhysicsState();
        }
    }

    // ДОДАНО: Допоміжна функція, яка перевіряє, чи ми натиснули на конкретний об'єкт
    private bool IsPointerOverTarget(Vector2 screenPosition, GameObject target)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            return hit.collider.gameObject == target;
        }
        return false;
    }

    private void PrepareObjectForManipulation(GameObject target)
    {
        selectedRb = target.GetComponent<Rigidbody>();
        if (selectedRb != null && !selectedRb.isKinematic)
        {
            wasKinematic = selectedRb.isKinematic;
            selectedRb.isKinematic = true; 
        }
    }

    private void ResetPhysicsState()
    {
        if (selectedRb != null)
        {
            selectedRb.isKinematic = wasKinematic; 
            selectedRb = null;
        }
    }
}