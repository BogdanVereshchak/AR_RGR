using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectSelectorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIController uiController;

    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = Color.cyan;

    private GameObject selectedObject;
    private Color originalColor;
    private Renderer selectedRenderer;
    private Rigidbody selectedRb;
    private Collider selectedCollider;

    void Update()
    {
        if (uiController == null || uiController.GetCurrentMode() != UIController.InteractionMode.Select) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject() || (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)))
                return;

            TrySelectObject(Input.mousePosition);
        }
    }

    private void TrySelectObject(Vector2 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject.CompareTag("ARPlane") || hitObject.layer == LayerMask.NameToLayer("ARPlane"))
            {
                DeselectCurrent();
                return;
            }

            if (selectedObject == hitObject) return;

            DeselectCurrent();
            SelectObject(hitObject);
        }
    }

    private void SelectObject(GameObject obj)
    {
        selectedObject = obj;
        selectedRenderer = obj.GetComponent<Renderer>();
        selectedRb = obj.GetComponent<Rigidbody>();
        selectedCollider = obj.GetComponent<Collider>();

        if (selectedRenderer != null)
        {
            originalColor = selectedRenderer.material.color;
            selectedRenderer.material.color = highlightColor;
        }

        // Читаємо поточну фізику об'єкта і ПЕРЕДАЄМО В ПОВЗУНКИ UI
        if (selectedRb != null && selectedCollider != null)
        {
            float currentBounce = selectedCollider.material != null ? selectedCollider.material.bounciness : 0f;
            float currentFriction = selectedCollider.material != null ? selectedCollider.material.dynamicFriction : 0.6f;

            // Передаємо тертя назад в UI
            uiController.SyncSlidersWithObject(selectedRb.mass, currentBounce, currentFriction);
        }
    }

    public void DeselectCurrent()
    {
        if (selectedRenderer != null) selectedRenderer.material.color = originalColor;

        selectedObject = null;
        selectedRenderer = null;
        selectedRb = null;
        selectedCollider = null;
    }

    // Застосовує нові налаштування до поточного вибраного об'єкта
    public void UpdateSelectedObjectPhysics(float newMass, float newBounce, float newFriction)
    {
        if (selectedRb != null) selectedRb.mass = newMass;
        
        if (selectedCollider != null && selectedCollider.material != null)
        {
            selectedCollider.material.bounciness = newBounce;
            // Застосовуємо тертя
            selectedCollider.material.dynamicFriction = newFriction;
            selectedCollider.material.staticFriction = newFriction;
        }
    }

    // Метод перевірки, чи вибрано зараз об'єкт
    public bool HasSelectedObject() => selectedObject != null;
    public GameObject GetSelectedObject() => selectedObject;
}