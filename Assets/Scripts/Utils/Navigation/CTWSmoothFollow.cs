using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Utils.MessageBus;
using Utils.Navigation.Messages;

namespace Utils.Navigation
{
    public class CTWSmoothFollow : MonoBehaviour
    {
        public NavMeshAgent NavAgent;

// Start is called before the first frame update
        void Start()
        {
            NavAgent = GetComponent<NavMeshAgent>();
            CTWMessageBus<CTWRegisterNavMeshAgentMessage>.Send(new CTWRegisterNavMeshAgentMessage(NavAgent));
        }


// Update is called once per frame
        void Update()
        {
            if (NavAgent.isOnNavMesh)
            {
                var destination = Camera.main.transform.position;
                destination.y = transform.position.y;
                NavAgent.SetDestination(destination);
            }
            else
            {
                Debug.LogWarning("Agent is not on NavMesh!");
            }
            
        }
    }
}