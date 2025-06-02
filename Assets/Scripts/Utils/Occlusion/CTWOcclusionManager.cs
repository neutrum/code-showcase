using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Utils.GameObjectExt;

namespace Utils
{
    public class CTWOcclusionManager : MonoBehaviour
    {
        // Singleton instance
        public static CTWOcclusionManager Instance { get; private set; }

        // Public list of renderers to manage
        public List<CTWOcludee> Occludees = new List<CTWOcludee>();

        [Header("Main Cameras")]
        public Camera CenterOcclusionCamera;
        public Camera LeftProxyOcclusionCamera;
        public Camera RightProxyOcclusionCamera;

        [Header("Frustum Settings")]
        [Range(60, 160)] public float centerFOV = 100f;
        [Range(60, 160)] public float proxyFOV = 110f;
        public float eyeOffset = 0.032f; // typical half-IPD offset in meters

        public Transform xrRigCenter; // Reference to XR Rig or XR Origin root

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // Optional: Make persistent
            // DontDestroyOnLoad(gameObject);
        }

        void LateUpdate()
        {
            if (xrRigCenter)
            {
                // Center Camera
                CenterOcclusionCamera.transform.position = xrRigCenter.position;
                CenterOcclusionCamera.transform.rotation = xrRigCenter.rotation;
                CenterOcclusionCamera.fieldOfView = centerFOV;

                // Left Proxy
                LeftProxyOcclusionCamera.transform.position = xrRigCenter.position + xrRigCenter.right * -eyeOffset;
                LeftProxyOcclusionCamera.transform.rotation = xrRigCenter.rotation;
                LeftProxyOcclusionCamera.transform.rotation *= Quaternion.Euler(0, 45f, 0);
                LeftProxyOcclusionCamera.fieldOfView = proxyFOV;

                // Right Proxy
                RightProxyOcclusionCamera.transform.position = xrRigCenter.position + xrRigCenter.right * eyeOffset;
                RightProxyOcclusionCamera.transform.rotation = xrRigCenter.rotation;
                RightProxyOcclusionCamera.transform.rotation *= Quaternion.Euler(0, -45f, 0);
                RightProxyOcclusionCamera.fieldOfView = proxyFOV;
            }
            
            float verticalInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;

            if (Mathf.Abs(verticalInput) > 0.1f) // Deadzone check
            {
                // Adjust FOV based on input direction
                float fovAdjustment = verticalInput * Time.deltaTime * 30f; // Smooth adjustment
                LeftProxyOcclusionCamera.fieldOfView = Mathf.Clamp(LeftProxyOcclusionCamera.fieldOfView + fovAdjustment, 60f, proxyFOV);
                RightProxyOcclusionCamera.fieldOfView = Mathf.Clamp(RightProxyOcclusionCamera.fieldOfView + fovAdjustment, 60f, proxyFOV);
            }
            else
            {
                // Reset to default FOV when no input
                LeftProxyOcclusionCamera.fieldOfView = Mathf.Lerp(LeftProxyOcclusionCamera.fieldOfView, proxyFOV, Time.deltaTime * 2f);
                RightProxyOcclusionCamera.fieldOfView = Mathf.Lerp(RightProxyOcclusionCamera.fieldOfView, proxyFOV, Time.deltaTime * 2f);
            }
            
            float centerverticalInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;
            
            if (Mathf.Abs(centerverticalInput) > 0.1f) 
            {
                // Adjust center FOV based on input direction
                float fovAdjustment = centerverticalInput * Time.deltaTime * 30f; // Smooth adjustment
                CenterOcclusionCamera.fieldOfView = Mathf.Clamp(CenterOcclusionCamera.fieldOfView + fovAdjustment, 60f, centerFOV);
            }
            else
            {
                // Reset to default FOV when no input
                CenterOcclusionCamera.fieldOfView = Mathf.Lerp(CenterOcclusionCamera.fieldOfView, centerFOV, Time.deltaTime * 2f);
            }
            
            foreach (var occludee in Occludees)
            {
                var obj = occludee.gameObject;
                if (obj == null) continue;

                // Get renderer bounds or fallback to object's position
                var bounds = obj.GetOrCalculateCombinedBounds(0.05f);
                
                bool isVisible = false;

                if (bounds.size != Vector3.zero)
                {
                    isVisible = IsVisible(bounds);
                }
                else
                {
                    Vector3 viewportFrustrum = CenterOcclusionCamera.WorldToViewportPoint(obj.transform.position); 
                    isVisible = viewportFrustrum.z > 0 &&
                                         viewportFrustrum.x >= 0 && viewportFrustrum.x <= 1 &&
                                         viewportFrustrum.y >= 0 && viewportFrustrum.y <= 1;

                    Debug.Log($"{obj.name} No Renderer {isVisible}");
                }

                obj.SetActive(isVisible);
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

        // Clear renderers
        public void ClearRenderers()
        {
            Occludees.Clear();
        }
        
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