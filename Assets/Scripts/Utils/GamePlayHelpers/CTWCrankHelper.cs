using UnityEngine;

namespace Utils.GamePlayHelpers
{
    public class CTWCrankHelper : MonoBehaviour
    {
        public Transform crank;
        public Transform handle;
        public Vector3 localRotationAxis = Vector3.forward; // Local axis of crank

        private Vector3 lastPos;

        void Start()
        {
            lastPos = handle.position;
        }

        void Update()
        {
            Vector3 delta = handle.position - lastPos;
            float influence = Vector3.Dot(delta, crank.transform.right); // Adjust axis if needed

            crank.Rotate(localRotationAxis, influence * 100f, Space.Self);
            lastPos = handle.position;
        }
    }
}
