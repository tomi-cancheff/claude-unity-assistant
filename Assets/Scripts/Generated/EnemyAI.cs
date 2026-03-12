using UnityEngine;
using UnityEngine.Events;

public class EnemyAI : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float fieldOfViewAngle = 60f;
    [SerializeField] private LayerMask obstacleLayerMask = -1;
    [SerializeField] private LayerMask playerLayerMask = 1;
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float stopDistance = 1.5f;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onPlayerDetected;
    [SerializeField] private UnityEvent onPlayerLost;
    [SerializeField] private UnityEvent onReachedPlayer;
    
    private Transform player;
    private bool playerDetected;
    private bool wasPlayerDetected;
    private Vector3 lastKnownPlayerPosition;
    
    private void Start()
    {
        FindPlayer();
    }
    
    private void Update()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }
        
        CheckLineOfSight();
        HandleMovement();
        HandleEvents();
    }
    
    /// <summary>
    /// Finds the player GameObject in the scene by tag
    /// </summary>
    private void FindPlayer()
    {
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
        }
    }
    
    /// <summary>
    /// Checks if player is within line of sight considering range, angle and obstacles
    /// </summary>
    private void CheckLineOfSight()
    {
        playerDetected = false;
        
        if (player == null) return;
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Check if player is within detection range
        if (distanceToPlayer > detectionRange) return;
        
        // Check if player is within field of view
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        if (angleToPlayer > fieldOfViewAngle * 0.5f) return;
        
        // Check for obstacles using raycast
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, directionToPlayer);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, distanceToPlayer, obstacleLayerMask))
        {
            // If we hit something that's not the player, line of sight is blocked
            if (((1 << hit.collider.gameObject.layer) & playerLayerMask) == 0)
            {
                return;
            }
        }
        
        playerDetected = true;
        lastKnownPlayerPosition = player.position;
    }
    
    /// <summary>
    /// Handles enemy movement towards player when detected
    /// </summary>
    private void HandleMovement()
    {
        if (!playerDetected || player == null) return;
        
        Vector3 targetPosition = player.position;

        //  — ignora la altura del player, solo se mueve en XZ
        Vector3 targetFlat = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        Vector3 direction = (targetFlat - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(targetFlat, transform.position);

        // Rotate towards player
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
        }
        
        // Move towards player if not too close
        if (distanceToPlayer > stopDistance)
        {
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
        else
        {
            onReachedPlayer.Invoke();
        }
    }
    
    /// <summary>
    /// Handles detection state change events
    /// </summary>
    private void HandleEvents()
    {
        if (playerDetected && !wasPlayerDetected)
        {
            onPlayerDetected.Invoke();
        }
        else if (!playerDetected && wasPlayerDetected)
        {
            onPlayerLost.Invoke();
        }
        
        wasPlayerDetected = playerDetected;
    }
    
    /// <summary>
    /// Draws detection gizmos in Scene view for debugging
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = playerDetected ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Field of view
        Vector3 leftBoundary = Quaternion.AngleAxis(-fieldOfViewAngle * 0.5f, Vector3.up) * transform.forward * detectionRange;
        Vector3 rightBoundary = Quaternion.AngleAxis(fieldOfViewAngle * 0.5f, Vector3.up) * transform.forward * detectionRange;
        
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        
        // Line to player if detected
        if (playerDetected && player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, player.position);
        }
        
        // Stop distance
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}