// FileName: EnemySpawner.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxEnemies = 10;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool useRandomSpawnPoints = true;
    [SerializeField] private bool useRandomEnemies = true;
    
    [Header("Wave System")]
    [SerializeField] private bool useWaveSystem = false;
    [SerializeField] private WaveData[] waves;
    
    [Header("Events")]
    public UnityEvent OnEnemySpawned;
    public UnityEvent OnWaveCompleted;
    public UnityEvent OnAllWavesCompleted;
    
    private List<GameObject> activeEnemies = new List<GameObject>();
    private int currentWave = 0;
    private bool isSpawning = false;
    private Coroutine spawnCoroutine;
    
    void Start()
    {
        if (spawnOnStart)
        {
            StartSpawning();
        }
    }
    
    /// <summary>
    /// Inicia el sistema de spawn de enemigos
    /// </summary>
    public void StartSpawning()
    {
        if (isSpawning) return;
        
        isSpawning = true;
        if (useWaveSystem && waves.Length > 0)
        {
            StartCoroutine(SpawnWaves());
        }
        else
        {
            spawnCoroutine = StartCoroutine(SpawnEnemiesContinuous());
        }
    }
    
    /// <summary>
    /// Detiene el sistema de spawn
    /// </summary>
    public void StopSpawning()
    {
        isSpawning = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        StopAllCoroutines();
    }
    
    /// <summary>
    /// Spawna un enemigo específico en una posición específica
    /// </summary>
    public GameObject SpawnEnemy(int enemyIndex, int spawnPointIndex)
    {
        if (enemyPrefabs.Length == 0 || spawnPoints.Length == 0) return null;
        if (activeEnemies.Count >= maxEnemies) return null;
        
        enemyIndex = Mathf.Clamp(enemyIndex, 0, enemyPrefabs.Length - 1);
        spawnPointIndex = Mathf.Clamp(spawnPointIndex, 0, spawnPoints.Length - 1);
        
        return CreateEnemy(enemyPrefabs[enemyIndex], spawnPoints[spawnPointIndex].position);
    }
    
    /// <summary>
    /// Spawna un enemigo aleatorio en una posición aleatoria
    /// </summary>
    public GameObject SpawnRandomEnemy()
    {
        if (enemyPrefabs.Length == 0 || spawnPoints.Length == 0) return null;
        if (activeEnemies.Count >= maxEnemies) return null;
        
        GameObject enemyPrefab = useRandomEnemies ? 
            enemyPrefabs[Random.Range(0, enemyPrefabs.Length)] : enemyPrefabs[0];
            
        Vector3 spawnPosition = useRandomSpawnPoints ?
            spawnPoints[Random.Range(0, spawnPoints.Length)].position : spawnPoints[0].position;
        
        return CreateEnemy(enemyPrefab, spawnPosition);
    }
    
    private GameObject CreateEnemy(GameObject prefab, Vector3 position)
    {
        GameObject enemy = Instantiate(prefab, position, Quaternion.identity);
        activeEnemies.Add(enemy);
        
        // Suscribirse a la destrucción del enemigo
        EnemyController enemyController = enemy.GetComponent<EnemyController>();
        if (enemyController != null)
        {
            enemyController.OnEnemyDestroyed.AddListener(() => RemoveEnemy(enemy));
        }
        
        OnEnemySpawned?.Invoke();
        return enemy;
    }
    
    private void RemoveEnemy(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }
    
    private IEnumerator SpawnEnemiesContinuous()
    {
        while (isSpawning)
        {
            if (activeEnemies.Count < maxEnemies)
            {
                SpawnRandomEnemy();
            }
            
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    
    private IEnumerator SpawnWaves()
    {
        for (int i = 0; i < waves.Length; i++)
        {
            if (!isSpawning) yield break;
            
            currentWave = i;
            yield return StartCoroutine(SpawnWave(waves[i]));
            
            // Esperar a que todos los enemigos de la wave sean eliminados
            yield return new WaitUntil(() => activeEnemies.Count == 0);
            
            OnWaveCompleted?.Invoke();
            
            if (i < waves.Length - 1)
            {
                yield return new WaitForSeconds(waves[i].delayAfterWave);
            }
        }
        
        OnAllWavesCompleted?.Invoke();
        isSpawning = false;
    }
    
    private IEnumerator SpawnWave(WaveData wave)
    {
        for (int i = 0; i < wave.enemyCount; i++)
        {
            if (!isSpawning) yield break;
            
            if (wave.specificEnemyIndex >= 0)
            {
                SpawnEnemy(wave.specificEnemyIndex, 
                          wave.specificSpawnPoint >= 0 ? wave.specificSpawnPoint : Random.Range(0, spawnPoints.Length));
            }
            else
            {
                SpawnRandomEnemy();
            }
            
            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }
    
    /// <summary>
    /// Limpia todos los enemigos activos
    /// </summary>
    public void ClearAllEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] != null)
            {
                Destroy(activeEnemies[i]);
            }
        }
        activeEnemies.Clear();
    }
    
    /// <summary>
    /// Obtiene el número de enemigos activos
    /// </summary>
    public int GetActiveEnemyCount()
    {
        // Limpiar referencias nulas
        activeEnemies.RemoveAll(enemy => enemy == null);
        return activeEnemies.Count;
    }
    
    /// <summary>
    /// Obtiene la wave actual (solo si usa sistema de waves)
    /// </summary>
    public int GetCurrentWave()
    {
        return currentWave;
    }
}