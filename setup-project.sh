#!/usr/bin/env bash
# Runs this toolkit's one-time setup pipeline against a Unity project.
# Requires the target project's Editor to be closed — batch mode needs the lock.
# Two invocation paths:
#   1. Unity CLI (`unity`, new in Unity 6) with --editor-version (default: lts);
#   2. No CLI — the Editor binary invoked directly in batch mode (Unity 2022.3+).
#      Pass --editor-version explicitly or --editor-path / UNITY_EDITOR_PATH.
# If a live Editor already has the project open, use the `unity-project-setup`
# Claude Code skill instead (skills/unity-project-setup/SKILL.md), which drives
# the same steps through a live Editor connection (Unity 6.0+ only, pipeline package).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

PROJECT_PATH=""
EDITOR_VERSION="lts"
EDITOR_PATH=""
SAMPLES=""
STYLE_GUIDE_REF="Unity2022"
SKIP_STYLE_GUIDE=false
FIX_VSCODE=false

usage() {
  cat <<'EOF'
Usage: setup-project.sh --project <path> [options]

Required:
  --project <path>          Path to the target Unity project (must already exist —
                             create one via the Hub UI, or `unity projects create`).

Options:
  --editor-path <exe>       Path to Unity.exe to run setup with (or UNITY_EDITOR_PATH).
                             With the Unity CLI installed, resolved automatically; without it,
                             auto-detected from Hub's default install locations for --editor-version.
  --editor-version <ver>    Editor version to run with (default: lts). Only used when --editor-path
                             is not given. Aliases: lts, lts:<year>, 6, 2022, or a full version.
  --samples <a,b,c>         Comma-separated sample names to copy from Samples/
                            (AudioManager, ObjectPooling, ScriptableObjectEventSystem,
                            StateMachine, UIToolkitMVP). Default: none.
  --style-guide-ref <ref>   Branch/tag of unity-code-style-guide to clone (default: Unity2022 — the
                             2022.3-retargeted line; pass main for the Unity 6 line).
  --skip-style-guide        Don't clone MUDV587/unity-code-style-guide into the project.
  --fix-vscode              Also run the VS Code solution fix (.sln/.slnx) (settings.json / extensions.json).
  -h, --help                Show this help.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --project) PROJECT_PATH="$2"; shift 2 ;;
    --editor-path) EDITOR_PATH="$2"; shift 2 ;;
    --editor-path=*) EDITOR_PATH="${1#*=}"; shift ;;
    --editor-version) EDITOR_VERSION="$2"; shift 2 ;;
    --samples) SAMPLES="$2"; shift 2 ;;
    --style-guide-ref) STYLE_GUIDE_REF="$2"; shift 2 ;;
    --skip-style-guide) SKIP_STYLE_GUIDE=true; shift ;;
    --fix-vscode) FIX_VSCODE=true; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; usage; exit 2 ;;
  esac
done

if [[ -z "$PROJECT_PATH" ]]; then
  echo "Error: --project <path> is required." >&2
  usage
  exit 2
fi
if [[ ! -d "$PROJECT_PATH/Assets" ]]; then
  echo "Error: '$PROJECT_PATH' does not look like a Unity project (no Assets/ folder)." >&2
  exit 2
fi

# The Unity CLI (`unity`, new in Unity 6) is optional. When it is absent — and whenever
# --editor-path is provided — the pipeline falls back to invoking the Editor binary
# directly in batch mode, which works on every Unity 2022.3+ Editor.
USE_UNITY_CLI=false
if [[ -n "$EDITOR_PATH" ]]; then
  if [[ ! -f "$EDITOR_PATH" ]]; then
    echo "Error: --editor-path '$EDITOR_PATH' does not exist or is not a file." >&2
    exit 2
  fi
  USE_UNITY_CLI=false
elif command -v unity >/dev/null 2>&1; then
  USE_UNITY_CLI=true
fi

if [[ "$USE_UNITY_CLI" == false && "$EDITOR_VERSION" == "lts" && -z "$EDITOR_PATH" ]]; then
  echo "Error: without the Unity CLI, --editor-version must be explicit (e.g. --editor-version 2022.3.62f1)" >&2
  echo "or pass --editor-path (or set UNITY_EDITOR_PATH) to the Editor binary." >&2
  exit 2
fi

# Resolve an Editor binary when the CLI path is not used.
resolve_editor() {
  local version="$1"
  # 1. Explicit --editor-path wins.
  [[ -n "$EDITOR_PATH" ]] && { echo "$EDITOR_PATH"; return; }
  # 2. Environment override.
  [[ -n "${UNITY_EDITOR_PATH:-}" ]] && { echo "$UNITY_EDITOR_PATH"; return; }

  local roots=()
  [[ -n "${PROGRAMFILES:-}" ]] && roots+=("$PROGRAMFILES")
  [[ -n "${ProgramFiles:-}" ]] && [[ "${roots[*]}" != *"$ProgramFiles"* ]] && roots+=("$ProgramFiles")
  [[ -n "${ProgramFiles(x86):-}" ]] && roots+=("${ProgramFiles(x86)}")
  [[ -d "${UNITY_HUB:-}" ]] && roots+=("$UNITY_HUB")
  [[ -d "${USERPROFILE:-}" ]] && roots+=("$USERPROFILE")
  roots+=("C:" "D:" "E:" "F:")

  local root pattern
  for root in "${roots[@]}"; do
    # Hub standard layouts, classic pre-Hub layouts, macOS and Linux layouts.
    for pattern in \
      "$root/Unity/Hub/Editor/$version/Editor/Unity.exe" \
      "$root/Unity/Hub/Editor/$version/Editor/Unity" \
      "$root/Unity/Hub/Unity/Editor/$version/Editor/Unity.exe" \
      "$root/Unity Hub/Editor/$version/Editor/Unity.exe" \
      "$root/Unity/$version/Editor/Unity.exe" \
      "$HOME/Unity/Hub/Editor/$version/Editor/Unity" \
      "/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/MacOS/Unity" \
      "/Applications/Unity_Hub/Editor/$version/Unity.app/Contents/MacOS/Unity" \
      "$HOME/Unity/Editor/$version/Editor/Unity" \
    do
      [[ -f "$pattern" ]] && { echo "$pattern"; return 0; }
    done
  done
  return 1
}

PROJECT_PATH="$(cd "$PROJECT_PATH" && pwd)"
PROJECT_NAME="$(basename "$PROJECT_PATH")"
STAGING_DIR="$PROJECT_PATH/Assets/_BestPracticesSetup"

# ── Step 2: preflight — refuse to run against a project a live Editor already has open ──────
if [[ -f "$PROJECT_PATH/Temp/UnityLockfile" ]]; then
  echo "Error: a live Editor appears to already have '$PROJECT_PATH' open (Temp/UnityLockfile" >&2
  echo "present). 'unity run' needs the batch-mode lock and cannot run alongside it." >&2
  echo "Close the Editor first, or drive the same steps through a live Editor connection" >&2
  echo "using the 'unity-project-setup' Claude Code skill instead." >&2
  exit 1
fi

echo "==> Staging setup scripts into $STAGING_DIR"
mkdir -p "$STAGING_DIR/Editor"
cp "$SCRIPT_DIR"/SetupScripts/Editor/*.cs "$STAGING_DIR/Editor/"

# ── Step 3: setup-config.json ────────────────────────────────────────────────────────────────
SAMPLES_JSON="[]"
if [[ -n "$SAMPLES" ]]; then
  SAMPLES_JSON=$(printf '%s\n' "$SAMPLES" | awk -F',' '{
    out = "["
    for (i = 1; i <= NF; i++) { if (i > 1) out = out ","; out = out "\"" $i "\"" }
    out = out "]"
    print out
  }')
fi
cat > "$STAGING_DIR/setup-config.json" <<EOF
{
  "projectName": "$PROJECT_NAME",
  "samples": $SAMPLES_JSON,
  "fixVSCodeSlnx": $FIX_VSCODE
}
EOF

# ── Step 4: deposit permanent static content (Presets must be on disk before the C# run) ────
echo "==> Copying import presets to Assets/Settings/Presets"
mkdir -p "$PROJECT_PATH/Assets/Settings/Presets"
cp -R "$SCRIPT_DIR"/Presets/* "$PROJECT_PATH/Assets/Settings/Presets/"
rm -f "$PROJECT_PATH/Assets/Settings/Presets/readme.md"

echo "==> Copying script templates to Assets/ScriptTemplates"
mkdir -p "$PROJECT_PATH/Assets/ScriptTemplates"
cp "$SCRIPT_DIR"/ScriptTemplates/*.cs.txt "$PROJECT_PATH/Assets/ScriptTemplates/"

# ── Step 5: clone the style guide ────────────────────────────────────────────────────────────
if [[ "$SKIP_STYLE_GUIDE" == false ]]; then
  # Fork with the .NET Runtime naming standard (m_/s_/k_ retired) — see CLAUDE.md.
  echo "==> Cloning unity-code-style-guide fork (ref: $STYLE_GUIDE_REF)"
  TMP_CLONE="$(mktemp -d)"
  git clone --depth 1 --branch "$STYLE_GUIDE_REF" \
    https://github.com/MUDV587/unity-code-style-guide "$TMP_CLONE"
  rm -rf "$TMP_CLONE/.git"

  DEST="$PROJECT_PATH/AI/unity-code-style-guide"
  mkdir -p "$DEST"
  cp -R "$TMP_CLONE"/. "$DEST"/
  rm -rf "$TMP_CLONE"

  mkdir -p "$PROJECT_PATH/.github" "$PROJECT_PATH/.junie"
  printf '# See AI/unity-code-style-guide/AGENTS.md for the full C# style guide and reference guides.\n' > "$PROJECT_PATH/CLAUDE.md"
  printf '# See AI/unity-code-style-guide/AGENTS.md for the full C# style guide and reference guides.\n' > "$PROJECT_PATH/AGENTS.md"
  printf '# See ../AI/unity-code-style-guide/AGENTS.md for the full C# style guide and reference guides.\n' > "$PROJECT_PATH/.github/copilot-instructions.md"
  printf '# See AI/unity-code-style-guide/AGENTS.md for the full C# style guide and reference guides.\n' > "$PROJECT_PATH/.cursorrules"
  printf '# See ../AI/unity-code-style-guide/AGENTS.md for the full C# style guide and reference guides.\n' > "$PROJECT_PATH/.junie/guidelines.md"
else
  echo "==> Skipping unity-code-style-guide (--skip-style-guide)"
fi

# ── Step 6: run the orchestrator headlessly ──────────────────────────────────────────────────
if [[ "$USE_UNITY_CLI" == true ]]; then
  echo "==> Running BestPracticesSetup.Run via unity run (editor-version: $EDITOR_VERSION)"
  if ! unity run "$PROJECT_PATH" \
    --editor-version "$EDITOR_VERSION" \
    --allow-install \
    -- -executeMethod BestPractices.Setup.BestPracticesSetup.Run -logFile "$PROJECT_PATH/setup.log"
  then
    echo "Error: setup failed. Log tail:" >&2
    tail -n 40 "$PROJECT_PATH/setup.log" >&2 || true
    echo "Staged scripts left in place at $STAGING_DIR for debugging." >&2
    exit 1
  fi
else
  UNITY_EXE="$(resolve_editor "$EDITOR_VERSION")" || {
    echo "Error: no Unity Editor found for --editor-version '$EDITOR_VERSION'." >&2
    echo "  Looked in Hub/classic install paths on the common roots (ProgramFiles, D:..F:, macOS, Linux)." >&2
    echo "  Pass --editor-path (or set UNITY_EDITOR_PATH) to the Editor binary, e.g.:" >&2
    echo "    --editor-path \"D:/Program Files/Unity/$EDITOR_VERSION/Editor/Unity.exe\"" >&2
    exit 2
  }
  echo "==> Running BestPracticesSetup.Run via the Editor binary in batch mode"
  echo "    Editor: $UNITY_EXE"
  echo "    (no Unity CLI required — direct batch-mode invocation, works on Unity 2022.3+)"
  if ! "$UNITY_EXE" -batchmode -nographics -quit -projectPath "$PROJECT_PATH" \
    -executeMethod BestPractices.Setup.BestPracticesSetup.Run -logFile "$PROJECT_PATH/setup.log"
  then
    echo "Error: setup failed. Log tail:" >&2
    tail -n 40 "$PROJECT_PATH/setup.log" >&2 || true
    echo "Staged scripts left in place at $STAGING_DIR for debugging." >&2
    exit 1
  fi
fi

# ── Step 7: copy selected samples (folder scaffold now exists) ──────────────────────────────
if [[ -n "$SAMPLES" ]]; then
  DEST_ROOT="$PROJECT_PATH/Assets/$PROJECT_NAME/Scripts/Samples"
  mkdir -p "$DEST_ROOT"
  IFS=',' read -ra SAMPLE_LIST <<< "$SAMPLES"
  for sample in "${SAMPLE_LIST[@]}"; do
    if [[ -d "$SCRIPT_DIR/Samples/$sample" ]]; then
      echo "==> Copying sample: $sample"
      cp -R "$SCRIPT_DIR/Samples/$sample" "$DEST_ROOT/$sample"
    else
      echo "Warning: sample '$sample' not found under Samples/ — skipped." >&2
    fi
  done
fi

# ── Step 8: clean up — no toolkit residue left behind ────────────────────────────────────────
rm -rf "$STAGING_DIR"

# ── Step 9: report ────────────────────────────────────────────────────────────────────────────
PRESET_COUNT=$(grep -c "Registered preset\|Added .* preset entries" "$PROJECT_PATH/setup.log" 2>/dev/null || true)
echo ""
echo "==> Setup complete for '$PROJECT_NAME' at $PROJECT_PATH"
echo "    Presets: registered (see Edit > Project Settings > Preset Manager)"
echo "    Script templates: staged at Assets/ScriptTemplates — restart the Editor to register them"
[[ -n "$SAMPLES" ]] && echo "    Samples copied: $SAMPLES"
[[ "$SKIP_STYLE_GUIDE" == false ]] && echo "    Style guide: unity-code-style-guide@$STYLE_GUIDE_REF cloned to AI/unity-code-style-guide"
echo "    Full log: $PROJECT_PATH/setup.log"
