#nullable enable
using System;
using UnityEngine;

namespace Utils.GameObjectExt
{
    public class CTWCameraFrustrumDetector : MonoBehaviour
    {
        public Camera targetCamera; // Set externally or via inspector
        public Action? onEnterFrustum;
        public Action? onExitFrustum;

        private bool wasVisible = false;
        private Renderer rend;

        private void Awake()
        {
            rend = GetComponent<Renderer>();
        }

        private void Update()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
            if (targetCamera == null || rend == null)
                return;

            // Early out if not visible to any camera
            if (!rend.isVisible)
            {
                if (wasVisible)
                {
                    wasVisible = false;
                    onExitFrustum?.Invoke();
                }
                return;
            }

            var planes = GeometryUtility.CalculateFrustumPlanes(targetCamera);
            var bounds = rend.bounds;
            bool isVisibleToCamera = GeometryUtility.TestPlanesAABB(planes, bounds);

            if (isVisibleToCamera && !wasVisible)
            {
                wasVisible = true;
                onEnterFrustum?.Invoke();
            }
            else if (!isVisibleToCamera && wasVisible)
            {
                wasVisible = false;
                onExitFrustum?.Invoke();
            }
        }
    }
}
