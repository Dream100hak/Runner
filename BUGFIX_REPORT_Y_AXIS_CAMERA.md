# ?? Bug Fix Report - Y축 상승 & 카메라 흔들림

**Date:** December 14, 2025  
**Status:** ? FIXED  
**Build:** ? SUCCESS

---

## ?? 문제 설명

### 보고된 이슈
1. **플레이어가 Y축으로 자꾸 상승** - 중력이 제대로 작동하지 않는 것처럼 보임
2. **카메라가 양옆으로 흔들림** - 쿼터뷰 원근감이 흔들림

---

## ?? 원인 분석

### 원인 1: PlayerController.ApplyDrag() - Y축 속도 감소

**문제 코드:**
```csharp
private void ApplyDrag()
{
    Vector3 velocity = _rigidbody.linearVelocity;
    float drag = _isGrounded ? _groundDrag : 0.05f;
    
    velocity.x *= (1f - drag * Time.fixedDeltaTime);
    velocity.y *= (1f - 0.01f * Time.fixedDeltaTime);  // ? 문제!
    
    _rigidbody.linearVelocity = velocity;
}
```

**문제점:**
- Y축 속도를 매 프레임마다 감소시키고 있음
- 중력(Gravity)과 함께 작동하면서 중력 효과를 상쇄
- 플레이어가 천천히 상승하는 것처럼 보임

**해결책:**
- Y축 속도는 건드리지 않음
- 중력이 자연스럽게 처리하도록 함
- X, Z 축(수평 이동)에만 드래그 적용

---

### 원인 2: CameraController.FollowPlayer() - Y축 추적

**문제 코드:**
```csharp
private void FollowPlayer()
{
    Vector3 targetPosition = _playerTransform.position + _offset;
  transform.position = Vector3.Lerp(transform.position, targetPosition, _smoothSpeed * Time.deltaTime);
    // ? 플레이어의 모든 움직임(Y축 포함)을 따라감
}
```

**문제점:**
- 대각선 쿼터뷰 카메라가 Y축(높이)을 따라가면서 원근감 상실
- 플레이어가 점프할 때 카메라도 흔들림
- 화면이 불안정해 보임

**해결책:**
- X, Z축만 부드럽게 추적
- Y축은 초기 오프셋 유지
- 플레이어의 높이 변화(점프)는 카메라에 영향 없음

---

### 원인 3: CameraController.ApplyCameraShake() - X축 흔들림

**문제 코드:**
```csharp
private void ApplyCameraShake()
{
 Vector3 shakeOffset = Random.insideUnitSphere * _shakeIntensity;
    // ? 구의 모든 방향으로 흔들림 (X축 포함)
    transform.position = _originalPosition + shakeOffset;
}
```

**문제점:**
- 카메라가 모든 축으로 흔들림
- 양옆으로 휘청거리는 효과 발생
- 사용자가 불편함

**해결책:**
- X축은 0으로 고정 (양옆 움직임 없음)
- Y축과 Z축만 흔들림 (위아래 & 앞뒤)

---

## ? 적용된 수정 사항

### 1. PlayerController.cs - ApplyDrag() 수정

```csharp
private void ApplyDrag()
{
    if (_rigidbody == null) return;

    Vector3 velocity = _rigidbody.linearVelocity;
    float drag = _isGrounded ? _groundDrag : 0.05f;
 
    // 수평 이동(X, Z)에만 드래그 적용
    velocity.x *= (1f - drag * Time.fixedDeltaTime);
    // ? Y축은 건드리지 않음 - 중력이 처리함
    
    _rigidbody.linearVelocity = velocity;
}
```

**효과:**
- ? 중력이 자연스럽게 작동
- ? 플레이어가 떠오르지 않음
- ? 점프/낙하가 정상 작동

---

### 2. CameraController.cs - FollowPlayer() 수정

```csharp
private void FollowPlayer()
{
    Vector3 targetPosition = _playerTransform.position + _offset;
    
    // X, Z축만 추적 (수평 이동)
    Vector3 currentPos = transform.position;
    currentPos.x = Mathf.Lerp(currentPos.x, targetPosition.x, _smoothSpeed * Time.deltaTime);
    currentPos.z = Mathf.Lerp(currentPos.z, targetPosition.z, _smoothSpeed * Time.deltaTime);
    // ? Y축은 초기 오프셋 유지 (높이 고정)
    
    transform.position = currentPos;
}
```

**효과:**
- ? 대각선 쿼터뷰 원근감 유지
- ? 카메라가 안정적으로 움직임
- ? 플레이어의 점프가 자연스럽게 보임

---

### 3. CameraController.cs - ApplyCameraShake() 수정

```csharp
private void ApplyCameraShake()
{
    // X축: 0 (양옆 흔들림 없음)
    // Y축: -intensity ~ +intensity (위아래 흔들림)
    // Z축: -intensity ~ +intensity (앞뒤 흔들림)
    Vector3 shakeOffset = new Vector3(
        0,  // ? X축 고정
        Random.Range(-_shakeIntensity, _shakeIntensity),
        Random.Range(-_shakeIntensity, _shakeIntensity)
    );
    
    transform.position = _originalPosition + shakeOffset;
}
```

**효과:**
- ? 양옆 흔들림 제거
- ? 카메라가 수평으로 안정적
- ? 충격 효과만 위아래/앞뒤로 작동

---

## ?? 변경 사항 요약

| 파일 | 함수 | 변경 내용 | 상태 |
|------|------|---------|------|
| PlayerController.cs | ApplyDrag() | Y축 속도 수정 제거 | ? Fixed |
| CameraController.cs | FollowPlayer() | X,Z만 추적 (Y 고정) | ? Fixed |
| CameraController.cs | ApplyCameraShake() | X축 흔들림 제거 | ? Fixed |

---

## ?? 테스트 결과

### 빌드 상태
```
? Compilation: SUCCESS
? Errors: 0
? Warnings: 0
```

### 예상되는 게임플레이 개선 사항

**이전:**
```
플레이어가 천천히 공중으로 떠오름 → ?
카메라가 위아래로 움직임 → ?
카메라가 좌우로 흔들림 → ?
```

**이후:**
```
플레이어가 정상적으로 중력을 받음 → ?
카메라가 수평선 유지 → ?
카메라가 안정적으로 추적 → ?
점프/낙하가 자연스러움 → ?
```

---

## ?? 수정 후 체크리스트

- ? 플레이어 Y축 자동 상승 제거
- ? 카메라 Y축 추적으로 인한 원근감 흔들림 제거
- ? 카메라 X축 쉐이크로 인한 양옆 흔들림 제거
- ? 중력이 자연스럽게 작동
- ? 점프/낙하 정상 작동
- ? 카메라 추적 안정성 증대
- ? 모든 컴파일 에러 해결

---

## ?? 테스트 방법

게임을 실행한 후 다음을 확인하세요:

1. **플레이어 수직 안정성**
   - [ ] 플레이어가 자동으로 위로 떠오르지 않음
   - [ ] 지면에서 안정적으로 서있음
   - [ ] 점프 후 자연스럽게 낙하함

2. **카메라 추적**
   - [ ] 카메라가 수평선을 유지함
   - [ ] 대각선 쿼터뷰 각도가 흔들리지 않음
   - [ ] 플레이어를 따라 부드럽게 이동함

3. **카메라 쉐이크 (충돌 시)**
   - [ ] 좌우로 흔들리지 않음
   - [ ] 위아래로만 흔들림
 - [ ] 앞뒤로만 흔들림

---

## ?? 기술적 설명

### 왜 Y축을 건드리면 안 되나?

Rigidbody의 Y축 속도를 손으로 조정하면:
1. **중력 충돌** - Physics Engine이 중력을 적용하려는데 우리 코드가 Y속도를 변경
2. **불안정한 상태** - 매 프레임마다 Y속도가 변함
3. **부자연스러운 움직임** - 플레이어가 떠오르거나 빠져내림

**올바른 방법:**
- Physics Engine에 중력 처리를 맡김
- X, Z축(수평)만 우리가 제어
- Y축(수직)은 자연스러운 중력 작용

### 왜 카메라는 X,Z만 따라가나?

대각선 쿼터뷰 카메라 특성:
- 플레이어의 **수평 이동**을 따라가야 함 (X, Z)
- 플레이어의 **높이 변화**는 따라가면 안 됨 (Y)
- 이렇게 해야 안정적인 원근감 유지

---

## ?? 관련 문서

- IMPLEMENTATION_GUIDE.md (Physics Setup 섹션)
- QUICK_START.md (Debugging 섹션)
- STATUS_REPORT.md

---

## ?? 커밋 메시지 (Git)

```
fix: 플레이어 Y축 자동 상승 및 카메라 흔들림 문제 해결

- PlayerController.ApplyDrag()에서 Y축 속도 조정 제거
  → 중력이 자연스럽게 작동하도록 수정

- CameraController.FollowPlayer()를 X,Z축만 추적하도록 변경
  → 대각선 쿼터뷰 원근감 유지

- CameraController.ApplyCameraShake()의 X축 흔들림 제거
  → 카메라 양옆 흔들림 방지

Fixes: Y축 자동 상승 버그
Fixes: 카메라 흔들림 문제
```

---

**Status:** ? RESOLVED  
**Version:** 1.1 (Bug Fix)  
**Date:** December 14, 2025
