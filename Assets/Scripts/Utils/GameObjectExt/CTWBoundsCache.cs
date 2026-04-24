using UnityEngine;

namespace Utils.GameObjectExt
{
    public class CTWBoundsCacheHybrid : MonoBehaviour
    {
        public Bounds CachedBounds { get; private set; }

        public void Calculate(float expandBy)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                CachedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    CachedBounds.Encapsulate(renderers[i].bounds);
                }
                CachedBounds.Expand(expandBy);
            }
            else
            {
                CachedBounds = new Bounds(transform.position, Vector3.zero);
            }
        }
    }
}