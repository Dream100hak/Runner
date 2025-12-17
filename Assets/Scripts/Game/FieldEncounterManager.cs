using UnityEngine;

/// <summary>
/// Simulates random field encounters while the player is running in the overworld.
/// When the player is moving fast enough and the core game state is Running, this class
/// periodically rolls for encounters and tells <see cref="CoreGameManager"/> to enter battle.
/// </summary>
[DisallowMultipleComponent]
public class FieldEncounterManager : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("How often (in seconds) we roll for a random encounter while the player is running.")]
    [SerializeField] private float checkInterval = 1.5f;
    [Tooltip("Minimum forward speed required to consider an encounter.")]
    [SerializeField] private float minSpeedForEncounter = 1f;

    [Header("Probability")]
    [Tooltip("Probability (0..1) that an encounter triggers when the check fires.")]
    [Range(0f, 1f)]
    [SerializeField] private float encounterChance = 0.25f;

    [Header("Cooldown")]
    [Tooltip("Grace period after a battle starts before another encounter may trigger.")]
    [SerializeField] private float battleCooldown = 3f;

    private float _checkTimer;
    private float _cooldownTimer;
    private PlayerController _playerController;
    private CoreGameManager _coreGameManager;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        if (_playerController == null)
        {
            _playerController = FindObjectOfType<PlayerController>();
        }
    }

    private void OnEnable()
    {
        SubscribeToCore();
    }

    private void OnDisable()
    {
        UnsubscribeFromCore();
    }

    private void SubscribeToCore()
    {
        if (_coreGameManager != null)
            return;

        if (CoreGameManager.Instance == null)
            return;

        _coreGameManager = CoreGameManager.Instance;
        _coreGameManager.OnStateChanged += OnCoreStateChanged;
    }

    private void UnsubscribeFromCore()
    {
        if (_coreGameManager == null)
            return;

        _coreGameManager.OnStateChanged -= OnCoreStateChanged;
        _coreGameManager = null;
    }

    private void Update()
    {
        if (_playerController == null || _coreGameManager == null)
        {
            SubscribeToCore();
            return;
        }

        if (!CanCheckEncounter())
        {
            _checkTimer = 0f;
            return;
        }

        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
            return;
        }

        _checkTimer += Time.deltaTime;
        if (_checkTimer < Mathf.Max(0.01f, checkInterval))
            return;

        _checkTimer = 0f;
        if (Random.value <= encounterChance)
        {
            _cooldownTimer = battleCooldown;
            _coreGameManager.EnterBattle();
        }
    }

    private bool CanCheckEncounter()
    {
        if (_coreGameManager.CurrentState != CoreGameManager.GameState.Running)
            return false;

        if (_playerController.CurrentSpeed < minSpeedForEncounter)
            return false;

        return _playerController.IsGrounded;
    }

    private void OnCoreStateChanged(CoreGameManager.GameState previous, CoreGameManager.GameState next)
    {
        if (next == CoreGameManager.GameState.Running)
        {
            _cooldownTimer = 0f;
        }
        else if (next == CoreGameManager.GameState.Battle)
        {
            _cooldownTimer = battleCooldown;
        }
    }
}
