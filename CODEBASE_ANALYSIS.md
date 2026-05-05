# Catch the Momo - Comprehensive Codebase Analysis

## Executive Summary

This is a Unity 2D arcade game where players catch falling "momos" (good objects) while dodging "guff" (bad objects). The codebase has solid core architecture with object pooling and physics-based gameplay, but contains **two critical issues**: uncleanup button event listeners and Time.timeScale not being restored after pause.

---

## 1. ALL RELEVANT SCRIPTS

### Core Scripts Location: `Assets/Scripts/`

| Script | Purpose | Key Responsibilities |
|--------|---------|----------------------|
| **GameManager.cs** | Game controller & spawning | Spawn management, object pooling, score/lives tracking, difficulty scaling |
| **PlayerMovement.cs** | Player movement controller | Smooth touch-based movement, blink effect, position bounds |
| **PlayerController.cs** | UI bridge | Life display updates, communicates with GameManager |
| **MomoController.cs** | Good object controller | Collision detection (player/ground), score +1, visual feedback |
| **GuffController.cs** | Bad object controller | Collision detection (player/ground), lose life, invincibility check |
| **UiManager.cs** | Menu UI management | Play/Quit button handlers, scene loading, donation box |
| **FinalTouch.cs** | Polish & feedback | Screen shake, camera zoom, sound effects, singleton pattern |

**Total: 7 scripts**

---

## 2. SCENE LOADING & UNLOADING

### Scene Structure
```
Assets/Scenes/
├── Menu.unity        (Main menu - Play/Quit buttons)
└── Game.unity        (Gameplay - spawning, player, physics)
```

### Scene Flow Diagram

```
START
  ↓
Menu.unity (UiManager attached)
  ├─ Play Button → SceneManager.LoadScene("Game")
  └─ Quit Button → Application.Quit()
  
  ↓ (after Play clicked)
  
Game.unity (GameManager, PlayerMovement, etc.)
  ├─ Spawn momos/guff continuously
  ├─ Player catches momos (+1 score)
  ├─ Player hits guff (-1 life)
  └─ When lives = 0:
       EndGame() → GameOverSequence()
  
  ↓ (after death)
  
GameOverSequence Coroutine:
  1. FinalTouch.OnGameOver() (death shake)
  2. GameManager disabled
  3. Wait 0.3s (death animation)
  4. Time.timeScale = 0f (PAUSE)
  5. Wait 0.5s real time
  6. SceneManager.LoadScene(gameOverScene)
       [gameOverScene = "Menu" (serialized field)]
  
  ↓
Back to Menu.unity (BUT TIME.TIMESCALE STILL 0!)
```

### Critical Issue: Time.timeScale Not Reset

**Location**: [GameManager.cs](GameManager.cs#L283)

When the game ends, `Time.timeScale` is set to 0 but **never restored to 1** before loading the menu. This means:
- ❌ Menu UI buttons won't respond properly
- ❌ Camera movement stutters
- ❌ Any time-based animations freeze

**Workaround Currently In Place**: UiManager likely has logic to handle this, but it should be explicit.

---

## 3. PLAYER INPUT & SENSITIVITY CONFIGURATION

### Movement Control System

**File**: [PlayerMovement.cs](PlayerMovement.cs)

```csharp
[SerializeField] private float smoothTime;  // KEY SETTING
```

#### How It Works:
1. **Input Detection** (line 39):
   ```csharp
   if (Input.GetMouseButton(0))
   {
       Vector3 touchPosition = mainCam.ScreenToWorldPoint(Input.mousePosition);
       targetX = Mathf.Clamp(touchPosition.x, minX, maxX);
   }
   ```

2. **Smooth Movement** (line 44):
   ```csharp
   float newX = Mathf.SmoothDamp(transform.position.x, targetX, ref velocityX, smoothTime);
   ```

#### Sensitivity Configuration:
- **`smoothTime`**: Controls responsiveness
  - Lower value (e.g., 0.1) = Snappier, more responsive
  - Higher value (e.g., 0.5) = Smoother, more floaty
  - **Recommended for mobile**: 0.1-0.2 for arcade feel

#### Screen Bounds:
- Dynamically calculated in `CalculateScreenBounds()` (line 51)
- Uses camera viewport to world space conversion
- Accounts for sprite width to prevent off-screen movement

#### Hit Feedback:
- `Blink()` method (line 64): Creates invincibility effect when hit
  - blinkDuration: How long blink lasts
  - blinkSpeed: How fast it blinks

---

## 4. OBJECT POOLING, RESOURCE CLEANUP & EVENT ISSUES

### Object Pooling Implementation (✅ Good)

**File**: [GameManager.cs](GameManager.cs#L80-L115)

```csharp
private Dictionary<GameObject, Queue<GameObject>> objectPools = new();
```

**How It Works:**
1. `InitializeObjectPools()` - Creates empty queues for each prefab type
2. `GetPooledObject()` - Reuses from queue or creates new
3. `ReturnToPool()` - Deactivates and requeues object
4. `OnObjectRemoved()` - Returns caught/destroyed objects to pool

**Benefits:**
- ✅ No Instantiate/Destroy calls during gameplay
- ✅ Smooth performance with fixed object pool
- ✅ activeObjects list cleaned each frame (line 80)

### Active Objects Management (✅ Good)

```csharp
activeObjects.RemoveAll(obj => obj == null || !obj.activeSelf);
```

Cleans destroyed/deactivated objects from tracking list every frame.

### Event Subscription Issues (❌ CRITICAL MEMORY LEAK)

**File**: [UiManager.cs](UiManager.cs#L18-L19)

```csharp
private void Start()
{
    audioSource = GetComponent<AudioSource>();
    playButton.onClick.AddListener(OnPlayButtonPressed);      // ⚠️ LEAK!
    quitButton.onClick.AddListener(OnQuitButtonPressed);      // ⚠️ LEAK!
}
```

**The Problem:**
- Listeners are **added but NEVER removed**
- When Menu → Game → Menu, new listeners accumulate
- After 10 scene transitions: **10 listeners per button!**
- Callbacks execute multiple times, causing:
  - Multiple scenes loading simultaneously
  - Audio playing multiple times
  - Performance degradation

**Missing Cleanup:**
```csharp
private void OnDestroy()  // ← NOT IMPLEMENTED
{
    playButton.onClick.RemoveListener(OnPlayButtonPressed);
    quitButton.onClick.RemoveListener(OnQuitButtonPressed);
}
```

### Coroutine Management (✅ Good)

**File**: [FinalTouch.cs](FinalTouch.cs#L128-L133)

```csharp
if (currentShakeCoroutine != null)
{
    StopCoroutine(currentShakeCoroutine);
}
currentShakeCoroutine = StartCoroutine(ShakeCoroutine(magnitude, duration));
```

✅ Properly stops previous coroutines before starting new ones.

### FindFirstObjectByType Usage

Multiple scripts use `FindFirstObjectByType<>()` which is:
- ✅ Safe for singleton patterns
- ⚠️ Slightly inefficient (searches entire object tree)
- But not a memory leak

**Locations:**
- [GameManager.cs](GameManager.cs#L242) - FinalTouch reference
- [GuffController.cs](GuffController.cs#L43) - GameManager reference  
- [MomoController.cs](MomoController.cs#L29) - GameManager reference
- [PlayerController.cs](PlayerController.cs#L13) - GameManager reference

---

## 5. OVERALL GAME ARCHITECTURE & FLOW

### Architecture Diagram

```
┌─────────────────────────────────────────────────────┐
│                   UiManager (Menu)                  │
│  ├─ Play Button → Load Game Scene                   │
│  └─ Quit Button → Exit Application                  │
└─────────────────────────────────────────────────────┘
                         ↓ (SceneManager.LoadScene)
┌─────────────────────────────────────────────────────┐
│                   Game Scene                        │
│                                                     │
│  ┌──────────────────────────────────────────┐      │
│  │         GameManager (Spawner)            │      │
│  │  ├─ Spawn momos & guff w/ difficulty    │      │
│  │  ├─ Object pooling system               │      │
│  │  ├─ Track score & lives                 │      │
│  │  └─ Trigger game over at lives=0        │      │
│  └──────────────────────────────────────────┘      │
│              ↑             ↓                        │
│              |             |                        │
│   ┌──────────┴────┐    ┌───┴──────────┐           │
│   │               |    |              |            │
│  ┌─────────┐   ┌──────┐  ┌──────────────┐          │
│  │ Momo    │   │Guff  │  │ Player       │          │
│  │Objects  │   │Objects   │(Paddle)    │          │
│  ├─ Catch  │   ├─Hit→ ├─ Touch input  │          │
│  │→ +Score │   │Lost  │  ├─ SmoothDamp │          │
│  │         │   │Life  │  └─ Bounds clamp          │
│  └─────────┘   │      │  └──────────────┘          │
│                └──────┘                            │
│                                                     │
│  ┌──────────────────────────────────────────┐      │
│  │      FinalTouch (Polish Effects)         │      │
│  │  ├─ Screen shake on events               │      │
│  │  ├─ Camera zoom on catch                 │      │
│  │  ├─ Sound effects                        │      │
│  │  ├─ Camera bob during gameplay           │      │
│  │  └─ Death shake on game over             │      │
│  └──────────────────────────────────────────┘      │
│                                                     │
│  ┌──────────────────────────────────────────┐      │
│  │    PlayerController (UI Bridge)          │      │
│  │  └─ Update life display images           │      │
│  └──────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────┘
```

### Game Loop Flow

```
GAMEPLAY LOOP (Every Frame):
  1. PlayerMovement.Update() - Handle touch input
  2. GameManager.Update():
     - Check if time to spawn
     - Calculate difficulty
     - Spawn momo/guff with weighted randomness
     - Clean dead objects from active list
     - Update score UI
  3. Physics2D - Rigidbody2D falls due to gravity
  4. Collision Detection:
     - OnTriggerEnter2D in MomoController/GuffController
     - Score +1 or Life -1
  5. FinalTouch.Update() - Camera bobbing
```

### Difficulty Progression

**File**: [GameManager.cs](GameManager.cs#L66-L75)

```csharp
float progressionFactor = Mathf.Clamp01((totalSpawned * difficultyScale) / 10f);
float curvedDifficulty = difficultyCurve.Evaluate(progressionFactor);
currentSpawnRate = Mathf.Lerp(initialSpawnRate, minSpawnRate, curvedDifficulty);
```

- Spawn rate decreases from 1.5s → 0.5s
- Uses AnimationCurve for smooth scaling
- difficultyScale = 0.02 controls progression speed

---

## 6. PLAYER DEATH FLOW

### Death Sequence (Step by Step)

```
EVENT: Player collides with Guff object
  ↓
GuffController.OnTriggerEnter2D (line 47)
  ├─ Check if player is invincible (blink period)
  ├─ If NOT invincible:
  │  ├─ PlayerMovement.Blink() → Start blink coroutine
  │  │  └─ Toggle sprite visibility for 0.3s (invincibility window)
  │  ├─ PlayerMovement.ResetPosition() → Back to start
  │  └─ GameManager.LoseLife()
  │     ├─ lives-- 
  │     ├─ FinalTouch.OnLoseLife() → Screen shake
  │     ├─ PlayerController.OnLivesChanged() → Update UI
  │     └─ IF lives <= 0: GameManager.EndGame()
  └─ RemoveObject() → Return guff to pool

IF GAME OVER (lives = 0):
  ↓
GameManager.EndGame() (line 264)
  ↓
StartCoroutine(GameOverSequence()) (line 267)
  ├─ FinalTouch.OnGameOver()
  │  ├─ ScreenShake(0.5, 0.6) → Violent death shake
  │  └─ PlaySound(gameOverSound)
  │
  ├─ GameManager.enabled = false (Stop spawning)
  │
  ├─ yield return WaitForSeconds(0.3f) (Death animation)
  │
  ├─ Time.timeScale = 0f (PAUSE GAME) ⚠️
  │
  ├─ yield return WaitForSecondsRealtime(0.5f) (Real time pause)
  │
  └─ SceneManager.LoadScene(gameOverScene)
     └─ Loads Menu.unity (gameOverScene serialized field)

BACK AT MENU:
  ✅ Play again? Click Play → Load Game.unity (Fresh start)
  ✅ Quit? Click Quit → Application.Quit()
  ❌ NOTE: Time.timeScale still = 0! (BUG - See Critical Issues)
```

### Invincibility Window

After getting hit:
1. Blink effect starts (sprite toggles on/off)
2. `IsInvincible()` returns true for duration
3. Player position resets to starting point
4. During blink, subsequent guff collisions are ignored
5. After 0.3s, invincibility ends

---

## 7. CRITICAL ISSUES SUMMARY

### 🔴 ISSUE #1: Button Listener Memory Leak

**Severity**: HIGH - Accumulates with each scene transition

**Location**: [UiManager.cs](UiManager.cs#L18-L19)

**Problem**:
```csharp
private void Start()
{
    playButton.onClick.AddListener(OnPlayButtonPressed);    // Added
    quitButton.onClick.AddListener(OnQuitButtonPressed);    // Added
    // NO CLEANUP!
}
```

**Impact**:
- After 5 playthroughs: 5 listeners per button
- PlayOneShot() called 5 times
- SceneManager.LoadScene() called 5 times
- Memory leak in button delegate list

**Fix Required**:
```csharp
private void OnDestroy()
{
    if (playButton != null)
        playButton.onClick.RemoveListener(OnPlayButtonPressed);
    if (quitButton != null)
        quitButton.onClick.RemoveListener(OnQuitButtonPressed);
}
```

### 🔴 ISSUE #2: Time.timeScale Not Restored

**Severity**: HIGH - Menu becomes unresponsive

**Location**: [GameManager.cs](GameManager.cs#L283)

**Problem**:
```csharp
Time.timeScale = 0f;  // Pause game
yield return new WaitForSecondsRealtime(0.5f);
SceneManager.LoadScene(gameOverScene);  // Load menu BUT timeScale still 0!
```

**Impact**:
- Menu animations don't play
- UI responsiveness reduced
- Camera bobbing frozen
- Any time-based game logic broken

**Fix Required**:
```csharp
// In UiManager.cs Start() or early in scene load:
Time.timeScale = 1f;  // Reset before anything else
```

Or better:
```csharp
// In GameOverSequence:
yield return new WaitForSecondsRealtime(0.5f);
Time.timeScale = 1f;  // Restore BEFORE loading scene
SceneManager.LoadScene(gameOverScene);
```

### ⚠️ ISSUE #3: Inefficient FindFirstObjectByType Calls

**Severity**: MINOR - Works but not optimal

**Locations**: 
- [GameManager.cs](GameManager.cs#L242) (called every time player loses life)
- [GuffController.cs](GuffController.cs#L43) (called in Start)
- [MomoController.cs](MomoController.cs#L29) (called in Start)

**Better Practice**: Cache references in Start()

---

## 8. POTENTIAL IMPROVEMENTS

### Short Term (Bug Fixes)
1. ✅ Fix button listener cleanup in UiManager
2. ✅ Reset Time.timeScale before menu load
3. ✅ Cache FindFirstObjectByType references

### Medium Term (Performance)
1. Reduce FindFirstObjectByType calls - cache in Start()
2. Consider object pool prewarming (create objects at startup)
3. Add null checks before FindFirstObjectByType

### Long Term (Architecture)
1. Implement proper Singleton pattern with initialization
2. Add SceneManager.sceneLoaded callback to reset Time.timeScale
3. Consider event bus/messenger pattern instead of FindFirstObjectByType
4. Add unit tests for spawn rates and difficulty progression

---

## 9. INPUT SENSITIVITY TUNING GUIDE

### For Mobile Phones

**Current Settings in PlayerMovement.cs:**

To adjust responsiveness:

1. **Increase Smoothing** (More floaty):
   ```
   smoothTime = 0.3f  (Default probably ~0.15)
   ```

2. **Decrease Smoothing** (Snappier):
   ```
   smoothTime = 0.05f
   ```

3. **Quick Test Values**:
   - `0.05` = Super responsive (arcade-like)
   - `0.15` = Balanced 
   - `0.3` = Smooth/floaty (relaxed)
   - `0.5+` = Very floaty (not recommended)

**Recommendation**: For a casual game like this, `0.1-0.15` is ideal for touch.

---

## 10. FILE DEPENDENCIES

```
GameManager.cs
  ├─ Requires: objectPrefabs array (momos & guff)
  ├─ Requires: gameOverScene string
  └─ Calls: FinalTouch, PlayerController

PlayerMovement.cs
  └─ Standalone (except for GetComponent calls)

MomoController.cs
  ├─ Requires: PlayerTag = "Player"
  ├─ Requires: ground tag
  └─ Calls: GameManager, FinalTouch

GuffController.cs
  ├─ Requires: PlayerTag = "Player"
  ├─ Requires: ground tag
  └─ Calls: GameManager, FinalTouch, PlayerMovement

FinalTouch.cs
  ├─ Singleton pattern (self-manages)
  └─ Calls: GetComponent for AudioSource

PlayerController.cs
  ├─ Requires: lifeImage array (UI)
  └─ Calls: GameManager

UiManager.cs
  ├─ Requires: playButton & quitButton
  ├─ Requires: gameScene string
  └─ Calls: SceneManager
```

---

## SUMMARY TABLE

| Aspect | Status | Details |
|--------|--------|---------|
| **Object Pooling** | ✅ Good | Dictionary-based pool, efficient reuse |
| **Event Cleanup** | ❌ Critical Issue | Button listeners accumulate - memory leak |
| **Time Management** | ❌ Critical Issue | Time.timeScale not reset after pause |
| **Coroutines** | ✅ Good | Properly stopped before new ones |
| **Input System** | ✅ Good | Smooth touch input with sensitivity control |
| **Scene Transitions** | ⚠️ Needs Work | Time.timeScale issue affects menu |
| **Death Flow** | ✅ Good | Clear sequence, proper invincibility window |
| **Resource Cleanup** | ✅ Mostly Good | Objects pooled properly, slight FindFirstObjectByType inefficiency |
| **Architecture** | ✅ Decent | Clear separation of concerns, FindFirstObjectByType pattern used |

---

**Last Updated**: 2026-05-05
**Analyzer**: Code Review AI
