// FileName: EnemyController.cs
using UnityEngine;
using UnityEngine.Events;

public class EnemyController : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] private float health = 100f;
    [SerializeField] private float maxHealth = 100f;
    
    [Header("Events")]
    public UnityEvent OnEnemyDestroyed;
    public UnityEvent<float> OnHealthChanged;
    
    void Start()
    {
        health = maxHealth;
        OnHealthChanged?.Invoke(health / maxHealth);
    }
    
    /// <summary>
    /// Aplica daño al enemigo
    /// </summary>
    public void TakeDamage(float damage)
    {
        health -= damage;
        OnHealthChanged?.Invoke(health / maxHealth);
        
        if (health <= 0f)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Destruye al enemigo
    /// </summary>
    public void Die()
    {
        OnEnemyDestroyed?.Invoke();
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Obtiene la vida actual del enemigo
    /// </summary>
    public float GetHealth()
    {
        return health;
    }
    
    /// <summary>
    /// Obtiene el porcentaje de vida del enemigo
    /// </summary>
    public float GetHealthPercentage()
    {
        return health / maxHealth;
    }
}