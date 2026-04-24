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
        public Image image;
        public TextMeshProUGUI subtitle;
        public AudioSource audioSource;

        private Transform _head;
        private Material _alwaysOnTopMaterial;

        private void SetupAlwaysOnTop()
        {
            if (_alwaysOnTopMaterial == null)
            {
                _alwaysOnTopMaterial = new Material(Shader.Find("UI/Default"));
                _alwaysOnTopMaterial.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                _alwaysOnTopMaterial.renderQueue = 4000;
            }
            CTWLog.Verbose("[BillboardPresenter] Configured for always-on-top rendering");
        }

        private void ApplyAlwaysOnTopMaterial()
        {
            if (!alwaysOnTop || _alwaysOnTopMaterial == null) return;

            if (image != null)
                image.material = _alwaysOnTopMaterial;

            if (subtitle != null)
            {
                subtitle.fontSharedMaterial.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                subtitle.fontSharedMaterial.renderQueue = 4000;
            }
            CTWLog.Verbose("[BillboardPresenter] Applied always-on-top materials");
        }

        private void InitializeImageComponent(Image img)
        {
            if (img == null) return;
            img.color = Color.white;
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.type = Image.Type.Simple;
            CTWLog.Verbose("[BillboardPresenter] Image component initialized");
        }

        private void InitializeTextMeshPro(TextMeshProUGUI tmp)
        {
            if (tmp == null)
            {
                Debug.LogError("[BillboardPresenter] TextMeshProUGUI is NULL! Cannot initialize.");
                return;
            }

            tmp.color = Color.white;
            tmp.fontSize = 36;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 18;
            tmp.fontSizeMax = 48;

            if (tmp.font == null)
            {
                CTWLog.Verbose("[BillboardPresenter] No font assigned, searching for TMP font asset...");

                TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

                if (defaultFont == null)
                {
                    CTWLog.Verbose("[BillboardPresenter] Trying TMP Essentials path...");
                    defaultFont = Resources.Load<TMP_FontAsset>("TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF");
                }

                if (defaultFont == null)
                {
                    CTWLog.Verbose("[BillboardPresenter] Searching for any TMP font asset...");
                    var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                    if (fonts != null && fonts.Length > 0)
                    {
                        defaultFont = fonts[0];
                        CTWLog.Verbose($"[BillboardPresenter] Using fallback TMP font: {defaultFont.name}");
                    }
                }

                if (defaultFont != null)
                {
                    tmp.font = defaultFont;
                    CTWLog.Verbose($"[BillboardPresenter] Font assigned: {defaultFont.name}");
                }
                else
                {
                    Debug.LogWarning("[BillboardPresenter] No TMP font asset found! Text may not render. Import TMP Essentials via Window > TextMeshPro > Import TMP Essential Resources");
                }
            }
            else
            {
                CTWLog.Verbose($"[BillboardPresenter] Font already assigned: {tmp.font.name}");
            }

            tmp.raycastTarget = false;
            tmp.richText = true;
            tmp.parseCtrlCharacters = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.horizontalMapping = TextureMappingOptions.Line;
            tmp.verticalMapping = TextureMappingOptions.Line;

            CTWLog.Verbose("[BillboardPresenter] TextMeshProUGUI initialization complete");
        }

        private void InitializeAudioSource(AudioSource audio)
        {
            if (audio == null) return;
            audio.playOnAwake = false;
            audio.loop = false;
            audio.spatialBlend = 1f;
            audio.rolloffMode = AudioRolloffMode.Linear;
            audio.minDistance = 1f;
            audio.maxDistance = 10f;
            audio.volume = 1f;
            CTWLog.Verbose("[BillboardPresenter] AudioSource initialized");
        }

        public override void Setup(Transform playerHead, Camera worldCamera)
        {
            _head = playerHead;

            if (!canvas) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = worldCamera;
            canvas.sortingOrder = sortingOrder;
            canvas.overrideSorting = true;

            if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1200, 700);
            canvas.transform.localScale = Vector3.one * 0.0012f;

            if (alwaysOnTop) SetupAlwaysOnTop();

            if (!image)
            {
                var imageGo = new GameObject("Image", typeof(RectTransform), typeof(Image));
                imageGo.transform.SetParent(transform, false);
                image = imageGo.GetComponent<Image>();
                var irt = image.rectTransform;
                irt.anchorMin = new Vector2(0.05f, 0.35f);
                irt.anchorMax = new Vector2(0.95f, 0.98f);
                irt.offsetMin = irt.offsetMax = Vector2.zero;
                CTWLog.Verbose("[BillboardPresenter] Created Image GameObject");
            }
            InitializeImageComponent(image);

            if (!subtitle)
            {
                var subtitleGo = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                subtitleGo.transform.SetParent(transform, false);
                subtitle = subtitleGo.GetComponent<TextMeshProUGUI>();
                var srt = subtitle.rectTransform;
                srt.anchorMin = new Vector2(0.05f, 0.02f);
                srt.anchorMax = new Vector2(0.95f, 0.30f);
                srt.offsetMin = srt.offsetMax = Vector2.zero;
                CTWLog.Verbose("[BillboardPresenter] Created Subtitle GameObject");
            }
            InitializeTextMeshPro(subtitle);

            if (!audioSource)
            {
                var audioGo = new GameObject("Audio", typeof(AudioSource));
                audioGo.transform.SetParent(transform, false);
                audioSource = audioGo.GetComponent<AudioSource>();
                CTWLog.Verbose("[BillboardPresenter] Created Audio GameObject");
            }
            InitializeAudioSource(audioSource);

            if (alwaysOnTop) ApplyAlwaysOnTopMaterial();

            cg.alpha = 0f;
        }

        private void LateUpdate()
        {
            if (!_head) return;

            var forward = Vector3.ProjectOnPlane(_head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 1e-4f) forward = _head.forward;

            var targetPos = _head.position + forward * distance + Vector3.up * heightOffset;
            transform.position = Vector3.Lerp(transform.position, targetPos, 1 - Mathf.Exp(-followLerp * Time.deltaTime));

            var toHead = _head.position - transform.position;
            var look = Quaternion.LookRotation(toHead.normalized * -1f, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 1 - Mathf.Exp(-followLerp * Time.deltaTime));
        }

        public override IEnumerator Show(CTWStoryEvent.Slide slide)
        {
            if (image != null)
            {
                image.sprite = slide.image;
                image.enabled = slide.image != null;
            }

            if (subtitle != null)
                subtitle.text = slide.subtitle ?? "";

            if (slide.audio != null)
            {
                if (audioSource != null)
                {
                    audioSource.clip = slide.audio;
                    audioSource.Play();
                    CTWLog.Verbose($"[BillboardPresenter] Playing spatial audio: {slide.audio.name}");
                }
                else
                {
                    Debug.LogWarning("[BillboardPresenter] AudioSource is null! Ensure Setup() was called properly.");
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
