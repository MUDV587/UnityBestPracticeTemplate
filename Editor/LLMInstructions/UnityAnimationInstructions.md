
# GitHub Copilot Instructions: Unity Animation

Use this cheat sheet for LLM completions. See the readme.md for the general rationale behind these guidelines.

Table of contents:
- [Unity Version-Specific Instructions](#unity-version-specific-instructions)
- [Animator Parameter IDs — Cache Everything](#animator-parameter-ids--cache-everything)
- [Parameter Hash Constants Class](#parameter-hash-constants-class)
- [Setting Parameters Correctly](#setting-parameters-correctly)
- [Blend Trees](#blend-trees)
- [AnimatorOverrideController](#animatoroverridecontroller)
- [Animation Events](#animation-events)
- [CrossFade and Transitions](#crossfade-and-transitions)
- [Avatar Masks](#avatar-masks)
- [Root Motion](#root-motion)
- [RuntimeAnimatorController](#runtimeanimatorcontroller)
- [Troubleshooting](#troubleshooting)

# Unity Version-Specific Instructions

- ℹ️ This project uses Unity 2022.3 LTS. The `Animator` API used here is unchanged across Unity 2022.x and Unity 6.
- ℹ️ `Animator.StringToHash()` has been the recommended approach since Unity 5 — no exceptions.
- ℹ️ `AnimatorControllerLayer.syncedLayerAffectsTiming` (added in Unity 6) is NOT available on 2022.3 — do not use it.

# Animator Parameter IDs — Cache Everything

- ✅ Always convert parameter names to integer hashes using `Animator.StringToHash()`.
- ✅ Store hashes as `static readonly int` fields — compute them once, reuse everywhere.
- ❌ Never pass string literals to `SetBool`, `SetFloat`, `SetInteger`, or `SetTrigger` in hot paths.
- ❌ Never call `Animator.StringToHash()` inside `Update`, `FixedUpdate`, or any per-frame method.

The string overloads of the `Set*` methods perform a hash lookup on every call. Using pre-computed integer hashes eliminates this overhead entirely.

```csharp
// AVOID — string lookup every frame
private void Update()
{
    _animator.SetFloat("Speed", _speed);   // hashes "Speed" each call
    _animator.SetBool("IsGrounded", _isGrounded);
}

// PREFER — hash computed once at class initialisation
private static readonly int s_speedHash = Animator.StringToHash("Speed");
private static readonly int s_isGroundedHash = Animator.StringToHash("IsGrounded");

private void Update()
{
    _animator.SetFloat(s_speedHash, _speed);
    _animator.SetBool(s_isGroundedHash, _isGrounded);
}
```

# Parameter Hash Constants Class

For projects with many animators, centralise all hashes in a dedicated static class. This prevents duplication and makes refactoring safer.

```csharp
/// <summary>
/// Cached Animator parameter hashes. Use these integer overloads in all
/// Animator.Set* calls to avoid per-frame string hashing.
/// </summary>
public static class AnimatorParams
{
    // Locomotion
    public static readonly int Speed = Animator.StringToHash("Speed");
    public static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");
    public static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    public static readonly int IsCrouching = Animator.StringToHash("IsCrouching");

    // Combat
    public static readonly int AttackTrigger = Animator.StringToHash("Attack");
    public static readonly int HitTrigger = Animator.StringToHash("Hit");
    public static readonly int DieTrigger = Animator.StringToHash("Die");

    // State layer indices
    public static readonly int BaseLayerIndex = 0;
    public static readonly int UpperBodyLayerIndex = 1;
}

// Usage in MonoBehaviour
public class CharacterAnimator : MonoBehaviour
{
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void SetSpeed(float speed)
    {
        _animator.SetFloat(AnimatorParams.Speed, speed);
    }

    public void TriggerAttack()
    {
        _animator.SetTrigger(AnimatorParams.AttackTrigger);
    }

    public void SetGrounded(bool grounded)
    {
        _animator.SetBool(AnimatorParams.IsGrounded, grounded);
    }
}
```

# Setting Parameters Correctly

- ✅ `SetFloat(hash, value)` — for blend tree parameters and continuous values.
- ✅ `SetBool(hash, value)` — for state toggles (IsGrounded, IsCrouching, IsAlive).
- ✅ `SetInteger(hash, value)` — for discrete state selectors (weapon type index, combo counter).
- ✅ `SetTrigger(hash)` — for one-shot events (attack, jump, hit reaction).
- ✅ `ResetTrigger(hash)` — call when cancelling an action to prevent stale triggers firing later.
- ❌ Never use `SetTrigger` for continuous state — a trigger fires once and resets; use `SetBool` instead.
- ❌ Never forget to call `ResetTrigger` when an action is interrupted — orphaned triggers cause unexpected state transitions.

```csharp
// Correctly resetting a trigger on action cancel
public void CancelAttack()
{
    _animator.ResetTrigger(AnimatorParams.AttackTrigger);
    _isAttacking = false;
}
```

# Blend Trees

- ✅ Use 1D blend trees for directional locomotion driven by a single float (Speed).
- ✅ Use 2D Freeform Directional blend trees for strafing locomotion driven by two floats (VelocityX, VelocityZ).
- ✅ Smooth blend tree inputs over time using `Mathf.Lerp` or `Mathf.MoveTowards` before passing to `SetFloat`.
- ❌ Never set blend tree parameters to raw, un-smoothed values — this causes jittery animation blending.

```csharp
private static readonly int s_speedXHash = Animator.StringToHash("VelocityX");
private static readonly int s_speedZHash = Animator.StringToHash("VelocityZ");

[SerializeField] private float _blendSpeed = 5f;

private Vector2 _currentBlend;

private void Update()
{
    Vector2 targetBlend = new Vector2(_velocity.x, _velocity.z);
    _currentBlend = Vector2.MoveTowards(_currentBlend, targetBlend, _blendSpeed * Time.deltaTime);

    _animator.SetFloat(s_speedXHash, _currentBlend.x);
    _animator.SetFloat(s_speedZHash, _currentBlend.y);
}
```

# AnimatorOverrideController

- ✅ Use `AnimatorOverrideController` to swap individual animation clips at runtime without duplicating the controller.
- ✅ Apply overrides in bulk using `AnimatorOverrideController.ApplyOverrides(list)` to avoid triggering a rebuild per clip.
- ❌ Never assign clips one at a time in a loop using the indexer (`controller["ClipName"] = newClip`) — each assignment triggers an animator rebuild.

```csharp
public class WeaponAnimationSwapper : MonoBehaviour
{
    private Animator _animator;
    private AnimatorOverrideController _overrideController;

    private readonly List<KeyValuePair<AnimationClip, AnimationClip>> _overrides =
        new List<KeyValuePair<AnimationClip, AnimationClip>>();

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _overrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
        _animator.runtimeAnimatorController = _overrideController;
    }

    public void ApplyWeaponAnimations(AnimationClip attackClip, AnimationClip idleClip)
    {
        _overrides.Clear();
        // Map original clips (keys) to replacement clips (values)
        _overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(
            _overrideController["BaseAttack"], attackClip));
        _overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(
            _overrideController["BaseIdle"], idleClip));

        // Apply all overrides in a single pass — one animator rebuild
        _overrideController.ApplyOverrides(_overrides);
    }
}
```

# Animation Events

- ✅ Animation events call public or private methods on `MonoBehaviour` components on the same `GameObject` as the `Animator`.
- ✅ Declare animation event methods as `private void` — Unity's animation system finds them via reflection; they do not need to be public.
- ✅ Use animation events for gameplay-critical frame-synchronised effects: footstep sounds, VFX spawns, hitbox activation.
- ❌ Never use animation events for logic that must not miss a frame — if the animator is disabled, events are skipped.
- ❌ Do not pass complex data through animation event parameters — they support only `int`, `float`, `string`, and `Object` references.

```csharp
public class CharacterFX : MonoBehaviour
{
    [SerializeField] private ParticleSystem _footstepFX;
    [SerializeField] private AudioClip _footstepClip;
    [SerializeField] private AudioSource _audioSource;

    // Called from Animation Event on the walk/run clip at each footfall frame
    private void OnFootstep()
    {
        _footstepFX.Play();
        _audioSource.PlayOneShot(_footstepClip);
    }

    // Called from Animation Event on the attack clip at the impact frame
    private void OnAttackImpact()
    {
        // Activate hitbox, trigger screen shake, etc.
    }
}
```

# CrossFade and Transitions

- ✅ Use `Animator.CrossFade(stateHash, normalizedTransitionDuration)` to trigger transitions programmatically with a fixed blend duration.
- ✅ Use `Animator.CrossFadeInFixedTime(stateHash, fixedDuration)` when you need the blend time in seconds rather than normalised time.
- ✅ Cache state name hashes with `Animator.StringToHash("StateName")` — same as parameter hashes.
- ❌ Avoid relying exclusively on Animator conditions for complex branching logic — drive transitions from code using `SetBool`/`SetTrigger` for clarity.

```csharp
private static readonly int s_deathStateHash = Animator.StringToHash("Death");

public void PlayDeath()
{
    // Cross-fade to Death state over 0.2 seconds (normalised)
    _animator.CrossFade(s_deathStateHash, normalizedTransitionDuration: 0.2f);
}
```

# Avatar Masks

- ✅ Use Avatar Masks to isolate animation to specific body regions (e.g. upper body only for aiming while lower body runs).
- ✅ Apply masks at the **layer** level in the Animator Controller — do not apply them in code.
- ✅ Set the layer weight and mask in `Awake` or `Start` via `Animator.SetLayerWeight(index, weight)` if transitioning dynamically.
- ❌ Avoid modifying layer weights every frame — cache the target weight and lerp to it.

```csharp
private static readonly int s_upperBodyLayerIndex = 1;

[SerializeField] private float _aimLayerBlendSpeed = 5f;
private float _targetAimWeight;

public void SetAiming(bool isAiming)
{
    _targetAimWeight = isAiming ? 1f : 0f;
}

private void Update()
{
    float current = _animator.GetLayerWeight(s_upperBodyLayerIndex);
    float next = Mathf.MoveTowards(current, _targetAimWeight, _aimLayerBlendSpeed * Time.deltaTime);
    _animator.SetLayerWeight(s_upperBodyLayerIndex, next);
}
```

# Root Motion

- ✅ Enable `Animator.applyRootMotion = true` when the animation clip drives character position/rotation (e.g. melee attacks, cinematic movements).
- ✅ Override `OnAnimatorMove()` to intercept root motion and apply it through the `CharacterController` or `Rigidbody` for physics-correct movement.
- ❌ Never leave `applyRootMotion` enabled on a character driven by a `Rigidbody` without overriding `OnAnimatorMove` — it will fight physics.

```csharp
public class RootMotionDriver : MonoBehaviour
{
    private Animator _animator;
    private CharacterController _controller;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
        _animator.applyRootMotion = true;
    }

    // Called by Unity instead of applying root motion automatically
    private void OnAnimatorMove()
    {
        Vector3 rootDelta = _animator.deltaPosition;
        rootDelta.y = 0f; // Ignore vertical root motion — let gravity handle it
        _controller.Move(rootDelta);
    }
}
```

# RuntimeAnimatorController

- ✅ Swap the entire `Animator.runtimeAnimatorController` at runtime to switch a character's complete animation set (e.g. different character skins or weapon types with distinct full-body animations).
- ✅ Store `RuntimeAnimatorController` references as serialized fields — do not load them from Resources.
- ℹ️ Swapping `runtimeAnimatorController` resets all animator state — use `AnimatorOverrideController` instead if you only need to swap individual clips.

```csharp
public class CharacterAnimatorSwapper : MonoBehaviour
{
    [SerializeField] private RuntimeAnimatorController _swordController;
    [SerializeField] private RuntimeAnimatorController _bowController;

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void EquipSword()
    {
        _animator.runtimeAnimatorController = _swordController;
    }

    public void EquipBow()
    {
        _animator.runtimeAnimatorController = _bowController;
    }
}

---

# Troubleshooting

## Animator State Issues

```csharp
// Debug: Log current animator state
private void Update()
{
    var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
    Debug.Log($"State hash: {stateInfo.shortNameHash}, NormalizedTime: {stateInfo.normalizedTime:F2}");
}

// Debug: Check if a specific state is currently playing
private bool IsPlaying(string stateName)
{
    return _animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
}

// Issue: State not transitioning — verify transition conditions are met
private void DebugTransitionConditions()
{
    Debug.Log($"IsGrounded: {_animator.GetBool(AnimatorParams.IsGrounded)}");
    Debug.Log($"Speed: {_animator.GetFloat(AnimatorParams.Speed)}");
    Debug.Log($"IsInTransition: {_animator.IsInTransition(0)}");
}
```

## Animator Parameter Issues

```csharp
// Debug: Dump all parameters and their current values
private void LogAnimatorParameters()
{
    foreach (var param in _animator.parameters)
    {
        string value = param.type switch
        {
            AnimatorControllerParameterType.Bool    => _animator.GetBool(param.name).ToString(),
            AnimatorControllerParameterType.Float   => _animator.GetFloat(param.name).ToString("F3"),
            AnimatorControllerParameterType.Int     => _animator.GetInteger(param.name).ToString(),
            AnimatorControllerParameterType.Trigger => "(trigger)",
            _                                       => "unknown"
        };
        Debug.Log($"Param: {param.name} ({param.type}) = {value}");
    }
}
```

> ⚠️ Parameter names are **case-sensitive**. `"isGrounded"` and `"IsGrounded"` are different parameters.

## Animation Event Issues

```csharp
// Issue: Animation event not firing
// Check 1: Is the method on a component on the SAME GameObject as the Animator?
// Check 2: Does the method signature match a supported type?

// Valid animation event signatures:
private void OnFootstep() { }                       // No parameters
private void OnFootstep(string sound) { }           // String parameter
private void OnFootstep(float volume) { }           // Float parameter
private void OnFootstep(int index) { }              // Int parameter
private void OnFootstep(AnimationEvent evt) { }     // Full event data

// Debug: add logging to the event method
private void OnFootstep(AnimationEvent evt)
{
    Debug.Log($"Footstep at {evt.time:F3}s, clip: {evt.animatorClipInfo.clip?.name ?? "none"}");
}
```

> ⚠️ If the Animator component is **disabled**, all animation events on that clip are **silently skipped**.

## Root Motion Issues

| Symptom | Check | Solution |
|---------|-------|----------|
| Character not moving | Apply Root Motion enabled? | Enable on Animator component |
| Movement jittery | Mixing root motion with script movement? | Use one or the other, not both |
| Wrong movement direction | Animation import settings | Check Bake Into Pose options |
| Sliding feet | Animation doesn't match character speed | Adjust animation or movement speed |
```
