using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.OpenXR;

/// <summary>
/// Dropped into a scene to emit startup diagnostics.
/// Stripped from Release builds — only active in Editor and Development Builds.
/// </summary>
public class CTWLogger : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Start()
    {
        DumpSceneHierarchy();
        LogASW();
    }

    private void DumpSceneHierarchy()
    {
        Debug.Log("=== Scene Hierarchy Start ===");
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            DumpHierarchy(root.transform, "");
        Debug.Log("=== Scene Hierarchy End ===");
    }

    private void DumpHierarchy(Transform current, string indent)
    {
        Debug.Log($"{indent}- {current.name}");
        foreach (Transform child in current)
            DumpHierarchy(child, indent + "  ");
    }

    private void LogASW()
    {
        var features = OpenXRSettings.Instance.GetFeatures();
        var spaceWarp = features.FirstOrDefault(f => f.GetType().Name.Contains("SpaceWarpFeature"));
        if (spaceWarp != null && spaceWarp.enabled)
            Debug.Log("SpaceWarpFeature is enabled.");
        else
        {
            Debug.LogWarning("SpaceWarpFeature is missing or disabled.");
            OVRManager.SetSpaceWarp(true);
        }
    }
#endif
}

/// <summary>
/// Conditional verbose logging. Call sites are removed from Release builds by the compiler.
/// Use for informational traces; keep Debug.LogWarning / Debug.LogError for real issues.
/// </summary>
public static class CTWLog
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Verbose(string message) => Debug.Log(message);

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Verbose(string message, Object context) => Debug.Log(message, context);
}
