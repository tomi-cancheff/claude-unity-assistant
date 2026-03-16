// FileName: HealthUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente UI para mostrar información de vida
/// </summary>
public class HealthUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image fillImage;
    
    [Header("Visual Configuration")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.3f;
    
    private HealthSystem healthSystem;
    
    private void Start()
    {
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged.AddListener(UpdateHealthUI);
            UpdateHealthUI(healthSystem.CurrentHealth);
        }
    }
    
    private void UpdateHealthUI(int currentHealth)
    {
        if (healthSystem == null) return;
        
        float healthPercentage = healthSystem.HealthPercentage;
        
        // Update slider
        if (healthSlider != null)
        {
            healthSlider.maxValue = healthSystem.MaxHealth;
            healthSlider.value = currentHealth;
        }
        
        // Update text
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{healthSystem.MaxHealth}";
        }
        
        // Update color
        if (fillImage != null)
        {
            fillImage.color = Color.Lerp(lowHealthColor, healthyColor, 
                healthPercentage > lowHealthThreshold ? 1f : healthPercentage / lowHealthThreshold);
        }
    }
    
    private void OnDestroy()
    {
        if (healthSystem != null)
            healthSystem.OnHealthChanged.RemoveListener(UpdateHealthUI);
    }
}