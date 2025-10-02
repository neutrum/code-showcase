using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
#if UNITY_XR_MANAGEMENT
using UnityEngine.XR.Management;
#endif
#if UNITY_RENDER_PIPELINE_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

public class CTWInitXR : MonoBehaviour
{
    [Header("Assign your XR rig camera (center-eye). If empty, uses Camera.main after activation.")]
    public Camera xrCamera;
    [Tooltip("Cameras you keep (e.g., your church camera). XR will be OFF on them, but they stay enabled.")]
    public Camera[] nonXRCameras;

    [Header("Options")]
    public bool setThisSceneActive = true;
    public bool tagXrAsMain = true;
    public bool forceXrCameraBase = true;    // URP: ensure Base (not Overlay)
    public bool ensureSkyboxOnXR = true;
    public bool turnOffXRonNonXRCams = true; // keep them enabled, just no XR

    IEnumerator Start()
    {
        // 1) Ensure XR subsystems running (safe even if already started)
#if UNITY_XR_MANAGEMENT
        var mgr = XRGeneralSettings.Instance ? XRGeneralSettings.Instance.Manager : null;
        if (mgr != null && mgr.activeLoader == null)
        {
            yield return mgr.InitializeLoader();
            if (mgr.activeLoader != null) mgr.StartSubsystems();
        }
#endif
        yield return null; // let subsystems spin up

        // 2) Make THIS additive scene active (input/event routing depends on it)
        if (setThisSceneActive)
            SceneManager.SetActiveScene(gameObject.scene);

        // 3) Lock down the XR camera
        var cam = xrCamera ? xrCamera : Camera.main;
        if (!cam)
        {
            Debug.LogError("[InitXR] No XR camera found (assign xrCamera or tag your rig camera MainCamera).");
        }
        else
        {
            if (tagXrAsMain) cam.tag = "MainCamera";
#if UNITY_RENDER_PIPELINE_UNIVERSAL
            if (forceXrCameraBase)
            {
                var data = cam.GetComponent<UniversalAdditionalCameraData>();
                if (data && data.renderType == CameraRenderType.Overlay)
                    data.renderType = CameraRenderType.Base;
            }
#endif
            if (ensureSkyboxOnXR && cam.clearFlags == CameraClearFlags.Nothing)
                cam.clearFlags = CameraClearFlags.Skybox;
        }

        // 4) Keep your extra cameras ON, just ensure they don’t render via XR or steal "Main"
        if (nonXRCameras != null)
        {
            foreach (var c in nonXRCameras)
            {
                if (!c || c == cam) continue;
                if (c.CompareTag("MainCamera")) c.tag = "Untagged";
#if UNITY_RENDER_PIPELINE_UNIVERSAL
                if (turnOffXRonNonXRCams)
                {
                    var d = c.GetComponent<UniversalAdditionalCameraData>();
                    if (d) d.allowXRRendering = false; // stays enabled, just not XR
                }
#endif
                // avoid duplicate listeners
                if (cam && cam.GetComponent<AudioListener>() && c.TryGetComponent<AudioListener>(out var al))
                    Destroy(al);
            }
        }

        // 5) Print XR health so you know what's wrong if still stuck
        var disp = new List<XRDisplaySubsystem>(); var input = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(disp);  SubsystemManager.GetSubsystems(input);
        Debug.Log($"[InitXR] XRDisplay running={(disp.Count>0 && disp[0].running)}  XRInput running={(input.Count>0 && input[0].running)}  ActiveScene={SceneManager.GetActiveScene().name}");

        var devices = new List<InputDevice>(); InputDevices.GetDevices(devices);
        foreach (var d in devices) Debug.Log($"[InitXR] Device: {d.name}  {d.characteristics}");
    }
}
