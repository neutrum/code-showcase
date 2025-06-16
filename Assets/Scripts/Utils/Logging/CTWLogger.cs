using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.OpenXR;

public class CTWLogger : MonoBehaviour
{
    void Start()
    {
        DumpSceneHierarchy();
        LogASW();
    }

    void DumpSceneHierarchy()
    {
        Debug.Log("=== Scene Hierarchy Start ===");
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            DumpHierarchy(root.transform, "");
        }

        Debug.Log("=== Scene Hierarchy End ===");
    }

    void DumpHierarchy(Transform current, string indent)
    {
        Debug.Log($"{indent}- {current.name}");

        foreach (Transform child in current)
        {
            DumpHierarchy(child, indent + "  ");
        }
    }

    void LogASW()
    {
        var features = OpenXRSettings.Instance.GetFeatures();
        var spaceWarpFeature = features.FirstOrDefault(f => f.GetType().Name.Contains("SpaceWarpFeature"));

        if (spaceWarpFeature != null && spaceWarpFeature.enabled)
        {
            Debug.Log("SpaceWarpFeature is enabled.");
        }
        else
        {
            Debug.LogWarning("SpaceWarpFeature is missing or disabled.");
            OVRManager.SetSpaceWarp(true);
        }
    }
}

