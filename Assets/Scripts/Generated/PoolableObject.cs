// FileName: PoolableObject.cs
using UnityEngine;

/// <summary>
/// Componente base para objetos poolables
/// </summary>
public class PoolableObject : MonoBehaviour, IPoolable
{
    [Header("Pool Settings")]
    [SerializeField] private float autoReturnTime = -1f; // -1 = no auto return
    
    private float spawnTime;
    
    public virtual void OnCreated()
    {
        // Override en clases derivadas para inicialización única
    }
    
    public virtual void OnSpawned()
    {
        spawnTime = Time.time;
        
        if (autoReturnTime > 0)
        {
            Invoke(nameof(ReturnToPool), autoReturnTime);
        }
    }
    
    public virtual void OnReturned()
    {
        CancelInvoke();
        
        // Reset del objeto
        ResetObject();
    }
    
    /// <summary>
    /// Devuelve este objeto al pool
    /// </summary>
    public void ReturnToPool()
    {
        if (ObjectPooling.Instance != null)
        {
            ObjectPooling.Instance.ReturnObject(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Reset del estado del objeto. Override en clases derivadas
    /// </summary>
    protected virtual void ResetObject()
    {
        transform.localScale = Vector3.one;
        
        // Reset de componentes comunes
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        var rb2d = GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
        }
    }
}