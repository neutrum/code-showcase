using UnityEngine;

namespace CTW.Story
{
    /// <summary>
    /// Generic trigger to fire a CTWStoryEvent. Works with OnTriggerEnter or via calling Fire() from code (e.g., item pickup).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CTWStoryTrigger : MonoBehaviour
    {
        public CTWStoryEvent storyEvent;           // Assign your Story Event asset
        public bool triggerOnStart;                // Optional testing
        public bool triggerOnEnter = true;         // Collider must be isTrigger
        public string requiredTag = "Player";      // Ensure your XR body/collider uses this tag
        public bool destroyAfter = true;           // One-shot

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Start()
        {
            if (triggerOnStart) Fire();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!triggerOnEnter) return;
            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;
            Fire();
        }

        public void Fire()
        {
            if (storyEvent) CTWStoryDirector.Instance?.TryPlay(storyEvent);
            if (destroyAfter) Destroy(gameObject);
        }
    }
}
