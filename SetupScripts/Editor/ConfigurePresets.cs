using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace BestPractices.Setup
{
    public static class ConfigurePresets
    {
        private const string PresetsPath = "Assets/Settings/Presets";
        private const string PresetManagerAssetPath = "ProjectSettings/PresetManager.asset";

        // ── Preset asset paths ────────────────────────────────────────────────

        private static readonly string s_ambiencePath = $"{PresetsPath}/Audio/AmbienceAudioImporter.preset";
        private static readonly string s_musicPath = $"{PresetsPath}/Audio/MusicAudioImporter.preset";
        private static readonly string s_sFXPath = $"{PresetsPath}/Audio/SFXAudioImporter.preset";
        private static readonly string s_uIAudioPath = $"{PresetsPath}/Audio/UIAudioImporter.preset";

        private static readonly string s_uISpritePath = $"{PresetsPath}/Textures/UISpriteTextureImporter.preset";
        private static readonly string s_albedoPath = $"{PresetsPath}/Textures/AlbedoTextureImporter.preset";
        private static readonly string s_normalPath = $"{PresetsPath}/Textures/NormalTextureImporter.preset";
        private static readonly string s_maskPath = $"{PresetsPath}/Textures/MaskTextureImporter.preset";

        private static readonly string s_fBXModelPath = $"{PresetsPath}/Models/FBXModelImporter.preset";
        private static readonly string s_fBXAnimPath = $"{PresetsPath}/Animations/FBXAnimationImporter.preset";

        // ── Folder filters ────────────────────────────────────────────────────

        private const string AmbienceFilter = "glob:\"Assets/**/Audio/Ambience/**\"";
        private const string MusicFilter = "glob:\"Assets/**/Audio/Music/**\"";
        private const string SFXFilter = "glob:\"Assets/**/Audio/SFX/**\"";
        private const string UIAudioFilter = "glob:\"Assets/**/Audio/UI/**\"";

        private const string UISpriteFilter = "glob:\"Assets/**/Textures/UI/**\"";
        private const string AlbedoFilter = "glob:\"Assets/**/Textures/Environment/Albedo/**\"";
        private const string NormalFilter = "glob:\"Assets/**/Textures/Environment/Normal/**\"";
        private const string MaskFilter = "glob:\"Assets/**/Textures/Environment/Masks/**\"";

        private const string FBXModelFilter = "glob:\"Assets/**/Art/Models/**\"";
        private const string FBXAnimFilter = "glob:\"Assets/**/Art/Animations/**\"";

        // ── Status checks ────────────────────────────────────────────────────

        public static bool IsAmbienceConfigured => IsPresetRegistered(s_ambiencePath, AmbienceFilter);
        public static bool IsMusicConfigured => IsPresetRegistered(s_musicPath, MusicFilter);
        public static bool IsSFXConfigured => IsPresetRegistered(s_sFXPath, SFXFilter);
        public static bool IsUIAudioConfigured => IsPresetRegistered(s_uIAudioPath, UIAudioFilter);

        public static bool IsUISpriteConfigured => IsPresetRegistered(s_uISpritePath, UISpriteFilter);
        public static bool IsAlbedoConfigured => IsPresetRegistered(s_albedoPath, AlbedoFilter);
        public static bool IsNormalConfigured => IsPresetRegistered(s_normalPath, NormalFilter);
        public static bool IsMaskConfigured => IsPresetRegistered(s_maskPath, MaskFilter);

        public static bool IsFBXModelConfigured => IsPresetRegistered(s_fBXModelPath, FBXModelFilter);
        public static bool IsFBXAnimationConfigured => IsPresetRegistered(s_fBXAnimPath, FBXAnimFilter);

        // ── Individual apply methods ──────────────────────────────────────────

        public static void ApplyAmbience() => ApplySingle(s_ambiencePath, AmbienceFilter);
        public static void ApplyMusic() => ApplySingle(s_musicPath, MusicFilter);
        public static void ApplySFX() => ApplySingle(s_sFXPath, SFXFilter);
        public static void ApplyUIAudio() => ApplySingle(s_uIAudioPath, UIAudioFilter);

        public static void ApplyUISprite() => ApplySingle(s_uISpritePath, UISpriteFilter);
        public static void ApplyAlbedo() => ApplySingle(s_albedoPath, AlbedoFilter);
        public static void ApplyNormal() => ApplySingle(s_normalPath, NormalFilter);
        public static void ApplyMask() => ApplySingle(s_maskPath, MaskFilter);

        public static void ApplyFBXModel() => ApplySingle(s_fBXModelPath, FBXModelFilter);
        public static void ApplyFBXAnimation() => ApplySingle(s_fBXAnimPath, FBXAnimFilter);

        // ── Bulk apply (used by New Project Wizard RunFullSetup) ──────────────

        public static void ApplyAllPresets()
        {
            if (!TryGetDefaultPresets(out SerializedObject pmSO, out SerializedProperty defaultList))
                return;

            int addedCount = 0;
            addedCount += RegisterPreset(defaultList, s_ambiencePath, AmbienceFilter);
            addedCount += RegisterPreset(defaultList, s_musicPath, MusicFilter);
            addedCount += RegisterPreset(defaultList, s_sFXPath, SFXFilter);
            addedCount += RegisterPreset(defaultList, s_uIAudioPath, UIAudioFilter);
            addedCount += RegisterPreset(defaultList, s_uISpritePath, UISpriteFilter);
            addedCount += RegisterPreset(defaultList, s_albedoPath, AlbedoFilter);
            addedCount += RegisterPreset(defaultList, s_normalPath, NormalFilter);
            addedCount += RegisterPreset(defaultList, s_maskPath, MaskFilter);
            addedCount += RegisterPreset(defaultList, s_fBXModelPath, FBXModelFilter);
            addedCount += RegisterPreset(defaultList, s_fBXAnimPath, FBXAnimFilter);

            if (addedCount > 0)
            {
                pmSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(pmSO.targetObject);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Best Practices] Added {addedCount} preset entries. See Edit > Project Settings > Preset Manager.");
            }
            else
            {
                Debug.Log("[Best Practices] All preset entries were already registered. Nothing added.");
            }
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        private static void ApplySingle(string presetPath, string filter)
        {
            if (!TryGetDefaultPresets(out SerializedObject pmSO, out SerializedProperty defaultList))
                return;

            int added = RegisterPreset(defaultList, presetPath, filter);
            if (added > 0)
            {
                pmSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(pmSO.targetObject);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Best Practices] Registered preset: {System.IO.Path.GetFileNameWithoutExtension(presetPath)}");
            }
            else
            {
                Debug.Log($"[Best Practices] Preset already registered: {System.IO.Path.GetFileNameWithoutExtension(presetPath)}");
            }
        }

        public static bool IsPresetRegistered(string presetPath, string folderFilter)
        {
            if (!TryGetDefaultPresets(out _, out SerializedProperty defaultList))
                return false;

            Preset preset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);
            if (preset == null) return false;

            // Match by preset object reference so the status reflects any registration,
            // regardless of which filter path was used when the entry was created.
            for (int i = 0; i < defaultList.arraySize; i++)
            {
                SerializedProperty entry = defaultList.GetArrayElementAtIndex(i);
                SerializedProperty innerArray = entry.FindPropertyRelative("second");
                if (innerArray == null) continue;

                for (int j = 0; j < innerArray.arraySize; j++)
                {
                    SerializedProperty presetProp = innerArray.GetArrayElementAtIndex(j).FindPropertyRelative("m_Preset");
                    if (presetProp != null && presetProp.objectReferenceValue == preset)
                        return true;
                }
            }
            return false;
        }

        private static bool TryGetDefaultPresets(out SerializedObject pmSO, out SerializedProperty defaultList)
        {
            pmSO = null;
            defaultList = null;

            UnityEngine.Object[] pmAssets = AssetDatabase.LoadAllAssetsAtPath(PresetManagerAssetPath);
            if (pmAssets.Length == 0 || pmAssets[0] == null)
            {
                Debug.LogWarning($"[Best Practices] Could not load {PresetManagerAssetPath}");
                return false;
            }

            pmSO = new SerializedObject(pmAssets[0]);
            defaultList = pmSO.FindProperty("m_DefaultPresets");
            if (defaultList == null)
            {
                Debug.LogWarning("[Best Practices] Could not find 'm_DefaultPresets' in PresetManager.asset");
                return false;
            }

            return true;
        }

        // Returns 1 if a new entry was added, 0 if skipped or failed.
        private static int RegisterPreset(SerializedProperty defaultList, string presetPath, string folderFilter)
        {
            Preset preset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);
            if (preset == null)
            {
                Debug.LogWarning($"[Best Practices] Preset not found: {presetPath}");
                return 0;
            }

            // Read the importer native type ID from the preset's own serialized data.
            SerializedObject presetSO = new SerializedObject(preset);
            SerializedProperty nativeTypeIDProp = presetSO.FindProperty("m_TargetType.m_NativeTypeID");
            if (nativeTypeIDProp == null)
            {
                Debug.LogWarning($"[Best Practices] Could not read native type ID from: {presetPath}");
                return 0;
            }
            int nativeTypeID = nativeTypeIDProp.intValue;

            // Find the outer group entry whose 'first.m_NativeTypeID' matches this importer type.
            int typeIndex = -1;
            for (int i = 0; i < defaultList.arraySize; i++)
            {
                SerializedProperty entry = defaultList.GetArrayElementAtIndex(i);
                SerializedProperty typeID = entry.FindPropertyRelative("first.m_NativeTypeID");
                if (typeID != null && typeID.intValue == nativeTypeID)
                {
                    typeIndex = i;
                    break;
                }
            }

            // No group for this importer type yet — create one.
            if (typeIndex == -1)
            {
                defaultList.InsertArrayElementAtIndex(defaultList.arraySize);
                typeIndex = defaultList.arraySize - 1;
                SerializedProperty newGroup = defaultList.GetArrayElementAtIndex(typeIndex);

                SerializedProperty newTypeID = newGroup.FindPropertyRelative("first.m_NativeTypeID");
                SerializedProperty newInner = newGroup.FindPropertyRelative("second");

                if (newTypeID == null || newInner == null)
                {
                    defaultList.DeleteArrayElementAtIndex(typeIndex);
                    Debug.LogWarning($"[Best Practices] Could not create type group for: {presetPath}");
                    return 0;
                }

                newTypeID.intValue = nativeTypeID;
                newInner.ClearArray();
            }

            // Get the inner 'second' array for this type group.
            SerializedProperty group = defaultList.GetArrayElementAtIndex(typeIndex);
            SerializedProperty innerArray = group.FindPropertyRelative("second");

            // Skip if this preset asset is already registered (regardless of filter path).
            for (int i = 0; i < innerArray.arraySize; i++)
            {
                SerializedProperty innerEntry = innerArray.GetArrayElementAtIndex(i);
                SerializedProperty presetRef = innerEntry.FindPropertyRelative("m_Preset");
                if (presetRef != null && presetRef.objectReferenceValue == preset)
                    return 0;
            }

            // Append new entry to the inner array.
            innerArray.InsertArrayElementAtIndex(innerArray.arraySize);
            SerializedProperty newEntry = innerArray.GetArrayElementAtIndex(innerArray.arraySize - 1);
            SerializedProperty newFilter = newEntry.FindPropertyRelative("m_Filter");
            SerializedProperty newPreset = newEntry.FindPropertyRelative("m_Preset");

            if (newFilter == null || newPreset == null)
            {
                innerArray.DeleteArrayElementAtIndex(innerArray.arraySize - 1);
                Debug.LogWarning($"[Best Practices] Inner entry missing m_Filter/m_Preset for: {presetPath}");
                return 0;
            }

            newFilter.stringValue = folderFilter;
            newPreset.objectReferenceValue = preset;
            return 1;
        }
    }
}
