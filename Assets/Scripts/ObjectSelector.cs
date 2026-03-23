using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ObjectSelector : MonoBehaviour
{
    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = Color.yellow;
    
    private GameObject selectedObject;
    private Color originalColor;
    private Renderer selectedRenderer;

    void Update()
    {
        // Логіка для Touch (телефон)
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (IsPointerOverUI(touch.position)) return;
                TrySelectObject(touch.position);
            }
        }
        // Логіка для мишки (редактор)
        else if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            TrySelectObject(Input.mousePosition);
        }
    }

    private void TrySelectObject(Vector2 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            GameObject hitObject = hit.collider.gameObject;

            // Якщо ми натиснули на той самий об'єкт — нічого не робимо
            if (selectedObject == hitObject) return;

            DeselectCurrent();
            SelectObject(hitObject);
        }
        else
        {
            // Якщо натиснули в порожнечу — знімаємо виділення
            DeselectCurrent();
        }
    }

    private void SelectObject(GameObject obj)
    {
        selectedObject = obj;
        selectedRenderer = obj.GetComponent<Renderer>();

        if (selectedRenderer != null)
        {
            // Беремо колір з головного матеріалу
            originalColor = selectedRenderer.material.color;
            selectedRenderer.material.color = highlightColor;
        }
        Debug.Log($"[ObjectSelector] Selected: {obj.name}");
    }

    private void DeselectCurrent()
    {
        if (selectedRenderer != null)
        {
            selectedRenderer.material.color = originalColor;
        }
        selectedObject = null;
        selectedRenderer = null;
    }

    public GameObject GetSelectedObject() => selectedObject;

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = screenPosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }

    // Допоміжний метод для зміни меша, якщо знадобиться
    public void ChangeSelectedMesh(Mesh newMesh)
    {
        if (selectedObject == null) return;
        if (selectedObject.TryGetComponent<MeshFilter>(out var filter)) filter.mesh = newMesh;
        if (selectedObject.TryGetComponent<MeshCollider>(out var collider)) collider.sharedMesh = newMesh;
    }
}