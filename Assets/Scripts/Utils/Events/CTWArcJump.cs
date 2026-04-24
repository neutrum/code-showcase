using UnityEngine;
using UnityEngine.Events;

namespace Utils.Actions
{
    public class CTWArcJump: MonoBehaviour, ITriggerReceiver
    {
        public float jumpDuration = 1.0f;
        public float arcHeight = 3f;

        [Header("Events")]
        public UnityEvent OnFinished;

        private Transform targetPosition;
        private Vector3 startPosition;
        private float elapsedTime = 0f;
        private bool isJumping = false;
        private bool reEnablePhysics = false;
        private bool callOnFinished = false;

        private Rigidbody rb;

        public void StartJump()
        {
            rb = GetComponent<Rigidbody>();

            // Disable gravity at the start
            rb.useGravity = false;
            rb.isKinematic = true;

            startPosition = transform.position;
            elapsedTime = 0f;
            isJumping = true;

            // Lock out physics while animating
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        void Update()
        {
            if (!isJumping) return;

            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / jumpDuration);

            // Linear interpolation XZ
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition.position, t);

            // Arc in Y
            float parabolicOffset = 4 * arcHeight * t * (1 - t);
            currentPos.y += parabolicOffset;

            transform.position = currentPos;

            if (t >= 1f)
            {
                isJumping = false;

                if (reEnablePhysics)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }
                if (callOnFinished) OnFinished?.Invoke();
            }
        }

        public void OnTriggerEnterZone(Transform destination, bool reenablePhysics, bool OnFinished)
        {
            targetPosition = destination;
            reEnablePhysics = reenablePhysics;
            callOnFinished = OnFinished;
            StartJump();
        }
    }
}
