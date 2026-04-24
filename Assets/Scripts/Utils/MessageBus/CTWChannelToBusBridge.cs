using UnityEngine;

namespace Utils.MessageBus
{
    public class CTWChannelToBusBridge : MonoBehaviour
    {
        [SerializeField] private CTWMessageChannel channel;

        void OnEnable()  { if (channel) channel.OnMessage += Handle; }
        void OnDisable() { if (channel) channel.OnMessage -= Handle; }

        private void Handle(ICTWMessage msg)
        {
            // Replace with your actual bus call if different:
            //
        }
    }
}
