# Unity 3D Runner Game Implementation Guide

## 프로젝트 개요 (Project Overview)

이 프로젝트는 1라인 러닝 게임의 핵심 시스템을 구현한 Unity 3D 게임입니다.

**Key Features:**
- 1-Line Running Mechanics (자동 전진 이동)
- Jump & Dash System (점프 및 대시 시스템)
- Stamina Management (체력/스태미너 관리)
- Camera Shake Effects (카메라 쉐이크 효과)
- Collision & Obstacle Destruction (충돌 및 장애물 파괴)

---

## 구현된 주요 클래스 (Main Classes)

### 1. **Player.cs** (Assets/Scripts/Player/Player.cs)
플레이어의 중추적인 상태 관리 클래스입니다.

**주요 기능:**
- PlayerController 및 PlayerInventory 컴포넌트 관리
- 데미지/회복 처리
- 게임 상태 관리 (생존/사망)
- Animator와 통합

**주요 메서드:**
- `TakeDamage(int damage)` - 데미지 적용
- `Die()` - 사망 처리
- `Heal(int healAmount)` - 회복
- `RestoreStamina(int staminaAmount)` - 스태미너 회복

**사용 예시:**
```csharp
Player player = GetComponent<Player>();
player.TakeDamage(10);
player.Heal(5);
```

---

### 2. **PlayerController.cs** (Assets/Scripts/Player/PlayerController.cs)
플레이어의 이동 및 입력을 관리하는 클래스입니다.

**주요 기능:**
- 자동 전진 이동 (Z축)
- 점프 입력 처리 (짧은 터치 = 점프)
- 대시 모드 (길게 누르기 = 대시)
- 스태미너 소모 시스템
- 장애물 충돌 처리 (대시 중일 때 파괴, 일반 모드에서는 데미지)
- Animator 파라미터 전송

**주요 설정값:**
```csharp
[SerializeField] private float _forwardSpeed = 10f;  // 기본 이동 속도
[SerializeField] private float _dashSpeedMultiplier = 1.5f;   // 대시 속도 배수
[SerializeField] private float _staminaDrainPerSecond = 30f;  // 초당 스태미너 소모량
[SerializeField] private float _jumpForce = 5f;  // 점프 력
```

**입력 시스템:**
- **탭 (짧게 터치)**: 점프 수행
- **홀드 (길게 누르기)**: 대시 모드 활성화

**터치 판정 임계값:** 0.2초

**주요 메서드:**
- `Jump()` - 점프 수행
- `EnterDashMode()` - 대시 모드 진입
- `ExitDashMode()` - 대시 모드 해제
- `GetCurrentSpeed()` - 현재 이동 속도 반환

**사용 예시:**
```csharp
public bool IsDashing => _isDashing;  // 현재 대시 상태 확인
public bool IsGrounded => _isGrounded; // 지면 접촉 확인
```

---

### 3. **PlayerInventory.cs** (Assets/Scripts/Player/PlayerInventory.cs)
플레이어의 체력 및 스태미너 시스템을 관리합니다.

**주요 설정값:**
```csharp
[SerializeField] private int _maxHealth = 100;           // 최대 체력
[SerializeField] private int _maxStamina = 100;// 최대 스태미너
[SerializeField] private int _staminaDrainPerSecond = 20; // 초당 스태미너 소모량
```

**자동 회복:**
- 스태미너는 초당 최대값의 50%씩 자동 회복

**주요 메서드:**
- `Initialize()` - 초기화 (최대값으로 설정)
- `DrainStamina(int amount)` - 스태미너 소모 (반환: 성공 여부)
- `TakeDamage(int damage)` - 데미지 적용
- `Heal(int healAmount)` - 회복
- `RestoreStamina(int staminaAmount)` - 스태미너 회복
- `HasStamina(int requiredAmount)` - 충분한 스태미너 보유 여부 확인

**프로퍼티:**
- `CurrentHealth` - 현재 체력
- `CurrentStamina` - 현재 스태미너
- `MaxHealth` - 최대 체력
- `MaxStamina` - 최대 스태미너

**사용 예시:**
```csharp
PlayerInventory inventory = GetComponent<PlayerInventory>();
if (inventory.DrainStamina(30))
{
    // 스태미너 소모 성공
}
else
{
    // 스태미너 부족
}
```

---

### 4. **CameraController.cs** (Assets/Scripts/CameraController.cs)
대각선 쿼터뷰 카메라 추적을 담당하는 클래스입니다.

**주요 기능:**
- 플레이어 추적 (부드러운 Lerp 보간)
- 초기 오프셋 저장 및 유지
- 카메라 쉐이크 효과
- 동적 오프셋 설정 가능

**주요 설정값:**
```csharp
[SerializeField] private float _smoothSpeed = 5f;        // 추적 부드러움 정도
[SerializeField] private float _shakeIntensity = 0.1f;   // 쉐이크 강도
[SerializeField] private float _shakeDuration = 0.2f;    // 쉐이크 지속 시간
```

**초기 오프셋 설정:**
- 게임 시작 시 씬 뷰에서 설정한 카메라와 플레이어 사이의 거리/각도를 자동 계산

**주요 메서드:**
- `FollowPlayer()` - 플레이어를 부드럽게 추적
- `Shake(float intensity, float duration)` - 카메라 쉐이크 트리거
- `GetOffset()` - 현재 오프셋 반환
- `SetOffset(Vector3 newOffset)` - 오프셋 변경
- `SetPlayerTransform(Transform playerTransform)` - 추적 대상 설정

**사용 예시:**
```csharp
CameraController camera = GetComponent<CameraController>();
camera.Shake(0.2f, 0.3f); // 강도 0.2, 지속시간 0.3초의 쉐이크

// 오프셋 동적 변경
camera.SetOffset(new Vector3(5, 3, -8));
```

---

## 게임 플로우 (Game Flow)

### 1. **초기화 (Initialization)**
```
Start()
├─ Player.Awake()
│  ├─ PlayerController 캐시
│  ├─ PlayerInventory 캐시
│  └─ Animator 캐시
├─ Player.Start()
│└─ PlayerInventory.Initialize()
└─ CameraController.Start()
   └─ 플레이어 오프셋 계산
```

### 2. **게임 루프 (Game Loop)**
```
Update()
├─ PlayerController.Update()
│  ├─ HandleInput() - 터치/마우스 입력 감지
│  └─ UpdateAnimatorParameters() - 애니메이터 상태 업데이트
├─ PlayerInventory.Update()
│  └─ 스태미너 자동 회복
└─ CameraController.LateUpdate()
   ├─ FollowPlayer() - 플레이어 추적
   └─ ApplyCameraShake() - 쉐이크 적용

FixedUpdate()
└─ PlayerController.FixedUpdate()
   ├─ CheckGroundCollision() - 지면 감지
├─ ApplyForwardMovement() - 전진 이동
   └─ ApplyDrag() - 드래그 적용
```

### 3. **충돌 처리 (Collision Handling)**
```
대시 중:
└─ 장애물 충돌
   └─ Destroy(obstacle) - 장애물 파괴

일반 모드:
└─ 장애물 충돌
   └─ Player.TakeDamage(10)
```

---
                                   
## 설정 및 구성 (Configuration)

### Unity Inspector에서 설정할 값:

**Player (GameObject)**
- Player (Script)
  - 자동 할당 (GetComponent)
- PlayerController (Script)
  - Forward Speed: 10
  - Dash Speed Multiplier: 1.5
  - Stamina Drain Per Second: 30
  - Jump Force: 5
  - Ground Layer: Default (또는 Ground)
  - Ground Check Distance: 0.1
- PlayerInventory (Script)
  - Max Health: 100
  - Max Stamina: 100
  - Stamina Drain Per Second: 20
- Animator
  - 런타임 애니메이션 조작용 (자동 설정)

**Camera (GameObject)**
- CameraController (Script)
  - Player Transform: Player 게임객체의 Transform
  - Smooth Speed: 5
  - Shake Intensity: 0.1
  - Shake Duration: 0.2

---

## 애니메이션 파라미터 (Animator Parameters)

| 파라미터 | 타입 | 설명 |
|---------|------|------|
| `IsRunning` | Bool | 달리는 상태 |
| `IsJumping` | Bool | 점프 중인 상태 |
| `IsDashing` | Bool | 대시 중인 상태 |

**설정 방법:**
```csharp
_animator.SetBool("IsRunning", true);
_animator.SetBool("IsJumping", jumpState);
_animator.SetBool("IsDashing", dashState);
```

---

## 물리 설정 (Physics Setup)

### Rigidbody 설정 (Player)
- Mass: 1
- Drag: 0.3
- Angular Drag: 0.05
- Freeze Rotation: X, Y, Z 모두 체크
- Use Gravity: 체크됨

### Collider 설정
- CapsuleCollider (Player) - 기본 설정
- BoxCollider (Obstacles) - isTrigger: True

### Layer 설정
- Player Layer: Player
- Obstacle Layer: Obstacle
- Ground Layer: Default

---

## 디버그 및 테스트 (Debugging & Testing)

### Console 로그 확인 사항:
```
[Player] Player jumped
[PlayerController] Entered dash mode
[PlayerController] Exited dash mode
[PlayerInventory] Took 10 damage. Current health: 90
[CameraController] Camera shake triggered
```

### 테스트 시나리오:
1. **점프 테스트**: 빠른 클릭 → 플레이어 점프
2. **대시 테스트**: 0.2초 이상 길게 누르기 → 대시 모드 활성화
3. **카메라 테스트**: 카메라가 플레이어를 부드럽게 추적
4. **장애물 테스트**: 대시 중 장애물 통과, 일반 모드에서는 데미지

---

## 확장 가능한 시스템 (Extensible Systems)

### 1. 새로운 Power-up 추가
```csharp
public class PowerUp : Interactable
{
    public override void Interact()
 {
     if (_player != null)
        {
            _player.Heal(20);
        }
        Destroy(gameObject);
    }
}
```

### 2. 보스 전투 시스템
```csharp
public class Boss : MonoBehaviour
{
    public void TakeDamage(int damage)
 {
      // 대시 중 접촉 시만 데미지 적용
        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc.IsDashing)
        {
            // 보스 데미지 로직
     }
    }
}
```

### 3. 점수 시스템
```csharp
public class ScoreManager : MonoBehaviour
{
    private int _score = 0;
    
    public void AddScore(int points)
    {
      _score += points;
        Debug.Log($"Score: {_score}");
    }
}
```

---

## 성능 최적화 팁 (Performance Tips)

1. **프레임 단위 할당 제거 (Update에서 new 키워드 사용 금지)**
   ? Vector3 캐싱
   ? Quaternion 재사용

2. **Physics 최적화**
   - Ground check를 FixedUpdate에서 수행
   - Raycast 거리 최소화
   - Layer mask 활용

3. **Animator 최적화**
   - 필요한 파라미터만 Set
   - 변경된 값만 전송

---

## 트러블슈팅 (Troubleshooting)

### 문제: 플레이어가 점프하지 않음
**해결:**
1. Ground Layer가 올바르게 설정되었는지 확인
2. Ground check distance 값 확인
3. Jump Force 값이 0 이상인지 확인

### 문제: 대시가 작동하지 않음
**해결:**
1. 스태미너 양 확인
2. Hold 시간이 0.2초 이상인지 확인
3. Stamina Drain Per Second 값 확인

### 문제: 카메라가 떨려서 움직임
**해결:**
1. Smooth Speed 값 낮추기 (권장: 3-5)
2. Frame rate 확인

---

## 코드 컨벤션 (Code Convention)

프로젝트 따르는 컨벤션:
- **클래스/메서드:** PascalCase
- **변수/파라미터:** camelCase
- **private 필드:** _camelCase (언더스코어 접두사)
- **상수:** UPPER_SNAKE_CASE
- **XML 주석:** /// 사용

---

## 라이선스 및 크레딧 (License & Credits)

이 프로젝트는 Unity 6000.2.10f1 기반으로 작성되었습니다.
C# 9.0, .NET Framework 4.7.1 환경에서 테스트됨.

---

**마지막 업데이트:** 2025-12-14
**버전:** 1.0
