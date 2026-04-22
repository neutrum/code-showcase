// XRPerfInit.cs
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR.OpenXR.Features.Meta;

public class CTWQuestQuickPreset : MonoBehaviour
{
    [Header("Targets")]
    public int targetRefreshHz = 90;          // 72 or 90
    [Range(0.6f, 1.5f)] public float eyeTexScale = 0.9f;

    [Header("Foveation")]
    public FoveationLevel foveation = FoveationLevel.Medium; // Off/Low/Medium/High/HighTop
    public bool dynamicFoveation = true;      // let runtime raise/lower up to your level

    void Awake()
    {
        // Basic pacing
        Application.targetFrameRate = targetRefreshHz;
        XRSettings.eyeTextureResolutionScale = eyeTexScale;

        // Try OpenXR display first (Unity route)
        TryOpenXRFoveationAndRefresh();

        // Then try Meta OVRPlugin (Meta route) if present
        TryOVRPluginFoveation();
    }

    void TryOpenXRFoveationAndRefresh()
    {
        var displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        if (displays.Count == 0) return;

        var disp = displays[0];

        // Request refresh rate (ignored if unsupported)
        try { disp.TryRequestDisplayRefreshRate(targetRefreshHz); } catch { }

        // Some Unity versions expose a float 0..1 property named "foveatedRenderingLevel".
        // Use reflection so this compiles across versions.
        var prop = disp.GetType().GetProperty("foveatedRenderingLevel", BindingFlags.Public | BindingFlags.Instance);
        if (prop != null && prop.CanWrite)
        {
            float level01 = foveation switch
            {
                FoveationLevel.Off     => 0f,
                FoveationLevel.Low     => 0.25f,
                FoveationLevel.Medium  => 0.5f,
                FoveationLevel.High    => 0.75f,
                FoveationLevel.HighTop => 1f,
                _ => 0.5f
            };
            try { prop.SetValue(disp, level01); Debug.Log($"[XRPerfInit] OpenXR foveation ≈ {foveation}"); } catch { }
        }
    }

    void TryOVRPluginFoveation()
    {
        // If Meta Core SDK is in the project, use OVRPlugin.* (new API that replaced OVRManager.fixedFoveatedRenderingLevel)
        var ovrType = GetTypeByName("OVRPlugin");
        if (ovrType == null) return;

        var enumType = ovrType.GetNestedType("FoveatedRenderingLevel");
        var levelProp = ovrType.GetProperty("foveatedRenderingLevel", BindingFlags.Public | BindingFlags.Static);
        var dynProp   = ovrType.GetProperty("useDynamicFoveatedRendering", BindingFlags.Public | BindingFlags.Static);

        if (enumType != null && levelProp != null)
        {
            object level = Enum.Parse(enumType, foveation.ToString());   // names match: Off/Low/Medium/High/HighTop
            try { levelProp.SetValue(null, level); Debug.Log($"[XRPerfInit] OVRPlugin foveation = {foveation}"); } catch { }
        }
        if (dynProp != null)
        {
            try { dynProp.SetValue(null, dynamicFoveation); } catch { }
        }
    }

    static Type GetTypeByName(string fullName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try { foreach (var t in asm.GetTypes()) if (t.FullName == fullName) return t; }
            catch { }
        }
        return null;
    }

    public enum FoveationLevel { Off, Low, Medium, High, HighTop }
}
