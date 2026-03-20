// FileName: CollectibleItem.cs
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Ejemplo de item coleccionable que usa el sistema de pooling
/// </summary>
public class CollectibleItem : PoolableObject
{
    [Header("Collectible Settings")]
    [SerializeField] private int value = 10;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float bobHeight = 0.5f;
    [SerializeField] private float bobSpeed = 2f;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem collectEffect;
    [SerializeField] private AudioClip collectSound;
    
    [Header("Events")]
    public UnityEvent<int> OnCollected;
    
    private Vector3 startPosition;
    private AudioSource audioSource;
    private bool isCollected = false;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    
    private void Update()
    {
        if (!isCollected)
        {
            // Rotación
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
            
            // Bobbing animation
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
    
    public override void OnSpawned()
    {
        base.OnSpawned();
        startPosition = transform.position;
        isCollected = false;
        
        // Activar collider y renderer
        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = true;
        
        var renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = true;
    }
    
    protected override void ResetObject()
    {
        base.ResetObject();
        isCollected = false;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            Collect();
        }
    }
    
    /// <summary>
    /// Recoge el item
    /// </summary>
    private void Collect()
    {
        isCollected = true;
        
        // Desactivar visualmente
        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
        
        var renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;
        
        // Efectos
        if (collectEffect != null)
        {
            collectEffect.Play();
        }
        
        if (audioSource != null && collectSound != null)
        {
            audioSource.PlayOneShot(collectSound);
        }
        
        // Evento
        OnCollected?.Invoke(value);
        
        // Devolver al pool después de los efectos
        Invoke(nameof(ReturnToPool), 1f);
    }
}