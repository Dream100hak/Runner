# Quick Start Guide - 1-Line Runner Game

## ?? Project Setup

### Required Components on Player GameObject
```
Transform
戍式 Rigidbody (Dynamic, with frozen rotation)
戍式 CapsuleCollider 
戍式 Player (Script)
戍式 PlayerController (Script)
戍式 PlayerInventory (Script)
戌式 Animator (with state machine)
```

### Required Components on Camera
```
Camera
戌式 CameraController (Script)
   戌式 Player Transform: [Assign Player]
```

---

## ?? Essential Configuration

### PlayerController Settings
| Setting | Value | Notes |
|---------|-------|-------|
| Forward Speed | 10 | Units per second |
| Dash Speed Multiplier | 1.5 | Speed boost while dashing |
| Jump Force | 5 | Impulse force |
| Stamina Drain Per Second | 30 | Stamina cost per dash second |
| Ground Check Distance | 0.1 | Raycast distance |
| Ground Layer | "Ground" | Physical ground layer |

### PlayerInventory Settings
| Setting | Value | Notes |
|---------|-------|-------|
| Max Health | 100 | Total health points |
| Max Stamina | 100 | Total stamina points |
| Stamina Drain | 20 | Per-second drain rate |

### CameraController Settings
| Setting | Value | Notes |
|---------|-------|-------|
| Smooth Speed | 5 | Higher = faster tracking |
| Shake Intensity | 0.1 | Displacement on shake |
| Shake Duration | 0.2 | Seconds per shake |
| Player Transform | Player | Reference to player |

---

## ?? Input Guide

### Touch Controls
```
Quick Tap (< 0.2 seconds)
戌式 Jump (if grounded)

Long Press (> 0.2 seconds)
戍式 Enter dash mode
戍式 Drains stamina
戍式 Destroys obstacles
戌式 Super armor effect
```

### Current Implementation
- Mouse = Touch simulation (Editor only)
- Left Click = Touch
- Release = Release

---

## ?? Game States

### Player States
```
忙式式式式式式式式式式式式式忖
弛   Normal    弛
戍式式式式式式成式式式式式式扣
弛      弛      弛
∪      ∪      ∪
Jump  Dash   Idle
  弛  弛   弛
  戌式式式式托式式式式式戎
       弛
  忙式式式式∪式式式式忖
  弛  Damage  弛
  戌式式式式式式式式式戎
```

### Obstacle Interactions
```
Normal Mode:
  Obstacle Collision ⊥ Take Damage (10 HP)

Dash Mode:
  Obstacle Collision ⊥ Destroy Obstacle
  ⊥ No Damage
        ⊥ Continue Moving
```

---

## ?? Common Tasks

### Enable/Disable Player
```csharp
Player player = GetComponent<Player>();
player.enabled = true;   // Enable
player.enabled = false;  // Disable (on death)
```

### Trigger Camera Shake
```csharp
CameraController camera = FindObjectOfType<CameraController>();
camera.Shake(intensity: 0.2f, duration: 0.3f);
```

### Apply Damage
```csharp
Player player = GetComponent<Player>();
player.TakeDamage(10);  // Deal 10 damage
```

### Check Player Status
```csharp
PlayerInventory inventory = GetComponent<PlayerInventory>();

// Health checks
if (inventory.CurrentHealth <= 0) { /* Dead */ }

// Stamina checks
if (inventory.HasStamina(20)) { /* Can perform action */ }
```

### Query Movement State
```csharp
PlayerController pc = GetComponent<PlayerController>();

if (pc.IsDashing) { /* In dash mode */ }
if (pc.IsGrounded) { /* On ground */ }

float speed = pc.GetCurrentSpeed();  // Get actual speed
```

---

## ?? Debugging

### Enable Debug Logs
All classes use `Debug.Log()` for state tracking:
```
PlayerController: Player jumped
PlayerController: Entered dash mode
PlayerController: Exited dash mode
PlayerInventory: Took 10 damage. Current health: 90
CameraController: Camera shake triggered
```

### Common Issues

**Issue:** Player doesn't jump
- Check Ground Layer assignment
- Verify Ground Check Distance > 0
- Confirm Jump Force > 0

**Issue:** Dash doesn't work
- Check stamina is not zero
- Verify hold duration > 0.2 seconds
- Check Stamina Drain setting

**Issue:** Camera jittery
- Lower Smooth Speed (try 3-5)
- Check frame rate

**Issue:** Obstacles pass through
- Verify BoxCollider is Trigger = True
- Check Tag = "Obstacle"

---

## ?? Performance Tips

1. **Optimize Ground Check**
   - Use Layer Mask
   - Minimize raycast distance
   - Only check in FixedUpdate

2. **Minimize Animator Calls**
   ```csharp
   // BAD: Every frame
   _animator.SetBool("IsRunning", true);
   
   // GOOD: Only when needed
   if (previousState != currentState)
   _animator.SetBool("IsRunning", true);
   ```

3. **Cache Component References**
   ```csharp
// Avoid repeated GetComponent calls
   private Rigidbody _rigidbody;
   
   private void Awake()
   {
   _rigidbody = GetComponent<Rigidbody>();
   }
   ```

---

## ?? Animation Setup

### Required Animator States
1. **Idle** - Standing still
2. **Running** - Normal forward movement
3. **Jump** - Jumping animation
4. **Dash** - Dashing animation

### Animator Transitions
```
Any State ⊥ Jump (IsJumping = true)
Any State ⊥ Dash (IsDashing = true)
Any State ∠ Running (IsRunning = true)
```

### Parameter Updates
```csharp
// In UpdateAnimatorParameters()
_animator.SetBool("IsRunning", true);      // Always true
_animator.SetBool("IsJumping", !_isGrounded && _isJumping);
_animator.SetBool("IsDashing", _isDashing);
```

---

## ?? Script Reference Quick Links

| Script | Path | Purpose |
|--------|------|---------|
| Player | Assets/Scripts/Player/Player.cs | Main player controller |
| PlayerController | Assets/Scripts/Player/PlayerController.cs | Movement & input |
| PlayerInventory | Assets/Scripts/Player/PlayerInventory.cs | Health & stamina |
| CameraController | Assets/Scripts/CameraController.cs | Camera tracking |
| Interactable | Assets/Scripts/Interactable/Interactable.cs | Interactive objects |

---

## ? Checklist Before Shipping

- [ ] All scripts compile without errors
- [ ] Player spawns at correct position
- [ ] Camera follows with correct offset
- [ ] Jump works consistently
- [ ] Dash uses stamina
- [ ] Obstacles are destroyed during dash
- [ ] Collision damage applies correctly
- [ ] Health system tracks properly
- [ ] Stamina regenerates when idle
- [ ] Camera shake triggers on impact
- [ ] Animator states transition smoothly
- [ ] Physics is stable (no jittering)
- [ ] No memory leaks in Update/LateUpdate
- [ ] Performance acceptable on target hardware

---

## ?? Support Notes

**Compile Issues?**
- Clean solution (delete Library/Temp folders)
- Regenerate project files
- Restart Unity

**Build Errors?**
- Check Assembly-CSharp.csproj includes all files
- Verify .meta files exist for all scripts
- Check C# version compatibility

**Runtime Crashes?**
- Enable Script Debugging
- Check null reference logs
- Verify all references assigned in Inspector

---

**Version:** 1.0  
**Last Updated:** December 14, 2025  
**Status:** ? Ready for Development
