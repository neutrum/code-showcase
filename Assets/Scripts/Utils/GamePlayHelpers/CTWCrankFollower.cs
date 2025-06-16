using UnityEngine;

namespace Utils.GamePlayHelpers
{
    public class CTWCrankFollower : MonoBehaviour
    {
        public Transform target;

        void LateUpdate()
        {
            var transform1 = transform;
            transform1.position = target.position;
            transform1.rotation = target.rotation;
        }
    }
}
