using UnityEngine;

/// <summary>
/// 실시간 박치기 전투를 담당. 플레이어와 적이 서로 돌진하면서 충돌/넉백/데미지를 관리한다.
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("Participants")]
    [SerializeField] private Player _player;
    [SerializeField] private EnemyStats _enemy;

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
    [SerializeField] private Vector3 stageCenterOffset = Vector3.zero;
    [SerializeField] private Vector3 playerBattleOffset = new Vector3(-1.4f, 0f, 0f);
    [SerializeField] private Vector3 enemyBattleOffset = new Vector3(1.4f, 0f, 0f);
    [SerializeField] private GameObject stagePrefab;
    [SerializeField] private Vector3 stageScale = new Vector3(3.5f, 0.18f, 3.5f);
    [SerializeField] private Color stageFloorColor = new Color(0.45f, 0.35f, 0.25f);

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

    private Vector3 _savedPlayerPosition;
    private Quaternion _savedPlayerRotation;
    private Vector3 _savedEnemyPosition;
    private Quaternion _savedEnemyRotation;
    private bool _playerControllerWasEnabled;
    private GameObject _battleStage;
    private Material _generatedFloorMaterial;

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
        if (!_battleActive || _player == null || _enemy == null)
            return;

        if (!_player.IsAlive || !_enemy.IsAlive)
        {
            EndBattle();
            return;
        }

        MoveTowardsTargets();
        DetectAndResolveImpact();
    }

    private void EnsureReferences()
    {
        if (_player == null)
            _player = FindObjectOfType<Player>();
        if (_enemy == null)
            _enemy = FindObjectOfType<EnemyStats>();

        if (_player != null && _playerStats == null)
            _playerStats = _player.GetComponent<PlayerStats>();
        if (_player != null && _playerController == null)
            _playerController = _player.GetComponent<PlayerController>();

        if (_cameraController == null)
            _cameraController = FindObjectOfType<CameraController>();
    }

    private void MoveTowardsTargets()
    {
        Vector3 playerPos = _player.transform.position;
        Vector3 enemyPos = _enemy.transform.position;

        Vector3 playerTarget = new Vector3(enemyPos.x, playerPos.y, enemyPos.z);
        Vector3 enemyTarget = new Vector3(playerPos.x, enemyPos.y, playerPos.z);

        _player.transform.position = Vector3.MoveTowards(playerPos, playerTarget, moveSpeed * Time.deltaTime);
        _enemy.transform.position = Vector3.MoveTowards(enemyPos, enemyTarget, moveSpeed * Time.deltaTime);
    }

    private void DetectAndResolveImpact()
    {
        float distance = Vector3.Distance(_player.transform.position, _enemy.transform.position);

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
            _impactBlocked = true;
            ResolveImpact();
        }
    }

    private void ResolveImpact()
    {
        Vector3 playerPos = _player.transform.position;
        Vector3 enemyPos = _enemy.transform.position;
        Vector3 normal = playerPos - enemyPos;
        if (normal.sqrMagnitude < 0.0001f)
            normal = Vector3.forward;
        normal.y = 0f;
        normal.Normalize();

        float playerAttack = _playerStats?.AttackPower ?? 5f;
        float enemyAttack = _enemy.AttackPower;

        int damageToPlayer = Mathf.CeilToInt(enemyAttack);
        int damageToEnemy = Mathf.CeilToInt(playerAttack);

        Debug.Log($"BattleManager: 충돌! 플레이어 -{damageToPlayer} / 적 -{damageToEnemy}");

        _player.TakeDamage(damageToPlayer);
        _enemy.ApplyDamage(damageToEnemy);

        SpawnImpactEffect(Vector3.Lerp(playerPos, enemyPos, 0.5f));
        _cameraController?.Shake(shakeIntensity, shakeDuration);

        ApplyKnockback(normal, playerAttack, enemyAttack);

        if (!_player.IsAlive)
        {
            Debug.Log("BattleManager: 플레이어 사망");
            ApplyDeathKnockback(_player.transform, normal, true);
            EndBattle();
            return;
        }

        if (!_enemy.IsAlive)
        {
            Debug.Log("BattleManager: 적 사망");
            ApplyDeathKnockback(_enemy.transform, normal, false);
            EndBattle();
        }
    }

    private void ApplyKnockback(Vector3 normal, float playerAttack, float enemyAttack)
    {
        float playerPush = knockbackDistance + Mathf.Abs(enemyAttack - playerAttack) * 0.1f;
        float enemyPush = knockbackDistance + Mathf.Abs(playerAttack - enemyAttack) * 0.1f;

        Vector3 playerShift = normal * playerPush;
        Vector3 enemyShift = -normal * enemyPush;

        playerShift.y = 0f;
        enemyShift.y = 0f;

        _player.transform.position += playerShift;
        _enemy.transform.position += enemyShift;
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
        _enemy?.ResetStats();
        SetupBattleScene();
    }

    private void EndBattle()
    {
        if (!_battleActive)
            return;

        Debug.Log("BattleManager: 전투 종료");
        _battleActive = false;
        _impactBlocked = false;
        CleanupBattleScene();
    }

    private void SetupBattleScene()
    {
        if (_player == null || _enemy == null)
            return;

        _savedPlayerPosition = _player.transform.position;
        _savedPlayerRotation = _player.transform.rotation;
        _savedEnemyPosition = _enemy.transform.position;
        _savedEnemyRotation = _enemy.transform.rotation;
        _playerControllerWasEnabled = _playerController != null && _playerController.enabled;
        if (_playerController != null)
            _playerController.enabled = false;

        Vector3 arenaCenter = _savedPlayerPosition + stageCenterOffset;
        BuildStage(arenaCenter);

        Vector3 playerBattlePosition = arenaCenter + playerBattleOffset;
        playerBattlePosition.y = arenaCenter.y;
        _player.transform.position = playerBattlePosition;

        Vector3 enemyBattlePosition = arenaCenter + enemyBattleOffset;
        enemyBattlePosition.y = arenaCenter.y;
        _enemy.transform.position = enemyBattlePosition;

        if (_cameraController != null)
        {
            Vector3 camPosition = arenaCenter + cameraOffset;
            Quaternion camRotation = Quaternion.Euler(cameraEulerAngles);
            _cameraController.SetManualView(camPosition, camRotation);
        }
    }

    private void CleanupBattleScene()
    {
        if (_player != null)
        {
            _player.transform.position = _savedPlayerPosition;
            _player.transform.rotation = _savedPlayerRotation;
        }

        if (_enemy != null)
        {
            _enemy.transform.position = _savedEnemyPosition;
            _enemy.transform.rotation = _savedEnemyRotation;
        }

        if (_playerController != null)
            _playerController.enabled = _playerControllerWasEnabled;

        if (_battleStage != null)
            Destroy(_battleStage);
        _battleStage = null;

        _cameraController?.ClearManualView();
    }

    private void BuildStage(Vector3 center)
    {
        if (_battleStage != null)
            Destroy(_battleStage);

        if (stagePrefab != null)
        {
            _battleStage = Instantiate(stagePrefab, center, Quaternion.identity);
            return;
        }

        _battleStage = new GameObject("GeneratedBattleStage");
        _battleStage.transform.position = center;

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        floor.transform.SetParent(_battleStage.transform, false);
        floor.transform.localScale = stageScale;
        floor.transform.localPosition = Vector3.zero;
        floor.transform.rotation = Quaternion.identity;
        Destroy(floor.GetComponent<Collider>());

        Renderer floorRenderer = floor.GetComponent<Renderer>();
        floorRenderer.sharedMaterial = GetFloorMaterial(stageFloorColor);
    }

    private Material GetFloorMaterial(Color color)
    {
        if (_generatedFloorMaterial == null)
        {
            _generatedFloorMaterial = new Material(Shader.Find("Standard"));
            _generatedFloorMaterial.SetFloat("_Glossiness", 0f);
        }
        _generatedFloorMaterial.color = color;
        return _generatedFloorMaterial;
    }

    private void OnCoreStateChanged(CoreGameManager.GameState previous, CoreGameManager.GameState next)
    {
        if (next == CoreGameManager.GameState.Battle)
            StartBattle();
        else if (_battleActive)
            EndBattle();
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
}
