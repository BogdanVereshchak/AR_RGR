using UnityEngine;

public class ProjectileBehavior : MonoBehaviour
{
    [SerializeField] private float lifetime = 3.0f; // М'яч зникне через 3 секунди

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}