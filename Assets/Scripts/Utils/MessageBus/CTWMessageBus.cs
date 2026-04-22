using System;
using UnityEngine;
using UnityEngine.Events;

namespace Utils.MessageBus
{
    public static class CTWMessageBus<T>
    {
        private static event Action<T> OnMessage;
        private static T lastMessage;
        private static bool hasMessage = false;

        public static void Subscribe(Action<T> callback, bool receiveLast = false)
        {
            OnMessage += callback;
            CTWLog.Verbose($"Subscribing to {typeof(T).Name}");

            if (receiveLast && hasMessage)
                callback(lastMessage);
        }

        public static void Unsubscribe(Action<T> callback)
        {
            OnMessage -= callback;
        }

        public static void Send(T message)
        {
            CTWLog.Verbose($"Sending message: {typeof(T).Name} => {message}");
            lastMessage = message;
            hasMessage = true;
            OnMessage?.Invoke(message);
        }
    }

    public interface ICTWMessage {}

    [Serializable]
    public abstract class CTWMessageSubscriptionBase
    {
        public abstract void Subscribe();
        public abstract void Unsubscribe();
    }

    [System.Serializable]
    public class CTWMessageSubscription<T> : CTWMessageSubscriptionBase where T : ICTWMessage
    {
        public UnityEvent<T> onMessage = new();
        public bool receiveLast = false;

        private Action<T> handler;

        public override void Subscribe()
        {
            handler = (msg) => onMessage.Invoke(msg);
            CTWMessageBus<T>.Subscribe(handler, receiveLast);
        }

        public override void Unsubscribe()
        {
            if (handler != null)
                CTWMessageBus<T>.Unsubscribe(handler);
        }
    }
}
