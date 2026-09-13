using UnityEditor;
using UnityEngine;

namespace BestPractices.Setup
{
    /// <summary>
    /// Registers a standard set of tags, sorting layers, and physics layers into
    /// <c>ProjectSettings/TagManager.asset</c>. Existing entries are preserved — only missing
    /// entries are added.
    /// </summary>
    /// <remarks>
    /// Called by <see cref="BestPracticesSetup"/> as part of the setup pipeline. Layer slots 0–7
    /// are reserved by Unity (Default, TransparentFX, Ignore Raycast, Water, UI, and three unused
    /// built-in slots) — user layers occupy slots 8–31.
    /// </remarks>
    public static class SetupTagsAndLayers
    {
        private static readonly string[] k_DefaultTags =
        {
            "Player", "Enemy", "NPC", "Projectile", "Pickup",
            "Interactable", "Checkpoint", "SpawnPoint", "Trigger", "MainCamera"
        };

        private static readonly string[] k_DefaultSortingLayers =
        {
            "Background", "Environment", "Props", "Characters", "Foreground", "UI", "Overlay"
        };

        private struct PhysicsLayerEntry
        {
            public int layerIndex;
            public string layerName;
        }

        private static readonly PhysicsLayerEntry[] k_DefaultPhysicsLayers =
        {
            new PhysicsLayerEntry { layerIndex = 8,  layerName = "Player" },
            new PhysicsLayerEntry { layerIndex = 9,  layerName = "Enemy" },
            new PhysicsLayerEntry { layerIndex = 10, layerName = "NPC" },
            new PhysicsLayerEntry { layerIndex = 11, layerName = "Projectile" },
            new PhysicsLayerEntry { layerIndex = 12, layerName = "Pickup" },
            new PhysicsLayerEntry { layerIndex = 13, layerName = "Interactable" },
            new PhysicsLayerEntry { layerIndex = 14, layerName = "Ground" },
            new PhysicsLayerEntry { layerIndex = 15, layerName = "Environment" },
            new PhysicsLayerEntry { layerIndex = 16, layerName = "Trigger" },
            new PhysicsLayerEntry { layerIndex = 17, layerName = "Ragdoll" },
        };

        /// <summary>
        /// Applies the default tags, sorting layers, and physics layers. Existing entries with
        /// the same name are left untouched; an occupied physics layer slot is skipped with a
        /// warning rather than overwritten.
        /// </summary>
        public static void Execute()
        {
            var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets.Length == 0)
            {
                Debug.LogError("[BestPractice] Could not load ProjectSettings/TagManager.asset");
                return;
            }

            var so = new SerializedObject(tagManagerAssets[0]);
            so.UpdateIfRequiredOrScript();

            int tagsAdded = ApplyTags(so);
            int sortingLayersAdded = ApplySortingLayers(so);
            int physicsLayersAdded = ApplyPhysicsLayers(so);

            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Debug.Log($"[BestPractice] Tags and Layers setup complete — " +
                $"{tagsAdded} tag(s), {sortingLayersAdded} sorting layer(s), {physicsLayersAdded} physics layer(s) added.");
        }

        private static int ApplyTags(SerializedObject so)
        {
            var tagsProp = so.FindProperty("tags");
            if (tagsProp == null)
            {
                Debug.LogWarning("[BestPractice] Could not find 'tags' in TagManager.asset");
                return 0;
            }

            int added = 0;
            foreach (string tag in k_DefaultTags)
            {
                if (TagExists(tagsProp, tag)) continue;

                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                added++;
            }
            return added;
        }

        private static bool TagExists(SerializedProperty tagsProp, string tag)
        {
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                    return true;
            }
            return false;
        }

        private static int ApplySortingLayers(SerializedObject so)
        {
            var sortingLayersProp = so.FindProperty("m_SortingLayers");
            if (sortingLayersProp == null)
            {
                Debug.LogWarning("[BestPractice] Could not find 'm_SortingLayers' in TagManager.asset");
                return 0;
            }

            int maxUniqueID = 0;
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                var idProp = sortingLayersProp.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID");
                if (idProp != null && idProp.intValue > maxUniqueID)
                    maxUniqueID = idProp.intValue;
            }

            int added = 0;
            foreach (string layerName in k_DefaultSortingLayers)
            {
                if (SortingLayerExists(sortingLayersProp, layerName)) continue;

                sortingLayersProp.InsertArrayElementAtIndex(sortingLayersProp.arraySize);
                var entry = sortingLayersProp.GetArrayElementAtIndex(sortingLayersProp.arraySize - 1);
                entry.FindPropertyRelative("name").stringValue = layerName;
                entry.FindPropertyRelative("uniqueID").intValue = ++maxUniqueID;
                var lockedProp = entry.FindPropertyRelative("locked");
                if (lockedProp != null) lockedProp.boolValue = false;
                added++;
            }
            return added;
        }

        private static bool SortingLayerExists(SerializedProperty sortingLayersProp, string layerName)
        {
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                var nameProp = sortingLayersProp.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                if (nameProp != null && nameProp.stringValue == layerName)
                    return true;
            }
            return false;
        }

        private static int ApplyPhysicsLayers(SerializedObject so)
        {
            var layersProp = so.FindProperty("layers");
            if (layersProp == null)
            {
                Debug.LogWarning("[BestPractice] Could not find 'layers' in TagManager.asset");
                return 0;
            }

            int added = 0;
            foreach (var entry in k_DefaultPhysicsLayers)
            {
                if (entry.layerIndex < 8 || entry.layerIndex >= layersProp.arraySize)
                {
                    Debug.LogWarning($"[BestPractice] Physics layer index {entry.layerIndex} is out of the user-definable range (8-31) — skipped.");
                    continue;
                }

                var slot = layersProp.GetArrayElementAtIndex(entry.layerIndex);
                if (!string.IsNullOrEmpty(slot.stringValue))
                {
                    if (slot.stringValue != entry.layerName)
                        Debug.LogWarning($"[BestPractice] Physics layer slot {entry.layerIndex} already holds '{slot.stringValue}' — skipped '{entry.layerName}'.");
                    continue;
                }

                slot.stringValue = entry.layerName;
                added++;
            }
            return added;
        }
    }
}
