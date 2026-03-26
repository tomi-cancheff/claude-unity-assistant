// FileName: ShootingScript.cs
using UnityEngine;

public class ShootingScript : MonoBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float fireRate = 0.5f;
    
    private float nextFireTime = 0f;
    
    private void Update()
    {
        HandleInput();
    }
    
    /// <summary>
    /// Handles shooting input
    /// </summary>
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }
    
    /// <summary>
    /// Creates and fires a bullet
    /// </summary>
    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogWarning("Bullet prefab or fire point not assigned!");
            return;
        }
        
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = firePoint.forward * bulletSpeed;
        }
    }
}