// FileName: SimpleRotator.cs
using UnityEngine;

public class SimpleRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 90, 0);
    [SerializeField] private Space rotationSpace = Space.Self;
    [SerializeField] private bool rotateOnStart = true;
    
    [Header("Control")]
    [SerializeField] private bool isRotating = true;
    
    private void Start()
    {
        if (!rotateOnStart)
        {
            isRotating = false;
        }
    }
    
    private void Update()
    {
        if (isRotating)
        {
            RotateObject();
        }
    }
    
    /// <summary>
    /// Rotates the object based on the configured rotation speed and space
    /// </summary>
    private void RotateObject()
    {
        Vector3 rotation = rotationSpeed * Time.deltaTime;
        transform.Rotate(rotation, rotationSpace);
    }
    
    /// <summary>
    /// Starts the rotation
    /// </summary>
    public void StartRotation()
    {
        isRotating = true;
    }
    
    /// <summary>
    /// Stops the rotation
    /// </summary>
    public void StopRotation()
    {
        isRotating = false;
    }
    
    /// <summary>
    /// Toggles the rotation state
    /// </summary>
    public void ToggleRotation()
    {
        isRotating = !isRotating;
    }
    
    /// <summary>
    /// Sets the rotation speed for all axes
    /// </summary>
    /// <param name="newSpeed">New rotation speed vector</param>
    public void SetRotationSpeed(Vector3 newSpeed)
    {
        rotationSpeed = newSpeed;
    }
    
    /// <summary>
    /// Sets the rotation space (Self or World)
    /// </summary>
    /// <param name="space">The space to rotate in</param>
    public void SetRotationSpace(Space space)
    {
        rotationSpace = space;
    }
}