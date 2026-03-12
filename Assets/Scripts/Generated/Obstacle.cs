using UnityEngine;

public class RotatingObstacle : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private bool useLocalSpace = true;
    
    [Header("Pendulum Settings")]
    [SerializeField] private bool isPendulum = false;
    [SerializeField] private float pendulumAngle = 45f;
    
    private float currentAngle = 0f;
    private Vector3 initialRotation;
    
    private void Start()
    {
        initialRotation = transform.eulerAngles;
        rotationAxis = rotationAxis.normalized;
    }
    
    private void Update()
    {
        if (isPendulum)
        {
            RotateAsPendulum();
        }
        else
        {
            RotateContinuously();
        }
    }
    
    /// <summary>
    /// Rotates the object continuously around the specified axis
    /// </summary>
    private void RotateContinuously()
    {
        float rotationAmount = rotationSpeed * Time.deltaTime;
        
        if (useLocalSpace)
        {
            transform.Rotate(rotationAxis * rotationAmount, Space.Self);
        }
        else
        {
            transform.Rotate(rotationAxis * rotationAmount, Space.World);
        }
    }
    
    /// <summary>
    /// Rotates the object as a pendulum with oscillating motion
    /// </summary>
    private void RotateAsPendulum()
    {
        currentAngle += rotationSpeed * Time.deltaTime;
        float swingAngle = Mathf.Sin(currentAngle * Mathf.Deg2Rad) * pendulumAngle;
        
        Vector3 targetRotation = initialRotation + (rotationAxis * swingAngle);
        
        if (useLocalSpace)
        {
            transform.localEulerAngles = targetRotation;
        }
        else
        {
            transform.eulerAngles = targetRotation;
        }
    }
    
    /// <summary>
    /// Sets the rotation speed of the obstacle
    /// </summary>
    /// <param name="speed">New rotation speed in degrees per second</param>
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }
    
    /// <summary>
    /// Sets the rotation axis of the obstacle
    /// </summary>
    /// <param name="axis">New rotation axis as Vector3</param>
    public void SetRotationAxis(Vector3 axis)
    {
        rotationAxis = axis.normalized;
    }
    
    /// <summary>
    /// Toggles between continuous rotation and pendulum motion
    /// </summary>
    /// <param name="enablePendulum">True for pendulum motion, false for continuous rotation</param>
    public void SetPendulumMode(bool enablePendulum)
    {
        isPendulum = enablePendulum;
        if (enablePendulum)
        {
            currentAngle = 0f;
            initialRotation = transform.eulerAngles;
        }
    }
    
    /// <summary>
    /// Stops the rotation by setting speed to zero
    /// </summary>
    public void StopRotation()
    {
        rotationSpeed = 0f;
    }
    
    /// <summary>
    /// Resets the obstacle to its initial rotation
    /// </summary>
    public void ResetRotation()
    {
        transform.eulerAngles = initialRotation;
        currentAngle = 0f;
    }
}