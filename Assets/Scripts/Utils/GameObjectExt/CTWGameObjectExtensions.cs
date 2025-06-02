using UnityEngine;

namespace Utils.GameObjectExt
{
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Gets the component of type T, or adds one if it doesn't exist.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }
        
        public static Bounds GetOrCalculateCombinedBounds(this GameObject obj, float expandBy = 0.05f)
        {
            // Check if already has a cache component
            var cache = obj.GetComponent<CTWBoundsCacheHybrid>();
            if (cache == null)
            {
                cache = obj.AddComponent<CTWBoundsCacheHybrid>();
                cache.Calculate(expandBy);
            }
            return cache.CachedBounds;
        }
    }
}