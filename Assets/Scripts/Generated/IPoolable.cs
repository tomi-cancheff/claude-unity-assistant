// FileName: IPoolable.cs
using UnityEngine;

/// <summary>
/// Interface para objetos que pueden ser pooled
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// Llamado cuando el objeto es creado por primera vez
    /// </summary>
    void OnCreated();
    
    /// <summary>
    /// Llamado cuando el objeto es sacado del pool
    /// </summary>
    void OnSpawned();
    
    /// <summary>
    /// Llamado cuando el objeto es devuelto al pool
    /// </summary>
    void OnReturned();
}