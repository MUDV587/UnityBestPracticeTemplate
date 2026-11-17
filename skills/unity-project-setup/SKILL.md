---
name: unity-project-setup
description: Apply this toolkit's one-time Unity project setup — folder scaffold, asmdefs, import
  presets, tags & layers, .gitignore/.gitattributes/.editorconfig, project/iteration/
  asset-pipeline settings, script templates, and the unity-code-style-guide AI instructions — to
  a new or existing Unity project. Use when setup-project.sh isn't available, didn't finish, or the
  user wants an agent to drive the steps interactively (e.g. against an already-open live Editor)
  instead of the raw script. Delegates all Unity CLI mechanics to the unity-cli skill — does not
  reimplement them.
allowed-tools:
  - Bash
  - Read
  - Write
---

# Unity Project Setup

Runs this repository's one-time setup pipeline against a Unity project. This is the agent-driven
equivalent of `setup-project.sh` — the two must stay in lockstep; if you change one, change the
other.

**This is a conformance/setup action, not ongoing project tooling.** There is no dashboard, no
health-check window, no installed package left behind. Everything this skill does either produces
a file on disk or a one-time Editor-settings change, and the scripts that make those changes are
deleted from the project once they've run.

## Before starting

1. **Confirm the target project.** If none exists yet, hand off project creation to the
   `new-unity-project` or `unity-cli` skill first — this skill only sets up an existing project
   directory (`Assets/` must already exist).
2. **Check for a live Editor**: run `unity status` (from the `unity-cli` skill). Also check whether
   `<project>/Temp/UnityLockfile` exists. Either signal means an Editor already has the project
   open — batch mode (`unity run`) cannot also acquire the lock, so use the **live-Editor path**
   below instead of the closed-Editor path.
3. Confirm you have write access to the project's `Assets/` folder and its repo root (for
   `.gitignore`/`.gitattributes`/`.editorconfig`/`AI/`).

## Phase 1 — Stage the setup scripts and static content

Mirrors `setup-project.sh` steps 3–5:

1. Create `<project>/Assets/_BestPracticesSetup/Editor/` and copy every `.cs` file from this repo's
   `SetupScripts/Editor/` into it.
2. Write `<project>/Assets/_BestPracticesSetup/setup-config.json`:
   ```json
   { "projectName": "<name>", "samples": ["<selected samples>"], "fixVSCodeSlnx": false }
   ```
   Ask the user for the project name and any samples to include if not already known — don't guess.
3. Copy `Presets/` (this repo) → `<project>/Assets/Settings/Presets/` and `ScriptTemplates/*.cs.txt`
   → `<project>/Assets/ScriptTemplates/`. Both must land on disk before Phase 2 runs — the preset
   registration step reads them from that path.
4. Unless the user says to skip it, clone `https://github.com/MUDV587/unity-code-style-guide`
   (shallow, then strip its `.git`) into `<project>/AI/unity-code-style-guide/`, checking out the
   **`Unity2022`** branch (the Unity 2022.3-retargeted style guide — pass `main` only for the
   Unity 6 line), and deposit thin
   pointer files at `CLAUDE.md`, `AGENTS.md`, `.github/copilot-instructions.md`, `.cursorrules`,
   `.junie/guidelines.md` in the project root, each pointing at
   `AI/unity-code-style-guide/AGENTS.md`. Never leave a nested `.git` inside the host project's
   working tree.

## Phase 2 — Run the orchestrator

**Delegate all Unity CLI mechanics to the `unity-cli` skill — don't reinvent its flags.**

- **Closed Editor** (the common case): run
  `unity run "<project>" --editor-version <ver> --allow-install -- -executeMethod BestPractices.Setup.BestPracticesSetup.Run -logFile "<project>/setup.log"`.
  Check the exit code; on failure, read the log tail and report it rather than guessing.
- **Live Editor already open**: trigger a recompile (e.g. touching a script, or via whatever the
  `unity-cli` skill's live-Editor commands expose) so the staged scripts compile in, then run
  `unity command eval 'BestPractices.Setup.BestPracticesSetup.Run();'` against the live connection.

Either way, `BestPracticesSetup.Run()` reads `setup-config.json` itself and runs every step in
order — you do not call the individual `SetupProjectFolders` / `ConfigurePresets` / etc. methods
one at a time unless the user specifically asked for a single step.

## Phase 3 — Samples and cleanup

Mirrors `setup-project.sh` steps 7–8:

1. Copy each requested `Samples/<name>` folder (from this repo) into
   `<project>/Assets/<ProjectName>/Scripts/Samples/<name>` — only now, since that folder scaffold
   was just created by Phase 2.
2. Delete `<project>/Assets/_BestPracticesSetup/` entirely. No trace of this toolkit's own scripts
   should remain in the project — only the folders, settings, presets, and templates they produced.

## Reporting

Tell the user: which steps ran, how many presets were registered, which samples were copied,
whether the style guide was cloned (and at which ref), and that script templates need an **Editor
restart** to register in **Assets → Create**. If anything failed, say which step and show the log
excerpt — don't silently retry.

## Ongoing actions (not part of one-shot setup)

After initial setup, a project may want to add a new game sub-system later. This is a lightweight,
on-demand action, not something `setup-project.sh` runs automatically:

- `SetupProjectFolders.CreateSubSystem(name)` scaffolds `Assets/<Project>/<name>/{Scripts,UI,Prefabs,Art}`.
- `GenerateAssemblyDefinitions.CreateSubSystemAsmdef(project, name)` adds a matching `.asmdef`.

Drive these the same way as Phase 2's live-Editor path — via `unity command eval` against an
already-open Editor — rather than staging and running the whole pipeline again.

## Scope boundary

If the project also needs new UPM packages, that's the `unity-package-management` skill's job, not
this one. If the user wants an ongoing conformance check against the style guide (naming, hot-path
violations, event leaks, project settings), that's `unity-code-style-guide`'s own
`unity-guide-audit` skill — this skill only performs the one-time setup.
