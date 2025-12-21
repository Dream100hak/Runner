using UnityEngine;

/// <summary>
/// 실시간 박치기 전투를 담당. 플레이어와 적이 서로 돌진하면서 충돌/넉백/데미지를 관리한다.
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("Participants")]
    [SerializeField] private Player _player;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float contactThreshold = 1f;

    [Header("Knockback")]
    [SerializeField] private float knockbackDistance = 1.5f;
    [SerializeField] private float deathKnockbackMultiplier = 3f;

    [Header("Effects")]
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float shakeIntensity = 0.25f;
    [SerializeField] private float shakeDuration = 0.15f;

    [Header("Stage")]
    [SerializeField] private Vector3 stageCenterOffset = Vector3.up * -1;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private EnemyStats enemyPrefab;
    [SerializeField] private Vector3 enemyEulerAngles = new Vector3(0f, 180f, 0f);

    [Header("Battle Camera")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 5.5f, -6f);
    [SerializeField] private Vector3 cameraEulerAngles = new Vector3(45f, 180f, 0f);

    private PlayerStats _playerStats;
    private PlayerController _playerController;
    private CameraController _cameraController;
    private CoreGameManager _coreGameManager;

    private bool _battleActive;
    private bool _impactBlocked;
    private bool _coreSubscribed;
    // timestamp of last processed impact to avoid double-processing on the same contact
    private float _lastImpactTime = -10f;
    [SerializeField]
    [Tooltip("Minimum time in seconds between processing separate impact events. Helps avoid duplicate hits on the same contact.")]
    private float minImpactInterval = 0.18f;

    private Vector3 _savedPlayerPosition;
    private Quaternion _savedPlayerRotation;
    private bool _playerControllerWasEnabled;
    [SerializeField]
    private EnemyStats _spawnedEnemy;

    private CharacterController _playerCharacterController;
    private bool _playerCharacterControllerWasEnabled;

    private void Awake()
    {
        EnsureReferences();
        SubscribeToCore();
    }

    private void OnEnable()
    {
        SubscribeToCore();
    }

    private void OnDisable()
    {
        UnsubscribeFromCore();
    }

    private void Update()
    {
        if (!_battleActive || _player == null || _spawnedEnemy == null)
            return;

        if (!_player.IsAlive || !_spawnedEnemy.IsAlive)
        {
            EndBattle();
            return;
        }

        MoveTowardsTargets();
        DetectAndResolveImpact();
    }

    private void EnsureReferences()
    {
        if (_playerController == null)
            _playerController = FindObjectOfType<PlayerController>();

        if (_player == null && _playerController != null)
            _player = _playerController.GetComponent<Player>();

        if (_player == null)
            _player = FindObjectOfType<Player>();

        if (_player != null && _playerStats == null)
            _playerStats = _player.GetComponent<PlayerStats>();
        if (_playerController == null && _player != null)
            _playerController = _player.GetComponent<PlayerController>();

        if (_playerController != null && _playerCharacterController == null)
            _playerCharacterController = _playerController.GetComponent<CharacterController>();

        if (_cameraController == null)
            _cameraController = FindObjectOfType<CameraController>();
    }

    private void MoveTowardsTargets()
    {
        // If an impact just happened, pause approach movement until participants separate
        if (_impactBlocked) return;

        Vector3 playerPos = _player.transform.position;
        Vector3 enemyPos = _spawnedEnemy.transform.position;

        Vector3 playerTarget = new Vector3(enemyPos.x, playerPos.y, enemyPos.z);
        Vector3 enemyTarget = new Vector3(playerPos.x, enemyPos.y, playerPos.z);

        _player.transform.position = Vector3.MoveTowards(playerPos, playerTarget, moveSpeed * Time.deltaTime);
        _spawnedEnemy.transform.position = Vector3.MoveTowards(enemyPos, enemyTarget, moveSpeed * Time.deltaTime);
    }

    private void DetectAndResolveImpact()
    {
        float distance = Vector3.Distance(_player.transform.position, _spawnedEnemy.transform.position);

        if (_impactBlocked)
        {
            if (distance > contactThreshold)
            {
                _impactBlocked = false;
            }
            return;
        }

        if (distance <= contactThreshold)
        {
            // prevent processing multiple impacts in quick succession
            if (Time.time - _lastImpactTime < minImpactInterval)
                return;

            _impactBlocked = true;
            _lastImpactTime = Time.time;
            ResolveImpact();
        }
    }

    private void ResolveImpact()
    {
        Vector3 playerPos = _player.transform.position;
        Vector3 enemyPos = _spawnedEnemy.transform.position;
        Vector3 normal = playerPos - enemyPos;
        if (normal.sqrMagnitude < 0.0001f)
            normal = Vector3.forward;
        normal.y = 0f;
        normal.Normalize();

        float playerAttack = _playerStats?.AttackPower ?? 5f;
        float enemyAttack = _spawnedEnemy.AttackPower;

        int damageToPlayer = Mathf.CeilToInt(enemyAttack);
        int damageToEnemy = Mathf.CeilToInt(playerAttack);

        Debug.Log($"BattleManager: 충돌! 플레이어 -{damageToPlayer} / 적 -{damageToEnemy}");
        BattleMessageManager.Instance.ShowPlayerDamage("용사1", damageToPlayer);
        BattleMessageManager.Instance.ShowEnemyDamage("오크1", damageToEnemy);

        // Show floating damage popups (if manager exists)
        if (DamagePopupManager.Instance != null)
        {
            // show near player and enemy world positions ? pass owner so manager uses configured directions
            DamagePopupManager.Instance.ShowDamage(_player.transform.position + Vector3.up * 1.2f, damageToPlayer, Color.red, true, DamagePopupManager.PopupOwner.Player);
            DamagePopupManager.Instance.ShowDamage(_spawnedEnemy.transform.position + Vector3.up * 1.2f, damageToEnemy, Color.white, true, DamagePopupManager.PopupOwner.Enemy);
        }

        _player.TakeDamage(damageToPlayer);
        _spawnedEnemy.ApplyDamage(damageToEnemy);

        SpawnImpactEffect(Vector3.Lerp(playerPos, enemyPos, 0.5f));

        // compute shifts and animate knockback so characters stay apart naturally
        float playerPush = knockbackDistance + Mathf.Abs(enemyAttack - playerAttack) * 0.1f;
        float enemyPush = knockbackDistance + Mathf.Abs(playerAttack - enemyAttack) * 0.1f;
        Vector3 playerShift = normal * playerPush;
        Vector3 enemyShift = -normal * enemyPush;
        playerShift.y = 0f;
        enemyShift.y = 0f;

        // start coroutine to apply knockback over time; movement paused while _impactBlocked is true
        StartCoroutine(ApplyKnockbackRoutine(playerShift, enemyShift, 0.18f));

        if (!_player.IsAlive)
        {
            Debug.Log("BattleManager: 플레이어 사망");
            ApplyDeathKnockback(_player.transform, normal, true);
            EndBattle();
            return;
        }

        if (!_spawnedEnemy.IsAlive)
        {
            Debug.Log("BattleManager: 적 사망");
            ApplyDeathKnockback(_spawnedEnemy.transform, normal, false);
            EndBattle();
            return;
        }
    }

    private System.Collections.IEnumerator ApplyKnockbackRoutine(Vector3 playerShift, Vector3 enemyShift, float duration)
    {
        if (_player == null || _spawnedEnemy == null) yield break;

        _impactBlocked = true; // ensure approach is paused during knockback

        Vector3 pStart = _player.transform.position;
        Vector3 eStart = _spawnedEnemy.transform.position;
        Vector3 pTarget = pStart + playerShift;
        Vector3 eTarget = eStart + enemyShift;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float f = Mathf.SmoothStep(0f, 1f, t / duration);
            if (_player != null) _player.transform.position = Vector3.Lerp(pStart, pTarget, f);
            if (_spawnedEnemy != null) _spawnedEnemy.transform.position = Vector3.Lerp(eStart, eTarget, f);
            yield return null;
        }

        // ensure final positions applied
        if (_player != null) _player.transform.position = pTarget;
        if (_spawnedEnemy != null) _spawnedEnemy.transform.position = eTarget;

        // small delay to avoid immediate re-impact
        yield return new WaitForSeconds(0.08f);

        _impactBlocked = false;
    }


    private void ApplyKnockback(Vector3 normal, float playerAttack, float enemyAttack)
    {
        float playerPush = knockbackDistance + Mathf.Abs(enemyAttack - playerAttack) * 0.1f;
        float enemyPush = knockbackDistance + Mathf.Abs(playerAttack - enemyAttack) * 0.1f;

        Vector3 playerShift = normal * playerPush;
        Vector3 enemyShift = -normal * enemyPush;

        playerShift.y = 0f;
        enemyShift.y = 0f;

        // apply translation so knockback persists; movement coroutine will resume after separation
        _player.transform.position += playerShift;
        _spawnedEnemy.transform.position += enemyShift;
    }

    private void ApplyDeathKnockback(Transform victim, Vector3 normal, bool isPlayer)
    {
        float pushDistance = knockbackDistance * deathKnockbackMultiplier;
        Vector3 direction = isPlayer ? normal : -normal;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.back;

        victim.transform.position += direction.normalized * pushDistance;
    }

    private void SpawnImpactEffect(Vector3 position)
    {
        if (impactEffectPrefab == null)
            return;

        GameObject effect = Instantiate(impactEffectPrefab, position, Quaternion.identity);
        Destroy(effect, 2f);
    }

    private void StartBattle()
    {
        if (_battleActive)
            return;

        Debug.Log("BattleManager: 전투 시작");
        _battleActive = true;
        _impactBlocked = false;
        SetupBattleScene();
        BattleMessageManager.Instance.ShowEncounter();
    }

    private void EndBattle()
    {
        if (!_battleActive)
            return;

        Debug.Log("BattleManager: 전투 종료");
        _battleActive = false;
        _impactBlocked = false;
        CleanupBattleScene();
        // Notify core game manager to resume running state
        if (CoreGameManager.Instance != null)
        {
            CoreGameManager.Instance.ResumeRunning();
        }
    }

    private void SetupBattleScene()
    {
        if (_player == null)
            return;
        if (enemyPrefab == null)
            return;

        _savedPlayerPosition = _player.transform.position;
        _savedPlayerRotation = _player.transform.rotation;
        EnemyStats battleEnemy = PrepareEnemyInstance();
        if (battleEnemy == null)
            return;

        _spawnedEnemy = battleEnemy;
        battleEnemy.ResetStats();

        _playerControllerWasEnabled = _playerController != null && _playerController.enabled;
        if (_playerController != null)
            _playerController.enabled = false;

        _playerCharacterControllerWasEnabled = _playerCharacterController != null && _playerCharacterController.enabled;
        if (_playerCharacterController != null)
            _playerCharacterController.enabled = false;

        Vector3 playerPosition = playerSpawnPoint != null ? playerSpawnPoint.position : _savedPlayerPosition;
        Vector3 enemyPosition = enemySpawnPoint != null ? enemySpawnPoint.position : _savedPlayerPosition + Vector3.right;

        _player.transform.position = playerPosition;
        if (_spawnedEnemy != null)
            _spawnedEnemy.transform.position = enemyPosition;

        AlignParticipantsFacing();

        if (_cameraController != null)
        {
            Vector3 camPosition = cameraOffset;
            Quaternion camRotation = Quaternion.Euler(cameraEulerAngles);
            _cameraController.SetManualView(camPosition, camRotation);
        }
    }

    private void AlignParticipantsFacing()
    {
        if (_player == null || _spawnedEnemy == null)
            return;

        Vector3 playerToEnemy = _spawnedEnemy.transform.position - _player.transform.position;
        playerToEnemy.y = 0f;
        if (playerToEnemy.sqrMagnitude > 0.001f)
        {
            _player.transform.rotation = Quaternion.LookRotation(playerToEnemy);
            _spawnedEnemy.transform.rotation = Quaternion.LookRotation(-playerToEnemy.normalized);
        }
    }

    private void SubscribeToCore()
    {
        if (_coreSubscribed)
            return;

        if (CoreGameManager.Instance == null)
            return;

        _coreGameManager = CoreGameManager.Instance;
        _coreGameManager.OnStateChanged += OnCoreStateChanged;
        _coreSubscribed = true;

        if (_coreGameManager.CurrentState == CoreGameManager.GameState.Battle)
            StartBattle();
        else
            EndBattle();
    }

    private void UnsubscribeFromCore()
    {
        if (!_coreSubscribed || _coreGameManager == null)
            return;

        _coreGameManager.OnStateChanged -= OnCoreStateChanged;
        _coreSubscribed = false;
    }

    private void OnCoreStateChanged(CoreGameManager.GameState previous, CoreGameManager.GameState next)
    {
        if (next == CoreGameManager.GameState.Battle)
            StartBattle();
        else if (_battleActive)
            EndBattle();
    }

    private EnemyStats PrepareEnemyInstance()
    {
        if (enemyPrefab == null)
        {
            return _spawnedEnemy;
        }

        if (_spawnedEnemy != null)
        {
            Destroy(_spawnedEnemy.gameObject);
            _spawnedEnemy = null;
        }

        _spawnedEnemy = Instantiate(enemyPrefab);
        _spawnedEnemy.transform.rotation = Quaternion.Euler(enemyEulerAngles);
        if (enemySpawnPoint != null)
        {
            _spawnedEnemy.transform.position = enemySpawnPoint.position;
        }
        return _spawnedEnemy;
    }

    private void CleanupBattleScene()
    {
        if (_player != null)
        {
            _player.transform.position = _savedPlayerPosition;
            _player.transform.rotation = _savedPlayerRotation;
        }

        if (_playerCharacterController != null)
            _playerCharacterController.enabled = _playerCharacterControllerWasEnabled;

        if (_playerController != null)
            _playerController.enabled = _playerControllerWasEnabled;

        _cameraController?.ClearManualView();

        if (_spawnedEnemy != null)
        {
            Destroy(_spawnedEnemy.gameObject);
            _spawnedEnemy = null;
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 playerPosition = playerSpawnPoint != null ? playerSpawnPoint.position : transform.position + stageCenterOffset;
        Vector3 enemyPosition = enemySpawnPoint != null ? enemySpawnPoint.position : transform.position + stageCenterOffset + Vector3.right;

        DrawSpawnGizmo(playerPosition, Color.green, "Player Spawn");
        DrawSpawnGizmo(enemyPosition, Color.red, "Enemy Spawn");

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(playerPosition, enemyPosition);
    }

    private static void DrawSpawnGizmo(Vector3 position, Color color, string label)
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(position, 0.25f);
        Gizmos.DrawLine(position + Vector3.up * 0.5f, position - Vector3.up * 0.5f);
        Gizmos.DrawLine(position + Vector3.forward * 0.5f, position - Vector3.forward * 0.5f);
        Gizmos.DrawLine(position + Vector3.right * 0.5f, position - Vector3.right * 0.5f);
    }
}
