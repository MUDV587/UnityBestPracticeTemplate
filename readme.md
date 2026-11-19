# Unity Best Practices Toolkit

A CLI/agent-driven toolkit that sets up a new (or existing) Unity project in one pass: folder
scaffold, assembly definitions, import presets, tags & layers, `.gitignore`/`.gitattributes`/
`.editorconfig`, recommended project/iteration/asset-pipeline settings, script templates, and
sample code — plus an up-to-date AI coding-style guide.

Supports **Unity 2022.3 LTS+** · License: **MIT**

**This is not a Unity package.** There is nothing to add to `Packages/manifest.json`. You run this
toolkit once against a project using the [Unity CLI](https://docs.unity.com/en-us/unity-cli), and
it leaves behind only the results — no installed assembly, no menu items, no ongoing footprint.

---

## Quick Start

Requires the **Unity 2022.3 LTS+** Editor installed (this branch) and the target project's
Editor **closed** (batch mode needs the lock — see below if it's open). Two ways to run:
with Unity 6's `unity` CLI on your PATH, or without any CLI — the Editor binary is then invoked
directly in batch mode; pass `--editor-version 2022.3.62f1` (or `--editor-path <exe>`) if the
default Hub install locations don't resolve automatically.

```bash
git clone https://github.com/MUDV587/UnityBestPracticeTemplate.git
cd UnityBestPracticeTemplate
./setup-project.sh --project /path/to/MyGame --editor-version 2022.3.62f1 --samples ObjectPooling,StateMachine
```

This:
1. Stages the setup scripts into the project, runs them headlessly via `unity run -- -executeMethod`,
   then deletes them — no trace of this toolkit is left behind.
2. Registers import presets and copies script templates in permanently (both need to persist for
   Unity to keep resolving them).
3. Clones the [`unity-code-style-guide` fork](https://github.com/MUDV587/unity-code-style-guide) into
   `AI/unity-code-style-guide/` and drops pointer files at `CLAUDE.md`, `AGENTS.md`,
   `.github/copilot-instructions.md`, `.cursorrules`, and `.junie/guidelines.md`.
4. Copies any requested sample code into `Assets/{ProjectName}/Scripts/Samples/`.

Run `./setup-project.sh --help` for every flag.

### Already have the project open in a live Editor?

`unity run` can't also hold the batch-mode lock. Instead, ask an agent to run the
**`unity-project-setup`** Claude Code skill (`skills/unity-project-setup/SKILL.md`) — it drives the
exact same steps through a live Editor connection via `unity command eval`.

---

## What's Included

### Setup pipeline (`setup-project.sh` / `SetupScripts/Editor/`)

One orchestrator, `BestPracticesSetup.Run()`, runs every step below in order:

| Step | What it does |
|---|---|
| Project Folders | Creates the recommended folder hierarchy under `Assets/{ProjectName}` |
| Assembly Definitions | Generates `.asmdef` files in Scripts, Editor, and Tests using `CompanyName.ProductName` as the root namespace |
| Import Presets | Registers the presets below in the Preset Manager |
| Tags & Layers | Registers a default set of tags, sorting layers, and physics layers — see below |
| `.gitignore` / `.gitattributes` / `.editorconfig` | Generated at the project root |
| Project Settings | Enter Play Mode, .NET Standard 2.1, Input System, Incremental GC, Asset Serialization, Version Control, Create at Origin, New Hierarchy (Unity 6 only — skipped on 2022.3), Asset Manager import location (Unity 6 only — skipped on 2022.3) |
| Iteration Settings | Script Changes → Recompile After Finished Playing, Async Shader Compilation on, Managed Stripping disabled |
| Asset Pipeline | Auto Refresh → Outside Playmode, Import Worker Count 50%, Compress Textures on Import |

Each step is also callable individually (see `SetupScripts/Editor/*.cs`) if you only want one part —
either by adjusting `BestPracticesSetup.Run()` for a headless re-run, or via `unity command eval`
against a live Editor.

### AI coding-style guide

Sourced live from the [`unity-code-style-guide` fork](https://github.com/MUDV587/unity-code-style-guide)
rather than duplicated here — that repo is the maintained source for `AGENTS.md`, the opinionated
`UnityStyleGuide.md`, per-domain `UnityReferenceGuides/`, and a `unity-guide-audit` skill that checks
a project against them. `--skip-style-guide` opts out if you'd rather manage this yourself.

### Import Presets (`Presets/`)

| Preset | Matches |
|--------|----------|
| `UISpriteTextureImporter` | `Assets/**/Textures/UI/**` |
| `AlbedoTextureImporter` | `Assets/**/Textures/Environment/Albedo/**` |
| `NormalTextureImporter` | `Assets/**/Textures/Environment/Normal/**` |
| `MaskTextureImporter` | `Assets/**/Textures/Environment/Masks/**` |
| `SFXAudioImporter` | `Assets/**/Audio/SFX/**` |
| `MusicAudioImporter` | `Assets/**/Audio/Music/**` |
| `AmbienceAudioImporter` | `Assets/**/Audio/Ambience/**` |
| `UIAudioImporter` | `Assets/**/Audio/UI/**` |
| `FBXModelImporter` | `Assets/**/Art/Models/**` |
| `FBXAnimationImporter` | `Assets/**/Art/Animations/**` |

Rationale, settings tables, and platform-specific guidance (including reference-only coverage of
Roughness/HDRI/Video/Font import, which don't ship as presets) live in [`Presets/readme.md`](Presets/readme.md).

### Script Templates (`ScriptTemplates/`)

Copied to the target project's `Assets/ScriptTemplates/` and available via **Assets → Create →
Scripting** after an Editor restart: MonoBehaviour, ScriptableObject, Empty C# Script, UI Toolkit
View, Interface, Custom Editor, Editor Window, Property Drawer, Unit Test (EditMode/PlayMode),
Static Event, Object Pool Handler, State, NetworkBehaviour. Full list with descriptions in
[`ScriptTemplates/readme.md`](ScriptTemplates/readme.md).

### Tags & Layers

**Default Tags:** `Player`, `Enemy`, `NPC`, `Projectile`, `Pickup`, `Interactable`, `Checkpoint`, `SpawnPoint`, `Trigger`, `MainCamera`

**Default Sorting Layers (render order):** `Background`, `Environment`, `Props`, `Characters`, `Foreground`, `UI`, `Overlay`

**Default Physics Layers (slots 8–17):** `Player`, `Enemy`, `NPC`, `Projectile`, `Pickup`, `Interactable`, `Ground`, `Environment`, `Trigger`, `Ragdoll`

Existing tags/layers are preserved — only missing entries are added. Occupied physics layer slots
are skipped with a warning rather than overwritten.

### Samples (`Samples/`)

Opt in via `--samples a,b,c`:

| Sample | Demonstrates |
|---|---|
| `ScriptableObjectEventSystem` | Observer pattern using `GameEvent` ScriptableObjects |
| `ObjectPooling` | `ObjectPool<T>` spawner with auto-release via coroutines |
| `UIToolkitMVP` | UXML + USS + presenter wiring for the Model-View-Presenter pattern |
| `StateMachine` | Enum-based state machine with `IState` and `OnEnter`/`OnUpdate`/`OnExit` |
| `AudioManager` | Pooled `AudioSource` manager with `AudioMixer` routing and PlayerPrefs volume persistence |

---

## Project Folder Structure

```
Assets/
├── Editor/
├── Resources/
├── Documentation/
├── ThirdParty/
├── Settings/
│   └── Presets/
├── ScriptTemplates/
└── {ProjectName}/
    ├── Art/
    │   ├── Animations/{Clips,Controllers}/
    │   ├── Audio/{Ambience,Music,SFX,UI}/
    │   ├── Fonts/
    │   ├── Materials/Physics/
    │   ├── Models/
    │   ├── Shaders/
    │   ├── Sprites/
    │   ├── Textures/{Albedo,Normal,Roughness,Mask,HDRI}/
    │   └── VFX/
    ├── Scripts/
    │   ├── Core/
    │   ├── UI/
    │   ├── Utilities/
    │   └── Samples/          (only if --samples was used)
    ├── Editor/
    ├── UI/
    │   ├── Sprites/Atlas/
    │   ├── UXML/
    │   ├── USS/
    │   └── Settings/
    ├── Data/
    ├── Prefabs/
    ├── Scenes/
    ├── Settings/
    ├── StreamingAssets/
    └── Plugins/
```

> UI sprites live under `UI/Sprites/` (not `Art/Textures/`) because they are logically part of the
> UI system rather than general art assets.

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| **`setup-project.sh` fails immediately with a lockfile error** | A live Editor already has the project open. Close it, or run setup via the `unity-project-setup` skill instead. |
| **`unity: command not found`** | Install the Unity CLI — see the `unity-cli` skill or https://docs.unity.com/en-us/unity-cli. |
| **Script templates not appearing** | Restart the Unity Editor. Templates appear under **Assets → Create → Scripting** and are only registered at Editor startup. |
| **Presets not applying to imported assets** | Verify the glob paths in **Edit → Project Settings → Preset Manager**. They use `**` wildcards and match the folder name at any depth under `Assets/`. |
| **`.gitignore` / `.gitattributes` / `.editorconfig` skipped, not overwritten** | Setup does not overwrite existing files at the project root — delete or edit them yourself if you want the toolkit's version. |
| **Tags and layers not appearing after setup** | Check `Edit → Project Settings → Tags and Layers`. If a physics layer slot was already occupied by a different name, a warning is logged and that entry is skipped. |
| **Assembly definitions not generating** | `SetupProjectFolders` must run first — `setup-project.sh` always runs the full pipeline in order, so this only happens if you're calling steps individually out of order. |

---

## Contributing

1. Clone the repository.
2. Edit the setup scripts under `SetupScripts/Editor/` directly — this repo is not a Unity project,
   so there's no local-install step. To test a change, point `setup-project.sh` at a scratch Unity
   project.
3. Update `Presets/readme.md`, `ScriptTemplates/readme.md`, or this file if you add a preset,
   template, or sample.
4. Propose a version bump (see `CLAUDE.md`) before submitting.

---

## License

MIT — see [LICENSE](LICENSE) for details.
