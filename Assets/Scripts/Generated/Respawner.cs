using UnityEngine;
using UnityEngine.Events;

public class RespawnZone : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float respawnDelay = 0.1f;
    [SerializeField] private UnityEvent OnPlayerRespawn;
    
    private void Start()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
        
        Collider2D trigger2D = GetComponent<Collider2D>();
        if (trigger2D != null)
        {
            trigger2D.isTrigger = true;
        }
    }
    
    /// <summary>
    /// Sets the spawn point where the player will respawn
    /// </summary>
    /// <param name="newSpawnPoint">Transform representing the spawn location</param>
    public void SetSpawnPoint(Transform newSpawnPoint)
    {
        spawnPoint = newSpawnPoint;
    }
    
    /// <summary>
    /// Respawns the player at the assigned spawn point
    /// </summary>
    /// <param name="player">GameObject of the player to respawn</param>
    public void RespawnPlayer(GameObject player)
    {
        if (spawnPoint == null)
        {
            Debug.LogWarning("No spawn point assigned to RespawnZone!");
            return;
        }
        
        StartCoroutine(RespawnCoroutine(player));
    }
    
    private System.Collections.IEnumerator RespawnCoroutine(GameObject player)
    {
        yield return new WaitForSeconds(respawnDelay);
        
        player.transform.position = spawnPoint.position;
        player.transform.rotation = spawnPoint.rotation;
        
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        Rigidbody2D rb2D = player.GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
        }
        
        OnPlayerRespawn?.Invoke();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            RespawnPlayer(other.gameObject);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            RespawnPlayer(other.gameObject);
        }
    }
}