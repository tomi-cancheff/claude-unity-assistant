// FileName: MovingPlatform.cs
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Vector3 pointA;
    [SerializeField] private Vector3 pointB;
    [SerializeField] private float speed = 2f;
    [SerializeField] private bool startAtPointA = true;
    
    private Vector3 targetPoint;
    private bool movingToB;
    
    void Start()
    {
        if (startAtPointA)
        {
            transform.position = pointA;
            targetPoint = pointB;
            movingToB = true;
        }
        else
        {
            transform.position = pointB;
            targetPoint = pointA;
            movingToB = false;
        }
    }
    
    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);
        
        if (Vector3.Distance(transform.position, targetPoint) < 0.1f)
        {
            if (movingToB)
            {
                targetPoint = pointA;
                movingToB = false;
            }
            else
            {
                targetPoint = pointB;
                movingToB = true;
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pointA, 0.3f);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pointB, 0.3f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pointA, pointB);
    }
}