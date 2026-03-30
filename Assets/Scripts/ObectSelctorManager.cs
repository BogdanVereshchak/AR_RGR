using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ObjectSelectorManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIManager uiManager; // ПРИВ'ЯЖИ UIMANAGER ТУТ!

    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = Color.yellow;
    
    private GameObject selectedObject;
    private Color originalColor;
    private Renderer selectedRenderer;

    void Update()
    {
        if (uiManager == null) return;

        // БЛОКУВАННЯ: Якщо ми не в режимі Select, нічого не виділяємо
        if (uiManager.GetCurrentMode() != UIManager.InteractionMode.Select)
        {
            if (selectedObject != null) DeselectCurrent(); // Знімаємо виділення при зміні режиму
            return;
        }

        // Обробка Touch (Мобілка)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (IsPointerOverUI(touch.position)) return;
                TrySelectObject(touch.position);
            }
        }
        // Обробка миші (XR Simulation / Editor)
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

            if (selectedObject == hitObject) return;

            DeselectCurrent();
            SelectObject(hitObject);
        }
        else
        {
            DeselectCurrent();
        }
    }

    private void SelectObject(GameObject obj)
    {
        selectedObject = obj;
        selectedRenderer = obj.GetComponent<Renderer>();

        if (selectedRenderer != null)
        {
            originalColor = selectedRenderer.material.color;
            selectedRenderer.material.color = highlightColor;
        }
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

    public void ChangeSelectedMesh(Mesh newMesh)
    {
        if (selectedObject == null) return;
        if (selectedObject.TryGetComponent<MeshFilter>(out var filter)) filter.mesh = newMesh;
        if (selectedObject.TryGetComponent<MeshCollider>(out var collider)) collider.sharedMesh = newMesh;
    }
}