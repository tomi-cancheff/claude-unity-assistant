// FileName: DamageDealer.cs
using UnityEngine;

/// <summary>
/// Componente para infligir daño por contacto o activación
/// </summary>
public class DamageDealer : MonoBehaviour
{
    [Header("Damage Configuration")]
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private bool damageOnTrigger = true;
    [SerializeField] private bool damageOnCollision = false;
    [SerializeField] private LayerMask targetLayers = -1;
    
    [Header("Damage Over Time")]
    [SerializeField] private bool continuousDamage = false;
    [SerializeField] private float damageInterval = 1f;
    
    private void OnTriggerEnter(Collider other)
    {
        if (damageOnTrigger)
            TryDealDamage(other.gameObject);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (damageOnTrigger)
            TryDealDamage(other.gameObject);
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (damageOnCollision)
            TryDealDamage(collision.gameObject);
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (damageOnCollision)
            TryDealDamage(collision.gameObject);
    }
    
    private void TryDealDamage(GameObject target)
    {
        // Check layer mask
        if ((targetLayers.value & (1 << target.layer)) == 0) return;
        
        // Try to deal damage
        HealthSystem healthSystem = target.GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            DealDamage(healthSystem);
        }
    }
    
    /// <summary>
    /// Inflige daño a un sistema de vida específico
    /// </summary>
    /// <param name="targetHealth">Sistema de vida objetivo</param>
    public void DealDamage(HealthSystem targetHealth)
    {
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damageAmount);
        }
    }
    
    /// <summary>
    /// Cambia la cantidad de daño
    /// </summary>
    /// <param name="newDamage">Nueva cantidad de daño</param>
    public void SetDamageAmount(int newDamage)
    {
        damageAmount = Mathf.Max(0, newDamage);
    }
}