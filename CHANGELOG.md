# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.1] - 2026-11-17
### Changed
- **Fork baseline** — `main` fast-forwarded to upstream `fc3e580` (`Complete migration from Unity package to CLI/agent-driven toolkit`); this branch tracks the upstream CLI toolkit and preserves it via `scripts/sync-upstream.ps1`
- **C# naming standard retired `m_`/`s_`/`k_`** — all toolkit code, samples (`Samples/`), script templates, docs, and the generated `.editorconfig` now follow the .NET Runtime team coding style: `_camelCase` for private/internal instance fields, `s_camelCase` for private/internal static fields (including static readonly), PascalCase for constants (any accessibility), PascalCase for public/protected fields, `XxxAsync` for Task-returning async methods
- **`GenerateEditorConfig.cs`** — now copies `AI/unity-code-style-guide/AgentsInstructions/CodeStyleGuide/EditorConfig/.editorconfig` verbatim (single source of truth, no embedded duplicate); falls back to an embedded copy of the identical content when the style guide is missing (e.g. `--skip-style-guide`)
- **Style guide source** — the setup pipeline now clones the fork [`MUDV587/unity-code-style-guide`](https://github.com/MUDV587/unity-code-style-guide) instead of upstream; `setup-project.sh` and the `unity-project-setup` skill are updated, and the fork carries the same .NET Runtime naming standard
- **`FixVSCodeSlnx.cs`** — renamed behaviour to "VS Code solution fix": prefers `.slnx` (Unity 6) and falls back to `.sln` (2022.3 and earlier); class and `fixVSCodeSlnx` config key names unchanged
- **`Samples/` folder** — renamed from `StarterSystems/` to `Samples/`, matching every reference in `setup-project.sh`, `readme.md`, the skill, and docs (previously the folder name didn't match, so `--samples` silently skipped every sample)
- **Toolkit code** — private constants renamed `k_Name` → `Name` per the new standard (Unity `SerializedProperty` names like `m_CompressAssetsOnImport` are unaffected)

### Added
- **`scripts/sync-upstream.ps1`** — one-command upstream sync: fast-forwards local `main` from `upstream/main`, then merges it through `Unity6000` into `Unity2022` (no rebasing; conflict surface is limited to the style-rename files)

### Unity 2022.3 line (Unity2022 branch)
- **`ObjectPooling` sample** — `Awaitable.WaitForSecondsAsync` replaced with a coroutine (`IEnumerator` + `WaitForSeconds`); readme updated
- **`Documentation/QuickReference.md`** — "Async" section retargeted from Awaitable to coroutines
- **`toolkit.json`** — `unityMinVersion` is now `2022.3`
- **`setup-project.sh`** — `--style-guide-ref` now defaults to `Unity2022` (the 2022.3-retargeted style guide); usage text updated. Use `--style-guide-ref main` for the Unity 6 line
- **`unity-project-setup` skill** — clones the `Unity2022` branch of the style-guide fork (Unity 6 line: `main`)
- **Unity 6-only project settings** — New Hierarchy window and Asset Manager import-location are `#if UNITY_6000_0_OR_NEWER` no-ops on earlier editors (the setup scripts compile on the target editor)
- **`ScriptTemplates/readme.md`** — retargeted to Unity 2022.3 docs

### Unity 2022.3 line — Unity CLI fallback (Nov 2026)
- **`setup-project.sh` runs without the Unity CLI** — the toolkit is no longer hard-blocked when `unity` is not on PATH. New behaviour:
  - `--editor-path <exe>` (or `UNITY_EDITOR_PATH`) picks the Editor binary explicitly
  - without a CLI, the Editor binary is resolved from the standard Hub install locations for `--editor-version` (also `D:..F:` roots, classic pre-Hub layouts, macOS/Linux/Universal paths)
  - the pipeline invokes the Editor directly in batch mode (`-batchmode -nographics -quit -executeMethod ...`), which works on every 2022.3+ Editor
  - `--editor-version` is required when no CLI is installed (no `lts` auto-pick — explicit or config value only)
- **`unity-project-setup` skill** — the live-Editor path is now explicitly annotated **Unity 6.0+ only** (Unity Pipeline package prerequisite); headless invocation via `--editor-path` is the supported route on 2022.3
- **Verified end-to-end on Unity 2022.3.62f1** (headless, no CLI): scratch project — full pipeline run including folder scaffold, `.asmdef` generation, 10 presets registered, tags/sorting layers/physics layers, `.gitignore`/`.gitattributes`, forked `.editorconfig` copy (DRY path), all project/iteration/asset-pipeline settings; Unity 6-only settings correctly recorded as skipped
- `readme.md` — quick start now documents the no-CLI route; `scripts/sync-upstream.ps1` unchanged

## [2.0.0] - 2026-09-08
### Changed
- **Complete migration from Unity package to CLI/agent-driven toolkit** — no longer installed via `manifest.json`; run `setup-project.sh` (or the `unity-project-setup` skill) against a project instead. Package identity `com.unity.best-practices` retired; `package.json` replaced by `toolkit.json` (plain metadata, not a UPM manifest)
- **Setup logic moved to `SetupScripts/Editor/`** — `NewProjectWizard.cs` replaced by `BestPracticesSetup.Run()`, a headless entry point invoked via `unity run -- -executeMethod`; every step's `[MenuItem]`/`EditorWindow` wrapper removed since there is no menu bar in this model. Scripts are staged into the target project, run once, then deleted — no residue
- **`ConfigurePresets.cs`** — preset path changed from `Packages/com.unity.best-practices/Editor/Presets` to `Assets/Settings/Presets` (its destination once copied in by the pipeline)
- **`Presets/` and `ScriptTemplates/`** moved from `Editor/` to the repo root; both keep their `.meta` files (everything else in the repo dropped `.meta`, since this is no longer a Unity project)
- **`GenerateGitIgnore`/`GenerateGitAttributes`/`GenerateEditorConfig`** — replaced the overwrite-confirmation `EditorUtility.DisplayDialog` (would hang in headless batch mode) with a skip-if-exists check
- **`SetupTagsAndLayers.cs`** — was an empty file; `NewProjectWizard` called `EditorApplication.ExecuteMenuItem("...Setup Tags and Layers")`, but no such menu item existed anywhere in the repo, so this step was a silent no-op in every prior release. Replaced with a real implementation applying tags/sorting layers/physics layers via `SerializedObject` on `TagManager.asset`; default physics layer slots corrected from 6–15 to the actually user-definable 8–17 (slots 0–7 are reserved by Unity)
- **LLM instruction content retired from this repo** — `Editor/LLMInstructions/` (19 files) and `Editor/AgentSkills/` (2 files) removed; sourced live instead from [`unity-code-style-guide`](https://github.com/krogh-jacobsen/unity-code-style-guide), cloned into the target project as a `setup-project.sh` step
- **Stale texture presets removed** — `HDRITextureImporter`, `RoughnessTextureImporter`, `SingleSpriteTextureImporter`, `SpriteAtlasTextureImporter` deleted from `Presets/Textures/`; these were superseded by the 1.4.3 consolidation but never actually removed from disk

### Removed
- **Ongoing Editor tooling** — Project Dashboard, all 10 Validators, PlayerPrefs Inspector, Assembly Definitions Window, Layer Collision Matrix Window, Package Boilerplate Window. These require a permanently-installed package to keep working, which contradicts the CLI/one-time-setup model
- **Unity MCP relay config** — `ConfigureUnityMCP.cs` and all related Dashboard status polling removed
- **`Runtime/` assembly** — `CodeStyleGuide.cs`, `RuntimeController.cs`, and `ProjectTagsAndLayers.cs` (ScriptableObject) removed; no Runtime assembly ships anymore since this isn't an installed package
- **`CopyAIFilesToProject.cs`** — its `PackageManager.PackageInfo.FindForAssembly` lookup depended on being an installed package; the copy-into-project pattern survives as a bash step in `setup-project.sh`, now pointed at the style guide clone

### Added
- **`setup-project.sh`** — the CLI pipeline: stages setup scripts, deposits Presets/ScriptTemplates, clones the style guide, runs `BestPracticesSetup.Run()` via `unity run`, copies selected samples, cleans up
- **`skills/unity-project-setup/SKILL.md`** — agent-driven fallback for driving the same steps against a live (already-open) Editor via `unity command eval`, when `setup-project.sh` can't acquire the batch-mode lock

## [1.4.3] - 2026-03-15
### Added
- **Unity MCP relay** — `ConfigureUnityMCP.cs` writes `UserSettings/mcp.json` to wire up the Unity MCP relay server; menu item `Tools → Unity Project Configurator → AI → Configure Unity MCP`; detects whether the relay binary is installed and whether the file already exists before writing
- **Fix VS Code .slnx** — `FixVSCodeSlnx.cs` resolves the "Active document not part of open workspace" error in VS Code by removing the OmniSharp preference and adding the C# Dev Kit extension recommendation; menu item `Tools → Unity Project Configurator → Fix VS Code .slnx`
- **PlayerPrefs Inspector** — `PlayerPrefsInspectorWindow.cs` lets you view, search, add, edit, and delete PlayerPrefs entries (String / Int / Float) without leaving the editor; menu item `Tools → Unity Project Configurator → Utilities → PlayerPrefs Inspector`
- **Layer Collision Matrix editor** — `LayerCollisionMatrixWindow.cs` renders the physics layer collision matrix as an editable triangular grid and saves/loads configurations to `LayerCollisionConfig` assets; menu item `Tools → Unity Project Configurator → Utilities → Layer Collision Matrix`

### Changed
- **Texture import presets consolidated** — reduced from 7 presets to 4 to match the recommended asset pipeline: `SingleSprite` + `SpriteAtlas` merged into `UISprite`; `Roughness` merged into `Mask`; `HDRI` removed; folder filters updated to `Assets/**/Textures/UI/**`, `Assets/**/Textures/Environment/Albedo/**`, `Assets/**/Textures/Environment/Normal/**`, and `Assets/**/Textures/Environment/Masks/**`
- **Copy AI Files to Project** — `CopyAIFilesToProject.cs` now copies LLM instruction files to `.github/instructions/*.instructions.md` (VS Code Copilot workspace instructions) and agent skill files to both `.github/prompts/*.prompt.md` (Copilot reusable prompts) and `.claude/commands/*.md` (Claude Code slash commands)

## [1.4.2] - 2026-03-14
### Added
- **? Explainer buttons** — every card in the Project Configuration panel now has a `?` button that opens a reusable `ExplainerWindow` popup with a description, folder/settings preview, and an optional "Run Now" action
- **Section dividers** — thin horizontal rules between dashboard panels improve visual separation without changing individual card layout

### Changed
- **Package renamed to Unity Project Configurator** — `displayName` updated to `Unity Project Configurator`; all menu items moved to `Tools/Unity Project Configurator/`; Create Asset menus updated from `Best Practices/` to `Project Configurator/`
- **Menu reorganisation** — all tools consolidated from the mixed `Window/Best Practices/` and `Tools/Unity Best Practices/` locations into a single `Tools/Unity Workbench/` hierarchy with submenus (`Setup`, `Code`, `Version Control`, `AI`, `Utilities`); `Open Dashboard` is pinned at the top (priority 1)
- **Import preset glob patterns** — preset folder filters no longer rely on the hardcoded `_ProjectName` root; patterns now use `Assets/**/<FolderName>/**` so they match the named folder at any depth regardless of project root name
- **Disable button colour** — the `Disable` button tint changed from saturated red `(0.9, 0.5, 0.5)` to a softer rose `(0.85, 0.65, 0.65)` to reduce visual noise

### Fixed
- **`CreateSubSystem` folder nesting** — `UI/UXML`, `UI/USS`, and `UI/Scripts` sub-system subfolders were passed as a single path string to `EditorController.CreateFolder`, causing Unity to create a flat `UI_UXML` folder; each component is now created with a separate `CreateFolder` call using the correct parent path

## [1.4.1] - 2026-03-13
### Added
- **Project Settings** — per-setting Disable/Reset toggle buttons let you revert individual settings without opening the settings window; `ConfigureProjectSettings` gains `DisableEnterPlayMode`, `DisableIncrementalGC`, `DisableCreateObjectsAtOrigin`, `DisableNewHierarchyWindow`, and `ResetAssetManagerImportLocation`
- **Project Settings** — each card description now uses an "Enabled: … / Disabled: …" format so the effect of toggling is always visible inline
- **Project Settings** — "Open" button replaced with "Settings" and routes to the correct Project Settings or Preferences panel per entry; file-linked buttons keep the "Open" label

### Fixed
- **Agent Skills / LLM Instructions** — presence detection was broken for local and registry packages because paths were resolved by string concatenation instead of `PackageInfo.FindForAssetPath`; `ProjectDashboardData` now stores a resolved `FullPath` on each file entry via `ResolveAssetPathToAbsolute()`
- **Agent Skills / LLM Instructions** — installed files now show `[Configured]`; the "Add" button is hidden once a file is installed; outdated files show `[Outdated]` with an "Update" button
- **Project Configuration** — `.gitignore`, `.gitattributes`, and `.editorconfig` cards now show `[Configured]` when the file exists (was `[OK]`)

## [1.4.0] - 2026-03-09
### Added
- `Editor/NewProjectWizard.cs` — **Window → Best Practices → New Project Wizard** opens a guided setup window that runs all 7 setup steps (folders, assembly definitions, import presets, tags & layers, .gitignore, .editorconfig, project settings) individually or all at once via "Run Full Setup"
- `Editor/ConfigureProjectSettings.cs` — **Window → Best Practices → Configure Project Settings** applies recommended Unity 6 project settings in one click: Enter Play Mode (DisableDomainReload + DisableSceneReload), IL2CPP scripting backend for Standalone/Android/iOS, and .NET Standard 2.1 API compatibility
- `Editor/Validator/ProjectSettingsValidator.cs` — new validator that checks whether Enter Play Mode optimisations are enabled and flags Mono scripting backend; results appear in the Project Dashboard validation details
- `ConfigurePresets.Execute()` — public entry point for the preset registration logic, allowing the wizard to call it without the confirmation dialog

### Fixed
- `ProjectDashboardWindow` — preset status display in Status Overview now correctly shows "Audio: OK / missing" and "Textures: OK / missing" for each preset type (previously all four branches displayed the same string due to stripped control characters)

### Changed
- `ProjectDashboardWindow` — header now shows the package version (read at runtime via `PackageInfo.FindForAssembly`); added shortcut button to open the New Project Wizard
- `ProjectDashboardWindow` — "Git" status row renamed to "Git & IDE" and now includes `.editorconfig` presence; reports "All configured" or lists what is missing
- `ProjectDashboardWindow` — validation details now show a green "[OK]" row for validators with zero issues, giving a complete pass/fail view of every validator
- `ProjectDashboardWindow` — window auto-refreshes on focus (`OnFocus`) so data stays current after running setup steps
- `ProjectDashboardData` — adds `HasEditorConfig` and `PackageVersion` fields; `GatherGitData` now checks for `.editorconfig`; `ProjectSettingsValidator` added to the validator list

## [1.3.3] - 2026-03-09
### Added
- `Editor/AgentSkills/` folder — agent skill prompt files for use with Claude Code and GitHub Copilot
- `UnityCodeReview.md` — skill that reviews C# code against `UnityCodeStyleInstructions.md` and `UnityPerformanceOptimizationInstructions.md`, producing a severity-rated issues table with a Pass/Fail verdict
- `UnityUIToolkitCreate.md` — skill for generating UI Toolkit UI (UXML + USS + C# View/Model/Presenter) following `UnityUIToolkitInstructions.md` conventions

### Changed
- `UnityDebuggingInstructions.md` — removed domain-specific sections (Input System, Animation, Audio, Performance, UI Toolkit debugging) now that they live in their respective domain files; added overview table pointing to each domain file
- `UnityInputSystemInstructions.md` — added Troubleshooting section (device connection, PlayerInput, Input Action debugging, common issues table)
- `UnityAnimationInstructions.md` — added Troubleshooting section (Animator state/parameter issues, Animation Events, Root Motion)
- `UnityAudioInstructions.md` — added Troubleshooting section (AudioSource, spatial audio, AudioMixer issues)
- `UnityPerformanceOptimizationInstructions.md` — added Troubleshooting section (diagnosing performance issues, Profiler setup, finding hidden allocations)
- `UnityProjectConfigurationInstructions.md` — added Version Control Settings and Script Compilation Settings sections, expanded TOC, added Troubleshooting section, corrected `applyTo` frontmatter scope, replaced hardcoded paths with general guidance

## [1.3.2] - 2026-03-04
### Added
- `Editor/Dashboard/ProjectDashboardData.cs` — stateless data collector driving all dashboard checks:
  - **Folder Structure** — verifies all expected `_ProjectName` subfolders exist
  - **Import Presets** — verifies all expected `.preset` assets exist in the package
  - **LLM Instruction Files** — verifies all expected markdown instruction files exist
  - **Git & IDE Config** — checks for `.git` repo, `.gitignore`, and `.editorconfig`
  - **Project Health** — checks assembly definitions, Tags & Layers config, scenes in Build Settings, company/product name defaults, scripting backend, oversized textures (>2048px), and uncompressed audio (PCM/ADPCM)
- `Editor/Dashboard/ProjectDashboardWindow.cs` — `EditorWindow` rendering all sections with colour-coded status icons, per-section action buttons, and a summary banner; open via **Window → Best Practices → Project Dashboard** or **Shift+Alt+D**

## [1.3.1] - 2026-03-04
### Added
- `ProjectDashboardWindow.cs` — **Window → Best Practices → Project Dashboard** opens a single EditorWindow showing the health of the entire project at a glance
- `ProjectDashboardData.cs` — stateless data collector that drives the dashboard; checks are separated from rendering following single responsibility
- **Folder Structure** section — shows which expected `_ProjectName` folders exist vs are missing, with a one-click "Setup Project Folders" button
- **Import Presets** section — verifies each expected `.preset` asset exists in the package, with a one-click "Configure Import Presets" button
- **LLM Instruction Files** section — checks all expected instruction markdown files are present in the package
- **Git & IDE Config** section — checks for `.git` repo, `.gitignore`, and `.editorconfig` with generation buttons
- **Project Health** section — checks assembly definitions, Tags & Layers config, scenes in Build Settings, company/product name defaults, and scripting backend; includes buttons to open Player Settings and Build Settings
- Summary banner at the top of the window showing total error and warning counts across all sections

## [1.3.0] - 2026-03-04
### Added
- `ProjectTagsAndLayers.cs` — ScriptableObject config for defining custom tags, sorting layers, and physics layers (create via **Assets → Create → Best Practices → Project Tags and Layers**)
- `SetupTagsAndLayers.cs` — **Window → Best Practices → Setup Tags and Layers** reads the config and registers entries into `TagManager.asset`, preserving existing entries
- Default config ships with common game-development tags (`Player`, `Enemy`, `NPC`, `Projectile`, `Pickup`, `Interactable`, `Checkpoint`, `SpawnPoint`, `Trigger`), sorting layers (`Background`, `Environment`, `Props`, `Characters`, `Foreground`, `UI`, `Overlay`), and physics layers (`Player`, `Enemy`, `NPC`, `Projectile`, `Pickup`, `Interactable`, `Ground`, `Environment`, `Trigger`, `Ragdoll`)

## [1.2.0] - 2026-03-04
### Added
- `GenerateEditorConfig.cs` — **Window → Best Practices → Generate .editorconfig** creates an `.editorconfig` at the project root enforcing the package's C# naming conventions, formatting rules, and code style preferences for Rider, Visual Studio, and VS Code
- `GenerateGitIgnore.cs` — **Window → Best Practices → Generate .gitignore** creates a Unity-optimised `.gitignore` at the project root excluding Library, Temp, Logs, IDE files, OS metadata, and build artifacts
- `GenerateAssemblyDefinitions.cs` — **Window → Best Practices → Generate Assembly Definitions** scaffolds `.asmdef` files in Scripts, Editor, Tests/Runtime, and Tests/Editor using `CompanyName.ProductName` as the root namespace
- `SetupProjectFolders.cs` — extracted folder scaffolding into its own file from `EditorController.cs`
- `UI/Sprites` and `UI/Sprites/Atlas` folders — UI sprite assets now live under `UI/` rather than `Art/Textures/UI/`
- Texture subfolder creation (`Albedo`, `Normal`, `Roughness`, `Mask`, `HDRI`) to match preset filter paths
- Tests folder creation (`Tests/Runtime`, `Tests/Editor`) in folder scaffolding

### Changed
- Refactored `EditorController.cs` to follow single responsibility — generator logic extracted into `SetupProjectFolders.cs`, `GenerateEditorConfig.cs`, `GenerateGitIgnore.cs`, and `GenerateAssemblyDefinitions.cs`
- `EditorController.CreateFolder` is now `public` so it can be shared across generator classes
- Moved UI sprite preset glob filters from `Assets/_ProjectName/Art/Textures/UI/` to `Assets/_ProjectName/UI/Sprites/`
- All preset glob paths in `ConfigurePresets.cs` now consistently use the `Assets/_ProjectName/` prefix
- Updated `readme.md` with comprehensive documentation: Quick Start steps, folder structure diagram, preset tables, installation instructions, troubleshooting guide, and contributing section

### Fixed
- `autoReferenced` field in generated `.asmdef` files was always outputting `true` regardless of the configured value

## [1.1.1] - 2026-03-03
### Added
- `ConfigurePresets.cs` editor utility — **Window > Best Practices > Configure Import Presets** registers all package audio and texture import presets in the Preset Manager with glob folder-path filters in one click

## [1.1.0] - 2026-03-02
### Added
- New LLM instruction files: Testing, Input System, Addressables, Localization, Animation, Scene Management, Audio, Netcode, Assembly Definitions
- New script templates: Interface, Custom Editor, Editor Window, Property Drawer, Unit Test (EditMode), Unit Test (PlayMode), Static Event, Object Pool Handler
- New texture import presets: Mask, Roughness, HDRI
- Sample sets in `Samples/`: ScriptableObject Event System, Object Pooling, UI Toolkit MVP, State Machine
- Documentation files in `Documentation/`: QuickReference, PresetSetup, LLMSetup
- `samples` array in `package.json` for Package Manager UI integration
- `CLAUDE.md` for Claude Code project context

## [1.0.2] - 2026-03-02
### Added
- `CHANGELOG.md` to track version history

### Changed
- Updated `readme.md` to fully document all package features

## [1.0.1] - 2026 Mar 01
### Added
- LLM instruction files for Claude Code, GitHub Copilot, Gemini CLI, and Codex
  - `UnityCodeStyleInstructions.md`
  - `UnityPerformanceOptimizationInstructions.md`
  - `UnityDesignPatternsInstructions.md`
  - `UnityDebuggingInstructions.md`
  - `UnityUIToolkitInstructions.md`
  - `UnityProjectConfigurationInstructions.md`
  - `DocumentationTemplate.md`
  - `GameDesignDocument.md`
  - `copilot-instructions.md`
  - `custom-packages.md`
- Audio import presets: SFX, Music, Ambience, UI
- Texture import presets: Albedo, Normal, SingleSprite, SpriteAtlas, UISprite

## [1.0.0] - 2026 Feb 25
### Added
- Initial release
- `CodeStyleGuide.cs` Unity C# coding style guide reference
- Custom script templates: MonoBehaviour, ScriptableObject, Empty C#, UI Toolkit View
- Editor utilities including folder structure generation

### Upcoming features
- Additional Presets for models, animations, and other asset types
- Updated folder structure generation to support larger projects
- Example scripts demonstrating best practices in various domains (e.g. gameplay, UI, systems)
- Assembly Definitions
- Tests