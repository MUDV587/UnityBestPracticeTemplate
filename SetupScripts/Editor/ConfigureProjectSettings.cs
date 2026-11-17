using UnityEditor;
using UnityEditor.Build;
using UnityEditor.VersionControl;
using UnityEngine;

namespace BestPractices.Setup
{
    /// <summary>
    /// Applies recommended Unity 6 project settings — either all at once or individually.
    /// Call <see cref="ApplySettings"/> to apply everything non-interactively; called by
    /// <see cref="BestPracticesSetup"/> as part of the setup pipeline.
    /// </summary>
    public static class ConfigureProjectSettings
    {
        // ── Status checks ────────────────────────────────────────────────────

        public static bool IsEnterPlayModeConfigured =>
            EditorSettings.enterPlayModeOptionsEnabled &&
            EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload) &&
            EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableSceneReload);

        public static bool IsApiCompatibilityConfigured =>
            PlayerSettings.GetApiCompatibilityLevel(NamedBuildTarget.Standalone) == ApiCompatibilityLevel.NET_Standard;

        public static bool IsAssetSerializationConfigured =>
            EditorSettings.serializationMode == SerializationMode.ForceText;

        public static bool IsVersionControlConfigured =>
            VersionControlSettings.mode == "Visible Meta Files";

        /// <summary>True when the Active Input Handling is set to the new Input System package (or Both).</summary>
        public static bool IsInputSystemConfigured
        {
            get
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
                if (assets.Length == 0) return false;
                var so = new SerializedObject(assets[0]);
                var prop = so.FindProperty("activeInputHandler");
                // 0 = Input Manager (old), 1 = Input System Package (new), 2 = Both
                return prop != null && prop.intValue >= 1;
            }
        }

        public static bool IsIncrementalGCConfigured => PlayerSettings.gcIncremental;

        public static bool IsCreateObjectsAtOriginConfigured =>
            EditorPrefs.GetBool("Scene/CreateObjectsAtWorldOrigin", false);

        public static bool IsNewHierarchyWindowConfigured =>
            EditorPrefs.GetBool("Hierarchy/UseNewHierarchyWindow", false);

        const string AssetManagerImportLocationKey = "System.String::AM4U.defaultImportLocation";
        const string AssetManagerSubfolderKey = "System.Boolean::AM4U.isSubfolderCreationEnabled";
        const string ThirdPartyAssetsPath = "Assets/ThirdPartyAssets";

        public static bool IsAssetManagerImportLocationConfigured =>
            EditorPrefs.GetString(AssetManagerImportLocationKey, "Assets") == ThirdPartyAssetsPath &&
            EditorPrefs.GetBool(AssetManagerSubfolderKey, false);

        // ── Individual apply methods ──────────────────────────────────────────

        public static void ApplyEnterPlayMode()
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions =
                EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            Debug.Log("[Best Practices] Enter Play Mode options configured.");
        }

        public static void ApplyApiCompatibility()
        {
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
            AssetDatabase.SaveAssets();
            Debug.Log("[Best Practices] API Compatibility set to .NET Standard 2.1 for Standalone.");
        }

        public static void ApplyAssetSerialization()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            Debug.Log("[Best Practices] Asset Serialization set to Force Text.");
        }

        public static void ApplyVersionControl()
        {
            VersionControlSettings.mode = "Visible Meta Files";
            Debug.Log("[Best Practices] Version Control set to Visible Meta Files.");
        }

        public static void ApplyInputSystem()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            so.UpdateIfRequiredOrScript();
            var prop = so.FindProperty("activeInputHandler");
            if (prop != null)
            {
                prop.intValue = 1; // Input System Package (New)
                so.ApplyModifiedProperties();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Best Practices] Active Input Handling set to Input System Package (New). A Unity restart may be required.");
        }

        public static void ApplyIncrementalGC()
        {
            PlayerSettings.gcIncremental = true;
            AssetDatabase.SaveAssets();
            Debug.Log("[Best Practices] Incremental GC enabled.");
        }

        public static void ApplyCreateObjectsAtOrigin()
        {
            EditorPrefs.SetBool("Scene/CreateObjectsAtWorldOrigin", true);
            Debug.Log("[Best Practices] Create Objects at Origin enabled. New GameObjects will spawn at (0,0,0).");
        }

        public static void ApplyNewHierarchyWindow()
        {
            EditorPrefs.SetBool("Hierarchy/UseNewHierarchyWindow", true);
            Debug.Log("[Best Practices] New Hierarchy window enabled. Reopen the Hierarchy window to apply.");
        }

        public static void ApplyAssetManagerImportLocation()
        {
            if (!AssetDatabase.IsValidFolder(ThirdPartyAssetsPath))
                AssetDatabase.CreateFolder("Assets", "ThirdPartyAssets");
            EditorPrefs.SetString(AssetManagerImportLocationKey, ThirdPartyAssetsPath);
            EditorPrefs.SetBool(AssetManagerSubfolderKey, true);
            Debug.Log("[Best Practices] Asset Manager default import location set to Assets/ThirdPartyAssets with subfolder creation enabled.");
        }

        // ── Individual disable / revert methods ──────────────────────────────────

        public static void DisableEnterPlayMode()
        {
            EditorSettings.enterPlayModeOptionsEnabled = false;
            Debug.Log("[Best Practices] Enter Play Mode options disabled. Full domain reload will run on every Play mode entry.");
        }

        public static void DisableIncrementalGC()
        {
            PlayerSettings.gcIncremental = false;
            AssetDatabase.SaveAssets();
            Debug.Log("[Best Practices] Incremental GC disabled.");
        }

        public static void DisableCreateObjectsAtOrigin()
        {
            EditorPrefs.SetBool("Scene/CreateObjectsAtWorldOrigin", false);
            Debug.Log("[Best Practices] Create Objects at Origin disabled. New GameObjects will spawn at the Scene View camera position.");
        }

        public static void DisableNewHierarchyWindow()
        {
            EditorPrefs.SetBool("Hierarchy/UseNewHierarchyWindow", false);
            Debug.Log("[Best Practices] New Hierarchy window disabled. Reopen the Hierarchy window to apply.");
        }

        public static void ResetAssetManagerImportLocation()
        {
            EditorPrefs.SetString(AssetManagerImportLocationKey, "Assets");
            EditorPrefs.SetBool(AssetManagerSubfolderKey, false);
            Debug.Log("[Best Practices] Asset Manager import location reset to Assets.");
        }

        // ── Apply all (used by New Project Wizard) ────────────────────────────

        /// <summary>Applies all recommended settings without showing a confirmation dialog.</summary>
        public static void ApplySettings()
        {
            ApplyEnterPlayMode();
            ApplyApiCompatibility();
            ApplyAssetSerialization();
            ApplyVersionControl();
            ApplyInputSystem();
            ApplyIncrementalGC();
            ApplyCreateObjectsAtOrigin();
            ApplyNewHierarchyWindow();
            ApplyAssetManagerImportLocation();
            Debug.Log("[Best Practices] All project settings configured. Review them in Edit > Project Settings.");
        }
    }
}
