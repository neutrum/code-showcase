using UnityEngine;

namespace Utils.Actions
{
    public class TriggerZone : MonoBehaviour
    {
        public Transform JumpTarget;
        public bool ReenablePhysics;
        public bool CallOnFinished;
        private void OnTriggerEnter(Collider other)
        {
            ITriggerReceiver receiver = other.GetComponent<ITriggerReceiver>();
            if (receiver != null)
            {
                Debug.Log("Triggered");
                if (null == JumpTarget) return;
                receiver.OnTriggerEnterZone(JumpTarget, ReenablePhysics, CallOnFinished);
            }
        }
    }
}
