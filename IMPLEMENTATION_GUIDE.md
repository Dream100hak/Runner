# Unity 3D Runner Game - Implementation Summary

## Overview

This project implements a complete 1-lane infinite runner game system with the following core mechanics:

- ? **1-Lane Running** - Automatic forward movement along Z-axis
- ? **Jump & Dash System** - Tap to jump, hold to dash
- ? **Stamina Management** - Health and stamina tracking with automatic regeneration
- ? **Camera Shake Effects** - Impact feedback for collisions
- ? **Obstacle Destruction** - Dash through obstacles with super armor

---

## Architecture Overview

### Class Hierarchy
```
MonoBehaviour
戍式 Player    (Player.cs)
弛  戌式 Manages: PlayerController, PlayerInventory, Animator
戍式 PlayerController       (PlayerController.cs)
弛  戌式 Handles: Movement, Input, Collision, Animation
戍式 PlayerInventory           (PlayerInventory.cs)
弛  戌式 Manages: Health, Stamina
戍式 CameraController          (CameraController.cs)
弛  戌式 Handles: Tracking, Shake, Offset
戌式 Interactable    (Interactable.cs)
   戌式 Base class for interactive objects
```

---

## Detailed API Reference

### Player.cs

**Public Methods:**
```csharp
// Damage & Healing
void TakeDamage(int damage)   // Apply damage to player
void Heal(int healAmount)      // Restore health
void RestoreStamina(int staminaAmount) // Restore stamina

// State Queries
bool IsAlive          // Check if player is alive
PlayerInventory GetInventory()         // Get inventory reference
```

**Events/Callbacks:**
```csharp
// Internal: Calls from damage
Die()       // Player death handler
```

---

### PlayerController.cs

**Configuration:**
```csharp
_forwardSpeed = 10f        // Base movement speed (units/sec)
_dashSpeedMultiplier = 1.5f      // Speed boost while dashing
_jumpForce = 5f       // Jump impulse (physics units)
_staminaDrainPerSecond = 30f    // Stamina cost per second of dash
_groundCheckDistance = 0.1f   // Ground detection raycast distance
```

**Input System:**
- **Short Tap** (<0.2s): Jump
- **Long Hold** (>0.2s): Dash Mode

**Public Methods:**
```csharp
// Status Queries
bool IsDashing         // Currently dashing
bool IsGrounded       // Touching ground
float GetCurrentSpeed()  // Actual movement speed
```

**Collision Handling:**
```
OnTriggerEnter(Collider):
戍式 If IsDashing: Destroy obstacle
戌式 If Not Dashing: TakeDamage(10)
```

---

### PlayerInventory.cs

**Stat System:**
```
Health:     Current (0-max) tracked separately
Stamina:    Drains on dash, regenerates at 50% max per second
```

**Public Methods:**
```csharp
void Initialize()                // Reset to max values
bool DrainStamina(int amount)      // Try to drain stamina
void TakeDamage(int damage)  // Apply damage
void Heal(int healAmount)             // Restore health
void RestoreStamina(int staminaAmount) // Add stamina
bool HasStamina(int requiredAmount)   // Query check

// Properties
int CurrentHealth { get; }
int CurrentStamina { get; }
int MaxHealth { get; }
int MaxStamina { get; }
```

---

### CameraController.cs

**Configuration:**
```csharp
_smoothSpeed = 5f           // Interpolation speed (higher = faster tracking)
_shakeIntensity = 0.1f     // Shake displacement amount
_shakeDuration = 0.2f      // Shake effect duration (seconds)
```

**Offset System:**
- Automatically calculated on Start from scene editor position
- Maintains diagonal quarter-view perspective
- Can be dynamically adjusted

**Public Methods:**
```csharp
void Shake(float intensity = -1f, float duration = -1f)  // Trigger shake
Vector3 GetOffset()     // Get current offset
void SetOffset(Vector3 newOffset)           // Change offset
void SetPlayerTransform(Transform player)   // Set tracking target
```

---

## Game Flow

### Initialization Phase
```
Start() -> Initialize Components -> Calculate Camera Offset
```

### Main Loop (Per Frame)
```
Update():
  戍式 ProcessInput()
  戍式 UpdateAnimatorState()
  戌式 CameraTracking()

FixedUpdate():
  戍式 CheckGroundCollision()
  戍式 ApplyMovement()
  戌式 ApplyPhysics()
```

### State Transitions
```
Normal Mode
戍式 Short Tap ⊥ Jump State
戌式 Long Hold ⊥ Dash State

Dash Mode
戍式 Stamina Depletion ⊥ Exit Dash
戌式 Release Button ⊥ Exit Dash

Jump State
戌式 Ground Contact ⊥ Normal Mode
```

---

## Animator Parameters

Required Animator State Machine Setup:

| Parameter | Type | Purpose |
|-----------|------|---------|
| IsRunning | Bool | Base running animation |
| IsJumping | Bool | Jump animation |
| IsDashing | Bool | Dash animation |

**Animation Blend Tree Example:**
```
Base Layer
戍式 Idle (IsRunning = false)
戍式 Running (IsRunning = true)
弛  戍式 Jump (IsJumping = true)
弛  戌式 Dash (IsDashing = true)
```

---

## Physics Setup

### Required Components

**Player GameObject:**
- Rigidbody:
  - Body Type: Dynamic
  - Mass: 1
  - Drag: 0.3
  - Angular Drag: 0.05
  - Constraints: Freeze Rotation (X, Y, Z)
  - Gravity: Enabled

- CapsuleCollider:
  - Height: ~2 units
  - Radius: ~0.4 units
  - Is Trigger: False

**Obstacles:**
- BoxCollider:
  - Is Trigger: True
  - Tag: "Obstacle"

**Ground:**
- Collider (BoxCollider/CapsuleCollider)
- Layer: "Ground"

---

## Input Handling

### Touch Input Simulation (Editor/Standalone)
```csharp
if (Input.GetMouseButtonDown(0))
  _touchStartTime = Time.time

if (Input.GetMouseButton(0))
  if (Time.time - _touchStartTime > 0.2s)
    EnterDashMode()

if (Input.GetMouseButtonUp(0))
  if (Time.time - _touchStartTime <= 0.2s)
    Jump()
```

### Mobile Input (Production)
```csharp
// Requires: Touch input layer implementation
// Same logic applied to Touch.Touches
```

---

## Performance Characteristics

### Frame Allocations
- ? No per-frame allocations in Update/LateUpdate
- ? Raycast pooling recommended for ground check
- ? Vector3 values cached and reused

### Draw Calls
- Based on obstacle density
- Camera shake uses Transform position only (no mesh changes)

### Physics Updates
- FixedUpdate at 50 Hz (0.02s timestep)
- Raycasts optimized with layer masks

---

## Extension Points

### Adding New Obstacles
```csharp
public class Obstacle : MonoBehaviour
{
    // Automatically handled by PlayerController.OnTriggerEnter
// Tag must be "Obstacle" for detection
}
```

### Adding Power-ups
```csharp
public class PowerUp : Interactable
{
    public override void Interact()
    {
        if (_player != null)
        {
     _player.Heal(50);
        }
        Destroy(gameObject);
    }
}
```

### Custom Stamina Drain
```csharp
// Override in PlayerController
private void DrainStamina()
{
    int amount = (int)(_staminaDrainPerSecond * Time.deltaTime);
    if (!_playerInventory.DrainStamina(amount))
        ExitDashMode();
}
```

---

## Known Limitations & TODOs

### Current Version (v1.0)
- [x] Forward movement
- [x] Jump mechanic
- [x] Dash mechanic with stamina
- [x] Camera tracking with shake
- [x] Obstacle collision
- [ ] Level progression
- [ ] Score system
- [ ] UI indicators (health bar, stamina bar)
- [ ] Sound effects
- [ ] Particle effects

### Potential Improvements
- Implement mobile touch input layer
- Add ObjectPool for obstacles
- Implement difficulty scaling
- Add procedural level generation
- Optimize physics with continuous collision detection

---

## Testing Checklist

- [ ] Player spawns at origin
- [ ] Camera positioned correctly with offset
- [ ] Forward movement works continuously
- [ ] Short tap triggers jump
- [ ] Long hold triggers dash
- [ ] Stamina drains during dash
- [ ] Dash prevents damage (obstacle destruction)
- [ ] Regular collision applies damage
- [ ] Camera shake triggers on impact
- [ ] Health system tracks damage
- [ ] Stamina regenerates when idle

---

## Code Standards Compliance

? **Naming Conventions:**
- Classes: PascalCase
- Methods: PascalCase
- Fields: _camelCase (private)
- Constants: UPPER_SNAKE_CASE
- Parameters: camelCase

? **Documentation:**
- XML comments on public members
- Method descriptions with parameters
- Code comments for complex logic

? **Best Practices:**
- Null checks on GetComponent calls
- Debug.Log for state tracking
- SerializeField for configuration
- Properties for read-only access

---

## Build Information

- **Target Framework:** .NET Framework 4.7.1
- **C# Version:** 9.0
- **Unity Version:** 6000.2.10f1
- **API Compatibility:** .NET Standard 2.1

---

## File Structure

```
Assets/
戍式 Scripts/
弛  戍式 Player/
弛  弛  戍式 Player.cs
弛  弛  戍式 PlayerController.cs
弛  弛  戌式 PlayerInventory.cs
弛  戍式 CameraController.cs
弛  戌式 Interactable/
弛   戌式 Interactable.cs
戍式 Resources/
弛  戍式 UI_Victory.uxml
弛  戌式 UI_Victory.uss
戌式 Scenes/
   戌式 Main.unity
```

---

**Last Updated:** December 14, 2025
**Version:** 1.0
**Status:** ? Build Successful
