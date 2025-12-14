using UnityEngine;

/// <summary>
/// Manages player stats including health and stamina.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private int _maxHealth = 100;

    [SerializeField] private float _hpRegenPerSecond = 1f;
    [SerializeField] private float _hpRegenDelay = 2f; // seconds after taking damage before regen starts

    private int _currentHealth;
    private float _regenAccumulator = 0f;
    private float _timeSinceDamage = Mathf.Infinity;

    /// <summary>
    /// Initializes the player inventory with max values.
    /// </summary>
    public void Initialize()
    {
        _currentHealth = _maxHealth;
    }

    private void Update()
    {
        // Handle HP regeneration after delay
        _timeSinceDamage += Time.deltaTime;

        if (_currentHealth < _maxHealth && _timeSinceDamage >= _hpRegenDelay)
        {
            _regenAccumulator += _hpRegenPerSecond * Time.deltaTime;
            int healAmount = Mathf.FloorToInt(_regenAccumulator);
            if (healAmount > 0)
            {
                _currentHealth += healAmount;
                _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
                _regenAccumulator -= healAmount;
            }
        }
    }

  /// <summary>
    /// Consumes player health to pay dash cost.
    /// Will not allow health to drop to 0 or below (minimum 1 remains).
    /// </summary>
    /// <param name="amount">Amount of HP to consume</param>
    /// <returns>True if HP was successfully consumed, false if insufficient HP to keep player alive</returns>
  public bool DrainStamina(int amount)
    {
        // Ensure draining doesn't kill the player: require that health after drain stays >= 1
        if (_currentHealth - amount < 1)
        {
            return false;
        }

        _currentHealth -= amount;
        _currentHealth = Mathf.Max(_currentHealth, 1);
        Debug.Log($"PlayerInventory: Consumed {amount} HP for dash. Current health: {_currentHealth}");
        return true;
    }

    /// <summary>
    /// Applies damage to the player.
    /// </summary>
    /// <param name="damage">Amount of damage</param>
    public void TakeDamage(int damage)
    {
        _currentHealth -= damage;
        _currentHealth = Mathf.Max(_currentHealth, 0);
        // Reset regen timer
        _timeSinceDamage = 0f;
        _regenAccumulator = 0f;
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
    /// Stamina system removed; health is used for dash cost instead.
    /// RestoreStamina method removed.

    /// <summary>
    /// Gets the current health.
    /// </summary>
  public int CurrentHealth => _currentHealth;

/// <summary>
    /// Gets the current stamina.
    /// </summary>
    // Stamina removed

    /// <summary>
    /// Gets the max health.
    /// </summary>
    public int MaxHealth => _maxHealth;

    /// <summary>
    /// Gets the max stamina.
    /// </summary>
    // Stamina removed

    /// <summary>
    /// Checks if player has enough health to pay a dash cost without dying.
    /// </summary>
    public bool HasStamina(int requiredAmount) => (_currentHealth - requiredAmount) >= 1;
}
