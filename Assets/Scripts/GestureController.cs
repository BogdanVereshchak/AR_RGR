using UnityEngine;

public class GestureController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ObjectSelector objectSelector;

    [Header("Scale Settings")]
    [SerializeField] private float minScale = 0.05f;
    [SerializeField] private float maxScale = 5.0f;
    [SerializeField] private float scrollScaleSpeed = 0.2f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 0.5f;

    [Header("Height Settings (Local)")]
    [SerializeField] private float liftSpeed = 0.0005f;
    [SerializeField] private float minHeight = -0.2f;
    [SerializeField] private float maxHeight = 1.0f;

    private float initialPinchDistance;
    private Vector3 initialScale;

    void Update()
    {
        if (objectSelector == null) return;

        GameObject selected = objectSelector.GetSelectedObject();
        if (selected == null) return;

        // --- MOBILE GESTURES ---
        if (Input.touchCount == 2)
        {
            HandlePinchToScale(selected);
        }
        else if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                HandleManipulation(selected, touch);
            }
        }

        // --- EDITOR / PC CONTROLS ---
        HandleMouseAndKeyboard(selected);
    }

    private void HandlePinchToScale(GameObject target)
    {
        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);
        float currentDist = Vector2.Distance(t0.position, t1.position);

        if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
        {
            initialPinchDistance = currentDist;
            initialScale = target.transform.localScale;
        }
        else if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
        {
            if (initialPinchDistance <= 0.01f) return;
            
            float factor = currentDist / initialPinchDistance;
            Vector3 newScale = initialScale * factor;
            
            float clampedX = Mathf.Clamp(newScale.x, minScale, maxScale);
            target.transform.localScale = Vector3.one * clampedX;
        }
    }

    private void HandleManipulation(GameObject target, Touch touch)
    {
        // 1. ВІЛЬНЕ ОБЕРТАННЯ (X та Y)
        // Поворот вліво-вправо (навколо світової осі Y)
        float rotY = -touch.deltaPosition.x * rotationSpeed;
        target.transform.Rotate(Vector3.up, rotY, Space.World);

        // Поворот вверх-вниз (навколо осі "вправо" відносно камери)
        // Це робить обертання інтуїтивним: куди тягнеш палець, туди об'єкт і крутиться
        float rotX = touch.deltaPosition.y * rotationSpeed;
        target.transform.Rotate(Camera.main.transform.right, rotX, Space.World);

        // 2. ПІДЙОМ (Lift)
        // Оскільки один палець тепер зайнятий обертанням у двох площинах, 
        // підйом краще залишити для ПК або додати окрему кнопку.
        // Але якщо дуже треба одним пальцем — він буде спрацьовувати одночасно з нахилом.
    }

    private void HandleMouseAndKeyboard(GameObject target)
    {
        // Масштаб (мишка)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float s = Mathf.Clamp(target.transform.localScale.x + scroll * scrollScaleSpeed, minScale, maxScale);
            target.transform.localScale = Vector3.one * s;
        }

        // Обертання (Ліва кнопка миші)
        if (Input.GetMouseButton(0))
        {
            float mouseX = -Input.GetAxis("Mouse X") * rotationSpeed * 10f;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * 10f;

            target.transform.Rotate(Vector3.up, -mouseX, Space.World);
            target.transform.Rotate(Camera.main.transform.right, mouseY, Space.World);
        }

        // ПІДЙОМ (Клавіші або Права кнопка миші)
        float liftInput = 0;
        if (Input.GetKey(KeyCode.W)) liftInput = 1;
        if (Input.GetKey(KeyCode.S)) liftInput = -1;
        
        if (liftInput != 0 || Input.GetMouseButton(1))
        {
            float verticalMove = liftInput != 0 ? liftInput * 0.1f : Input.GetAxis("Mouse Y");
            Vector3 localPos = target.transform.localPosition;
            localPos.y = Mathf.Clamp(localPos.y + verticalMove * liftSpeed * 100f, minHeight, maxHeight);
            target.transform.localPosition = localPos;
        }
    }
}