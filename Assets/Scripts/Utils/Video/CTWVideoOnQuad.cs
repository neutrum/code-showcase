// U6VideoWithTriggers_PosterFade.cs
// Unity 6 + URP. Attach to a Quad with URP/Unlit material.
// Shows a poster (or black) first, then fades to video on first frame.
// Includes end-of-video and custom time/percent triggers.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;
using UnityEngine.Networking;

[RequireComponent(typeof(Renderer), typeof(VideoPlayer), typeof(AudioSource))]
public class CTWVideoOnQuad : MonoBehaviour
{
    [Header("Source (StreamingAssets)")]
    public string fileName = "promo.mp4";

    [Header("Playback")]
    public bool loop = false;
    public bool playOnStart = true;
    public bool autoAspect = true;

    [Header("Idle Visuals")]
    [Tooltip("Shown before video starts (optional). If null, quad will be tinted by idleTint.")]
    public Texture2D posterTexture;
    [Tooltip("Tint before playback (use black).")]
    public Color idleTint = Color.black;
    [Tooltip("Tint used during playback (use white for correct colors).")]
    public Color playTint = Color.white;
    [Tooltip("Fade from idleTint → playTint once the first frame arrives.")]
    public float fadeDuration = 0.25f;
    [Tooltip("When the video ends, fade back to idleTint and restore poster.")]
    public bool fadeBackOnEnd = false;

    [Header("Events")]
    public UnityEvent onPrepared;
    public UnityEvent onFirstFrame;
    public UnityEvent onStarted;
    public UnityEvent onPaused;
    public UnityEvent onResumed;
    public UnityEvent onStopped;
    public UnityEvent onReachedEnd;
    public UnityEvent onError;

    [Serializable] public class PercentTrigger { [Range(0f,100f)] public float percent = 100f; public UnityEvent onTrigger; [NonSerialized] public bool fired; }
    [Serializable] public class TimeTrigger    { public double timeSeconds = 0; public UnityEvent onTrigger; [NonSerialized] public bool fired; }

    [Header("Custom Triggers")]
    public List<PercentTrigger> percentTriggers = new();
    public List<TimeTrigger> timeTriggers = new();

    [Header("End Event")]
    public bool endEventOnlyOnce = false;

    Renderer rend;
    Material mat;             // instance for this renderer
    VideoPlayer vp;
    AudioSource audioSrc;

    bool firstFrameFired;
    bool started;
    bool endFired;

    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        rend = GetComponent<Renderer>();
        mat = rend.material; // safe: this object gets its own instance
        vp = GetComponent<VideoPlayer>();
        audioSrc = GetComponent<AudioSource>();

        // Idle visuals
        if (posterTexture != null) mat.SetTexture(BaseMapId, posterTexture);
        mat.SetColor(BaseColorId, idleTint);

        // VideoPlayer wiring
        vp.playOnAwake = false;
        vp.waitForFirstFrame = true;
        vp.isLooping = loop;
        vp.renderMode = VideoRenderMode.MaterialOverride;
        vp.targetMaterialRenderer = rend;
        vp.targetMaterialProperty = "_BaseMap";
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        vp.EnableAudioTrack(0, true);
        vp.SetTargetAudioSource(0, audioSrc);

        vp.sendFrameReadyEvents = true;
        vp.prepareCompleted += OnPreparedInternal;
        vp.loopPointReached += OnLoopPointReachedInternal;
        vp.errorReceived += OnErrorInternal;
        vp.frameReady += OnFrameReadyInternal;
    }

    void OnDestroy()
    {
        vp.prepareCompleted -= OnPreparedInternal;
        vp.loopPointReached -= OnLoopPointReachedInternal;
        vp.errorReceived -= OnErrorInternal;
        vp.frameReady -= OnFrameReadyInternal;
    }

    IEnumerator Start()
    {
        yield return SetupAndPrepareFromStreamingAssets();

        while (!vp.isPrepared) yield return null;
        if (playOnStart) StartPlayback();
    }

    void Update()
    {
        if (!started && vp.isPrepared && vp.isPlaying)
        {
            started = true;
            onStarted?.Invoke();
        }

        if (vp.isPrepared && vp.length > 0.0)
        {
            double t = vp.time;
            double len = vp.length;
            double pct = (len > 0.0) ? (t / len) * 100.0 : 0.0;

            foreach (var trig in percentTriggers)
                if (!trig.fired && pct >= trig.percent) { trig.fired = true; trig.onTrigger?.Invoke(); }

            foreach (var trig in timeTriggers)
                if (!trig.fired && t >= trig.timeSeconds) { trig.fired = true; trig.onTrigger?.Invoke(); }
        }
    }

    // ---------- Public controls ----------
    public void StartPlayback()
    {
        if (!vp.isPrepared) return;
        ResetPerPlaybackFlags();
        vp.Play();
        audioSrc.Play();
    }

    public void PausePlayback()
    {
        if (!vp.isPrepared) return;
        vp.Pause(); audioSrc.Pause();
        onPaused?.Invoke();
    }

    public void ResumePlayback()
    {
        if (!vp.isPrepared) return;
        vp.Play(); audioSrc.Play();
        onResumed?.Invoke();
    }

    public void StopPlayback()
    {
        if (!vp.isPrepared) return;
        vp.Stop(); audioSrc.Stop();
        onStopped?.Invoke();
        if (fadeBackOnEnd) StartCoroutine(FadeBaseColor(playTint, idleTint, 0.15f));
        if (posterTexture != null) mat.SetTexture(BaseMapId, posterTexture);
    }

    public void Restart()
    {
        if (!vp.isPrepared) return;
        vp.time = 0;
        ResetAllFlags();
        StartPlayback();
    }

    // ---------- Internals ----------
    IEnumerator SetupAndPrepareFromStreamingAssets()
    {
        string sa = Path.Combine(Application.streamingAssetsPath, fileName);

        if (sa.Contains("://")) // Android/Quest
        {
            string dst = Path.Combine(Application.persistentDataPath, fileName);
            if (!File.Exists(dst))
            {
                using var req = UnityWebRequest.Get(sa);
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("[Video] Copy failed: " + req.error);
                    onError?.Invoke(); yield break;
                }
                File.WriteAllBytes(dst, req.downloadHandler.data);
            }
            vp.source = VideoSource.Url;
            vp.url = dst;
        }
        else
        {
            vp.source = VideoSource.Url;
            vp.url = sa;
        }

        vp.Prepare();
    }

    void OnPreparedInternal(VideoPlayer _)
    {
        if (autoAspect && vp.height > 0)
        {
            var s = transform.localScale; // assume s.y is desired height
            s.x = s.y * ((float)vp.width / vp.height);
            transform.localScale = s;
        }
        onPrepared?.Invoke();
    }

    void OnFrameReadyInternal(VideoPlayer _, long __)
    {
        if (firstFrameFired) return;
        firstFrameFired = true;
        // Fade to correct tint for playback (usually white)
        if (fadeDuration > 0f) StartCoroutine(FadeBaseColor(idleTint, playTint, fadeDuration));
        else mat.SetColor(BaseColorId, playTint);
        onFirstFrame?.Invoke();

        // We can stop further frame events to save overhead
        vp.sendFrameReadyEvents = false;
    }

    void OnLoopPointReachedInternal(VideoPlayer _)
    {
        if (endEventOnlyOnce && endFired) return;
        endFired = true;
        onReachedEnd?.Invoke();

        if (fadeBackOnEnd)
        {
            StartCoroutine(FadeBaseColor(playTint, idleTint, 0.2f));
            if (posterTexture != null) mat.SetTexture(BaseMapId, posterTexture);
        }
    }

    void OnErrorInternal(VideoPlayer _, string message)
    {
        Debug.LogError("[Video] Error: " + message);
        onError?.Invoke();
    }

    IEnumerator FadeBaseColor(Color from, Color to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            mat.SetColor(BaseColorId, Color.Lerp(from, to, Mathf.Clamp01(t / dur)));
            yield return null;
        }
        mat.SetColor(BaseColorId, to);
    }

    void ResetPerPlaybackFlags()
    {
        firstFrameFired = false;
        started = false;
        foreach (var p in percentTriggers) p.fired = false;
        foreach (var t in timeTriggers) t.fired = false;
        vp.sendFrameReadyEvents = true;
    }

    void ResetAllFlags()
    {
        ResetPerPlaybackFlags();
        endFired = false;
    }
}
