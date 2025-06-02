using UnityEngine;
using UnityEngine.Events;

namespace Utils.Events
{
    public class CTWProximityEvent : MonoBehaviour
    {
        [Header("Proximity Distance (meters)")]
        public float Threshold = 0.5f;

        [Header("When Main Camera ENTERS Range")]
        public UnityEvent OnEnter;

        [Header("When Main Camera LEAVES Range")]
        public UnityEvent OnLeave;

        private bool _wasInside = false;

        void Update()
        {
            if (Camera.main == null)
                return;

            Vector3 camPos = Camera.main.transform.position;
            Vector3 targetPos = transform.position;

            camPos.y = 0;
            targetPos.y = 0;

            float distance = Vector3.Distance(camPos, targetPos);
            bool isInside = distance <= Threshold;

            if (isInside && !_wasInside)
            {
                _wasInside = true;
                OnEnter?.Invoke();
            }
            else if (!isInside && _wasInside)
            {
                _wasInside = false;
                OnLeave?.Invoke();
            }
        }
    }
}
