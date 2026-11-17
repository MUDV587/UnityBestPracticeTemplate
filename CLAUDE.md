# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Toolkit identity

- Not a Unity package — there is no `package.json`, no `.asmdef`, and this repo is not installed via
  `Packages/manifest.json`. It is a CLI/agent-driven setup toolkit: a collection of setup routines,
  reference assets, and a Claude Code skill, applied to a Unity project on demand.
- Metadata/versioning lives in `toolkit.json` (name, version, `unityMinVersion`, keywords) — it is not
  a UPM manifest and nothing reads it at runtime.
- Unity minimum version: 6000.3 (targets Unity 6 APIs such as the Asset Manager import-location keys
  and `Il2CppCodeGeneration`).
- Style/instruction content is **not** authored here — it's sourced live from
  `https://github.com/krogh-jacobsen/unity-code-style-guide` as one step of the setup pipeline.

## Development workflow

There is no standalone build or test runner. Validation is end-to-end against a real Unity project:

- Run `./setup-project.sh --project <scratch-project-path> --editor-version <ver>` and check the exit
  code, or drive the same steps via the `unity-project-setup` skill.
- **Script templates**: take effect only after restarting the Unity Editor in the target project —
  templates are registered at startup, and this is unrelated to whether this repo is a package.
- **Import presets**: registered programmatically by `SetupScripts/Editor/ConfigurePresets.cs` when
  the pipeline runs; review them afterwards in **Edit > Project Settings > Preset Manager**.
- **Folder scaffold**: `SetupScripts/Editor/SetupProjectFolders.cs`, invoked as part of
  `BestPracticesSetup.Run()` — creates `Assets/{ProjectName}/{Art,Audio,Prefabs,Scripts,Scenes,
  Settings,UI}` in the target project.

## Architecture

Three categories of content, by lifetime:

| Category | Contents | Lifetime in the target project |
|---|---|---|
| **Temporary** | `SetupScripts/Editor/*.cs` — the orchestrator and every setup step | Staged into `Assets/_BestPracticesSetup/`, run once via `unity run -- -executeMethod`, then deleted. Zero residue. |
| **Permanent** | `Presets/`, `ScriptTemplates/` | Copied into `Assets/Settings/Presets/` and `Assets/ScriptTemplates/` and left there — Unity resolves `Preset` references by GUID and discovers templates by filename, so these must persist. |
| **Agent fallback** | `skills/unity-project-setup/SKILL.md` | Not copied anywhere — lives in this repo and is loaded directly by an agent when driving setup interactively (e.g. against an already-open live Editor) instead of running `setup-project.sh`. |

`Samples/` is a fourth, opt-in category: example code copied into
`Assets/{ProjectName}/Scripts/Samples/{name}/` only when requested via `--samples`.

### The pipeline (`setup-project.sh`)

Bash script that: stages `SetupScripts/Editor/*.cs` and a `setup-config.json` into the target
project, copies `Presets/`/`ScriptTemplates/` in, clones `unity-code-style-guide` (unless skipped),
runs `unity run <project> -- -executeMethod BestPractices.Setup.BestPracticesSetup.Run`, copies
selected samples, then deletes the staged scripts. Requires the target project's Editor to be
**closed** (`unity run` needs the batch-mode lock) — `skills/unity-project-setup/SKILL.md` is the
counterpart for a live-Editor session, and the two must be kept in lockstep.

### `BestPracticesSetup.Run()` step order

`SetupProjectFolders` → `GenerateAssemblyDefinitions` → `ConfigurePresets.ApplyAllPresets` →
`SetupTagsAndLayers` → `GenerateGitIgnore` → `GenerateGitAttributes` → `GenerateEditorConfig` →
`ConfigureProjectSettings.ApplySettings` → `ConfigureIterationSettings` (script changes / async
shaders / managed stripping) → `ConfigureAssetPipeline` (auto refresh / import workers / texture
compression) → optionally `FixVSCodeSlnx`. Every step reads `SetupConfig` (parsed from
`setup-config.json`) rather than taking `-executeMethod` arguments directly, since Unity's
`-executeMethod` has no argument-passing convention that survives a list value (`samples`) cleanly
through shell → CLI → Unity argv.

### Script templates (`ScriptTemplates/`)
File naming follows Unity's convention: `{priority}-{menu path}-{default filename}.cs.txt`. Menu
separators use `__` and item separators use `_`. Must land at the target project's
`Assets/ScriptTemplates/` exactly (not nested under `Assets/Editor/`) — this is a Unity requirement,
unrelated to package status. The UI Toolkit View template depends on `UITKBaseClass` from a
`Core.UI` namespace — that base class must exist in the target project.

## C# conventions enforced by this toolkit

These apply to every `.cs` file in this repo, and are what the setup pipeline's generated
`.editorconfig` enforces in target projects.

**Field naming** (.NET Runtime team coding style — also what the cloned `unity-code-style-guide` enforces)
- `_camelCase` — private/internal instance fields
- `s_camelCase` — private/internal static fields (including static readonly)
- PascalCase — constants at any accessibility (`const` is implicitly static)
- PascalCase — public/protected/protected_internal fields (any modifier)
- By convention only (not enforceable via .editorconfig): `[ThreadStatic]` fields use `t_`

**Class member order**
1. Using statements → namespace → fields → constructors → properties → events → MonoBehaviour methods (Awake, OnEnable, Start, OnDisable, Update order) → public methods → private methods
2. Within each group: access level public → internal → protected → private, static before instance; fields ordered `const` → `static readonly` → `static` → instance, with `[SerializeField]` first

**Method naming**
- `ProcessX` — game logic
- `HandleX` — event callbacks
- `IsX` / `HasX` / `CanX` — boolean queries
- `XxxAsync` — Task-returning async methods

**Events**
- Past-tense names (e.g. `PlayerDied`, `ScoreChanged`)
- Subscribe in `OnEnable`, unsubscribe in `OnDisable`

**Braces**: Allman style (opening brace on its own line).

**UXML/USS**: kebab-case with BEM pattern (`block__element--modifier`).

## Adding new content

- **New setup step**: add a static class under `SetupScripts/Editor/`, no `[MenuItem]` (there is no
  menu — everything runs headlessly), and call it from `BestPracticesSetup.Run()` in the right order.
  Add any new config it needs to `SetupConfig`.
- **New preset**: add a `.preset` file (with its `.meta`) under `Presets/{category}/`; register it in
  `ConfigurePresets.cs`; document it in `Presets/readme.md`.
- **New script template**: follow the `{priority}-{menu}__{submenu}-{filename}.cs.txt` naming
  pattern; update `ScriptTemplates/readme.md`.
- **New sample**: add a folder under `Samples/`; list it in `setup-project.sh`'s `--samples` help
  text and `readme.md`.
- **Version bump**: after adding new functionality, propose a version bump (see rule below) and
  update both `toolkit.json` and `CHANGELOG.md` once confirmed.

## Versioning rule

After completing any work that adds new functionality, fixes a bug, or changes behaviour, always propose a version bump before making the change. Use semantic versioning (`MAJOR.MINOR.PATCH`):

- `PATCH` — default for everything: bug fixes, non-functional changes, AND new features (e.g. `1.4.1` → `1.4.2`)
- `MINOR` — only when the user explicitly requests a minor bump (e.g. `1.4.x` → `1.5.0`)
- `MAJOR` — breaking change (renamed package ID, removed API, restructured assembly)

**Do not update `toolkit.json` or `CHANGELOG.md` until the user confirms the proposed version.**

Once confirmed:
1. Update the `"version"` field in `toolkit.json`.
2. Add a new entry at the top of `CHANGELOG.md` following the existing format.

## Meta files

This repo is not Unity-project-shaped, so almost nothing in it needs a `.meta` sibling. The
exception is `Presets/**/*.preset` and `ScriptTemplates/*.cs.txt` — both are read by Unity as real
assets once copied into a target project's `Assets/`, and preserving their `.meta` avoids GUID
churn across toolkit versions. When adding a new preset or template:
- Use `DefaultImporter` for the `.meta` (or `NativeFormatImporter` for `.preset` — check an existing
  sibling file for the exact importer block).
- Empty YAML values must have a trailing space: `userData: ` not `userData:`
- Generate GUIDs with `python3 -c "import uuid; print(uuid.uuid4().hex)"`
