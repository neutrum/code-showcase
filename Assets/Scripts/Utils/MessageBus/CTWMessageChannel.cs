using System;
using UnityEngine;

// for ICTWMessage

namespace Utils.MessageBus
{
    /// Create one asset (Create → CTW → Messaging → Message Channel),
    /// assign it to senders and listeners to relay messages via Inspector.
    [CreateAssetMenu(menuName = "CTW/Messaging/Message Channel", fileName = "CTWMessageChannel")]
    public class CTWMessageChannel : ScriptableObject
    {
        public event Action<ICTWMessage> OnMessage;
        public void Raise(ICTWMessage message) => OnMessage?.Invoke(message);
    }
}
