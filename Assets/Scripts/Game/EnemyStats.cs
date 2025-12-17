using System;
using UnityEngine;

/// <summary>
/// 단순한 전투 상대 스탯. BattleManager가 데미지를 적용하고 상태를 조회하는 용도로 사용.
/// </summary>
public class EnemyStats : MonoBehaviour
{
    [SerializeField] private float _maxHP = 60f;
    [SerializeField] private float _attackPower = 12f;

    private float _currentHP;

    public float MaxHP => _maxHP;
    public float CurrentHP => _currentHP;
    public float AttackPower => _attackPower;
    public bool IsAlive => _currentHP > 0f;

    public event Action? OnDeath;

    private void Awake()
    {
        ResetStats();
    }

    public void ResetStats()
    {
        _currentHP = _maxHP;
    }

    public bool ApplyDamage(float amount)
    {
        if (_currentHP <= 0f)
            return false;
        if (amount <= 0f)
            return IsAlive;

        _currentHP -= amount;
        if (_currentHP <= 0f)
        {
            _currentHP = 0f;
            OnDeath?.Invoke();
        }

        return IsAlive;
    }
}
