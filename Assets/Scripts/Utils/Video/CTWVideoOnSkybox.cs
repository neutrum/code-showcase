using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

[DisallowMultipleComponent]
[RequireComponent(typeof(VideoPlayer))]
public class CTWVideoOnSkybox : MonoBehaviour
{
    [Header("Video")]
    [Tooltip("File name in Assets/StreamingAssets (e.g. intro360.mp4)")]
    public string videoFileName = "intro360.mp4";
    [Tooltip("Copy to persistentDataPath on Android/Quest (recommended).")]
    public bool copyToPersistentOnAndroid = true;
    public bool loop = true;

    [Header("Skybox")]
    [Range(0, 360)] public float yawRotation = 0f;
    [Range(0.1f, 5f)] public float exposure = 1f;
    public bool stereo = false;
    public StereoLayout stereoLayout = StereoLayout.OverUnder; // if stereo
    public enum StereoLayout { None = 0, SideBySide = 1, OverUnder = 2 }

    [Header("Behavior")]
    public bool playOnAwake = true;
    public bool attachAudioSource = true;
    public bool verboseLogs = true;

    private VideoPlayer _vp;
    private RenderTexture _rt;
    private Material _skyboxMat;

    void Awake()
    {
        _vp = GetComponent<VideoPlayer>();
        _vp.playOnAwake = false;
        _vp.waitForFirstFrame = true;
        _vp.skipOnDrop = true;
        _vp.isLooping = loop;

        if (attachAudioSource)
        {
            var a = GetComponent<AudioSource>();
            if (!a) a = gameObject.AddComponent<AudioSource>();
            _vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _vp.EnableAudioTrack(0, true);
            _vp.SetTargetAudioSource(0, a);
        }
        else
        {
            _vp.audioOutputMode = VideoAudioOutputMode.None;
        }

        // Create Skybox material
        var shader = Shader.Find("Skybox/Panoramic");
        if (!shader)
        {
            Debug.LogError("[AutoSkybox360] Missing shader 'Skybox/Panoramic'.");
            enabled = false; return;
        }
        _skyboxMat = new Material(shader);
        _skyboxMat.SetFloat("_Mapping", 0f);   // 0 = Lat-Long
        _skyboxMat.SetFloat("_ImageType", 0f); // 0 = 360
        _skyboxMat.SetFloat("_Layout", stereo ? (float)stereoLayout : 0f);
        _skyboxMat.SetFloat("_Exposure", exposure);
        _skyboxMat.SetFloat("_Rotation", yawRotation);
        RenderSettings.skybox = _skyboxMat;

        // Hook events
        _vp.prepareCompleted += OnPrepared;
        _vp.errorReceived += (src, msg) => Debug.LogError("[AutoSkybox360] Video error: " + msg);
        _vp.seekCompleted += src => { if (verboseLogs) Debug.Log("[AutoSkybox360] Seek completed"); };
        _vp.loopPointReached += src => { if (verboseLogs) Debug.Log("[AutoSkybox360] Loop point reached"); };

        // Warn if camera won't draw skybox
        var cam = Camera.main;
        if (cam && cam.clearFlags != CameraClearFlags.Skybox)
            Debug.LogWarning($"[AutoSkybox360] Main Camera Clear Flags is {cam.clearFlags}. Set to Skybox to see the video.");
    }

    void Start()
    {
        StartCoroutine(SetupAndPlay());
    }

    void Update()
    {
        if (_skyboxMat)
        {
            _skyboxMat.SetFloat("_Rotation", yawRotation);
            _skyboxMat.SetFloat("_Exposure", exposure);
        }
    }

    private IEnumerator SetupAndPlay()
    {
        // Build a usable URL
        string url;
#if UNITY_ANDROID && !UNITY_EDITOR
        // On Android StreamingAssets lives inside the .apk (jar). Copy to a real file first.
        string src = Path.Combine(Application.streamingAssetsPath, videoFileName);
        string dst = Path.Combine(Application.persistentDataPath, videoFileName);

        if (copyToPersistentOnAndroid)
        {
            if (!File.Exists(dst))
            {
                if (verboseLogs) Debug.Log($"[AutoSkybox360] Copying from SA → persistent: {src} → {dst}");
                using (var req = UnityWebRequest.Get(src))
                {
                    yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
                    if (req.result != UnityWebRequest.Result.Success)
#else
                    if (req.isNetworkError || req.isHttpError)
#endif
                    {
                        Debug.LogError("[AutoSkybox360] Copy failed: " + req.error);
                        yield break;
                    }
                    File.WriteAllBytes(dst, req.downloadHandler.data);
                }
            }
            url = new Uri(dst).AbsoluteUri; // file://
        }
        else
        {
            // Some devices can read jar: URLs; copying is safer though.
            url = src; // usually starts with jar:file://
        }
#else
        // Desktop/Editor/iOS/etc.
        string path = Path.Combine(Application.streamingAssetsPath, videoFileName);
        url = new Uri(path).AbsoluteUri; // proper file:// URL with escaping
#endif

        if (verboseLogs) Debug.Log("[AutoSkybox360] Using URL: " + url);

        _vp.source = VideoSource.Url;
        _vp.url = url;

        // Prepare & wait
        _vp.Prepare();
        float t = 0f, timeout = 10f;
        while (!_vp.isPrepared)
        {
            if ((_vp.isPrepared) || (_vp.frameCount > 0)) break; // some platforms update these differently
            if ((t += Time.unscaledDeltaTime) > timeout)
            {
                Debug.LogError("[AutoSkybox360] Prepare timed out. Check path/codec.");
                yield break;
            }
            yield return null;
        }

        if (verboseLogs) Debug.Log($"[AutoSkybox360] Prepared. size={_vp.width}x{_vp.height}, frames={_vp.frameCount}");

        if (playOnAwake) _vp.Play();
    }

    private void OnPrepared(VideoPlayer vp)
    {
        // Create RT matching native size (min clamp)
        int w = Mathf.Max(512, (int)vp.width);
        int h = Mathf.Max(256, (int)vp.height);

        float aspect = (float)vp.width / vp.height;
        if (Mathf.Abs(aspect - 2f) > 0.2f)
            Debug.LogWarning($"[AutoSkybox360] Video aspect {vp.width}x{vp.height} ≈ {aspect:0.00}:1; 360 equirect expects ~2:1.");


        if (_rt != null && (_rt.width != w || _rt.height != h))
        {
            vp.targetTexture = null;
            _rt.Release();
            Destroy(_rt);
            _rt = null;
        }

        if (_rt == null)
        {
            _rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
            {
                name = "RT_Skybox_360",
                useMipMap = false,
                autoGenerateMips = false,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1
            };
            _rt.Create();
        }

        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = _rt;

        // Assign texture to skybox
        if (_skyboxMat)
        {
            _skyboxMat.SetTexture("_Tex", _rt);                  // Spherical (HDR)
            _skyboxMat.SetFloat("_Layout", stereo ? (float)stereoLayout : 0f);
        }

        if (verboseLogs) Debug.Log("[AutoSkybox360] RenderTexture bound and skybox assigned.");
    }

    void OnDestroy()
    {
        if (_vp != null) _vp.prepareCompleted -= OnPrepared;
        if (_rt != null)
        {
            if (_vp) _vp.targetTexture = null;
            _rt.Release();
            Destroy(_rt);
        }
        if (_skyboxMat) Destroy(_skyboxMat);
    }
}
