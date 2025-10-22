using System.Collections.Generic;
using UnityEngine;

namespace CTW.Story
{
    [CreateAssetMenu(menuName = "CTW/Story Event", fileName = "CTW_SE_NewEvent")]
    public class CTWStoryEvent : ScriptableObject
    {
        [Header("Identity / Logic")]
        public string eventId;                 // Optional identifier for once-only / analytics
        public bool onlyOnce = true;
        public List<string> requireFlags;      // All must be set (optional)
        public List<string> forbidFlags;       // None may be set (optional)
        public List<string> setFlags;          // Set when event finishes (optional)

        [System.Serializable]
        public class Slide
        {
            public Sprite image;
            [TextArea] public string subtitle;
            public AudioClip audio;            // Optional voiceover / sfx
            public float duration = 3f;        // Seconds (ignored if waitForInput)
            public float fade = 0.25f;         // Seconds to fade in/out
            public bool waitForInput;          // Require player input to advance
        }

        [Header("Slides")]
        public List<Slide> slides = new();
    }
}
