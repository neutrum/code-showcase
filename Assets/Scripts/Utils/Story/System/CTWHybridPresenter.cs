using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CTW.Story
{
    /// <summary>
    /// Meta-presenter that delegates to different presenters based on slide configuration.
    /// Use slide.subtitle prefix tags like [billboard], [fullscreen], [diorama], [event] to route.
    /// Allows mixing presentation styles within a single story event sequence.
    /// </summary>
    public class CTWHybridPresenter : CTWStoryPresenter
    {
        [System.Serializable]
        public class PresenterMapping
        {
            [Tooltip("Prefix tag to match in slide.subtitle, e.g. 'billboard', 'fullscreen', 'diorama'")]
            public string tag;
            
            [Tooltip("The presenter prefab to use for this tag")]
            public CTWStoryPresenter presenterPrefab;
            
            [HideInInspector] public CTWStoryPresenter instance;
        }

        [Header("Presenter Mappings")]
        [Tooltip("Define presenter prefabs for different tags")]
        public List<PresenterMapping> presenters = new();

        [Header("Fallback")]
        [Tooltip("Default presenter if no tag matches")]
        public CTWStoryPresenter defaultPresenterPrefab;
        private CTWStoryPresenter _defaultPresenterInstance;

        [Header("Settings")]
        [Tooltip("Tag format: [tag] at start of subtitle")]
        public string tagPrefix = "[";
        public string tagSuffix = "]";
        
        [Tooltip("Remove tag from subtitle when showing?")]
        public bool stripTagFromSubtitle = true;

        private CTWStoryPresenter _currentPresenter;
        private CTWStoryPresenter _previousPresenter;
        private Transform _head;
        private Camera _camera;

        public override void Setup(Transform playerHead, Camera worldCamera)
        {
            _head = playerHead;
            _camera = worldCamera;
            
            // Instantiate and setup all mapped presenters
            foreach (var mapping in presenters)
            {
                if (mapping.presenterPrefab != null)
                {
                    mapping.instance = Instantiate(mapping.presenterPrefab, transform);
                    mapping.instance.gameObject.name = $"Presenter_{mapping.tag}";
                    mapping.instance.Setup(playerHead, worldCamera);
                    mapping.instance.gameObject.SetActive(false); // Start inactive
                    
                    Debug.Log($"[HybridPresenter] Setup presenter for tag: [{mapping.tag}]");
                }
            }
            
            // Setup default presenter
            if (defaultPresenterPrefab != null)
            {
                _defaultPresenterInstance = Instantiate(defaultPresenterPrefab, transform);
                _defaultPresenterInstance.gameObject.name = "Presenter_Default";
                _defaultPresenterInstance.Setup(playerHead, worldCamera);
                _defaultPresenterInstance.gameObject.SetActive(false);
                
                Debug.Log("[HybridPresenter] Setup default presenter");
            }

            Debug.Log($"[HybridPresenter] Setup complete with {presenters.Count} mapped presenters");
        }

        public override IEnumerator Show(CTWStoryEvent.Slide slide)
        {
            // Parse slide to determine presenter
            var (presenter, modifiedSlide) = GetPresenterForSlide(slide);
            
            if (presenter == null)
            {
                Debug.LogError("[HybridPresenter] No presenter available for slide!");
                yield break;
            }

            // If switching presenters, hide the previous one first
            if (_previousPresenter != null && _previousPresenter != presenter)
            {
                yield return _previousPresenter.Hide(0.2f);
                _previousPresenter.gameObject.SetActive(false);
            }

            // Activate and show current presenter
            _currentPresenter = presenter;
            _currentPresenter.gameObject.SetActive(true);
            
            Debug.Log($"[HybridPresenter] Using presenter: {_currentPresenter.GetType().Name}");
            
            yield return _currentPresenter.Show(modifiedSlide);
            
            _previousPresenter = _currentPresenter;
        }

        public override IEnumerator Hide(float fadeSeconds)
        {
            if (_currentPresenter != null)
            {
                yield return _currentPresenter.Hide(fadeSeconds);
                _currentPresenter.gameObject.SetActive(false);
            }
        }

        private (CTWStoryPresenter presenter, CTWStoryEvent.Slide modifiedSlide) GetPresenterForSlide(CTWStoryEvent.Slide slide)
        {
            CTWStoryPresenter selectedPresenter = null;
            CTWStoryEvent.Slide modifiedSlide = slide;

            // Check if subtitle contains a tag
            if (!string.IsNullOrEmpty(slide.subtitle))
            {
                string tag = ExtractTag(slide.subtitle);
                
                if (!string.IsNullOrEmpty(tag))
                {
                    // Find matching presenter
                    var mapping = presenters.Find(m => m.tag.Equals(tag, System.StringComparison.OrdinalIgnoreCase));
                    
                    if (mapping != null && mapping.instance != null)
                    {
                        selectedPresenter = mapping.instance;
                        
                        // Strip tag if requested
                        if (stripTagFromSubtitle)
                        {
                            modifiedSlide = CloneSlideWithoutTag(slide, tag);
                        }
                        
                        Debug.Log($"[HybridPresenter] Matched tag [{tag}] to presenter: {selectedPresenter.GetType().Name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[HybridPresenter] Tag [{tag}] found but no presenter mapped!");
                    }
                }
            }

            // Use default if no match
            if (selectedPresenter == null)
            {
                selectedPresenter = _defaultPresenterInstance;
                Debug.Log("[HybridPresenter] Using default presenter");
            }

            return (selectedPresenter, modifiedSlide);
        }

        private string ExtractTag(string subtitle)
        {
            if (subtitle.StartsWith(tagPrefix))
            {
                int endIndex = subtitle.IndexOf(tagSuffix);
                if (endIndex > tagPrefix.Length)
                {
                    return subtitle.Substring(tagPrefix.Length, endIndex - tagPrefix.Length);
                }
            }
            return null;
        }

        private CTWStoryEvent.Slide CloneSlideWithoutTag(CTWStoryEvent.Slide original, string tag)
        {
            // Create a modified copy of the slide with tag removed from subtitle
            var clone = new CTWStoryEvent.Slide
            {
                image = original.image,
                subtitle = original.subtitle.Replace($"{tagPrefix}{tag}{tagSuffix}", "").Trim(),
                audio = original.audio,
                duration = original.duration,
                fade = original.fade,
                waitForInput = original.waitForInput
            };
            return clone;
        }

        /// <summary>
        /// Manually switch to a specific presenter by tag (useful for testing)
        /// </summary>
        public void ForcePresenter(string tag)
        {
            var mapping = presenters.Find(m => m.tag == tag);
            if (mapping != null && mapping.instance != null)
            {
                if (_currentPresenter != null)
                {
                    _currentPresenter.gameObject.SetActive(false);
                }
                _currentPresenter = mapping.instance;
                _currentPresenter.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Get list of available presenter tags
        /// </summary>
        public List<string> GetAvailableTags()
        {
            var tags = new List<string>();
            foreach (var mapping in presenters)
            {
                if (mapping.instance != null)
                {
                    tags.Add(mapping.tag);
                }
            }
            return tags;
        }
    }
}

