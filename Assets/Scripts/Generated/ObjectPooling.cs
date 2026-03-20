// FileName: ObjectPooling.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de pooling genérico para objetos reutilizables
/// </summary>
public class ObjectPooling : MonoBehaviour
{
    [System.Serializable]
    public class PoolSettings
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialSize = 10;
        [SerializeField] private int maxSize = 100;
        [SerializeField] private bool allowExpansion = true;
        
        public GameObject Prefab => prefab;
        public int InitialSize => initialSize;
        public int MaxSize => maxSize;
        public bool AllowExpansion => allowExpansion;
    }
    
    [Header("Pool Configuration")]
    [SerializeField] private List<PoolSettings> poolConfigurations = new List<PoolSettings>();
    [SerializeField] private Transform poolParent;
    
    [Header("Events")]
    public UnityEvent<GameObject> OnObjectSpawned;
    public UnityEvent<GameObject> OnObjectReturned;
    
    private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, PoolSettings> poolSettings = new Dictionary<string, PoolSettings>();
    private Dictionary<GameObject, string> instanceToPoolKey = new Dictionary<GameObject, string>();
    
    private static ObjectPooling instance;
    public static ObjectPooling Instance => instance;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Inicializa todos los pools configurados
    /// </summary>
    private void InitializePools()
    {
        if (poolParent == null)
            poolParent = transform;
            
        foreach (var config in poolConfigurations)
        {
            if (config.Prefab != null)
            {
                CreatePool(config.Prefab.name, config);
            }
        }
    }
    
    /// <summary>
    /// Crea un pool para un tipo específico de objeto
    /// </summary>
    private void CreatePool(string poolKey, PoolSettings settings)
    {
        pools[poolKey] = new Queue<GameObject>();
        poolSettings[poolKey] = settings;
        
        for (int i = 0; i < settings.InitialSize; i++)
        {
            GameObject obj = Instantiate(settings.Prefab, poolParent);
            obj.SetActive(false);
            obj.name = $"{settings.Prefab.name}_{i}";
            
            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnCreated();
            
            pools[poolKey].Enqueue(obj);
            instanceToPoolKey[obj] = poolKey;
        }
    }
    
    /// <summary>
    /// Obtiene un objeto del pool
    /// </summary>
    /// <param name="prefab">Prefab del objeto a obtener</param>
    /// <param name="position">Posición donde spawnearlo</param>
    /// <param name="rotation">Rotación del objeto</param>
    /// <returns>GameObject del pool o null si no está disponible</returns>
    public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        return GetObject(prefab.name, position, rotation);
    }
    
    /// <summary>
    /// Obtiene un objeto del pool por nombre
    /// </summary>
    /// <param name="poolKey">Nombre del pool</param>
    /// <param name="position">Posición donde spawnearlo</param>
    /// <param name="rotation">Rotación del objeto</param>
    /// <returns>GameObject del pool o null si no está disponible</returns>
    public GameObject GetObject(string poolKey, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(poolKey))
        {
            Debug.LogWarning($"Pool '{poolKey}' no existe");
            return null;
        }
        
        GameObject obj = null;
        var pool = pools[poolKey];
        var settings = poolSettings[poolKey];
        
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else if (settings.AllowExpansion)
        {
            obj = Instantiate(settings.Prefab, poolParent);
            obj.name = $"{settings.Prefab.name}_{System.Guid.NewGuid().ToString("N")[..8]}";
            instanceToPoolKey[obj] = poolKey;
            
            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnCreated();
        }
        
        if (obj != null)
        {
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            
            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnSpawned();
            
            OnObjectSpawned?.Invoke(obj);
        }
        
        return obj;
    }
    
    /// <summary>
    /// Devuelve un objeto al pool
    /// </summary>
    /// <param name="obj">Objeto a devolver</param>
    public void ReturnObject(GameObject obj)
    {
        if (obj == null || !instanceToPoolKey.ContainsKey(obj))
            return;
            
        string poolKey = instanceToPoolKey[obj];
        var settings = poolSettings[poolKey];
        
        if (pools[poolKey].Count < settings.MaxSize)
        {
            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnReturned();
            
            obj.SetActive(false);
            obj.transform.SetParent(poolParent);
            pools[poolKey].Enqueue(obj);
            
            OnObjectReturned?.Invoke(obj);
        }
        else
        {
            instanceToPoolKey.Remove(obj);
            Destroy(obj);
        }
    }
    
    /// <summary>
    /// Limpia todos los pools
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var pool in pools.Values)
        {
            while (pool.Count > 0)
            {
                var obj = pool.Dequeue();
                if (obj != null)
                {
                    instanceToPoolKey.Remove(obj);
                    Destroy(obj);
                }
            }
        }
        
        pools.Clear();
        poolSettings.Clear();
        instanceToPoolKey.Clear();
    }
    
    /// <summary>
    /// Obtiene información del estado de un pool
    /// </summary>
    /// <param name="poolKey">Nombre del pool</param>
    /// <returns>Información del pool</returns>
    public PoolInfo GetPoolInfo(string poolKey)
    {
        if (!pools.ContainsKey(poolKey))
            return null;
            
        return new PoolInfo
        {
            PoolName = poolKey,
            AvailableObjects = pools[poolKey].Count,
            MaxSize = poolSettings[poolKey].MaxSize,
            AllowExpansion = poolSettings[poolKey].AllowExpansion
        };
    }
}