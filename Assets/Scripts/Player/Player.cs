using UnityEngine;

/// <summary>
/// Main Player class that manages the player's state and interactions with the game world.
/// </summary>
public class Player : MonoBehaviour
{
    private PlayerController _playerController;
    private PlayerInventory _playerInventory;
    private Animator _animator;
    private bool _isAlive = true;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        _playerInventory = GetComponent<PlayerInventory>();
        _animator = GetComponent<Animator>();

        if (_playerController == null)
            Debug.LogError("Player: PlayerController component not found");
        if (_playerInventory == null)
            Debug.LogError("Player: PlayerInventory component not found");
        if (_animator == null)
            Debug.LogError("Player: Animator component not found");
    }

    private void Start()
    {
        // Initialize player state
        // Health is now managed by PlayerStats via PlayerInventory
    }

    private void Update()
    {
        if (!_isAlive) return;

        // Input processing handled by PlayerController
    }

    /// <summary>
    /// Called when the player takes damage.
    /// </summary>
    /// <param name="damage">Amount of damage to take</param>
    public void TakeDamage(int damage)
    {
        if (!_isAlive) return;

        if (_playerInventory != null)
        {
            _playerInventory.TakeDamage(damage);

            // Check if player is dead
            if (_playerInventory.CurrentHealth <= 0f)
            {
                Die();
            }
        }
    }

    /// <summary>
    /// Called when the player dies.
    /// </summary>
    private void Die()
    {
        _isAlive = false;

        if (_playerController != null)
        {
            _playerController.enabled = false;
        }

        if (_animator != null)
        {
            _animator.SetBool("IsDashing", false);
            _animator.SetBool("IsJumping", false);
        }

        Debug.Log("Player: Player has died");
    }

    /// <summary>
    /// Restores player health.
    /// </summary>
    /// <param name="healAmount">Amount of health to restore</param>
    public void Heal(int healAmount)
    {
        if (_playerInventory != null)
        {
            _playerInventory.Heal(healAmount);
        }
    }

    /// <summary>
    /// Gets the current player inventory.
    /// </summary>
    public PlayerInventory GetInventory() => _playerInventory;

    /// <summary>
    /// Checks if the player is alive.
    /// </summary>
    public bool IsAlive => _isAlive;
}
