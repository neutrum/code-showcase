using System.Collections.Generic;
using UnityEngine;

namespace Utils.MessageBus
{
        public class CTWGenericMessageListener : MonoBehaviour
        {
                [SerializeReference]
                public List<CTWMessageSubscriptionBase> subs = new();
                
                private void OnEnable()
                {
                        foreach (var sub in subs)
                        {
                                sub.Subscribe();
                        }
                }

                private void OnDisable()
                {
                        foreach (var sub in subs)
                        {
                                sub.Unsubscribe();
                        }
                }
        }
}

