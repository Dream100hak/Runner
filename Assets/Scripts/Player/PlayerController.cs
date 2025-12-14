using UnityEngine;

/// <summary>
/// Controls player movement, jumping, and dashing mechanics for 1-line runner gameplay.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _forwardSpeed = 10f;
    [SerializeField] private float _dashSpeedMultiplier = 1.5f;
    [SerializeField] private float _staminaDrainPerSecond = 30f;
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _groundDrag = 0.3f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckDistance = 0.1f;
    
    private Rigidbody _rigidbody;
    private PlayerInventory _playerInventory;
    private Animator _animator;
    private bool _isDashing = false;
    private bool _isGrounded = false;
    private bool _isJumping = false;
    private float _touchStartTime;
    private const float _tapThreshold = 0.2f;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
 _playerInventory = GetComponent<PlayerInventory>();
        _animator = GetComponent<Animator>();
 
 if (_rigidbody == null)
   Debug.LogError("PlayerController: Rigidbody component not found");
        if (_playerInventory == null)
     Debug.LogError("PlayerController: PlayerInventory component not found");
        if (_animator == null)
          Debug.LogError("PlayerController: Animator component not found");
    }

    private void Start()
    {
        // Initialize rigidbody constraints
 if (_rigidbody != null)
        {
_rigidbody.freezeRotation = true;
        }
    }

    private void FixedUpdate()
    {
   // Check if player is grounded
        CheckGroundCollision();

        // Apply forward movement
        ApplyForwardMovement();

        // Apply drag
    ApplyDrag();
    }

    private void Update()
    {
        // Handle input
 HandleInput();

        // Update animator parameters
        UpdateAnimatorParameters();
    }

    /// <summary>
    /// Checks if the player is touching the ground using raycasting.
    /// </summary>
    private void CheckGroundCollision()
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        _isGrounded = Physics.Raycast(ray, _groundCheckDistance, _groundLayer);
    }

    /// <summary>
    /// Handles player input for jumping and dashing.
    /// </summary>
    private void HandleInput()
    {
        // Mobile input simulation (touch-based)
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
    _touchStartTime = Time.time;
        }

        if (Input.GetMouseButton(0))
   {
     float holdDuration = Time.time - _touchStartTime;
    
      // If held longer than tap threshold, enter dash mode
       if (holdDuration > _tapThreshold && !_isDashing && _isGrounded)
 {
      if (_playerInventory != null && _playerInventory.HasStamina((int)_staminaDrainPerSecond))
                {
              EnterDashMode();
       }
          }
  }

       if (Input.GetMouseButtonUp(0))
   {
 float holdDuration = Time.time - _touchStartTime;
     
          // If released quickly (tap), perform jump
 if (holdDuration <= _tapThreshold && _isGrounded && !_isDashing)
 {
       Jump();
    }
     // Exit dash mode
 else if (_isDashing)
    {
           ExitDashMode();
   }
        }
#endif
    }

    /// <summary>
    /// Applies forward movement to the player.
    /// </summary>
    private void ApplyForwardMovement()
    {
        if (_rigidbody == null) return;

     float currentSpeed = _forwardSpeed;

        // Increase speed if dashing
        if (_isDashing)
        {
currentSpeed *= _dashSpeedMultiplier;
 }

        // Apply forward velocity
      Vector3 velocity = _rigidbody.linearVelocity;
 velocity.z = currentSpeed;
        _rigidbody.linearVelocity = velocity;
    }

    /// <summary>
    /// Makes the player jump with the specified force.
    /// </summary>
    private void Jump()
    {
  if (_rigidbody == null || !_isGrounded || _isDashing) return;

        _isJumping = true;
        _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
  _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        
        Debug.Log("PlayerController: Player jumped");
 }

    /// <summary>
    /// Enters dash mode and starts draining stamina.
    /// </summary>
    private void EnterDashMode()
    {
        _isDashing = true;
     Debug.Log("PlayerController: Entered dash mode");
    }

    /// <summary>
    /// Exits dash mode and stops stamina drain.
    /// </summary>
    private void ExitDashMode()
    {
        _isDashing = false;
        Debug.Log("PlayerController: Exited dash mode");
 }

    /// <summary>
    /// Applies drag to simulate air resistance.
    /// </summary>
    private void ApplyDrag()
    {
     if (_rigidbody == null) return;

      Vector3 velocity = _rigidbody.linearVelocity;
        float drag = _isGrounded ? _groundDrag : 0.05f;
        
        // Apply drag to lateral movement
        velocity.x *= (1f - drag * Time.fixedDeltaTime);
      velocity.y *= (1f - 0.01f * Time.fixedDeltaTime);
        
        _rigidbody.linearVelocity = velocity;
    }

    /// <summary>
    /// Applies stamina drain while dashing.
    /// </summary>
    private void DrainStamina()
    {
      if (_playerInventory == null || !_isDashing) return;

        int drainAmount = (int)(_staminaDrainPerSecond * Time.deltaTime);
        if (!_playerInventory.DrainStamina(drainAmount))
        {
     // Exit dash mode if stamina runs out
            ExitDashMode();
        }
 }

    /// <summary>
    /// Handles collision with obstacles and enemies.
    /// </summary>
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Obstacle"))
    {
            if (_isDashing)
            {
        // Destroy obstacle while dashing
         Destroy(collision.gameObject);
  }
            else
   {
            // Take damage if not dashing
    Player player = GetComponent<Player>();
           if (player != null)
     {
     player.TakeDamage(10);
                }
            }
        }
    }

    /// <summary>
    /// Updates animator parameters based on current state.
    /// </summary>
    private void UpdateAnimatorParameters()
  {
        if (_animator == null) return;

 _animator.SetBool("IsRunning", true);
        _animator.SetBool("IsJumping", !_isGrounded && _isJumping);
  _animator.SetBool("IsDashing", _isDashing);
        
        // Reset jump state when grounded again
        if (_isGrounded && _isJumping)
    {
       _isJumping = false;
        }

     // Drain stamina while dashing
        if (_isDashing)
        {
         DrainStamina();
        }
    }

    /// <summary>
    /// Gets the current movement speed.
    /// </summary>
 public float GetCurrentSpeed() => _isDashing ? _forwardSpeed * _dashSpeedMultiplier : _forwardSpeed;

    /// <summary>
    /// Checks if the player is currently dashing.
    /// </summary>
    public bool IsDashing => _isDashing;

    /// <summary>
    /// Checks if the player is grounded.
    /// </summary>
    public bool IsGrounded => _isGrounded;
}
