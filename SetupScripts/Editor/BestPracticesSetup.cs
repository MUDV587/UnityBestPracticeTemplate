using UnityEngine;

namespace BestPractices.Setup
{
    /// <summary>
    /// Headless entry point for the setup pipeline. Invoked via
    /// <c>unity run &lt;project&gt; -- -executeMethod BestPractices.Setup.BestPracticesSetup.Run</c>
    /// by <c>setup-project.sh</c>, after it has staged this folder's scripts, the config JSON,
    /// Presets, and ScriptTemplates into the target project.
    /// </summary>
    /// <remarks>
    /// Reproduces, in the same order, the setup steps formerly run one-at-a-time from the
    /// New Project Wizard's "Run Full Setup" button.
    /// </remarks>
    public static class BestPracticesSetup
    {
        public static void Run()
        {
            SetupConfig config = SetupConfig.Load();

            SetupProjectFolders.Execute(config.projectName);
            GenerateAssemblyDefinitions.Execute();
            ConfigurePresets.ApplyAllPresets();
            SetupTagsAndLayers.Execute();
            GenerateGitIgnore.Execute();
            GenerateGitAttributes.Execute();
            GenerateEditorConfig.Execute();
            ConfigureProjectSettings.ApplySettings();
            ConfigureIterationSettings.ApplyScriptChanges();
            ConfigureIterationSettings.ApplyAsyncShadersOn();
            ConfigureIterationSettings.ApplyManagedStrippingDev();
            ConfigureAssetPipeline.ApplyAutoRefreshRecommended();
            ConfigureAssetPipeline.ApplyImportWorkerCountRecommended();
            ConfigureAssetPipeline.ApplyCompressTextures();

            if (config.fixVSCodeSlnx)
                FixVSCodeSlnx.Execute();

            Debug.Log("[BestPractices] Full project setup complete.");
        }
    }
}
