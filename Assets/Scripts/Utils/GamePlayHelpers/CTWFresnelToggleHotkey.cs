// CTWFresnelToggleHotkey.cs
// Unity 6 + Meta XR SDK 78+ (XR Simulator) safe version

using UnityEngine;
using UnityEngine.Rendering.Universal;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
#endif

[RequireComponent(typeof(Camera))]
public class CTWFresnelToggleHotkey : MonoBehaviour
{
    [Header("Path A — Camera stacking (recommended)")]
    public Camera overlayFresnelCamera;

    [Header("Path B — Renderer swap (single camera)")]
    public int defaultRendererIndex = 0;
    public int fresnelRendererIndex = 1;
    public bool forceStartInDefault = true;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    [Header("Input System")]
    public InputActionReference toggleActionRef;

    InputAction _action;
    bool simulatorActive;
#else
    public KeyCode legacyToggleKey = KeyCode.F;
#endif

    UniversalAdditionalCameraData camData;
    bool fresnelOn;

    void Awake()
    {
        camData = GetComponent<UniversalAdditionalCameraData>();

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        DetectSimulator();
#endif

        if (overlayFresnelCamera != null)
        {
            fresnelOn = overlayFresnelCamera.gameObject.activeSelf;
        }
        else if (camData != null && forceStartInDefault)
        {
            camData.SetRenderer(defaultRendererIndex);
            fresnelOn = false;
        }
    }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    void DetectSimulator()
    {
        // Meta XR Simulator devices contain "XRSimulator" or "XRSim"
        // Check XR HMD device description for simulator indicators
        var xrHMD = InputSystem.GetDevice<XRHMD>();
        simulatorActive = xrHMD != null && 
                         (xrHMD.description.deviceClass.Contains("XRSim") || 
                          xrHMD.description.deviceClass.Contains("Simulator"));

        Debug.Log($"[FresnelToggle] XR Simulator active = {simulatorActive}");
    }
#endif

    void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // 1) Resolve InputAction source
        _action = toggleActionRef ? toggleActionRef.action
                                  : new InputAction("ToggleFresnel", InputActionType.Button);

        // 2) Add simulator + keyboard bindings if missing
        if (_action.bindings.Count == 0)
        {
            // Always available
            _action.AddBinding("<Keyboard>/f");

            // XR Simulator primary button
            _action.AddBinding("<XRController>{RightHand}/primaryButton");
            _action.AddBinding("<XRController>{LeftHand}/primaryButton");

            // XR Simulator triggers
            _action.AddBinding("<XRController>{RightHand}/trigger");
            _action.AddBinding("<XRController>{LeftHand}/trigger");

            // Gamepad fallback
            _action.AddBinding("<Gamepad>/start");

            Debug.Log("[FresnelToggle] Added default simulator + keyboard bindings.");
        }

        // 3) Subscribe
        _action.performed += OnPerformed;
        _action.Enable();

        // 4) Required on Meta SDK 78+ (fixes suppressed keyboard)
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
#endif
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (_action != null)
        {
            _action.performed -= OnPerformed;
            if (!toggleActionRef) _action.Dispose();
            _action = null;
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    void OnPerformed(InputAction.CallbackContext ctx)
    {
        // In XR Simulator, trigger delivers continuous values: only accept "pressed"
        if (!ctx.performed) return;

        float value = ctx.ReadValue<float>();
        if (value > 0.5f || value == 1f) Toggle();
    }
#else
    void Update()
    {
        if (Input.GetKeyDown(legacyToggleKey))
            Toggle();
    }
#endif

    public void Toggle()
    {
        fresnelOn = !fresnelOn;

        if (overlayFresnelCamera != null)
        {
            overlayFresnelCamera.gameObject.SetActive(fresnelOn);
            Debug.Log($"[FresnelToggle] Overlay {(fresnelOn ? "ENABLED" : "DISABLED")}.");
            return;
        }

        if (camData != null)
        {
            camData.SetRenderer(fresnelOn ? fresnelRendererIndex : defaultRendererIndex);
            Debug.Log($"[FresnelToggle] Renderer index → {(fresnelOn ? fresnelRendererIndex : defaultRendererIndex)}.");
        }
    }
}
