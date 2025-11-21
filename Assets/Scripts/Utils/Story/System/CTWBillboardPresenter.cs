using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

namespace CTW.Story
{
    /// <summary>
    /// World-space billboard presenter (image + subtitle) that sits in front of the player's head.
    /// </summary>
    public class CTWBillboardPresenter : CTWStoryPresenter
    {
        [Header("Placement")]
        public float distance = 1.8f;
        public float heightOffset = -0.05f;
        public float followLerp = 12f;

        [Header("Rendering")]
        [Tooltip("Render on top of everything (no depth testing)")]
        public bool alwaysOnTop = true;
        [Tooltip("Canvas sorting order (higher = on top)")]
        public int sortingOrder = 100;

        [Header("UI (auto-wired if empty)")]
        public Canvas canvas;
        public CanvasGroup cg;
        
        [Header("Three separate GameObjects")]
        public Image image;                  // Image GameObject
        public TextMeshProUGUI subtitle;     // Subtitle GameObject
        public AudioSource audioSource;      // Audio GameObject

        private Transform _head;
        private Material _alwaysOnTopMaterial;

        /// <summary>
        /// Setup rendering to always be on top (no depth testing)
        /// </summary>
        private void SetupAlwaysOnTop()
        {
            // Create a material with ZTest Always for rendering on top
            if (_alwaysOnTopMaterial == null)
            {
                // Use UI/Default shader but with ZTest Always
                _alwaysOnTopMaterial = new Material(Shader.Find("UI/Default"));
                _alwaysOnTopMaterial.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                _alwaysOnTopMaterial.renderQueue = 4000; // Render after transparent objects
            }

            Debug.Log("[BillboardPresenter] Configured for always-on-top rendering");
        }

        /// <summary>
        /// Apply always-on-top material to UI components after they're created
        /// </summary>
        private void ApplyAlwaysOnTopMaterial()
        {
            if (!alwaysOnTop || _alwaysOnTopMaterial == null) return;

            // Apply to main image
            if (image != null)
            {
                image.material = _alwaysOnTopMaterial;
            }

            // Apply to subtitle (TextMeshPro has its own material, so we need to modify it)
            if (subtitle != null)
            {
                subtitle.fontSharedMaterial.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                subtitle.fontSharedMaterial.renderQueue = 4000;
            }

            Debug.Log("[BillboardPresenter] Applied always-on-top materials");
        }

        /// <summary>
        /// Initialize Image component for displaying sprites
        /// </summary>
        private void InitializeImageComponent(Image img)
        {
            if (img == null) return;
            
            img.color = Color.white;
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.type = Image.Type.Simple;
            
            Debug.Log("[BillboardPresenter] Image component initialized");
        }

        /// <summary>
        /// Initialize TextMeshPro component with all necessary settings.
        /// Handles font asset loading programmatically.
        /// </summary>
        private void InitializeTextMeshPro(TextMeshProUGUI tmp)
        {
            if (tmp == null)
            {
                Debug.LogError("[BillboardPresenter] TextMeshProUGUI is NULL! Cannot initialize.");
                return;
            }

            Debug.Log("[BillboardPresenter] Initializing TextMeshProUGUI...");

            // Basic appearance
            tmp.color = Color.white;
            tmp.fontSize = 36;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TMPro.TextWrappingModes.Normal; // Updated from enableWordWrapping
            
            // Auto-sizing for better readability at different distances
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 18;
            tmp.fontSizeMax = 48;
            
            // Try to assign a font asset if none is set
            if (tmp.font == null)
            {
                Debug.Log("[BillboardPresenter] No font assigned, searching for TMP font asset...");
                
                // Try to load the default TMP font from Resources
                TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                
                // Fallback: Try TMP essentials default
                if (defaultFont == null)
                {
                    Debug.Log("[BillboardPresenter] Trying TMP Essentials path...");
                    defaultFont = Resources.Load<TMP_FontAsset>("TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF");
                }
                
                // Fallback: Try to find any TMP font in resources
                if (defaultFont == null)
                {
                    Debug.Log("[BillboardPresenter] Searching for any TMP font asset...");
                    var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                    if (fonts != null && fonts.Length > 0)
                    {
                        defaultFont = fonts[0];
                        Debug.Log($"[BillboardPresenter] Using fallback TMP font: {defaultFont.name}");
                    }
                }
                
                if (defaultFont != null)
                {
                    tmp.font = defaultFont;
                    Debug.Log($"[BillboardPresenter] ✅ Font assigned: {defaultFont.name}");
                }
                else
                {
                    Debug.LogWarning("[BillboardPresenter] ❌ No TMP font asset found! Text may not render. Please import TMP Essentials via Window > TextMeshPro > Import TMP Essential Resources");
                }
            }
            else
            {
                Debug.Log($"[BillboardPresenter] ✅ Font already assigned: {tmp.font.name}");
            }
            
            // Additional settings for better world-space rendering
            tmp.raycastTarget = false; // Optimize for non-interactive text
            tmp.richText = true;       // Enable rich text tags
            tmp.parseCtrlCharacters = true;
            tmp.overflowMode = TextOverflowModes.Overflow; // Don't truncate
            tmp.horizontalMapping = TextureMappingOptions.Line;
            tmp.verticalMapping = TextureMappingOptions.Line;
            
            Debug.Log("[BillboardPresenter] TextMeshProUGUI initialization complete!");
        }

        /// <summary>
        /// Initialize AudioSource for audio playback
        /// </summary>
        private void InitializeAudioSource(AudioSource audio)
        {
            if (audio == null) return;
            
            // Configure for 3D spatial audio in world space
            audio.playOnAwake = false;
            audio.loop = false;
            audio.spatialBlend = 1f; // Fully 3D
            audio.rolloffMode = AudioRolloffMode.Linear;
            audio.minDistance = 1f;
            audio.maxDistance = 10f;
            audio.volume = 1f;
            
            Debug.Log("[BillboardPresenter] AudioSource initialized");
        }

        public override void Setup(Transform playerHead, Camera worldCamera)
        {
            _head = playerHead;

            if (!canvas) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = worldCamera;
            
            // Set sorting order to render on top
            canvas.sortingOrder = sortingOrder;
            canvas.overrideSorting = true;

            if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1200, 700);
            canvas.transform.localScale = Vector3.one * 0.0012f;
            
            // If always on top is enabled, disable depth testing
            if (alwaysOnTop)
            {
                SetupAlwaysOnTop();
            }

            // 1. Create/Initialize IMAGE GameObject
            if (!image)
            {
                var imageGo = new GameObject("Image", typeof(RectTransform), typeof(Image));
                imageGo.transform.SetParent(transform, false);
                image = imageGo.GetComponent<Image>();
                
                // Setup rect transform
                var irt = image.rectTransform;
                irt.anchorMin = new Vector2(0.05f, 0.35f);
                irt.anchorMax = new Vector2(0.95f, 0.98f);
                irt.offsetMin = irt.offsetMax = Vector2.zero;
                
                Debug.Log("[BillboardPresenter] Created Image GameObject");
            }
            // Always initialize image component
            InitializeImageComponent(image);

            // 2. Create/Initialize SUBTITLE GameObject (TextMeshPro only)
            if (!subtitle)
            {
                var subtitleGo = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                subtitleGo.transform.SetParent(transform, false);
                subtitle = subtitleGo.GetComponent<TextMeshProUGUI>();
                
                // Setup rect transform
                var srt = subtitle.rectTransform;
                srt.anchorMin = new Vector2(0.05f, 0.02f);
                srt.anchorMax = new Vector2(0.95f, 0.30f);
                srt.offsetMin = srt.offsetMax = Vector2.zero;
                
                Debug.Log("[BillboardPresenter] Created Subtitle GameObject with TextMeshProUGUI");
            }
            // Always initialize TextMeshPro
            InitializeTextMeshPro(subtitle);

            // 3. Create/Initialize AUDIO GameObject
            if (!audioSource)
            {
                var audioGo = new GameObject("Audio", typeof(AudioSource));
                audioGo.transform.SetParent(transform, false);
                audioSource = audioGo.GetComponent<AudioSource>();
                
                Debug.Log("[BillboardPresenter] Created Audio GameObject");
            }
            // Always initialize AudioSource
            InitializeAudioSource(audioSource);

            // Apply always-on-top rendering if enabled
            if (alwaysOnTop)
            {
                ApplyAlwaysOnTopMaterial();
            }

            cg.alpha = 0f;
        }

        private void LateUpdate()
        {
            if (!_head) return;

            var forward = Vector3.ProjectOnPlane(_head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 1e-4f) forward = _head.forward;

            var targetPos = _head.position + forward * distance + Vector3.up * heightOffset;
            transform.position = Vector3.Lerp(transform.position, targetPos, 1 - Mathf.Exp(-followLerp * Time.deltaTime));

            var toHead = (_head.position - transform.position);
            var look = Quaternion.LookRotation(toHead.normalized * -1f, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 1 - Mathf.Exp(-followLerp * Time.deltaTime));
        }

        public override IEnumerator Show(CTWStoryEvent.Slide slide)
        {
            // Set image content
            if (image != null)
            {
                image.sprite = slide.image;
                image.enabled = slide.image != null; // Hide if no sprite
            }
            
            // Set subtitle text
            if (subtitle != null)
            {
                subtitle.text = slide.subtitle ?? "";
            }
            
            // Primary audio handling - spatial 3D audio positioned at the billboard
            if (slide.audio != null)
            {
                if (audioSource != null)
                {
                    audioSource.clip = slide.audio;
                    audioSource.Play();
                    Debug.Log($"[BillboardPresenter] Playing spatial audio: {slide.audio.name}");
                }
                else
                {
                    Debug.LogWarning("[BillboardPresenter] AudioSource is null! Audio will not play. Ensure Setup() was called properly.");
                }
            }
            
            yield return FadeTo(1f, slide.fade);
        }

        public override IEnumerator Hide(float fadeSeconds) => FadeTo(0f, fadeSeconds);

        private IEnumerator FadeTo(float target, float time)
        {
            float start = cg.alpha, t = 0f;
            while (t < time)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(start, target, t / Mathf.Max(0.0001f, time));
                yield return null;
            }
            cg.alpha = target;
        }
    }
}
