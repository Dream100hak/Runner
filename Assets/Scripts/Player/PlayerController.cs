using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _forwardSpeed = 10f;
    [SerializeField] private float _dashSpeedMultiplier = 1.5f;
    [SerializeField] private float _staminaDrainPerSecond = 30f;
    [SerializeField] private float _jumpHeight = 4.5f;
    [SerializeField] private float _gravity = -25f;
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private LayerMask _groundLayer = 1;
    [SerializeField] private float _groundCheckDistance = 0.2f;

    private CharacterController _characterController;
    private PlayerInventory _playerInventory;
    private Animator _animator;

    private float _verticalVelocity;
    private bool _isDashing;
    private bool _isGrounded;
    private bool _isJumping;
    private float _touchStartTime;
    private const float _tapThreshold = 0.2f;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _playerInventory = GetComponent<PlayerInventory>();
        _animator = GetComponent<Animator>();

        if (_groundCheck == null)
        {
            Debug.LogWarning("PlayerController: Ground check transform is missing. Using player position.");
        }

        if (_playerInventory == null)
        {
            Debug.LogError("PlayerController: PlayerInventory component not found");
        }
    }

    private void Update()
    {
        HandleInput();
        MovePlayer();
        UpdateAnimatorParameters();
    }

    private void MovePlayer()
    {
        Vector3 groundPosition = _groundCheck != null ? _groundCheck.position : transform.position;
        _isGrounded = Physics.CheckSphere(groundPosition, _groundCheckDistance, _groundLayer, QueryTriggerInteraction.Ignore);

        if (_isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -2f;
        }

        if (!_isGrounded)
        {
            _verticalVelocity += _gravity * Time.deltaTime;
        }

        float speed = _forwardSpeed * (_isDashing ? _dashSpeedMultiplier : 1f);
        Vector3 motion = transform.forward * speed;
        motion.y = _verticalVelocity;

        _characterController.Move(motion * Time.deltaTime);

        if (_isDashing)
        {
            DrainDashStamina();
        }
    }

    private void HandleInput()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            _touchStartTime = Time.time;
        }

        if (Input.GetMouseButton(0))
        {
            float holdDuration = Time.time - _touchStartTime;
            if (holdDuration > _tapThreshold && !_isDashing && _isGrounded && CanStartDash())
            {
                EnterDashMode();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            float holdDuration = Time.time - _touchStartTime;
            if (holdDuration <= _tapThreshold && _isGrounded && !_isDashing)
            {
                Jump();
            }
            else if (_isDashing)
            {
                ExitDashMode();
            }
        }
#endif
    }

    private bool CanStartDash()
    {
        if (_playerInventory == null)
            return true;

        return _playerInventory.HasStamina(1);
    }

    private void DrainDashStamina()
    {
        if (_playerInventory == null)
            return;

        int cost = Mathf.Max(1, Mathf.CeilToInt(_staminaDrainPerSecond * Time.deltaTime));
        if (!_playerInventory.DrainStamina(cost))
        {
            ExitDashMode();
        }
    }

    private void Jump()
    {
        if (!_isGrounded)
            return;

        _isJumping = true;
        _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        Debug.Log("PlayerController: Player jumped");
    }

    private void EnterDashMode()
    {
        _isDashing = true;
        Debug.Log("PlayerController: Entered dash mode");
    }

    private void ExitDashMode()
    {
        _isDashing = false;
        Debug.Log("PlayerController: Exited dash mode");
    }

    private void UpdateAnimatorParameters()
    {
        if (_animator == null)
            return;

        _animator.SetBool("IsRunning", true);
        _animator.SetBool("IsJumping", !_isGrounded && _isJumping);
        _animator.SetBool("IsDashing", _isDashing);

        if (_isGrounded && _isJumping)
        {
            _isJumping = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            if (_isDashing)
            {
                Destroy(other.gameObject);
            }
            else
            {
                Player player = GetComponent<Player>();
                if (player != null)
                {
                    player.TakeDamage(10);
                }
            }
        }
    }

    public bool IsDashing => _isDashing;
    public bool IsGrounded => _isGrounded;
    public float CurrentSpeed => _forwardSpeed * (_isDashing ? _dashSpeedMultiplier : 1f);
}
