# LLM Instruction Setup Guide

This package ships instruction files for AI coding assistants. Here's how to wire them up for each tool.

All instruction files are located at:
```
Packages/Unity Best Practices/Editor/LLMInstructions/
```

## Claude Code (CLAUDE.md)

Create a `CLAUDE.md` file in your project root. Reference the instruction files you want Claude to follow:

```markdown
# CLAUDE.md

@Packages/com.unity.best-practices/Editor/LLMInstructions/UnityCodeStyleInstructions.md
@Packages/com.unity.best-practices/Editor/LLMInstructions/UnityPerformanceOptimizationInstructions.md
@Packages/com.unity.best-practices/Editor/LLMInstructions/UnityDesignPatternsInstructions.md
@Packages/com.unity.best-practices/Editor/LLMInstructions/UnityDebuggingInstructions.md
@Packages/com.unity.best-practices/Editor/LLMInstructions/UnityUIToolkitInstructions.md
```

Or use the consolidated file for all topics at once:
```markdown
@Packages/com.unity.best-practices/Editor/LLMInstructions/copilot-instructions.md
```

### Claude Code slash commands (Agent Skills)

Agent Skills are also available as Claude Code slash commands. Run **Tools → Unity Project Configurator → AI → Copy AI Files to Project** to copy them into your project.

The skills are placed in `.claude/commands/` which is where Claude Code looks for project-scoped slash commands. Invoke them in Claude Code with:

```
/project:UnityCodeReview
/project:UnityUIToolkitCreate
```

> **Note:** `.github/prompts/` is for GitHub Copilot reusable prompts — those files do **not** appear as Claude Code slash commands. Claude Code requires files in `.claude/commands/`.

## GitHub Copilot

Create `.github/copilot-instructions.md` and paste the contents of the instruction files you want, or reference them:

The `copilot-instructions.md` file in `LLMInstructions/` is already formatted for Copilot — copy it to `.github/copilot-instructions.md`.

## Gemini CLI

Create `.gemini/GEMINI.md` in your project root and reference or copy the instruction files.

## Codex / OpenAI

Create `AGENTS.md` in your project root. Paste or reference the relevant instruction file contents.

## OpenCode

OpenCode reads project rules from an `AGENTS.md` in your project root (it also falls back to `CLAUDE.md` when no `AGENTS.md` exists). Unlike Claude Code, OpenCode does **not** automatically parse `@file` references inside `AGENTS.md` — the recommended way to wire up the instruction files is the `instructions` field in `opencode.json`:

```json
{
  "$schema": "https://opencode.ai/config.json",
  "instructions": [
    ".github/instructions/*.instructions.md",
    ".github/copilot-instructions.md"
  ]
}
```

Run **Tools → Unity Project Configurator → AI → Copy AI Files to Project** first — it writes the individual instruction files to `.github/instructions/` and the consolidated file to `.github/copilot-instructions.md`. Alternatively, put explicit "read this file when relevant" instructions into `AGENTS.md`.

### OpenCode skills (Agent Skills)

Agent skills are also written to `.opencode/skills/<name>/SKILL.md` (with `name`/`description` frontmatter). OpenCode discovers these automatically and loads them on demand via its skill tool:

```
unity-code-review
unity-uitoolkit-create
```

> **Note:** the `.github/prompts/` and `.claude/commands/` copies serve GitHub Copilot and Claude Code respectively; OpenCode reads `.opencode/skills/` (and `.claude/skills/`, `.agents/skills/`) — a file in `.claude/commands/` alone is **not** exposed as an OpenCode skill.

## Recommended file selection per task

| You're working on... | Use these files |
|---|---|
| New scripts / refactoring | `UnityCodeStyleInstructions.md` |
| Performance-sensitive systems | `UnityPerformanceOptimizationInstructions.md` |
| Architecture / patterns | `UnityDesignPatternsInstructions.md` |
| Debugging issues | `UnityDebuggingInstructions.md` |
| UI Toolkit screens | `UnityUIToolkitInstructions.md` |
| Tests | `UnityTestingInstructions.md` |
| Input System | `UnityInputSystemInstructions.md` |
| Scene loading | `UnitySceneManagementInstructions.md` |
| Audio systems | `UnityAudioInstructions.md` |
| Animations | `UnityAnimationInstructions.md` |
| Addressables | `UnityAddressablesInstructions.md` |
| Multiplayer | `UnityNetcodeInstructions.md` |
| Assembly setup | `UnityAssemblyDefinitionsInstructions.md` |
| Localization | `UnityLocalizationInstructions.md` |
| Everything | `copilot-instructions.md` (consolidated) |
