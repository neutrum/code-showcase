
using UnityEngine;

namespace Utils.Occlusion
{
    public class MultiOcclusionCameraManager : MonoBehaviour
    {
        [Header("Main Cameras")]
        public Camera CenterOcclusionCamera;
        public Camera LeftProxyOcclusionCamera;
        public Camera RightProxyOcclusionCamera;

        [Header("Frustum Settings")]
        [Range(60, 140)] public float centerFOV = 100f;
        [Range(60, 140)] public float proxyFOV = 110f;
        public float eyeOffset = 0.032f; // typical half-IPD offset in meters

        public Transform xrRigCenter; // Reference to XR Rig or XR Origin root

        void LateUpdate()
        {
            // Sync cameras with XR Rig center
            if (xrRigCenter)
            {
                // Center Camera
                CenterOcclusionCamera.transform.position = xrRigCenter.position;
                CenterOcclusionCamera.transform.rotation = xrRigCenter.rotation;
                CenterOcclusionCamera.fieldOfView = centerFOV;

                // Left Proxy
                LeftProxyOcclusionCamera.transform.position = xrRigCenter.position + xrRigCenter.right * -eyeOffset;
                LeftProxyOcclusionCamera.transform.rotation = xrRigCenter.rotation;
                LeftProxyOcclusionCamera.fieldOfView = proxyFOV;

                // Right Proxy
                RightProxyOcclusionCamera.transform.position = xrRigCenter.position + xrRigCenter.right * eyeOffset;
                RightProxyOcclusionCamera.transform.rotation = xrRigCenter.rotation;
                RightProxyOcclusionCamera.fieldOfView = proxyFOV;
            }
            
            
        }

        public bool IsVisible(Bounds bounds)
        {
            Plane[] centerFrustum = GeometryUtility.CalculateFrustumPlanes(CenterOcclusionCamera);
            Plane[] leftFrustum = GeometryUtility.CalculateFrustumPlanes(LeftProxyOcclusionCamera);
            Plane[] rightFrustum = GeometryUtility.CalculateFrustumPlanes(RightProxyOcclusionCamera);

            return GeometryUtility.TestPlanesAABB(centerFrustum, bounds)
                || GeometryUtility.TestPlanesAABB(leftFrustum, bounds)
                || GeometryUtility.TestPlanesAABB(rightFrustum, bounds);
        }

        // Optional debug visualizer
        private void OnDrawGizmos()
        {
            if (CenterOcclusionCamera)
                DrawFrustum(CenterOcclusionCamera, Color.yellow);
            if (LeftProxyOcclusionCamera)
                DrawFrustum(LeftProxyOcclusionCamera, Color.red);
            if (RightProxyOcclusionCamera)
                DrawFrustum(RightProxyOcclusionCamera, Color.blue);
        }

        private void DrawFrustum(Camera cam, Color color)
        {
            Gizmos.color = color;
            Gizmos.matrix = cam.transform.localToWorldMatrix;
            Gizmos.DrawFrustum(Vector3.zero, cam.fieldOfView, cam.farClipPlane, cam.nearClipPlane, cam.aspect);
        }
    }
}
