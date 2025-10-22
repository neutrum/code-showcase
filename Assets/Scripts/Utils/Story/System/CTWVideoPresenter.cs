using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

namespace CTW.Story
{
    /// <summary>
    /// Plays video clips in world space or fullscreen.
    /// Uses slide.subtitle as video file path or video clip reference.
    /// Great for cutscenes and narrative sequences.
    /// </summary>
    public class CTWVideoPresenter : CTWStoryPresenter
    {
        [Header("Display Mode")]
        public VideoDisplayMode displayMode = VideoDisplayMode.Fullscreen;
        
        public enum VideoDisplayMode
        {
            Fullscreen,     // Screen space overlay
            Billboard,      // World space in front of player
            Projection      // Project onto a surface
        }

        [Header("Fullscreen Settings")]
        public Canvas fullscreenCanvas;
        public RawImage videoDisplay;
        public CanvasGroup canvasGroup;

        [Header("Billboard Settings")]
        public float billboardDistance = 2.5f;
        public Vector2 billboardSize = new Vector2(1.92f, 1.08f); // 16:9 aspect
        public bool followPlayer = true;

        [Header("Video")]
        public VideoPlayer videoPlayer;
        public RenderTexture renderTexture;
        public bool autoCreateRenderTexture = true;
        public Vector2Int renderTextureSize = new Vector2Int(1920, 1080);

        [Header("Playback")]
        public bool loopVideo = false;
        public bool waitForVideoEnd = true;
        public float fadeInDuration = 0.3f;
        public float fadeOutDuration = 0.3f;

        [Header("Audio")]
        public AudioSource videoAudioSource;

        private Transform _head;
        private bool _videoFinished;
        private Material _billboardMaterial;
        private GameObject _billboardQuad;

        public override void Setup(Transform playerHead, Camera worldCamera)
        {
            _head = playerHead;

            SetupVideoPlayer();
            
            switch (displayMode)
            {
                case VideoDisplayMode.Fullscreen:
                    SetupFullscreen(worldCamera);
                    break;
                case VideoDisplayMode.Billboard:
                    SetupBillboard();
                    break;
                case VideoDisplayMode.Projection:
                    // Projection mode would need a target surface
                    Debug.LogWarning("[VideoPresenter] Projection mode not fully implemented");
                    break;
            }

            Debug.Log($"[VideoPresenter] Setup complete in {displayMode} mode");
        }

        private void SetupVideoPlayer()
        {
            if (!videoPlayer)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }

            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = loopVideo;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;

            // Create render texture
            if (autoCreateRenderTexture && renderTexture == null)
            {
                renderTexture = new RenderTexture(renderTextureSize.x, renderTextureSize.y, 0);
                renderTexture.name = "VideoPresenter_RT";
            }
            
            videoPlayer.targetTexture = renderTexture;

            // Setup audio
            if (!videoAudioSource)
            {
                videoAudioSource = gameObject.AddComponent<AudioSource>();
            }
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, videoAudioSource);

            // Subscribe to events
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        private void SetupFullscreen(Camera worldCamera)
        {
            if (!fullscreenCanvas)
            {
                var canvasGo = new GameObject("VideoCanvas_Fullscreen");
                canvasGo.transform.SetParent(transform);
                fullscreenCanvas = canvasGo.AddComponent<Canvas>();
                fullscreenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                fullscreenCanvas.sortingOrder = 999;
                
                canvasGroup = canvasGo.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0;

                // Create RawImage for video display
                var imageGo = new GameObject("VideoDisplay", typeof(RectTransform), typeof(RawImage));
                imageGo.transform.SetParent(canvasGo.transform, false);
                videoDisplay = imageGo.GetComponent<RawImage>();
                
                var rt = videoDisplay.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
            }

            videoDisplay.texture = renderTexture;
            fullscreenCanvas.gameObject.SetActive(false);
        }

        private void SetupBillboard()
        {
            // Create a quad in world space
            _billboardQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _billboardQuad.name = "VideoBillboard";
            _billboardQuad.transform.SetParent(transform);
            _billboardQuad.transform.localPosition = Vector3.zero;
            _billboardQuad.transform.localScale = new Vector3(billboardSize.x, billboardSize.y, 1f);

            // Destroy collider
            Destroy(_billboardQuad.GetComponent<Collider>());

            // Create material for video
            _billboardMaterial = new Material(Shader.Find("Unlit/Texture"));
            _billboardMaterial.mainTexture = renderTexture;
            _billboardQuad.GetComponent<Renderer>().material = _billboardMaterial;

            _billboardQuad.SetActive(false);
        }

        private void LateUpdate()
        {
            if (displayMode == VideoDisplayMode.Billboard && followPlayer && _head != null && _billboardQuad != null && _billboardQuad.activeSelf)
            {
                // Position billboard in front of player
                var forward = Vector3.ProjectOnPlane(_head.forward, Vector3.up).normalized;
                if (forward.sqrMagnitude < 1e-4f) forward = _head.forward;

                var targetPos = _head.position + forward * billboardDistance;
                _billboardQuad.transform.position = Vector3.Lerp(_billboardQuad.transform.position, targetPos, Time.deltaTime * 5f);

                // Face player
                var lookDir = (_head.position - _billboardQuad.transform.position).normalized;
                _billboardQuad.transform.rotation = Quaternion.LookRotation(-lookDir, Vector3.up);
            }
        }

        public override IEnumerator Show(CTWStoryEvent.Slide slide)
        {
            _videoFinished = false;

            // Try to load video from different sources
            bool videoLoaded = false;

            // Option 1: Check if subtitle is a video file path
            if (!string.IsNullOrEmpty(slide.subtitle))
            {
                string path = slide.subtitle.Trim();
                if (path.EndsWith(".mp4") || path.EndsWith(".mov") || path.EndsWith(".webm"))
                {
                    videoPlayer.source = VideoSource.Url;
                    videoPlayer.url = path;
                    videoLoaded = true;
                    Debug.Log($"[VideoPresenter] Loading video from path: {path}");
                }
            }

            // Option 2: Future - could use slide.customData for VideoClip reference
            // if (slide has VideoClip reference)
            // {
            //     videoPlayer.source = VideoSource.VideoClip;
            //     videoPlayer.clip = slide.videoClip;
            //     videoLoaded = true;
            // }

            if (!videoLoaded)
            {
                Debug.LogError("[VideoPresenter] No valid video source found in slide!");
                yield break;
            }

            // Prepare video
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared)
            {
                yield return null;
            }

            // Show display
            switch (displayMode)
            {
                case VideoDisplayMode.Fullscreen:
                    fullscreenCanvas.gameObject.SetActive(true);
                    yield return FadeCanvasGroup(canvasGroup, 0f, 1f, fadeInDuration);
                    break;
                case VideoDisplayMode.Billboard:
                    _billboardQuad.SetActive(true);
                    // Could add fade here too
                    break;
            }

            // Play video
            videoPlayer.Play();
            Debug.Log("[VideoPresenter] Playing video");

            // Wait for video to finish if requested
            if (waitForVideoEnd)
            {
                while (!_videoFinished && videoPlayer.isPlaying)
                {
                    yield return null;
                }
            }
        }

        public override IEnumerator Hide(float fadeSeconds)
        {
            // Stop video
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }

            // Fade out
            switch (displayMode)
            {
                case VideoDisplayMode.Fullscreen:
                    if (canvasGroup)
                    {
                        yield return FadeCanvasGroup(canvasGroup, canvasGroup.alpha, 0f, fadeSeconds);
                        fullscreenCanvas.gameObject.SetActive(false);
                    }
                    break;
                case VideoDisplayMode.Billboard:
                    if (_billboardQuad)
                    {
                        _billboardQuad.SetActive(false);
                    }
                    break;
            }

            Debug.Log("[VideoPresenter] Hidden");
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            cg.alpha = to;
        }

        private void OnVideoFinished(VideoPlayer vp)
        {
            _videoFinished = true;
            Debug.Log("[VideoPresenter] Video finished");
        }

        private void OnDestroy()
        {
            if (videoPlayer)
            {
                videoPlayer.loopPointReached -= OnVideoFinished;
            }

            if (renderTexture && autoCreateRenderTexture)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }

        /// <summary>
        /// Manually stop video playback
        /// </summary>
        public void StopVideo()
        {
            if (videoPlayer && videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
                _videoFinished = true;
            }
        }
    }
}

