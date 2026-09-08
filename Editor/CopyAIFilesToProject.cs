using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Unity.BestPractices.Editor
{
    /// <summary>
    /// Copies LLM Instruction and AgentSkill files from this package into the host project's
    /// standard AI-tool config folders so they are available locally for AI assistants.
    ///
    /// Destinations:
    /// <list type="bullet">
    ///   <item><description><c>Editor/LLMInstructions/*.md</c> → <c>.github/instructions/*.instructions.md</c> (VS Code Copilot workspace instructions)</description></item>
    ///   <item><description><c>Editor/LLMInstructions/copilot-instructions.md</c> → <c>.github/copilot-instructions.md</c> (GitHub Copilot repository instructions)</description></item>
    ///   <item><description><c>Editor/AgentSkills/*.md</c> → <c>.github/prompts/*.prompt.md</c> (Copilot reusable prompts) and <c>.claude/commands/*.md</c> (Claude Code slash commands)</description></item>
    ///   <item><description><c>Editor/AgentSkills/*.md</c> → <c>.opencode/skills/&lt;name&gt;/SKILL.md</c> (OpenCode native skills — SKILL.md format)</description></item>
    /// </list>
    ///
    /// All destination files are created (or overwritten) without confirmation — re-run safely at any time.
    /// </summary>
    public static class CopyAIFilesToProject
    {
        private const string k_LLMInstructionsFolder = "Editor/LLMInstructions";
        private const string k_AgentSkillsFolder = "Editor/AgentSkills";
        private const string k_CopilotInstructionsFile = "copilot-instructions.md";

        [MenuItem("Tools/Unity Project Configurator/AI/Copy AI Files to Project", false, 200)]
        public static void Execute()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                Assembly.GetExecutingAssembly());

            if (packageInfo == null)
            {
                EditorUtility.DisplayDialog(
                    "Copy AI Files",
                    "Could not resolve package path. Make sure the package is installed correctly.",
                    "OK");
                return;
            }

            string packageRoot = packageInfo.resolvedPath;
            string projectRoot = Path.GetDirectoryName(Application.dataPath);

            int llmCount = CopyLLMInstructions(packageRoot, projectRoot);
            int skillCount = CopyAgentSkills(packageRoot, projectRoot);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Copy AI Files — Done",
                $"Copied to project root:\n\n" +
                $"• {llmCount} LLM Instruction file(s) → .github/instructions/\n" +
                $"• .github/copilot-instructions.md (consolidated)\n" +
                $"• {skillCount} AgentSkill file(s) → .github/prompts/, .claude/commands/ and .opencode/skills/",
                "OK");
        }
        /// <summary>Copies only LLM instruction files to .github/instructions/ (and .github/copilot-instructions.md).</summary>
        public static void ExecuteLLMInstructionsOnly()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                Assembly.GetExecutingAssembly());
            if (packageInfo == null) return;

            string packageRoot = packageInfo.resolvedPath;
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            int count = CopyLLMInstructions(packageRoot, projectRoot);
            AssetDatabase.Refresh();
            Debug.Log($"[Best Practices] Copied {count} LLM instruction file(s) to .github/instructions/");
        }

        /// <summary>Copies only AgentSkill files to .github/prompts/ and .claude/commands/.</summary>
        public static void ExecuteAgentSkillsOnly()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                Assembly.GetExecutingAssembly());
            if (packageInfo == null) return;

            string packageRoot = packageInfo.resolvedPath;
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            int count = CopyAgentSkills(packageRoot, projectRoot);
            AssetDatabase.Refresh();
            Debug.Log($"[Best Practices] Copied {count} agent skill file(s) to .github/prompts/, .claude/commands/ and .opencode/skills/");
        }
        /// <summary>Returns the absolute destination path where the given LLM instruction source would be copied in the project.</summary>
        public static string GetLLMInstructionDestPath(string srcAbsPath, string projectRoot)
        {
            string fileName = Path.GetFileName(srcAbsPath);
            if (string.Equals(fileName, k_CopilotInstructionsFile, System.StringComparison.OrdinalIgnoreCase))
                return Path.Combine(projectRoot, ".github", "copilot-instructions.md");

            string baseName = Path.GetFileNameWithoutExtension(fileName);
            return Path.Combine(projectRoot, ".github", "instructions", baseName + ".instructions.md");
        }

        /// <summary>Returns true if the destination file for the given LLM instruction source already exists in the project.</summary>
        public static bool LLMInstructionExistsInProject(string srcAbsPath, string projectRoot)
            => File.Exists(GetLLMInstructionDestPath(srcAbsPath, projectRoot));

        /// <summary>Returns true if the local copy exists but its content differs from the package source.</summary>
        public static bool LLMInstructionIsOutdated(string srcAbsPath, string projectRoot)
        {
            string destPath = GetLLMInstructionDestPath(srcAbsPath, projectRoot);
            if (!File.Exists(destPath)) return false;
            return File.ReadAllText(srcAbsPath) != File.ReadAllText(destPath);
        }

        /// <summary>Copies a single LLM instruction file to .github/instructions/ (and to .github/copilot-instructions.md if it is the consolidated file).</summary>
        public static void CopySingleLLMInstruction(string srcAbsPath, string projectRoot)
        {
            string fileName = Path.GetFileName(srcAbsPath);

            if (string.Equals(fileName, k_CopilotInstructionsFile, System.StringComparison.OrdinalIgnoreCase))
            {
                string dest = Path.Combine(projectRoot, ".github", "copilot-instructions.md");
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                File.Copy(srcAbsPath, dest, overwrite: true);
            }
            else
            {
                string instructionsDir = Path.Combine(projectRoot, ".github", "instructions");
                Directory.CreateDirectory(instructionsDir);
                string baseName = Path.GetFileNameWithoutExtension(fileName);
                File.Copy(srcAbsPath, Path.Combine(instructionsDir, baseName + ".instructions.md"), overwrite: true);
            }
        }

        /// <summary>Returns the absolute destination path for the .github/prompts/ copy of the given AgentSkill source.</summary>
        public static string GetAgentSkillPromptDestPath(string srcAbsPath, string projectRoot)
        {
            string baseName = Path.GetFileNameWithoutExtension(Path.GetFileName(srcAbsPath));
            return Path.Combine(projectRoot, ".github", "prompts", baseName + ".prompt.md");
        }

        /// <summary>Returns the absolute destination path for the .claude/commands/ copy of the given AgentSkill source.</summary>
        public static string GetAgentSkillClaudeDestPath(string srcAbsPath, string projectRoot)
        {
            string baseName = Path.GetFileNameWithoutExtension(Path.GetFileName(srcAbsPath));
            return Path.Combine(projectRoot, ".claude", "commands", baseName + ".md");
        }

        /// <summary>Returns the OpenCode skill folder name for the given AgentSkill source — the
        /// <c>name:</c> field from its SKILL.md frontmatter (must match the containing directory).
        /// Falls back to a kebab-cased file name when no frontmatter is present.</summary>
        public static string GetOpenCodeSkillFolderName(string srcAbsPath)
        {
            string name = ReadFrontmatterName(srcAbsPath);
            if (!string.IsNullOrEmpty(name))
                return name;

            string baseName = Path.GetFileNameWithoutExtension(Path.GetFileName(srcAbsPath));
            return Regex.Replace(baseName, "(?<=[a-z0-9])(?=[A-Z])", "-").ToLowerInvariant();
        }

        /// <summary>Returns the absolute destination path for the .opencode/skills/ copy of the given AgentSkill source.</summary>
        public static string GetOpenCodeSkillDestPath(string srcAbsPath, string projectRoot)
        {
            return Path.Combine(projectRoot, ".opencode", "skills",
                GetOpenCodeSkillFolderName(srcAbsPath), "SKILL.md");
        }

        /// <summary>Reads the <c>name:</c> field from the YAML frontmatter of the given file, or null when absent.</summary>
        private static string ReadFrontmatterName(string path)
        {
            string[] lines = File.ReadAllLines(path);
            if (lines.Length == 0 || !lines[0].Trim().Equals("---"))
                return null;

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line == "---")
                    break;
                if (line.StartsWith("name:", System.StringComparison.OrdinalIgnoreCase))
                    return line.Substring("name:".Length).Trim();
            }

            return null;
        }

        /// <summary>Returns true if all destination copies (.github/prompts/, .claude/commands/ and .opencode/skills/) exist for the given AgentSkill source.</summary>
        public static bool IsAgentSkillInstalled(string srcAbsPath, string projectRoot)
            => File.Exists(GetAgentSkillPromptDestPath(srcAbsPath, projectRoot))
            && File.Exists(GetAgentSkillClaudeDestPath(srcAbsPath, projectRoot))
            && File.Exists(GetOpenCodeSkillDestPath(srcAbsPath, projectRoot));

        /// <summary>Returns true if any destination copy exists but differs from the package source.</summary>
        public static bool AgentSkillIsOutdated(string srcAbsPath, string projectRoot)
        {
            string promptPath = GetAgentSkillPromptDestPath(srcAbsPath, projectRoot);
            string claudePath = GetAgentSkillClaudeDestPath(srcAbsPath, projectRoot);
            string openCodePath = GetOpenCodeSkillDestPath(srcAbsPath, projectRoot);
            string src = File.ReadAllText(srcAbsPath);
            if (File.Exists(promptPath) && File.ReadAllText(promptPath) != src) return true;
            if (File.Exists(claudePath) && File.ReadAllText(claudePath) != src) return true;
            if (File.Exists(openCodePath) && File.ReadAllText(openCodePath) != src) return true;
            return false;
        }

        /// <summary>Copies a single AgentSkill file to .github/prompts/, .claude/commands/ and .opencode/skills/.</summary>
        public static void CopySingleAgentSkill(string srcAbsPath, string projectRoot)
        {
            string baseName = Path.GetFileNameWithoutExtension(Path.GetFileName(srcAbsPath));

            string promptsDir = Path.Combine(projectRoot, ".github", "prompts");
            Directory.CreateDirectory(promptsDir);
            File.Copy(srcAbsPath, Path.Combine(promptsDir, baseName + ".prompt.md"), overwrite: true);

            string claudeDir = Path.Combine(projectRoot, ".claude", "commands");
            Directory.CreateDirectory(claudeDir);
            File.Copy(srcAbsPath, Path.Combine(claudeDir, baseName + ".md"), overwrite: true);

            string openCodeDir = Path.Combine(projectRoot, ".opencode", "skills",
                GetOpenCodeSkillFolderName(srcAbsPath));
            Directory.CreateDirectory(openCodeDir);
            File.Copy(srcAbsPath, Path.Combine(openCodeDir, "SKILL.md"), overwrite: true);
        }

        private static int CopyLLMInstructions(string packageRoot, string projectRoot)
        {
            string sourceDir = Path.Combine(packageRoot, k_LLMInstructionsFolder);
            if (!Directory.Exists(sourceDir))
                return 0;

            string instructionsDir = Path.Combine(projectRoot, ".github", "instructions");
            Directory.CreateDirectory(instructionsDir);

            string[] sourceFiles = Directory.GetFiles(sourceDir, "*.md");
            int count = 0;

            foreach (string srcPath in sourceFiles)
            {
                string fileName = Path.GetFileName(srcPath);

                if (string.Equals(fileName, k_CopilotInstructionsFile, System.StringComparison.OrdinalIgnoreCase))
                {
                    // Copy consolidated file to .github/copilot-instructions.md
                    string copilotDest = Path.Combine(projectRoot, ".github", "copilot-instructions.md");
                    Directory.CreateDirectory(Path.GetDirectoryName(copilotDest));
                    File.Copy(srcPath, copilotDest, overwrite: true);
                    // Do not also add it to .github/instructions/ to avoid redundancy
                    continue;
                }

                // Rename FileName.md → FileName.instructions.md
                string baseName = Path.GetFileNameWithoutExtension(fileName);
                string destFileName = baseName + ".instructions.md";
                string destPath = Path.Combine(instructionsDir, destFileName);
                File.Copy(srcPath, destPath, overwrite: true);
                count++;
            }

            return count;
        }

        private static int CopyAgentSkills(string packageRoot, string projectRoot)
        {
            string sourceDir = Path.Combine(packageRoot, k_AgentSkillsFolder);
            if (!Directory.Exists(sourceDir))
                return 0;

            string promptsDir = Path.Combine(projectRoot, ".github", "prompts");
            string claudeDir = Path.Combine(projectRoot, ".claude", "commands");
            string openCodeDir = Path.Combine(projectRoot, ".opencode", "skills");
            Directory.CreateDirectory(promptsDir);
            Directory.CreateDirectory(claudeDir);
            Directory.CreateDirectory(openCodeDir);

            string[] sourceFiles = Directory.GetFiles(sourceDir, "*.md");
            int count = 0;

            foreach (string srcPath in sourceFiles)
            {
                string baseName = Path.GetFileNameWithoutExtension(Path.GetFileName(srcPath));

                // .github/prompts/<Name>.prompt.md  (VS Code Copilot reusable prompts)
                string promptDest = Path.Combine(promptsDir, baseName + ".prompt.md");
                File.Copy(srcPath, promptDest, overwrite: true);

                // .claude/commands/<Name>.md  (Claude Code slash commands)
                string claudeDest = Path.Combine(claudeDir, baseName + ".md");
                File.Copy(srcPath, claudeDest, overwrite: true);

                // .opencode/skills/<name>/SKILL.md  (OpenCode native skills)
                string skillDestDir = Path.Combine(openCodeDir, GetOpenCodeSkillFolderName(srcPath));
                Directory.CreateDirectory(skillDestDir);
                File.Copy(srcPath, Path.Combine(skillDestDir, "SKILL.md"), overwrite: true);

                count++;
            }

            return count;
        }
    }
}
