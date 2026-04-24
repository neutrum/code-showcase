using UnityEngine;

/// <summary>
/// Applied once on Awake to configure application-level runtime settings.
/// XR display frequency and foveation are handled by CTWQuestQuickPreset.
/// </summary>
public class CTWSettings : MonoBehaviour
{
    [Header("Audio")]
    [Range(0f, 1f)]
    [SerializeField] private float _masterVolume = 1f;

    [Header("Physics")]
    [Tooltip("Fixed timestep in seconds (default 0.02 = 50 Hz)")]
    [SerializeField] private float _fixedTimestep = 0.02f;

    private void Awake()
    {
        AudioListener.volume = _masterVolume;
        Time.fixedDeltaTime = _fixedTimestep;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AudioListener.volume = _masterVolume;
    }
#endif
}
