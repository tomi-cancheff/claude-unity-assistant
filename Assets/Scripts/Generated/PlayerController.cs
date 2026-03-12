using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SphereController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float maxSpeed = 15f;
    
    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.6f;
    [SerializeField] private LayerMask groundLayer = 1;
    
    private Rigidbody rb;
    private bool isGrounded;
    private Vector3 moveInput;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    private void Update()
    {
        HandleInput();
        CheckGrounded();
        
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }
    
    private void FixedUpdate()
    {
        Move();
        LimitSpeed();
    }
    
    /// <summary>
    /// Handles player input for movement
    /// </summary>
    private void HandleInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        moveInput = new Vector3(horizontal, 0f, vertical).normalized;
    }
    
    /// <summary>
    /// Applies movement force to the sphere
    /// </summary>
    private void Move()
    {
        if (moveInput.magnitude > 0.1f)
        {
            Vector3 force = moveInput * moveSpeed;
            rb.AddForce(force, ForceMode.Acceleration);
        }
    }
    
    /// <summary>
    /// Makes the sphere jump
    /// </summary>
    private void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    
    /// <summary>
    /// Checks if the sphere is touching the ground
    /// </summary>
    private void CheckGrounded()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }
    
    /// <summary>
    /// Limits the sphere's horizontal speed
    /// </summary>
    private void LimitSpeed()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        
        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);
    }
}