using UnityEditor;
using UnityEngine;

namespace Unity.BestPractices.Editor
{
    /// <summary>
    /// Reads a <see cref="ProjectTagsAndLayers"/> config and registers its tags, sorting layers,
    /// and physics layers into the project's <c>TagManager.asset</c>, preserving existing entries.
    /// Accessible via <b>Tools → Unity Project Configurator → Setup → Setup Tags and Layers</b>.
    /// </summary>
    /// <remarks>
    /// If no config asset exists in the project, the package defaults
    /// (the field initialisers on <see cref="ProjectTagsAndLayers"/>) are used.
    /// </remarks>
    public static class SetupTagsAndLayers
    {
        private const string TagManagerAssetPath = "ProjectSettings/TagManager.asset";

        [MenuItem("Tools/Unity Project Configurator/Setup/Setup Tags and Layers", false, 53)]
        public static void Execute()
        {
            ProjectTagsAndLayers config = LoadConfig();

            var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath(TagManagerAssetPath);
            if (tagManagerAssets.Length == 0 || tagManagerAssets[0] == null)
            {
                Debug.LogWarning($"[Best Practices] Could not load {TagManagerAssetPath}");
                return;
            }

            var so = new SerializedObject(tagManagerAssets[0]);
            so.UpdateIfRequiredOrScript();

            int tagsAdded = AddTags(so, config.tags);
            int sortingAdded = AddSortingLayers(so, config.sortingLayers);
            int layersAdded = AddPhysicsLayers(so, config.physicsLayers);

            if (tagsAdded + sortingAdded + layersAdded > 0)
            {
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[Best Practices] Tags & Layers setup complete. " +
                      $"Tags: {tagsAdded} added, Sorting Layers: {sortingAdded} added, Physics Layers: {layersAdded} added.");
        }

        /// <summary>
        /// Returns the first <see cref="ProjectTagsAndLayers"/> config asset found in the project,
        /// or a default instance when none exists.
        /// </summary>
        private static ProjectTagsAndLayers LoadConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:ProjectTagsAndLayers");
            if (guids.Length > 0)
            {
                var config = AssetDatabase.LoadAssetAtPath<ProjectTagsAndLayers>(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (config != null)
                    return config;
            }

            // No config asset in the project — use the package defaults.
            return ScriptableObject.CreateInstance<ProjectTagsAndLayers>();
        }

        private static int AddTags(SerializedObject so, string[] tags)
        {
            var prop = so.FindProperty("m_Tags");
            if (prop == null || tags == null)
                return 0;

            int added = 0;
            foreach (string tag in tags)
            {
                if (string.IsNullOrEmpty(tag))
                    continue;
                if (ContainsString(prop, tag))
                    continue;

                prop.InsertArrayElementAtIndex(prop.arraySize);
                prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = tag;
                added++;
            }

            return added;
        }

        private static int AddSortingLayers(SerializedObject so, string[] sortingLayers)
        {
            var prop = so.FindProperty("m_SortingLayers");
            if (prop == null || sortingLayers == null)
                return 0;

            int added = 0;
            foreach (string layerName in sortingLayers)
            {
                if (string.IsNullOrEmpty(layerName))
                    continue;
                if (ContainsSortingLayer(prop, layerName))
                    continue;

                prop.InsertArrayElementAtIndex(prop.arraySize);
                SerializedProperty newLayer = prop.GetArrayElementAtIndex(prop.arraySize - 1);
                newLayer.FindPropertyRelative("name").stringValue = layerName;
                newLayer.FindPropertyRelative("uniqueID").intValue = layerName.GetHashCode();
                added++;
            }

            return added;
        }

        private static int AddPhysicsLayers(SerializedObject so, ProjectTagsAndLayers.PhysicsLayerEntry[] physicsLayers)
        {
            var prop = so.FindProperty("m_LayerNames");
            if (prop == null || physicsLayers == null)
                return 0;

            int added = 0;
            foreach (var entry in physicsLayers)
            {
                if (entry.layerIndex < 0 || entry.layerIndex >= prop.arraySize)
                    continue;
                if (string.IsNullOrEmpty(entry.layerName))
                    continue;

                SerializedProperty slot = prop.GetArrayElementAtIndex(entry.layerIndex);
                if (!string.IsNullOrEmpty(slot.stringValue))
                {
                    Debug.LogWarning($"[Best Practices] Physics layer slot {entry.layerIndex} is already occupied by " +
                                     $"'{slot.stringValue}' — skipping '{entry.layerName}'. Free the slot first if you want to use it.");
                    continue;
                }

                slot.stringValue = entry.layerName;
                added++;
            }

            return added;
        }

        private static bool ContainsString(SerializedProperty array, string value)
        {
            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).stringValue == value)
                    return true;
            }
            return false;
        }

        private static bool ContainsSortingLayer(SerializedProperty array, string layerName)
        {
            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == layerName)
                    return true;
            }
            return false;
        }
    }
}