using UnityEngine;

/// <summary>
/// Handles health regeneration and dash cost using PlayerStats data.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private PlayerStats _playerStats;

    [SerializeField] private float _hpRegenPerSecond = 1f;
    [SerializeField] private float _hpRegenDelay = 2f; // seconds after taking damage before regen starts

    private float _regenAccumulator = 0f;
    private float _timeSinceDamage = Mathf.Infinity;

    private void Awake()
    {
        if (_playerStats == null)
        {
            _playerStats = GetComponent<PlayerStats>();
        }

        if (_playerStats == null)
        {
            Debug.LogError("PlayerInventory: PlayerStats component not found");
        }
    }

    private void Update()
    {
        if (_playerStats == null) return;

        _timeSinceDamage += Time.deltaTime;

        if (_playerStats.CurrentHP < _playerStats.MaxHP && _timeSinceDamage >= _hpRegenDelay)
        {
            _regenAccumulator += _hpRegenPerSecond * Time.deltaTime;
            float healAmount = Mathf.Floor(_regenAccumulator);
            if (healAmount >= 1f)
            {
                _playerStats.Heal(healAmount);
                _regenAccumulator -= healAmount;
                _timeSinceDamage = 0f;
            }
        }
    }

    /// <summary>
    /// Consumes player health to pay dash cost.
    /// </summary>
    public bool DrainStamina(int amount)
    {
        if (_playerStats == null) return false;

        if (!_playerStats.DrainHP(amount))
        {
            return false;
        }

        _timeSinceDamage = 0f;
        _regenAccumulator = 0f;
        Debug.Log($"PlayerInventory: Consumed {amount} HP for dash. Current health: {_playerStats.CurrentHP}");
        return true;
    }

    /// <summary>
    /// Applies damage to the player.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (_playerStats == null) return;

        _playerStats.DrainHP(damage);
        _timeSinceDamage = 0f;
        _regenAccumulator = 0f;
        Debug.Log($"PlayerInventory: Took {damage} damage. Current health: {_playerStats.CurrentHP}");
    }

    /// <summary>
    /// Heals the player.
    /// </summary>
    public void Heal(float healAmount)
    {
        if (_playerStats == null) return;

        _playerStats.Heal(healAmount);
    }

    public float CurrentHealth => _playerStats?.CurrentHP ?? 0f;
    public float MaxHealth => _playerStats?.MaxHP ?? 0f;

    public bool HasStamina(int requiredAmount)
    {
        if (_playerStats == null) return false;
        return _playerStats.CurrentHP - requiredAmount >= 1f;
    }
}
