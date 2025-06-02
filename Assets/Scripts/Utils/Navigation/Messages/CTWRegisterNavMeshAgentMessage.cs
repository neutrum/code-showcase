using UnityEngine.AI;
using Utils.MessageBus;

namespace Utils.Navigation.Messages
{
    [System.Serializable]
    public class CTWRegisterNavMeshAgentMessage : ICTWMessage
    {
        public NavMeshAgent NavMeshAgent;

        public CTWRegisterNavMeshAgentMessage(NavMeshAgent navMeshAgent)
        {
            NavMeshAgent = navMeshAgent;
        }
    }
}