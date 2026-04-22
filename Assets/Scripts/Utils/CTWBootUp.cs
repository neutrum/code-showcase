using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

/// <summary>
/// Entry point that lives in the Bootstrap scene.
/// Waits for the XR loader to finish initialising before signalling the rest of the app.
/// Other systems (CTWSceneManager, CTWGameManager) listen to OnBootComplete or poll
/// Instance availability rather than being called directly here.
/// </summary>
public class CTWBootUp : MonoBehaviour
{
    public static CTWBootUp Instance { get; private set; }

    [SerializeField] private float _xrInitTimeoutSeconds = 5f;

    public event System.Action OnBootComplete;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        yield return WaitForXR();
        CTWLog.Verbose("[BootUp] XR initialised. Application ready.");
        OnBootComplete?.Invoke();
    }

    private IEnumerator WaitForXR()
    {
        var xrManager = XRGeneralSettings.Instance?.Manager;
        if (xrManager == null) yield break;

        float elapsed = 0f;
        while (!xrManager.isInitializationComplete && elapsed < _xrInitTimeoutSeconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!xrManager.isInitializationComplete)
            Debug.LogWarning($"[BootUp] XR did not finish initialising within {_xrInitTimeoutSeconds}s.");
    }
}
