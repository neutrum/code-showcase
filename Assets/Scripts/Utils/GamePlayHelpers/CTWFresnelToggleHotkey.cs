// CTWFresnelToggleHotkey.cs
// Unity 6 + URP + Input System.
// Press F (or any bound control) to toggle Fresnel mode.
// Attach to your MAIN camera.

using UnityEngine;
using UnityEngine.Rendering.Universal;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Camera))]
public class CTWFresnelToggleHotkey : MonoBehaviour
{
    [Header("Path A — Camera stacking (recommended)")]
    [Tooltip("Overlay camera that uses the Fresnel renderer. Keep disabled by default.")]
    public Camera overlayFresnelCamera;

    [Header("Path B — Renderer swap (single camera)")]
    [Tooltip("Index of your normal/default renderer in the URP Asset (usually 0).")]
    public int defaultRendererIndex = 0;
    [Tooltip("Index of your Fresnel renderer in the URP Asset (e.g., 1).")]
    public int fresnelRendererIndex = 1;
    [Tooltip("Force start in default renderer on Awake (only for swap path).")]
    public bool forceStartInDefault = true;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    [Header("Input System")]
    [Tooltip("Optional: reference an action from your Input Actions asset. If empty, F key is used by default.")]
    public InputActionReference toggleActionRef;

    InputAction _action;       // runtime action we listen to
#else
    [Header("Legacy Input (only if old system is enabled)")]
    public KeyCode legacyToggleKey = KeyCode.F;
#endif

    UniversalAdditionalCameraData camData;
    bool fresnelOn;

    void Awake()
    {
        camData = GetComponent<UniversalAdditionalCameraData>();

        if (overlayFresnelCamera != null)
        {
            fresnelOn = overlayFresnelCamera.gameObject.activeSelf;
            Debug.Log($"[FresnelToggle] Stacking mode. Overlay active = {fresnelOn}");
        }
        else
        {
            if (camData == null)
                Debug.LogWarning("[FresnelToggle] No UniversalAdditionalCameraData on this camera.");

            if (forceStartInDefault && camData != null)
            {
                camData.SetRenderer(defaultRendererIndex);
                fresnelOn = false;
                Debug.Log($"[FresnelToggle] Swap mode. Forcing start in default renderer index {defaultRendererIndex}.");
            }
            else
            {
                Debug.Log("[FresnelToggle] Swap mode. Starting without forcing renderer.");
            }
        }
    }

    void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        _action = toggleActionRef != null ? toggleActionRef.action
                                          : new InputAction("ToggleFresnel", InputActionType.Button);

        if (toggleActionRef == null && _action.bindings.Count == 0)
        {
            _action.AddBinding("<Keyboard>/f");
            _action.AddBinding("<Gamepad>/start");
            _action.AddBinding("<XRController>{RightHand}/primaryButton");
        }

        _action.performed += OnPerformed;
        _action.Enable();

        if (toggleActionRef == null)
            Debug.Log("[FresnelToggle] Input System: listening on default bindings (Keyboard F, Gamepad Start, XR primary).");
        else
            Debug.Log($"[FresnelToggle] Input System: using action '{toggleActionRef.name}'.");
#endif
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (_action != null)
        {
            _action.performed -= OnPerformed;
            if (toggleActionRef == null) _action.Dispose();
            _action = null;
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    void OnPerformed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        Toggle();
    }
#else
    void Update()
    {
        if (Input.GetKeyDown(legacyToggleKey)) Toggle();
    }
#endif

    public void Toggle()
    {
        fresnelOn = !fresnelOn;

        if (overlayFresnelCamera != null)
        {
            overlayFresnelCamera.gameObject.SetActive(fresnelOn);
            Debug.Log($"[FresnelToggle] Stacking: overlay {(fresnelOn ? "ENABLED" : "DISABLED")}.");
            return;
        }

        if (camData != null)
        {
            camData.SetRenderer(fresnelOn ? fresnelRendererIndex : defaultRendererIndex);
            Debug.Log($"[FresnelToggle] Swap: set renderer index → {(fresnelOn ? fresnelRendererIndex : defaultRendererIndex)}.");
        }
        else
        {
            Debug.LogWarning("[FresnelToggle] No overlay camera and no UniversalAdditionalCameraData found.");
        }
    }
}
