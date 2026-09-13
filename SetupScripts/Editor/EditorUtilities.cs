using UnityEditor;

namespace BestPractices.Setup
{
    /// <summary>
    /// Shared editor utilities used by the setup scripts.
    /// </summary>
    public static class EditorUtilities
    {
        /// <summary>
        /// Creates a subfolder inside the given parent path if it does not already exist.
        /// Shared utility used by other generator classes.
        /// </summary>
        /// <param name="parent">The parent folder path (e.g. <c>"Assets/_ProjectName"</c>).</param>
        /// <param name="folderName">The name of the subfolder to create.</param>
        public static void CreateFolder(string parent, string folderName)
        {
            string path = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
