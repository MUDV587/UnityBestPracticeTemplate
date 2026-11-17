# Unity Best Practices — Quick Reference

## Field Naming

| Style | Use |
|---|---|
| `_camelCase` | Private/internal instance fields (`private int _health;`) |
| `s_camelCase` | Private/internal static fields, including static readonly (`private static int s_instance;`) |
| PascalCase | Constants at any accessibility (`private const int MaxHealth = 100;`) |
| PascalCase | Public/protected fields and all properties (`public int MaxHealth`) |
| PascalCase | Methods, regardless of accessibility (`private void UpdateHealth()`) |
| camelCase | Parameters, local variables |

## Class Member Order

1. Using statements
2. Namespace declaration
3. Fields (`const` → `static readonly` → `static` → instance; `[SerializeField]` first)
4. Constructors
5. Properties
6. Events
7. MonoBehaviour methods (Awake → OnEnable → Start → OnDisable → OnDestroy → FixedUpdate → Update → LateUpdate)
8. Public methods
9. Private methods
10. Nested classes / structs

Within each group: access level public → internal → protected → private, static before instance.

## Method Naming

| Pattern | Purpose | Example |
|---|---|---|
| `ProcessX` | Game logic computation | `ProcessDamage()` |
| `HandleX` | Event callback | `HandlePlayerDied()` |
| `IsX` / `HasX` / `CanX` | Boolean query | `IsAlive()`, `CanJump()` |
| `SetX` / `GetX` | Explicit accessor | `SetHealth(int)` |
| `XxxAsync` | Async method (Task-returning) | `LoadDataAsync()`, `SaveAsync()` |

## Local Variables

- Use `var` when the type is apparent from the right side (`var player = GetComponent<Player>();`); explicit types otherwise.
- Fields, properties, and method signatures always use explicit types — `var` is for locals only.

## Event Conventions

- Past-tense names: `PlayerDied`, `ScoreChanged`, `LevelLoaded`
- Subscribe in `OnEnable`, unsubscribe in `OnDisable`
- Static events for global bus: `static event Action<T> EventName`

## Braces — Allman Style

```csharp
// Correct
if (condition)
{
    DoSomething();
}

// Wrong
if (condition) {
    DoSomething();
}
```

## Performance — Hot Path Rules

- Never allocate in `Update` / `FixedUpdate` / `LateUpdate`
- Cache all component refs in `Awake` (never call `GetComponent` in Update)
- Use `CompareTag("Tag")` not `gameObject.tag == "Tag"` (string alloc)
- Use `TryGetComponent<T>()` not `GetComponent<T>()` (avoids exception on miss)
- Prefer `ObjectPool<T>` over Instantiate/Destroy for frequent objects
- Cache `Animator` param hashes: `private static readonly int s_speedHash = Animator.StringToHash("Speed");`
- Use `MaterialPropertyBlock` for per-instance shader values (not `material.SetX`)

## UI Toolkit — UXML/USS BEM Naming

```
block__element--modifier
```
- Block: `inventory-panel`
- Element: `inventory-panel__slot`
- Modifier: `inventory-panel__slot--selected`
- CSS variables: `--color-primary`, `--spacing-md`

## Async — Coroutines

Unity 2022.3 has no `Awaitable`; coroutines are the standard for simple delays and sequences:

```csharp
private IEnumerator ReturnAfterDelay()
{
    yield return new WaitForSeconds(_lifetime);

    // Guard after every yield — the object may have been disabled:
    if (this == null || !isActiveAndEnabled) yield break;

    DoThing();
}
```

Start with `StartCoroutine(ReturnAfterDelay())`. For anything more complex than a delay,
prefer a typed async path (e.g. `addressables.Handle.Task`) or an explicit state machine.

## Assembly Definitions

- One `.asmdef` per logical layer: `MyGame.Runtime`, `MyGame.Editor`, `MyGame.Tests`
- Editor assemblies: `"includePlatforms": ["Editor"]`
- Test assemblies: `"testPlatforms": ["EditMode", "PlayMode"]`
- `rootNamespace` must match the `.asmdef` name
