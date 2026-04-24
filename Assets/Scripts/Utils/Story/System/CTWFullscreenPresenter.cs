using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CTW.Story
{
    /// <summary>
    /// Fullscreen overlay presenter - perfect for dramatic story moments.
    /// Uses Screen Space Overlay canvas for maximum visibility.
    /// Supports typewriter effect for text and customizable fade curves.
    /// </summary>
    public class CTWFullscreenPresenter : CTWStoryPresenter
    {
        [Header("UI References (auto-created if null)")]
        public Canvas canvas;
        public CanvasGroup canvasGroup;
        public Image backgroundImage;
        public Image foregroundImage;
        public TextMeshProUGUI subtitleText;
        
        [Header("Visual Style")]
        public Color backgroundColor = Color.black;
        [Range(0f, 1f)] public float backgroundAlpha = 0.9f;
        public bool preserveImageAspect = true;
        
        [Header("Text Effects")]
        public bool typewriterEffect = true;
        public float typewriterSpeed = 30f; // characters per second
        public AudioClip typewriterSound;
        public float typewriterSoundVolume = 0.3f;
        
        [Header("Animations")]
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public FadeMode imageFadeMode = FadeMode.WithText;
        
        [Header("Layout (normalized 0-1)")]
        public Rect imageArea = new Rect(0.1f, 0.3f, 0.8f, 0.6f);
        public Rect textArea = new Rect(0.1f, 0.05f, 0.8f, 0.2f);

        public enum FadeMode
        {
            WithText,       // Fade in image and text together
            ImageFirst,     // Image fades in, then text appears
            TextFirst       // Text appears, then image fades in
        }

        private Coroutine _typewriterCoroutine;
        private AudioSource _typewriterAudioSource;

        /// <summary>
        /// Initialize TextMeshPro component with all necessary settings.
        /// Handles font asset loading programmatically.
        /// </summary>
        private void InitializeTextMeshPro(TextMeshProUGUI tmp)
        {
            if (tmp == null) return;

            // Basic appearance
            tmp.fontSize = 36;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.textWrappingMode = TMPro.TextWrappingModes.Normal; // Updated from enableWordWrapping
            
            // Try to assign a font asset if none is set
            if (tmp.font == null)
            {
                // Try to load the default TMP font from Resources
                TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                
                // Fallback: Try TMP essentials default
                if (defaultFont == null)
                {
                    defaultFont = Resources.Load<TMP_FontAsset>("TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF");
                }
                
                // Fallback: Try to find any TMP font in resources
                if (defaultFont == null)
                {
                    var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                    if (fonts != null && fonts.Length > 0)
                    {
                        defaultFont = fonts[0];
                        Debug.Log($"[FullscreenPresenter] Using fallback TMP font: {defaultFont.name}");
                    }
                }
                
                if (defaultFont != null)
                {
                    tmp.font = defaultFont;
                }
                else
                {
                    Debug.LogWarning("[FullscreenPresenter] No TMP font asset found! Text may not render. Please import TMP Essentials via Window > TextMeshPro > Import TMP Essential Resources");
                }
            }
            
            // Additional settings for better rendering
            tmp.raycastTarget = false;
            tmp.richText = true;
            tmp.parseCtrlCharacters = true;
        }

        public override void Setup(Transform playerHead, Camera worldCamera)
        {
            SetupCanvas();
            SetupUI();
            
            canvasGroup.alpha = 0;
            
            Debug.Log("[FullscreenPresenter] Setup complete");
        }

        private void SetupCanvas()
        {
            if (!canvas)
            {
                var go = new GameObject("FullscreenStoryCanvas");
                go.transform.SetParent(transform);
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000; // On top of everything
                
                go.AddComponent<GraphicRaycaster>();
                canvasGroup = go.AddComponent<CanvasGroup>();
            }
            else if (!canvasGroup)
            {
                canvasGroup = canvas.GetComponent<CanvasGroup>() ?? canvas.gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void SetupUI()
        {
            // Background
            if (!backgroundImage)
            {
                var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bgGo.transform.SetParent(canvas.transform, false);
                backgroundImage = bgGo.GetComponent<Image>();
                var rt = backgroundImage.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
            }
            backgroundColor.a = backgroundAlpha;
            backgroundImage.color = backgroundColor;

            // Foreground Image
            if (!foregroundImage)
            {
                var imgGo = new GameObject("Image", typeof(RectTransform), typeof(Image));
                imgGo.transform.SetParent(canvas.transform, false);
                foregroundImage = imgGo.GetComponent<Image>();
                foregroundImage.preserveAspect = preserveImageAspect;
                
                var rt = foregroundImage.rectTransform;
                rt.anchorMin = new Vector2(imageArea.x, imageArea.y);
                rt.anchorMax = new Vector2(imageArea.x + imageArea.width, imageArea.y + imageArea.height);
                rt.sizeDelta = Vector2.zero;
            }

            // Subtitle Text
            if (!subtitleText)
            {
                var txtGo = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(canvas.transform, false);
                subtitleText = txtGo.GetComponent<TextMeshProUGUI>();
                
                // Initialize TMP settings
                InitializeTextMeshPro(subtitleText);
                
                var rt = subtitleText.rectTransform;
                rt.anchorMin = new Vector2(textArea.x, textArea.y);
                rt.anchorMax = new Vector2(textArea.x + textArea.width, textArea.y + textArea.height);
                rt.sizeDelta = Vector2.zero;
            }
            else
            {
                // Ensure existing subtitle is properly initialized
                InitializeTextMeshPro(subtitleText);
            }

            // Audio source for typewriter
            if (typewriterSound && !_typewriterAudioSource)
            {
                _typewriterAudioSource = gameObject.AddComponent<AudioSource>();
                _typewriterAudioSource.playOnAwake = false;
                _typewriterAudioSource.spatialBlend = 0f; // 2D sound
                _typewriterAudioSource.volume = typewriterSoundVolume;
            }
        }

        public override IEnumerator Show(CTWStoryEvent.Slide slide)
        {
            foregroundImage.sprite = slide.image;
            foregroundImage.enabled = slide.image != null;

            // Different fade modes
            switch (imageFadeMode)
            {
                case FadeMode.WithText:
                    yield return FadeTo(1f, slide.fade);
                    if (!string.IsNullOrEmpty(slide.subtitle))
                    {
                        yield return ShowText(slide.subtitle);
                    }
                    break;

                case FadeMode.ImageFirst:
                    subtitleText.text = "";
                    yield return FadeTo(1f, slide.fade);
                    if (!string.IsNullOrEmpty(slide.subtitle))
                    {
                        yield return ShowText(slide.subtitle);
                    }
                    break;

                case FadeMode.TextFirst:
                    canvasGroup.alpha = 1f;
                    foregroundImage.enabled = false;
                    if (!string.IsNullOrEmpty(slide.subtitle))
                    {
                        yield return ShowText(slide.subtitle);
                    }
                    foregroundImage.enabled = slide.image != null;
                    yield return FadeImage(0f, 1f, slide.fade);
                    break;
            }
        }

        public override IEnumerator Hide(float fadeSeconds)
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }
            
            yield return FadeTo(0f, fadeSeconds);
            subtitleText.text = "";
        }

        private IEnumerator ShowText(string text)
        {
            if (typewriterEffect)
            {
                _typewriterCoroutine = StartCoroutine(TypewriterEffect(text));
                yield return _typewriterCoroutine;
                _typewriterCoroutine = null;
            }
            else
            {
                subtitleText.text = text;
            }
        }

        private IEnumerator FadeTo(float target, float time)
        {
            float start = canvasGroup.alpha;
            float elapsed = 0;
            
            while (elapsed < time)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / time);
                canvasGroup.alpha = Mathf.Lerp(start, target, fadeCurve.Evaluate(progress));
                yield return null;
            }
            
            canvasGroup.alpha = target;
        }

        private IEnumerator FadeImage(float fromAlpha, float toAlpha, float time)
        {
            Color startColor = foregroundImage.color;
            startColor.a = fromAlpha;
            foregroundImage.color = startColor;

            float elapsed = 0;
            while (elapsed < time)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / time);
                Color c = foregroundImage.color;
                c.a = Mathf.Lerp(fromAlpha, toAlpha, fadeCurve.Evaluate(progress));
                foregroundImage.color = c;
                yield return null;
            }

            Color finalColor = foregroundImage.color;
            finalColor.a = toAlpha;
            foregroundImage.color = finalColor;
        }

        private IEnumerator TypewriterEffect(string fullText)
        {
            subtitleText.text = "";
            float charsPerSecond = typewriterSpeed;
            int totalChars = fullText.Length;
            float charInterval = 1f / charsPerSecond;
            
            for (int i = 0; i <= totalChars; i++)
            {
                subtitleText.text = fullText.Substring(0, i);
                
                // Play typewriter sound occasionally
                if (typewriterSound && _typewriterAudioSource && i < totalChars)
                {
                    char c = fullText[i];
                    if (!char.IsWhiteSpace(c) && Random.value < 0.3f)
                    {
                        _typewriterAudioSource.PlayOneShot(typewriterSound);
                    }
                }
                
                yield return new WaitForSeconds(charInterval);
            }
        }

        /// <summary>
        /// Skip typewriter effect and show full text immediately
        /// </summary>
        public void SkipTypewriter()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
                // Text will be set by the caller
            }
        }
    }
}

