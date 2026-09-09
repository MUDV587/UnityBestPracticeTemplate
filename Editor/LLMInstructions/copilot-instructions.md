# Comprehensive Copilot Instructions for Strategy Game Template

**Consolidated Reference Guide** — Merges all project documentation into one authoritative source for Claude Code.

**Target Environment:**
- **Unity Version**: 6.3.4f1 exclusively
- **C# Version**: 9.0+ features supported
- **Render Pipeline**: URP 17.3.0
- **UI System**: UI Toolkit (not UGUI)
- **Input System**: Input System (not Input Manager)

---

## Table of Contents

1. [Unity Version-Specific Instructions](#unity-version-specific-instructions)
2. [Project Architecture Overview](#project-architecture-overview)
3. [C# Code Style & Naming Conventions](#c-code-style--naming-conventions)
4. [Class Organization](#class-organization)
5. [Events & Subscriptions](#events--subscriptions)
6. [Methods & Naming Patterns](#methods--naming-patterns)
7. [Design Patterns & SOLID](#design-patterns--solid)
8. [Project-Specific Patterns](#project-specific-patterns)
9. [UI Toolkit Complete Reference](#ui-toolkit-complete-reference)
10. [Debugging Instructions](#debugging-instructions)
11. [Performance Optimization](#performance-optimization)
12. [Project Configuration & Editor Settings](#project-configuration--editor-settings)
13. [Common Mistakes & Troubleshooting](#common-mistakes--troubleshooting)

---

## Unity Version-Specific Instructions

- ℹ️ This project uses **Unity 2022.3 LTS**. Use the Unity 2022.3 documentation and APIs — do not use Unity 6-only features (`Awaitable`, runtime data binding, `[UxmlElement]`, etc.).
- ℹ️ Use the newer **Input System**, not the Input Manager.
- ℹ️ Use the newer **UI Toolkit**, not UGUI.
- ℹ️ Use **Universal Render Pipeline (URP)**, not the built-in render pipeline.
- ℹ️ Prefer **coroutines** over async/await for Unity sequencing: `yield return new WaitForSeconds(delay);` Guard continuations with `if (this == null || !isActiveAndEnabled) yield break;`.
- ℹ️ When instantiating frequently, favor `UnityEngine.Pool.ObjectPool<T>` with `actionOnGet`/`actionOnRelease` to toggle active state.
- ℹ️ There is no runtime data binding (`[CreateProperty]`, `dataSource`, `binding-path`) on Unity 2022.3 — update UI elements manually via a `Refresh()` method or a change event.
- ℹ️ Burst compiler is enabled. Math-heavy code can use Jobs + Burst for significant performance gains.
- ℹ️ IL2CPP builds have different performance characteristics than Mono — profile on target platform.

---

## Project Architecture Overview

### Centralized Event System

All game events flow through **`Assets/Scripts/Core/StaticGameEvents.cs`**. This is the single source of truth for inter-system communication.

**Key Events:**
- `OnTileSelected` / `OnArmySelected` — User selection
- `OnTurnStarted` / `OnTurnEnded` — Turn lifecycle
- `OnUIStateChanged` — UI state machine transitions
- `OnMapOwnershipChanged` / `OnMapTileAttacked` — Map changes
- `OnResourcesChanged` — Resource inventory updates
- `OnWarDeclared` / `OnPeaceDeclared` / `OnAgreementFormed` — Diplomacy events

**Pattern:** Static methods control event invocation to prevent external code from firing events inappropriately.

### UI State Machine

`Assets/UI Toolkit/Scripts/UIGameController.cs` manages UI state with enum `UIGameState`:
- `DefaultMapView` — Main game view
- `ArmyView` — Army selected
- `ArmyRecruitmentView` — Recruitment panel
- `EventPopupView` — Random events
- `TownView` / `TownConstructionView` / `ConquestView` — Tile interactions
- `DiplomacyView` — Faction diplomacy

### UI Toolkit Architecture

All UI views inherit from `Assets/UI Toolkit/Scripts/UITKBaseClass.cs`:

```csharp
protected abstract void InitializeElements();  // Cache UI elements
protected abstract void RegisterCallbacks();   // Subscribe to events
protected abstract void UnregisterCallbacks(); // Cleanup
public abstract void ShowPanel(bool show);     // Toggle visibility
```

### ScriptableObject Data Pattern

Game data is defined via ScriptableObjects (suffix with `DataSO` or `SO`):
- `ArmyUnitSO` — Unit definitions (stats, costs, category)
- `ArmyCommanderSO` — Commander attributes
- `FactionDataSO` — Faction visuals and colors
- `MapTileConfigDataSO` — Tile defaults

---

## C# Code Style & Naming Conventions

### General Guidelines

- ⚠️ **Readability is key.** Keep lines short (max 120–140 chars). Use Allman style (opening brace on new line).
- ✅ Single space after commas, before/after comparison operators. No spaces inside parentheses or brackets.
- ✅ Use vertical spacing (blank lines) for visual separation. One variable declaration per line.
- ❌ Avoid spaces inside brackets: `x = dataArray[index]`, not `dataArray[ index ]`.

### Use of Regions

- ℹ️ Use `#region` sparingly as it can hide code and reduce readability.
- ✅ Use `#region` to group Animation Event Handlers or Input Event Handlers, called from the animation system.

### Comments

- ✅ Comment intent ("why") rather than restating code ("what").
- ✅ Use `[Tooltip]`, `[Header]`, `[Space]` for serialized fields that need Inspector context.

---

## Class Organization

Organize classes in the **Unity Script Execution Order**:

1. **Using statements** — System namespaces, then Unity, then project-specific
2. **Namespace declaration**
3. **Fields** — `const` → `static readonly` → `static` → instance; `[SerializeField]` first; private/internal instance fields in `_camelCase`, private/internal static fields in `s_camelCase`, constants and public/protected fields in PascalCase
4. **Constructors**
5. **Properties** — PascalCase, no prefixes
6. **Events** — Event declarations with `On` prefix for raise methods
7. **MonoBehaviour Methods** — In execution order (Awake → OnEnable → Start → Update → LateUpdate → OnDisable)
8. **Public Methods**
9. **Private Methods**
10. **Other Classes** (nested)

- ✅ Within each group, order members by access level: public → internal → protected → private; static members before instance members.

### Using Statements & Namespaces

- ✅ Keep using statements at the top of your file.
- ✅ Order: System namespaces → Unity namespaces → project-specific
- ✅ Remove unused `using` statements to keep the file clean.
- ✅ Use namespaces to prevent class conflicts. Use PascalCase without special symbols.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Core.UI;

namespace Core.MapGameplay { ... }
```

### Nullable Reference Types

- ✅ Add `#nullable enable` at the top of new `.cs` files so the compiler catches potential null dereferences.
- ✅ Annotate members and parameters that can be null with `?` (e.g., `string?`, `Transform?`).
- ✅ Use `!` (null-forgiving) only when you are certain the value is non-null.
- ⚠️ Unity engine APIs are not fully annotated on 2022.3 — expect warnings at engine call sites; keep your own contracts annotated.
- ❌ Don't disable nullable project-wide just to silence warnings — prefer file-level opt-in.

### Fields

- ✅ Use `_camelCase` for private and internal instance fields
- ✅ Use `s_camelCase` for private and internal static fields (including static readonly)
- ✅ Use PascalCase for constants at any accessibility, and for public/protected fields
- ✅ Use descriptive names that clearly indicate the field's purpose.
- ❌ Avoid abbreviations unless they are widely understood (e.g., `UI`, `ID`).
- ✅ Include units in the name if applicable (e.g., `_speedInMetersPerSecond`).
- ✅ Prefix Boolean fields with verbs like `is`, `has`, or `can` (e.g., `_isActive`, `_hasPermission`).
- ❌ Avoid redundancy by not repeating the class name in field names.
- ✅ Expose fields in the Inspector with `[SerializeField]`.

```csharp
private int _health;                  // _camelCase for private instance fields
private static int s_sharedCount;     // s_camelCase for private static fields
private const int MaxCount = 100;     // PascalCase for constants (any accessibility)

[SerializeField] private int _health; // Serialized private field

private int _elapsedTimeInSeconds;    // Include units
private bool _isPlayerDead;           // Prefix booleans
```

### Properties

- ✅ Place properties after fields and before MonoBehaviour methods.
- ✅ Use PascalCase for properties and avoid prefixes/suffixes.
- ✅ Prefer verb-like names for boolean properties (Is/Has/Can).
- ❌ Do not serialize properties. Instead use [SerializeField] private T _field with a public property.
- ✅ Use Properties for accessing or modifying the state of an object.
- ℹ️ Use methods for actions or operations.

```csharp
// Private backing field
private int _maxHealth;

// Read-only property
public int MaxHealthReadOnly => _maxHealth;

// Property with full implementation
public int MaxHealth
{
    get => _maxHealth;
    set => _maxHealth = value;
}

// Auto-implemented property
public string DescriptionName { get; set; } = "Fireball";
```

---

## Events & Subscriptions

### Event Declaration

- ✅ Use `event Action` or `event Action<T>` for declaring events.
- ✅ Use UnityEvent only when you need to expose callbacks to the Inspector.
- ✅ Follow the C# event naming convention: use past tense verbs (e.g., `DoorOpened`).
- ✅ Use the `On` prefix for methods that raise events (e.g., `OnDoorOpened`).
- ✅ Use the observer pattern to decouple systems and reduce dependencies.
- ✅ Use the null-conditional operator (`?.`) when raising events.

```csharp
// Event declarations
public event Action DoorOpened;         // Use past tense verbs
public event Action<int> PointsScored;
public event Action<CustomEventArgs> ThingHappened;

// Event raising methods
public void OnDoorOpened()
{
    DoorOpened?.Invoke();
}
```

### Subscribing and Unsubscribing

- ✅ Subscribe in `OnEnable()` and always unsubscribe in `OnDisable()` to prevent memory leaks.
- ✅ Avoid using lambda expressions when subscribing to events — makes unsubscribing impossible.
- ⚠️ Be cautious when subscribing long-lived objects (e.g., singletons) to events from short-lived objects.

```csharp
private void OnEnable()
{
    _gameManager.DoorOpened += HandleDoorOpened;
    StaticGameEvents.OnTileSelected += HandleTileSelected;
}

private void OnDisable()
{
    _gameManager.DoorOpened -= HandleDoorOpened;
    StaticGameEvents.OnTileSelected -= HandleTileSelected;
}
```

---

## Methods & Naming Patterns

### Method Naming Conventions

- ✅ Use methods for behavior and event callbacks (actions, side effects, or inputs).
- ✅ Name methods with descriptive verbs to state the action clearly.
- ✅ Use clear prefixes: `SetX` for assigning/updating a value, `ChangeX` for modifying state.
- ✅ **Use "Process" for game logic operations** — turn-based, scheduled, or system-driven operations that are part of game flow. Examples: `ProcessTradeIncome()`, `ProcessModifierDecay()`.
- ✅ **Use "Handle" for event-driven callbacks** — responding to external input or events. Examples: `HandleTileSelected()`, `HandleTurnEnded()`.
- ✅ Boolean methods should pose a question and return bool, using Is, Has, or Can.
- ❌ Avoid noun-style method names except for factory methods or event handlers.

```csharp
// ✅ "Handle" for event-driven callbacks (responding to external events/input)
private void HandleTileSelected(MapTile tile) => ChangeState(UIGameState.TownView);

// ✅ "Process" for game logic operations (turn-based or system-driven)
private void ProcessTradeIncome()
{
    foreach (var relationship in _relationships)
    {
        if (relationship.InvolvesFaction(_playerFactionData))
            _gameResources.ModifyCurrentGold(_tradeAgreementGold);
    }
}

// Boolean methods asking questions
public bool IsPlayerAlive() => _health > 0;
```

### Local Variables & var

- ✅ Use `var` when the type is apparent from the right side (e.g., `var player = GetComponent<Player>();`, `var items = new List<Item>();`).
- ✅ Use explicit types when the type is not obvious (casts, ambiguous method results, or when the literal differs from the intended type).
- ✅ `var` is for local variables only — fields, properties, and method signatures always use explicit types.
- ❌ Don't use `var` when it obscures what the value is.

---

## Design Patterns & SOLID

### SOLID Principles

- **S** — Single Responsibility: one reason to change; break down large classes.
- **O** — Open/Closed: open for extension, closed for modification.
- **L** — Liskov Substitution: subtypes must be substitutable for their base types.
- **I** — Interface Segregation: no class should implement interfaces it doesn't use.
- **D** — Dependency Inversion: depend on abstractions, not concretions.

### Interfaces

- ✅ Use interfaces to define clear "contracts" and decouple systems.
- ✅ Use the one responsibility rule per interface (Interface Segregation).
- ✅ Use the `I` prefix and PascalCase (e.g., `IDamageable`, `IAudioService`).
- ✅ Name methods with verbs and boolean members with Is/Has/Can.
- ✅ Use an interface for a pure contract with no shared implementation.

```csharp
public interface IDamageable
{
    string DamageTypeName { get; }
    float DamageValue { get; }

    bool ApplyDamage(string description, float damage, int numberOfHits);
}
```

### Enums for State Management

- ✅ Use enums for mutually exclusive states (e.g., animation, movement, UI, or game phases).
- ✅ Use enums in switch statements for clear, maintainable logic.
- ❌ Avoid using strings or integers directly for state tracking.
- ✅ Use PascalCase for enum names and values.

```csharp
public enum Direction { North, South, East, West }

switch (_currentDirection)
{
    case Direction.North: /* ... */ break;
}
```

### String Operations & Allocation Management

- ✅ Use string interpolation (`$""`) for building strings instead of concatenation (`+`).
- ✅ Use `StringBuilder` for building strings dynamically in loops.

```csharp
// Good - use string interpolation
private void UpdateScoreDisplay(int score, float time)
{
    var result = $"Score: {score} Time: {time:F1}";
}
```

### Collection Type Selection

- ✅ Use `List<T>` when the collection size changes dynamically.
- ✅ Use arrays when the size is fixed and performance matters.
- ✅ Use `Stack<T>` for Last-In-First-Out (LIFO) logic.
- ✅ Use `Dictionary<TKey, TValue>` for fast lookups by key.
- ❌ Avoid allocations inside loops — reuse collections and call `.Clear()`.

```csharp
private List<Enemy> _enemies = new();
private Enemy[] _enemyPool = new Enemy[100];
private Dictionary<int, Enemy> _enemyById = new();
```

### Coroutines

- ✅ Use coroutines (`IEnumerator` + `yield return`) for timed delays and sequencing — this is the standard approach on Unity 2022.3 (there is no `Awaitable`).
- ✅ Name coroutines with the Co suffix (e.g., `OpenDoorCo`) and start them with `StartCoroutine(...)`.
- ✅ Prefer StartCoroutine over async/await for Unity lifecycle-coupled delays.

```csharp
private IEnumerator OpenDoorCo()
{
    Debug.Log("Door opening...");
    yield return new WaitForSeconds(2f);
    Debug.Log("Door opened!");
}
```

### Async / Await

- ✅ Use `async`/`await` for I/O-bound or API-driven work that returns `Task`/`Task<T>` (file I/O, web requests, Addressables `Task` APIs).
- ✅ Name `Task`-returning methods with the `Async` suffix (e.g., `LoadSaveDataAsync()`, `FetchPlayerProfileAsync()`).
- ❌ Avoid `async void` outside event handlers (UnityEvent/button callbacks) — unhandled exceptions crash the app.
- ❌ Never block with `.Result` / `.Wait()` — always `await`.
- ✅ Guard after `await` with `if (this == null) return;` — 2022.3 has no built-in `destroyCancellationToken`.
- ✅ Coroutines for lifecycle-coupled timing, `async`/`await` for I/O — pick one per operation.

### ScriptableObjects

- ✅ Favor ScriptableObjects for static configuration data.
- ❌ Don't use ScriptableObjects to store data that changes during gameplay.
- ✅ Always mark ScriptableObjects with `[CreateAssetMenu]`.
- ✅ Append a `DataSO` suffix (e.g., `WeaponDataSO`).

```csharp
[CreateAssetMenu(fileName = "WeaponData", menuName = "Game Data/Weapon")]
public class WeaponDataSO : ScriptableObject
{
    [SerializeField] private string _weaponName;
    [SerializeField] private int _damage;

    public string WeaponName => _weaponName;
    public int Damage => _damage;
}
```

### Animation Parameters, Tags, and Input Action Names

- ✅ **PascalCase** is recommended for all text-based references.
- ✅ Prefix boolean animation parameters with **Is**, **Has**, or **Can**.
- ✅ Always define these names as constants in code.

```csharp
private const string IsRunningParam = "IsRunning";
private const string SpeedParam = "Speed";

_animator.SetBool(IsRunningParam, isMoving);
_animator.SetFloat(SpeedParam, currentSpeed);
```

### Debugging

- ✅ Log strategically: use `Debug.Log`, `Debug.LogWarning`, and `Debug.LogError` selectively.
- ✅ Use conditional compilation (e.g., `#if UNITY_EDITOR`) to strip logs in release builds.
- ✅ Always include context in log messages.
- ✅ When using `Debug.Log`, pass a GameObject or component as the second parameter.

```csharp
// Include context in log messages
Debug.Log("Player has entered the trigger zone.", this.gameObject);

// Better error logging
Debug.LogError($"[{GetType().Name}] Failed to load data: {exception.Message}", this);
```

### Using Try-Catch & Debugger Breaks

- ✅ Use try-catch blocks for handling external dependencies (file I/O, network requests).
- ❌ Avoid using try-catch for internal logic or expected conditions.
- ✅ Always log the exception details.

```csharp
public void SaveGame(GameData data)
{
    try
    {
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(SaveFilePath, json);
    }
    catch (Exception ex)
    {
        Debug.LogError($"[{GetType().Name}] Error saving: {ex}", this);
        ShowSaveErrorToPlayer();
    }
}
```

---

## Project-Specific Patterns

### Observer Pattern (Centralized Static Events)

**Location:** `StaticGameEvents.cs`

All game systems communicate through a single static event bus. Events are public for subscribing, but invocation is controlled via static methods.

```csharp
public static class StaticGameEvents
{
    public static event Action<MapTile> OnTileSelected;
    public static event Action<UIGameState> OnUIStateChanged;

    public static void InvokeOnTileSelected(MapTile tile) => OnTileSelected?.Invoke(tile);
    public static void InvokeOnUIStateChanged(UIGameState newState) => OnUIStateChanged?.Invoke(newState);
}
```

### State Pattern (Enum + Switch)

**Location:** `UIGameController.cs`

UI state is managed with an enum and switch statement.

```csharp
public enum UIGameState
{
    DefaultMapView,
    ArmyView,
    TownView,
}

public void ChangeState(UIGameState newState)
{
    _currentState = newState;
    ApplyStateToPanels(newState);
}
```

### Template Method Pattern

**Location:** `UITKBaseClass.cs`

All UI views inherit from a base class that enforces a consistent lifecycle.

```csharp
public abstract class UITKBaseClass : MonoBehaviour
{
    protected virtual void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
    }

    protected virtual void OnEnable()
    {
        InitializeElements();
        RegisterCallbacks();
    }

    protected virtual void OnDisable()
    {
        UnregisterCallbacks();
    }

    protected abstract void InitializeElements();
    protected abstract void RegisterCallbacks();
    protected abstract void UnregisterCallbacks();
    public abstract void ShowPanel(bool show);
}
```

### Service Locator & Dependency Injection

**Location:** `ServiceLocator.cs` / `DependencyInjector.cs` — services registered at startup, resolved by dependent systems.

### Composition over Inheritance

**Location:** MapTile system

Rather than deep inheritance hierarchies, use focused MonoBehaviour components on the same GameObject.

```csharp
public class MapTile : MonoBehaviour
{
    [SerializeField] private MapTileMilitary _mapTileMilitary;

    private void Awake()
    {
        _mapTileMilitary = GetComponent<MapTileMilitary>();
    }
}
```

### Object Pooling

- ✅ Use `UnityEngine.Pool.ObjectPool<T>` for frequently spawned/despawned objects.
- ✅ Pre-warm pools with a reasonable number of objects.
- ✅ Always reset pooled object state before reusing.

```csharp
public class BulletPool : MonoBehaviour
{
    [SerializeField] private Bullet _bulletPrefab;
    private ObjectPool<Bullet> _pool;

    private void Awake()
    {
        _pool = new ObjectPool<Bullet>(
            createFunc: () => Instantiate(_bulletPrefab),
            actionOnGet: bullet => bullet.gameObject.SetActive(true),
            actionOnRelease: bullet => bullet.gameObject.SetActive(false),
            defaultCapacity: 20,
            maxSize: 100
        );
    }

    public Bullet GetFromPool() => _pool.Get();
    public void ReturnToPool(Bullet bullet) => _pool.Release(bullet);
}
```

---

## UI Toolkit Complete Reference

### File Naming & Organization

- ✅ Use **PascalCase** for UXML and USS filenames (e.g., `MainMenu.uxml`, `PlayerHUD.uss`).
- ✅ Organize UXML and USS files in a consistent folder structure.
- ✅ Name USS files to match their corresponding UXML files.

### Critical USS vs CSS Differences

**USS (Unity Style Sheets) is NOT standard CSS.** It is a subset with Unity-specific extensions.

| Feature | CSS | USS |
|---------|-----|-----|
| Layout model | Flexbox + Grid | **Flexbox only** |
| Length units | `px`, `%`, `em`, `rem` | **`px` and `%` only** |
| `calc()` | ✅ Supported | ❌ Not supported |
| Color values | Hex `#FF6432` | **`rgb()` and `rgba()` only** |
| Text alignment | `text-align: center` | `-unity-text-align: middle-center` |
| `:nth-child()` | ✅ | ❌ Not supported |

**Color Values — Critical:**
```css
/* ✅ USS - use rgb() or rgba() only */
background-color: rgb(255, 100, 50);

/* ❌ Hex values do NOT work in USS */
background-color: #FF6432;  /* FAILS */
```

### USS Naming Conventions (BEM)

- ✅ Use **kebab-case** for UXML `name` and `class` values.
- ✅ Use **BEM** (Block-Element-Modifier) pattern: `block-name__element-name--modifier-name`
- ✅ Use `name` for unique identifiers (elements queried in C#).
- ✅ Use `class` for reusable styles.

**Examples:**
- ✅ `navbar-menu__item`, `button--primary`, `login-form__input-field--error`
- ❌ `navbar-item` (missing block), `button-primary` (missing `--`)

### Flexbox Layout System

**Container Properties (Parent):**
```css
flex-direction: column;         /* Default - vertical */
flex-direction: row;            /* Horizontal layout */
justify-content: flex-start;   /* Main axis alignment */
justify-content: center;
align-items: stretch;          /* Cross axis alignment */
align-items: center;
/* ⚠️ gap is NOT supported in USS — use margin on children instead */
```

**Item Properties (Children):**
```css
flex-grow: 1;                  /* Grow to fill available space */
flex-shrink: 0;                /* Don't shrink */
flex: 1;                       /* Shorthand */
```

### USS Common Properties Reference

```css
/* Display */
display: flex;          display: none;          visibility: hidden;

/* Sizing & Spacing */
width: 100px;           flex-grow: 1;           flex-shrink: 0;
padding: 10px;          margin: 10px;           margin-left: auto; /* push right */

/* Borders & Backgrounds */
border-width: 2px;      border-radius: 8px;     border-color: rgb(0, 0, 0);
background-color: rgb(50, 50, 50);
background-image: url('project://database/Assets/UI/bg.png');

/* Text (Unity-specific — not standard CSS) */
-unity-font-style: bold;
-unity-text-align: middle-center;

/* Pseudo-classes (only these are supported) */
.btn:hover { }  .btn:active { }  .btn:focus { }  .btn:disabled { }  .toggle:checked { }
```

### USS Variables (Design Tokens)

Variables must be declared in `:root {}`:

```css
:root {
    --color-primary: rgb(72, 144, 226);
    --color-surface: rgb(40, 40, 40);
    --spacing-sm: 8px;
    --spacing-md: 16px;
}

.card {
    background-color: var(--color-surface);
    padding: var(--spacing-md);
}
```

### UXML Structure

**File Structure:**
```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <Style src="project://database/Assets/UI/Styles/MainStyles.uss" />
    <ui:VisualElement name="root" class="container">
        <!-- Content -->
    </ui:VisualElement>
</ui:UXML>
```

### Runtime Data Binding — Not Available on Unity 2022.3

Unity's runtime data binding (`dataSource`, `binding-path`, `[CreateProperty]`) is Unity 6-only — it does not exist on 2022.3 and will not compile. Update the UI manually:

**Model with a change event:**

```csharp
[CreateAssetMenu]
public class PlayerDataSO : ScriptableObject
{
    public event System.Action Changed;

    [SerializeField] private int _health = 100;

    public int Health
    {
        get => _health;
        set
        {
            if (_health != value)
            {
                _health = value;
                Changed?.Invoke();
            }
        }
    }
}
```

**Populating the UI in C#:**
```csharp
private void OnEnable()
{
    _playerData.Changed += Refresh;
    Refresh();
}

private void OnDisable()
{
    _playerData.Changed -= Refresh;
}

private void Refresh()
{
    _healthBar.value = _playerData.Health;
}
```

### Querying Elements

```csharp
// By name
var button = root.Q<Button>("submit-button");

// By type only
var firstLabel = root.Q<Label>();

// By USS class
var cards = root.Query<VisualElement>(className: "card").ToList();

// Cache queries in OnEnable (NOT Update)
private void OnEnable()
{
    var root = _uiDocument.rootVisualElement;
    _submitButton = root.Q<Button>("submit-button");
    _submitButton.clicked += OnSubmitClicked;
}

private void OnDisable()
{
    _submitButton.clicked -= OnSubmitClicked;
}
```

### Show/Hide Patterns

```csharp
// Hide — removes from layout
element.style.display = DisplayStyle.None;

// Show — returns to layout
element.style.display = DisplayStyle.Flex;

// Hide but keep space
element.style.visibility = Visibility.Hidden;

// Helper method
public void SetPanelVisible(bool isVisible)
{
    _panel.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
}
```

### ListView & Template Spawning

```csharp
// Manual template instantiation
private void PopulateCards()
{
    foreach (var cardData in _cards)
    {
        var cardElement = _cardTemplate.Instantiate();
        cardElement.Q<Label>("card-title").text = cardData.Title;
        cardElement.dataSource = cardData;
        _cardContainer.Add(cardElement);
    }
}

// ListView with virtualization
private void SetupListView()
{
    _listView.makeItem = () => _itemTemplate.Instantiate();
    _listView.bindItem = (element, index) =>
    {
        var item = _items[index];
        element.Q<Label>("item-name").text = item.ItemName;
    };
    _listView.itemsSource = _items;
}
```

### Performance Tips for UI Toolkit

1. **Cache VisualElement references** in `OnEnable` — never call `Q<>()` in `Update`
2. **Use USS classes** for style changes, not inline `element.style.*`
3. **Use `ListView`** for lists with more than ~20 items
4. **Avoid `Query<>().ToList()`** in per-frame code
5. **Use USS variables** for colours and sizes
6. **Minimize UXML nesting** — each level adds traversal cost

---

## Debugging Instructions

### Diagnostic Priority Order

When investigating Unity issues, check these areas in order:

1. Console errors and warnings
2. Null reference exceptions
3. Serialization state (Inspector values vs runtime)
4. Lifecycle timing (Script execution order)
5. Scene/Prefab state
6. Physics/Rendering settings

### Console Output Analysis

| Prefix | Meaning | Typical Cause |
|--------|---------|---------------|
| `NullReferenceException` | Missing object reference | Unassigned SerializeField, destroyed object |
| `MissingComponentException` | GetComponent returned null | Component not attached |
| `IndexOutOfRangeException` | Array/List bounds exceeded | Off-by-one errors |

### Script Execution Order Issues

```
Awake()           ← Object initialization
    ↓
OnEnable()        ← Subscribe to events
    ↓
Start()           ← Initialization depending on other objects
    ↓
FixedUpdate()     ← Physics updates
    ↓
Update()          ← Game logic
    ↓
LateUpdate()      ← Camera follow
    ↓
OnDisable()       ← Unsubscribe from events
```

**Diagnosing Order Problems:**
```csharp
private void Awake()
{
    Debug.Log($"{GetType().Name}.Awake() on {gameObject.name}", this);
}
```

### Null Reference Debugging

```csharp
// Pattern: Validate all SerializeFields in Awake
private void Awake()
{
    Debug.Assert(_playerTransform != null, "PlayerTransform not assigned!", this);
    if (_requiredComponent == null)
    {
        Debug.LogError($"Required component missing on {gameObject.name}", this);
        enabled = false;
        return;
    }
}
```

### UI Toolkit Debugging

```csharp
// Element not found
var button = root.Q<Button>("myButton");  // Returns null if not found

// Debug: List all elements
root.Query().ForEach(e => Debug.Log($"{e.GetType().Name}: {e.name}"));

// UI not updating after data changes
// Check: the View calls Refresh() (or listens to the model's Changed event)
// Check: element names match the UXML name attributes (case-sensitive)
// Note: runtime data binding (binding-path / dataSource) is Unity 6-only — not available on 2022.3
```

---

## Performance Optimization

### Code Review Priority Checklist

When reviewing Unity code for performance, check in this order:

1. **Update loops** — Allocations, expensive operations, unnecessary work
2. **Physics** — OverlapSphere, Raycast frequency
3. **Memory** — String concatenation, LINQ in hot paths, boxing
4. **GetComponent/Find** — Uncached lookups, per-frame calls
5. **Rendering** — Material instances, draw calls

### Key Rules

- ❌ **Never allocate in Update/FixedUpdate/LateUpdate** — no `new List<T>()`, no string concatenation, no LINQ.
- ✅ Pre-allocate collections with expected capacity; call `.Clear()` to reuse.
- ✅ Cache component references (`GetComponent`, `Transform`, `Rigidbody`) in `Awake()`.
- ❌ Never call `GetComponent`, `FindObjectOfType`, or `Camera.main` in Update loops.
- ✅ Use `Physics.OverlapSphereNonAlloc` / `RaycastNonAlloc` with a pre-allocated buffer array.
- ✅ Use `.sharedMaterial` instead of `.material`; use `MaterialPropertyBlock` for per-instance changes.
- ✅ Use dirty flags to recalculate expensive values only when state changes.
- ✅ Cache `WaitForSeconds` instances when using coroutines (there is no `Awaitable` on 2022.3).

### Common Anti-Patterns

| Anti-Pattern | Impact | Solution |
|--------------|--------|----------|
| `GetComponent` in Update | High | Cache in Awake |
| `FindObjectOfType` at runtime | High | Use references or events |
| `new List<T>()` in Update | High | Pre-allocate and Clear() |
| String concatenation in loops | Medium | Use StringBuilder |
| `Camera.main` in Update | Medium | Cache reference |
| LINQ in Update | Medium | Use explicit loops |

---

## Project Configuration & Editor Settings

### Enter Play Mode Options

**Location:** Edit → Project Settings → Editor

| Setting | Value | Why |
|---------|-------|-----|
| Reload Domain | ❌ Disabled | Faster iteration (saves 2-5 seconds per play) |
| Reload Scene | ❌ Disabled | Keeps scene state |

**Caveat:** Static fields persist. Use `[RuntimeInitializeOnLoadMethod]` to reset statics.

### Asset Pipeline Settings

**Location:** Unity → Settings or Edit → Preferences

| Setting | Value |
|---------|-------|
| Auto Refresh | ❌ Disabled |

**Manual refresh:** `Cmd+R` (macOS) or `Ctrl+R` (Windows).

### Burst Compiler Settings

**Location:** Edit → Project Settings → Burst AOT Settings

| Setting | Value |
|---------|-------|
| Enable Burst | ✅ Enabled |
| Synchronous Compilation | ✅ Enabled |

---

## Common Mistakes & Troubleshooting

### Quick Reference Table

| ❌ Wrong | ✅ Correct | Notes |
|----------|-----------|-------|
| `color: #FF0000;` | `color: rgb(255, 0, 0);` | USS doesn't support hex |
| `button.onClick += ...` | `button.clicked += ...` | Use `clicked` |
| Using `[CreateProperty]` / `dataSource` | Don't — Unity 6-only | Manual `Refresh()` on 2022.3 |
| `root.Q("name")` | `root.Q<VisualElement>("name")` | Always include type |
| Not unsubscribing events | Unsubscribe in `OnDisable` | Memory leaks |
| `navBarMenu` | `navbar-menu` | Use kebab-case |
| `GetComponent` in Update | Cache in Awake | Major performance issue |
| String concatenation in Update | Use StringBuilder | Creates garbage |
| LINQ in Update | Explicit loops | Creates garbage |

### UI Not Updating After Data Changes

Checklist:
1. ✅ The View subscribes to the model's `Changed` event in `OnEnable` and unsubscribes in `OnDisable`
2. ✅ The model raises `Changed` in every property setter
3. ✅ The View calls `Refresh()` after subscribing (initial population)
4. ✅ Element `name`s match the UXML exactly (case-sensitive)
5. ✅ `Refresh()` updates every displayed value (no runtime data binding on 2022.3)

### Element Not Visible

1. Check `display` is not `None`
2. Check `visibility` is not `Hidden`
3. Check parent has `flex-grow: 1` or explicit size
4. Open the **UI Toolkit Debugger** (Window → UI Toolkit → Debugger)

### Button Not Responding

1. Verify subscription is in `OnEnable`
2. Check not disabled (`button.SetEnabled(false)`)
3. Check no overlay blocking pointer events
4. Check `picking-mode` is `position`

### Query Returns Null

1. Verify `name` attribute is set in UXML
2. Query in `OnEnable`, not `Awake` (for UIDocument)
3. Use correct type (`Q<Button>` not `Q<VisualElement>`)
4. Check for typos — name matching is case-sensitive

---

**Document Version:** 2.1
**Last Updated:** March 2026
**Target Platform:** Unity 2022.3+
