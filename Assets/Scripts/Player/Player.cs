using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main Player class that manages the player's state and interactions with the game world.
/// </summary>
public class Player : MonoBehaviour
{
    private PlayerController _playerController;
    private PlayerInventory _playerInventory;
    private Animator _animator;
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private TextMeshProUGUI _hpText;
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

        // If HP slider not assigned in inspector, try to find by name
        if (_hpSlider == null)
        {
            var go = GameObject.Find("UI_HpBar");
            if (go != null)
            {
                _hpSlider = go.GetComponent<Slider>();
                if (_hpSlider == null)
                    Debug.LogWarning("Player: Found UI_HpBar but no Slider component attached");
            }
        }

        // If HP text not assigned, try to find TextMeshPro object named UI_HPBarText
        if (_hpText == null)
        {
            var textGo = GameObject.Find("UI_HPBarText");
            if (textGo != null)
            {
                _hpText = textGo.GetComponent<TextMeshProUGUI>();
                if (_hpText == null)
                    Debug.LogWarning("Player: Found UI_HPBarText but no TextMeshProUGUI component attached");
            }
        }
    }

    private void Start()
    {
        // Initialize player state
        if (_playerInventory != null)
        {
            _playerInventory.Initialize();
            // Initialize HP bar values
            if (_hpSlider != null)
            {
                _hpSlider.maxValue = _playerInventory.MaxHealth;
                _hpSlider.value = _playerInventory.CurrentHealth;
            }
            if (_hpText != null)
            {
                _hpText.text = $"{_playerInventory.CurrentHealth}/{_playerInventory.MaxHealth}";
            }
        }
    }

    private void Update()
    {
        // Update HP bar every frame
        if (_hpSlider != null && _playerInventory != null)
        {
            _hpSlider.value = _playerInventory.CurrentHealth;
        }

        if (_hpText != null && _playerInventory != null)
        {
            _hpText.text = $"{_playerInventory.CurrentHealth}/{_playerInventory.MaxHealth}";
        }

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
            if (_playerInventory.CurrentHealth <= 0)
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
    /// Restores player stamina.
    /// </summary>
    /// <param name="staminaAmount">Amount of stamina to restore</param>
    // Stamina removed from PlayerInventory; RestoreStamina wrapper removed.

    /// <summary>
    /// Gets the current player inventory.
    /// </summary>
    public PlayerInventory GetInventory() => _playerInventory;

    /// <summary>
    /// Checks if the player is alive.
    /// </summary>
    public bool IsAlive => _isAlive;
}
