// FileName: WaveData.cs
using UnityEngine;

[System.Serializable]
public class WaveData
{
    [Header("Wave Configuration")]
    public string waveName = "Wave 1";
    public int enemyCount = 5;
    public float spawnInterval = 1f;
    public float delayAfterWave = 3f;
    
    [Header("Specific Settings (Optional)")]
    [Tooltip("Index del enemigo específico a spawnear. -1 para aleatorio")]
    public int specificEnemyIndex = -1;
    [Tooltip("Index del spawn point específico. -1 para aleatorio")]
    public int specificSpawnPoint = -1;
}