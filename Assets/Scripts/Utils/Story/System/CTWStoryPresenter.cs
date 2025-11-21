using System.Collections;
using UnityEngine;

namespace CTW.Story
{
    /// <summary>
    /// Pluggable presenter for story slides. Implement your own to swap visuals (billboard, fullscreen, projector, timeline, etc).
    /// </summary>
    public abstract class CTWStoryPresenter : MonoBehaviour
    {
        /// <summary>Provide XR head (for billboards) and the world camera (for canvases).</summary>
        public abstract void Setup(Transform playerHead, Camera worldCamera);
        /// <summary>Show the slide (use slide.fade for fade-in). Must finish when visible.</summary>
        public abstract IEnumerator Show(CTWStoryEvent.Slide slide);
        /// <summary>Hide the slide (fadeSeconds). Must finish when fully hidden.</summary>
        public abstract IEnumerator Hide(float fadeSeconds);
        
        /// <summary>Check if this presenter can handle audio playback. Override to return false if no AudioSource.</summary>
        public virtual bool CanHandleAudio() => GetComponent<AudioSource>() != null;
    }
}
