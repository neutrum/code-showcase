using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Utils.MessageBus;

namespace CTW.Story
{
    /// <summary>
    /// Invisible presenter that just fires UnityEvents and MessageBus messages.
    /// Perfect for triggering gameplay without showing UI.
    /// Use slide.subtitle as event key/identifier.
    /// </summary>
    public class CTWEventOnlyPresenter : CTWStoryPresenter
    {
        [System.Serializable]
        public class EventMapping
        {
            [Tooltip("Match this key with slide.subtitle to trigger events")]
            public string eventKey;
            public UnityEvent onShow;
            public UnityEvent onHide;
        }

        [Header("Event Mappings")]
        public List<EventMapping> eventMappings = new();

        [Header("Message Bus Integration")]
        [Tooltip("If true, sends slide.subtitle as a generic message via CTWMessageBus")]
        public bool sendToMessageBus = false;

        [Header("Debug")]
        public bool logEvents = true;

        public override void Setup(Transform playerHead, Camera worldCamera)
        {
            // No visual setup needed
            if (logEvents)
                Debug.Log("[EventOnlyPresenter] Setup complete - ready to fire events");
        }

        public override IEnumerator Show(CTWStoryEvent.Slide slide)
        {
            if (logEvents)
                Debug.Log($"[EventOnlyPresenter] Show: {slide.subtitle}");

            // Find matching event mapping
            if (!string.IsNullOrEmpty(slide.subtitle))
            {
                var mapping = eventMappings.Find(m => m.eventKey == slide.subtitle);
                if (mapping != null)
                {
                    mapping.onShow?.Invoke();
                    if (logEvents)
                        Debug.Log($"[EventOnlyPresenter] Fired onShow event for: {slide.subtitle}");
                }
                else if (logEvents)
                {
                    Debug.LogWarning($"[EventOnlyPresenter] No mapping found for key: {slide.subtitle}");
                }

                // Optional: Send to message bus
                if (sendToMessageBus)
                {
                    // You can customize this to send specific message types
                    // For now, just log - you'd integrate with your actual message types
                    if (logEvents)
                        Debug.Log($"[EventOnlyPresenter] Would send message: {slide.subtitle}");
                    
                    // Example integration:
                    // CTWMessageBus<YourMessageType>.Send(new YourMessageType(slide.subtitle));
                }
            }

            yield return null; // Complete instantly
        }

        public override IEnumerator Hide(float fadeSeconds)
        {
            if (logEvents)
                Debug.Log("[EventOnlyPresenter] Hide");
            
            yield return null; // Complete instantly
        }

        /// <summary>
        /// Manually trigger an event by key (useful for testing)
        /// </summary>
        public void TriggerEvent(string eventKey)
        {
            var mapping = eventMappings.Find(m => m.eventKey == eventKey);
            mapping?.onShow?.Invoke();
        }
    }
}

