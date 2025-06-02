using System.Linq;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace Utils
{
    public class CTWUtilities
    {
        public static Bounds GetFakeBounds(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("Provided prefab is null.");
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            var fakeBounds = prefab.GetComponentInChildren<CTWFakeBounds>();
            if (fakeBounds == null) return new Bounds(Vector3.zero, Vector3.zero);
            return fakeBounds.GetBounds();
        }
        
        public static Bounds GetMaxBoundsOverFirstAnimation(GameObject prefab, float samplingFPS = 30f)
        {
            if (prefab == null)
            {
                Debug.LogError("Provided prefab is null.");
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            GameObject instance = GameObject.Instantiate(prefab);
            instance.SetActive(true);

            try
            {
                Animator animator = instance.GetComponentInChildren<Animator>();
                if (animator == null)
                {
                    Debug.LogError("Animator not found on prefab.");
                    return new Bounds(Vector3.zero, Vector3.zero);
                }

                RuntimeAnimatorController controller = animator.runtimeAnimatorController;
                if (controller == null || controller.animationClips.Length == 0)
                {
                    Debug.LogError("No animation clips found on Animator Controller.");
                    return new Bounds(Vector3.zero, Vector3.zero);
                }

                AnimationClip clip = controller.animationClips[0];
                float duration = clip.length;
                int sampleCount = Mathf.CeilToInt(duration * samplingFPS);

                Mesh bakedMesh = new Mesh();
                Bounds? accumulatedBounds = null;

                SkinnedMeshRenderer[] skinnedMeshes = instance.GetComponentsInChildren<SkinnedMeshRenderer>();

                for (int i = 0; i <= sampleCount; i++)
                {
                    float normalizedTime = i / (float)sampleCount;

                    animator.Play(clip.name, 0, normalizedTime);
                    animator.Update(0f); // Sample pose

                    foreach (var smr in skinnedMeshes)
                    {
                        smr.BakeMesh(bakedMesh);
                        Bounds localBounds = TransformBoundsToWorld(smr.transform, bakedMesh.bounds);

                        if (accumulatedBounds == null)
                            accumulatedBounds = localBounds;
                        else
                        {
                            accumulatedBounds.Value.Encapsulate(localBounds.min);
                            accumulatedBounds.Value.Encapsulate(localBounds.max);
                        }
                    }
                }

                return accumulatedBounds ?? new Bounds(Vector3.zero, Vector3.zero);
            }
            finally
            {
                GameObject.DestroyImmediate(instance);
            }
        }

        private static Bounds TransformBoundsToWorld(Transform transform, Bounds localBounds)
        {
            // Get the 8 corners of the bounds in local space, then transform them to world
            Vector3 center = localBounds.center;
            Vector3 extents = localBounds.extents;

            Vector3[] localCorners = new Vector3[8]
            {
                center + new Vector3(-extents.x, -extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y, extents.z),
                center + new Vector3(-extents.x, extents.y, -extents.z),
                center + new Vector3(-extents.x, extents.y, extents.z),
                center + new Vector3(extents.x, -extents.y, -extents.z),
                center + new Vector3(extents.x, -extents.y, extents.z),
                center + new Vector3(extents.x, extents.y, -extents.z),
                center + new Vector3(extents.x, extents.y, extents.z),
            };

            Bounds worldBounds = new Bounds(transform.TransformPoint(localCorners[0]), Vector3.zero);
            for (int i = 1; i < 8; i++)
            {
                worldBounds.Encapsulate(transform.TransformPoint(localCorners[i]));
            }

            return worldBounds;
        }
        
        public static Bounds GetMaxBoundsOverFirstAnimationV2(GameObject animatedPrefab)
        {
            var animator = animatedPrefab.GetComponentInChildren<Animator>();

            var instance = GameObject.Instantiate(animatedPrefab);
            var skinnedMeshRenderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>();

            Bounds totalBounds = new Bounds(instance.transform.position, Vector3.zero);
            float duration = animator.runtimeAnimatorController.animationClips[0].length;

            const int sampleRate = 30;
            float step = duration / sampleRate;

            for (float t = 0; t <= duration; t += step)
            {
                animator.Play(animator.GetCurrentAnimatorStateInfo(0).shortNameHash, 0, t / duration);
                animator.Update(0);

                foreach (var smr in skinnedMeshRenderers)
                {
                    totalBounds.Encapsulate(smr.bounds);
                }
            }
            
            GameObject.DestroyImmediate(instance);
            return totalBounds;
        }
        
        public static void DrawBoundsDebug(Bounds bounds, Color color, float duration = 1f)
        {
            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;

            Vector3[] corners = new Vector3[8]
            {
                c + new Vector3( e.x,  e.y,  e.z),
                c + new Vector3( e.x,  e.y, -e.z),
                c + new Vector3( e.x, -e.y,  e.z),
                c + new Vector3( e.x, -e.y, -e.z),
                c + new Vector3(-e.x,  e.y,  e.z),
                c + new Vector3(-e.x,  e.y, -e.z),
                c + new Vector3(-e.x, -e.y,  e.z),
                c + new Vector3(-e.x, -e.y, -e.z)
            };

            void DrawLine(int i, int j) => Debug.DrawLine(corners[i], corners[j], color, duration);

            // top square
            DrawLine(0, 1); DrawLine(1, 3); DrawLine(3, 2); DrawLine(2, 0);
            // bottom square
            DrawLine(4, 5); DrawLine(5, 7); DrawLine(7, 6); DrawLine(6, 4);
            // vertical lines
            DrawLine(0, 4); DrawLine(1, 5); DrawLine(2, 6); DrawLine(3, 7);
        }
        
        public static void DrawWireBounds(Bounds bounds, Vector3 position, Quaternion rotation, Color color, float duration = 5f)
        {
            Vector3 center = position + rotation * bounds.center;
            Vector3 size = bounds.size;

            Vector3[] corners = new Vector3[8];
            Vector3 ext = size * 0.5f;

            // Local space corners
            corners[0] = center + rotation * new Vector3(-ext.x, -ext.y, -ext.z);
            corners[1] = center + rotation * new Vector3(ext.x, -ext.y, -ext.z);
            corners[2] = center + rotation * new Vector3(ext.x, -ext.y, ext.z);
            corners[3] = center + rotation * new Vector3(-ext.x, -ext.y, ext.z);
            corners[4] = center + rotation * new Vector3(-ext.x, ext.y, -ext.z);
            corners[5] = center + rotation * new Vector3(ext.x, ext.y, -ext.z);
            corners[6] = center + rotation * new Vector3(ext.x, ext.y, ext.z);
            corners[7] = center + rotation * new Vector3(-ext.x, ext.y, ext.z);

            // Bottom face
            Debug.DrawLine(corners[0], corners[1], color, duration);
            Debug.DrawLine(corners[1], corners[2], color, duration);
            Debug.DrawLine(corners[2], corners[3], color, duration);
            Debug.DrawLine(corners[3], corners[0], color, duration);

            // Top face
            Debug.DrawLine(corners[4], corners[5], color, duration);
            Debug.DrawLine(corners[5], corners[6], color, duration);
            Debug.DrawLine(corners[6], corners[7], color, duration);
            Debug.DrawLine(corners[7], corners[4], color, duration);

            // Sides
            Debug.DrawLine(corners[0], corners[4], color, duration);
            Debug.DrawLine(corners[1], corners[5], color, duration);
            Debug.DrawLine(corners[2], corners[6], color, duration);
            Debug.DrawLine(corners[3], corners[7], color, duration);
        }

    }
}