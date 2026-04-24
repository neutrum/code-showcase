using UnityEngine;

namespace Utils
{
    public class CTWSpeedTracker : MonoBehaviour
    {
        private Vector3 lastPosition; // To store the last frame's position
        private float speed; // To store the current speed
        public float slowDownThreshold = 0.5f; // Threshold to detect slowdown
        public float checkInterval = 0.1f; // Interval in seconds to check the speed
        private float timeSinceLastCheck = 0f; // Time tracker for the interval

        void Start()
        {
            // Initialize the last position to the current position at the start
            lastPosition = transform.position;
        }

        void Update()
        {
            // Accumulate time since last check
            timeSinceLastCheck += Time.deltaTime;

            // Check speed at regular intervals
            if (timeSinceLastCheck >= checkInterval)
            {
                // Calculate the current speed
                Vector3 currentPosition = transform.position;
                speed = (currentPosition - lastPosition).magnitude / timeSinceLastCheck;

                // Check if the speed is below the threshold
                if (speed < slowDownThreshold)
                {
                    Debug.Log("User is slowing down or stopped.");
                    // You can add additional logic here, such as notifying the player or marking the spot
                }
                else
                {
                    Debug.Log($"User is moving . {transform.position}");
                }

                // Update the last position and reset the timer
                lastPosition = currentPosition;
                timeSinceLastCheck = 0f;
            }
        }

        public float GetCurrentSpeed()
        {
            return speed;
        }
    }
}
