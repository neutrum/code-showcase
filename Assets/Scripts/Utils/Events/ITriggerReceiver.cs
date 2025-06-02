using UnityEngine;

namespace Utils.Actions
{
    public interface ITriggerReceiver
    {
        void OnTriggerEnterZone(Transform destination, bool reenablePhysics, bool onFinished);
    }
}
