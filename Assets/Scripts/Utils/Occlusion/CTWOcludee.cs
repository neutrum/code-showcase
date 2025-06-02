using Unity.VisualScripting;
using UnityEngine;

namespace Utils
{
    public class CTWOcludee : MonoBehaviour
    {
        public bool SpeciaCondition { get; set; }
        private void Awake()
        {
            if (!SpawnedCorrectly) return;
            CTWOcclusionManager.Instance.Occludees.Add(this);
            gameObject.isStatic = true;
        }

        private void OnDestroy()
        {
            if (!SpawnedCorrectly) return;
            CTWOcclusionManager.Instance.Occludees.Remove(this);
        }

        private bool SpawnedCorrectly => CTWOcclusionManager.Instance != null;
    }
}
