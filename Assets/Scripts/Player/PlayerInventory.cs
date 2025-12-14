using UnityEngine;

/// <summary>
/// Manages player stats including health and stamina.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private int _maxStamina = 100;
    [SerializeField] private int _staminaDrainPerSecond = 20;

    private int _currentHealth;
  private int _currentStamina;

    /// <summary>
    /// Initializes the player inventory with max values.
    /// </summary>
    public void Initialize()
    {
        _currentHealth = _maxHealth;
        _currentStamina = _maxStamina;
    }

    private void Update()
    {
        // Regenerate stamina over time when not dashing
  if (_currentStamina < _maxStamina)
        {
            _currentStamina += (int)(_maxStamina * 0.5f * Time.deltaTime);
       _currentStamina = Mathf.Min(_currentStamina, _maxStamina);
        }
    }

/// <summary>
    /// Drains stamina for dash ability.
    /// </summary>
    /// <param name="amount">Amount of stamina to drain</param>
  /// <returns>True if stamina was successfully drained, false if insufficient stamina</returns>
  public bool DrainStamina(int amount)
    {
 if (_currentStamina >= amount)
        {
            _currentStamina -= amount;
            return true;
  }
        return false;
    }

    /// <summary>
    /// Applies damage to the player.
    /// </summary>
    /// <param name="damage">Amount of damage</param>
    public void TakeDamage(int damage)
    {
        _currentHealth -= damage;
        _currentHealth = Mathf.Max(_currentHealth, 0);
        Debug.Log($"PlayerInventory: Took {damage} damage. Current health: {_currentHealth}");
    }

    /// <summary>
    /// Heals the player.
    /// </summary>
    /// <param name="healAmount">Amount to heal</param>
    public void Heal(int healAmount)
    {
        _currentHealth += healAmount;
        _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
        Debug.Log($"PlayerInventory: Healed {healAmount}. Current health: {_currentHealth}");
    }

  /// <summary>
    /// Restores stamina.
    /// </summary>
    /// <param name="staminaAmount">Amount of stamina to restore</param>
    public void RestoreStamina(int staminaAmount)
    {
        _currentStamina += staminaAmount;
   _currentStamina = Mathf.Min(_currentStamina, _maxStamina);
    }

    /// <summary>
    /// Gets the current health.
    /// </summary>
  public int CurrentHealth => _currentHealth;

/// <summary>
    /// Gets the current stamina.
    /// </summary>
    public int CurrentStamina => _currentStamina;

    /// <summary>
    /// Gets the max health.
    /// </summary>
    public int MaxHealth => _maxHealth;

    /// <summary>
    /// Gets the max stamina.
    /// </summary>
    public int MaxStamina => _maxStamina;

    /// <summary>
    /// Checks if player has enough stamina for an action.
    /// </summary>
    public bool HasStamina(int requiredAmount) => _currentStamina >= requiredAmount;
}
