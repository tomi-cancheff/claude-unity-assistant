// FileName: BulletScript.cs
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float damage = 10f;
    
    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            // Apply damage logic here
            Debug.Log($"Hit {other.name} for {damage} damage");
        }
        
        if (!other.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }
}