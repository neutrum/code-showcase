using UnityEditor;
using UnityEngine;
using System.IO;

public static class FindMetaMaterials
{
    [MenuItem("Tools/Find Meta Shaders")]
    static void FindBrokenMetaShaders()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int found = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string text = File.ReadAllText(path);

            if (text.Contains("Meta/")) // look directly in YAML
            {
                Debug.Log($"🧱 Meta shader reference found: {path}");
                found++;
            }
        }

        Debug.Log($"✅ Found {found} materials using Meta shaders.");
    }
}
