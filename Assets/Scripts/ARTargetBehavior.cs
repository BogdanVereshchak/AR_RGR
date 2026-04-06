using UnityEngine;

public class TargetBehavior : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Damage & Score")]
    [SerializeField] private float maxDamage = 60f; // Урон при влучанні прямо в центр
    [SerializeField] private float minDamage = 15f; // Урон при влучанні в самий край
    [SerializeField] private int basePoints = 10;   // Очки за знищення
    [SerializeField] private int bullseyeBonus = 25; // Бонус за ідеальне влучання

    [Header("Visuals (Bonus)")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color criticalColor = Color.red;
    [SerializeField] private GameObject hitEffectPrefab; // Ефект іскор при влучанні
    [SerializeField] private GameObject destroyEffectPrefab; // Ефект вибуху при знищенні

    private ARGameManager gameManager;
    private Renderer targetRenderer;
    private Collider targetCollider;

    void Start()
    {
        gameManager = FindFirstObjectByType<ARGameManager>();
        targetRenderer = GetComponent<Renderer>();
        targetCollider = GetComponent<Collider>();
        
        currentHealth = maxHealth;
        UpdateColor();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Projectile"))
        {
            // 1. РОЗРАХУНОК ТОЧНОСТІ (Відстань від центру)
            ContactPoint contact = collision.contacts[0]; // Точка, куди вдарив м'яч
            Vector3 center = targetCollider.bounds.center; // Центр мішені
            
            // Відстань від центру до точки удару
            float distanceToCenter = Vector3.Distance(center, contact.point);
            
            // Максимальний радіус мішені (приблизно)
            float maxRadius = targetCollider.bounds.extents.magnitude; 
            
            // Визначаємо точність від 0 (край) до 1 (ідеальний центр)
            float accuracy = 1f - Mathf.Clamp01(distanceToCenter / maxRadius);

            // 2. РОЗРАХУНОК УРОНУ
            float damageTaken = Mathf.Lerp(minDamage, maxDamage, accuracy);
            currentHealth -= damageTaken;

            // Спавн ефекту попадання (якщо є)
            if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, contact.point, Quaternion.identity);

            // 3. ПЕРЕВІРКА НА ЗНИЩЕННЯ
            if (currentHealth <= 0)
            {
                int finalScore = basePoints;
                
                // Якщо останній удар був дуже точним (точність > 80%), даємо бонус!
                if (accuracy > 0.8f) finalScore += bullseyeBonus; 

                // Повідомляємо менеджеру, що нас знищено
                if (gameManager != null) gameManager.TargetDestroyed(gameObject, finalScore);

                // Спавн ефекту знищення
                if (destroyEffectPrefab != null) Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);

                Destroy(gameObject);
            }
            else
            {
                // Якщо ще живе - міняємо колір
                UpdateColor();
            }
        }
    }

    // Бонусна фіча: зміна кольору залежно від ХП
    private void UpdateColor()
    {
        if (targetRenderer != null)
        {
            float healthPercentage = currentHealth / maxHealth;
            // Плавно переходимо від червоного (0) до зеленого (1)
            targetRenderer.material.color = Color.Lerp(criticalColor, healthyColor, healthPercentage);
        }
    }
}