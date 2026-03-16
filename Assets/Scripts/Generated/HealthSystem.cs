// FileName: HealthSystem.cs
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de vida escalable y reutilizable para cualquier entidad del juego
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [Header("Health Configuration")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private bool initializeOnStart = true;
    
    [Header("Regeneration (Optional)")]
    [SerializeField] private bool enableRegeneration = false;
    [SerializeField] private int regenerationAmount = 1;
    [SerializeField] private float regenerationInterval = 1f;
    private float regenerationTimer;
    
    [Header("Events")]
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent<int, int> OnDamageTaken; // damage amount, remaining health
    public UnityEvent<int> OnHealed;
    public UnityEvent OnDeath;
    public UnityEvent OnHealthFull;
    
    // Properties for external access
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float HealthPercentage => (float)currentHealth / maxHealth;
    public bool IsAlive => currentHealth > 0;
    public bool IsAtFullHealth => currentHealth >= maxHealth;
    
    private void Start()
    {
        if (initializeOnStart)
            InitializeHealth();
    }
    
    private void Update()
    {
        if (enableRegeneration && IsAlive && !IsAtFullHealth)
            HandleRegeneration();
    }
    
    /// <summary>
    /// Inicializa la vida al máximo
    /// </summary>
    public void InitializeHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    /// <summary>
    /// Aplica daño a la entidad
    /// </summary>
    /// <param name="damage">Cantidad de daño a aplicar</param>
    public void TakeDamage(int damage)
    {
        if (!IsAlive || damage <= 0) return;
        
        int previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        OnHealthChanged?.Invoke(currentHealth);
        OnDamageTaken?.Invoke(damage, currentHealth);
        
        if (currentHealth <= 0 && previousHealth > 0)
            Die();
    }
    
    /// <summary>
    /// Cura a la entidad
    /// </summary>
    /// <param name="amount">Cantidad de curación</param>
    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0 || IsAtFullHealth) return;
        
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        
        OnHealthChanged?.Invoke(currentHealth);
        OnHealed?.Invoke(amount);
        
        if (IsAtFullHealth)
            OnHealthFull?.Invoke();
    }
    
    /// <summary>
    /// Establece la vida actual directamente
    /// </summary>
    /// <param name="newHealth">Nueva cantidad de vida</param>
    public void SetHealth(int newHealth)
    {
        bool wasAlive = IsAlive;
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        
        OnHealthChanged?.Invoke(currentHealth);
        
        if (wasAlive && !IsAlive)
            Die();
        else if (IsAtFullHealth)
            OnHealthFull?.Invoke();
    }
    
    /// <summary>
    /// Modifica la vida máxima
    /// </summary>
    /// <param name="newMaxHealth">Nueva vida máxima</param>
    /// <param name="adjustCurrentHealth">Si debe ajustar la vida actual proporcionalmente</param>
    public void SetMaxHealth(int newMaxHealth, bool adjustCurrentHealth = false)
    {
        if (newMaxHealth <= 0) return;
        
        if (adjustCurrentHealth && maxHealth > 0)
        {
            float healthRatio = HealthPercentage;
            maxHealth = newMaxHealth;
            currentHealth = Mathf.RoundToInt(maxHealth * healthRatio);
        }
        else
        {
            maxHealth = newMaxHealth;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }
        
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    /// <summary>
    /// Mata instantáneamente a la entidad
    /// </summary>
    public void Kill()
    {
        if (!IsAlive) return;
        
        currentHealth = 0;
        OnHealthChanged?.Invoke(currentHealth);
        Die();
    }
    
    /// <summary>
    /// Revive a la entidad con vida completa o específica
    /// </summary>
    /// <param name="healthAmount">Cantidad de vida al revivir (0 = vida máxima)</param>
    public void Revive(int healthAmount = 0)
    {
        if (IsAlive) return;
        
        currentHealth = healthAmount <= 0 ? maxHealth : Mathf.Min(healthAmount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
        
        if (IsAtFullHealth)
            OnHealthFull?.Invoke();
    }
    
    private void HandleRegeneration()
    {
        regenerationTimer += Time.deltaTime;
        
        if (regenerationTimer >= regenerationInterval)
        {
            Heal(regenerationAmount);
            regenerationTimer = 0f;
        }
    }
    
    private void Die()
    {
        OnDeath?.Invoke();
    }
    
    // Métodos de utilidad para debugging
    [ContextMenu("Take 10 Damage")]
    private void Debug_TakeDamage() => TakeDamage(10);
    
    [ContextMenu("Heal 10")]
    private void Debug_Heal() => Heal(10);
    
    [ContextMenu("Kill")]
    private void Debug_Kill() => Kill();
    
    [ContextMenu("Revive")]
    private void Debug_Revive() => Revive();
}