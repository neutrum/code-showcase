using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.XR.Management;

#if UNITY_EDITOR
// This script runs only in the Editor to prevent XR crashes when restarting Play Mode.
[InitializeOnLoad]
public static class XRLoaderReset
{
    static XRLoaderReset()
    {
        // Hook into the PlayMode change events
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
    {
        if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
        {
            // Before Play Mode exits, force XR to shut down cleanly
            if (XRGeneralSettings.Instance != null &&
                XRGeneralSettings.Instance.Manager != null)
            {
                XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            }
            Debug.Log("Deinitializing XR Loader...");
        }

        if (state == UnityEditor.PlayModeStateChange.EnteredPlayMode)
        {
            // After entering Play Mode, reinitialize XR
            if (XRGeneralSettings.Instance != null &&
                XRGeneralSettings.Instance.Manager != null)
            {
                XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
                XRGeneralSettings.Instance.Manager.StartSubsystems();
            }
            Debug.Log("Reinitializing XR Loader...");
        }
    }
}
#endif
