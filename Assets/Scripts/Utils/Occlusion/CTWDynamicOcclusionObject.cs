using UnityEngine;

namespace Utils
{
    [RequireComponent(typeof(Renderer))]
    public class CTWDynamicOcclusionObject : MonoBehaviour
    {
        private Renderer rend;

        private void Awake()
        {
            rend = GetComponent<Renderer>();
        }

        private void OnBecameVisible()
        {
            rend.enabled = true;
        }

        private void OnBecameInvisible()
        {
            rend.enabled = false;
        }
    }
}