using UnityEngine;

public class SmoothCameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    
    [Header("Follow Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 5, -10);
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float rotationSpeed = 2f;
    
    [Header("Look Settings")]
    [SerializeField] private bool lookAtTarget = true;
    [SerializeField] private Vector3 lookOffset = Vector3.zero;
    
    [Header("Constraints")]
    [SerializeField] private bool useHeightLimit = false;
    [SerializeField] private float minHeight = 1f;
    [SerializeField] private float maxHeight = 20f;
    
    private Vector3 currentVelocity;
    
    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
    }
    
    private void LateUpdate()
    {
        if (target == null) return;
        
        FollowTarget();
        
        if (lookAtTarget)
            LookAtTarget();
    }
    
    /// <summary>
    /// Smoothly follows the target with the specified offset
    /// </summary>
    private void FollowTarget()
    {
        Vector3 desiredPosition = target.position + offset;
        
        if (useHeightLimit)
        {
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minHeight, maxHeight);
        }

        transform.position = Vector3.Lerp(
        transform.position, desiredPosition, followSpeed * Time.deltaTime);
    }
    
    /// <summary>
    /// Smoothly rotates camera to look at target
    /// </summary>
    private void LookAtTarget()
    {
        Vector3 lookPosition = target.position + lookOffset;
        Vector3 direction = lookPosition - transform.position;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Sets a new target for the camera to follow
    /// </summary>
    /// <param name="newTarget">The new transform to follow</param>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    /// <summary>
    /// Updates the camera offset from the target
    /// </summary>
    /// <param name="newOffset">New offset position</param>
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(target.position + offset, 0.5f);
            
            if (lookAtTarget)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(target.position + lookOffset, 0.3f);
            }
        }
    }
}