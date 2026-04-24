using UnityEngine;

namespace Utils.VR
{
    public class CTWSmoothHandFollow : MonoBehaviour
    {
        public Transform xrHandTarget; // XR Input hand/controller target (from Meta XR SDK)
        public float followSpeed = 20f;
        public float rotationSpeed = 20f;

        private Rigidbody handRb;

        void Awake()
        {
            handRb = GetComponent<Rigidbody>();
            handRb.interpolation = RigidbodyInterpolation.Interpolate; // Ensure smooth physics visuals
        }

        void Update()
        {
            if (xrHandTarget == null)
                return;

            // Smooth position follow
            Vector3 targetPos = Vector3.Lerp(transform.position, xrHandTarget.position, followSpeed * Time.deltaTime);
        
            // Smooth rotation follow
            Quaternion targetRot = Quaternion.Slerp(transform.rotation, xrHandTarget.rotation, rotationSpeed * Time.deltaTime);

            // Move hand Rigidbody via MovePosition/MoveRotation (physics-safe)
            handRb.MovePosition(targetPos);
            handRb.MoveRotation(targetRot);
        }
    }
}