using Meta.XR.MRUtilityKit;
using UnityEngine;
using Utils.MessageBus;
using Utils.Navigation.Messages;

public class CTWNavigationHelper : MonoBehaviour
{
    public void NavMeshEnabled()
    {
        Debug.Log("Called");
        CTWMessageBus<CTWNavMeshEnabledMessage>.Send(new CTWNavMeshEnabledMessage());
    }

    public void RegisterNavAgent(CTWRegisterNavMeshAgentMessage message)
    {
        var sceneNavigation = GetComponent<SceneNavigation>();
        sceneNavigation.Agents.Add(message.NavMeshAgent);
        RebakeSurface(sceneNavigation);
    }

    public void RebakeSurface(SceneNavigation sceneNavigation)
    {
        
        sceneNavigation.BuildSceneNavMeshForRoom(MRUK.Instance.GetCurrentRoom());
    }
}
