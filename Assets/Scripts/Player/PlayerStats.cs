using System;
using UnityEngine;

/// <summary>
/// Manages player HP, EXP, Level, AttackPower and exposes event hooks when values change.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public event Action<PlayerStats>? OnStatsChanged;
    public event Action? OnGameOver;

    [SerializeField] private float _maxHP = 100f;
    [SerializeField] private float _attackPower = 10f;
    [SerializeField] private float _expToNextLevel = 100f;
    [SerializeField] private float _expGrowthFactor = 1.2f;
    [SerializeField] private int _recommendedBossLevel = 8;

    private float _currentHP;
    private float _currentExp;
    private int _level = 1;

    public float MaxHP => _maxHP;
    public float CurrentHP => _currentHP;
    public float CurrentExp => _currentExp;
    public float AttackPower => _attackPower;
    public int Level => _level;
    public float ExpToNextLevel => _expToNextLevel;
    public bool IsBossReady => _level >= _recommendedBossLevel;

    private void Awake()
    {
        ResetStats();
    }

    public void ResetStats()
    {
        _currentHP = _maxHP;
        _currentExp = 0f;
        _level = 1;
        _attackPower = Mathf.Max(1f, _attackPower);
        _expToNextLevel = Mathf.Max(1f, _expToNextLevel);
        DispatchChanged();
    }

    public bool DrainHP(float amount)
    {
        if (amount <= 0f) return true;
        if (_currentHP <= 0f) return false;

        _currentHP -= amount;
        if (_currentHP <= 0f)
        {
            _currentHP = 0f;
            OnGameOver?.Invoke();
        }

        DispatchChanged();
        return _currentHP > 0f;
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || _currentHP <= 0f) return;
        _currentHP = Mathf.Min(_maxHP, _currentHP + amount);
        DispatchChanged();
    }

    public void GainExp(float amount)
    {
        if (amount <= 0f) return;
        _currentExp += amount;
        while (_currentExp >= _expToNextLevel)
        {
            _currentExp -= _expToNextLevel;
            LevelUp();
        }
        DispatchChanged();
    }

    private void LevelUp()
    {
        _level++;
        _maxHP += 10f;
        _attackPower += 2f;
        _currentHP = _maxHP;
        _expToNextLevel *= _expGrowthFactor;
        DispatchChanged();
    }

    private void DispatchChanged()
    {
        OnStatsChanged?.Invoke(this);
    }
}
