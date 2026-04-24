using UnityEngine;

public class CTWProximitySwapper : MonoBehaviour
{
    public Transform TargetObject;
    public GameObject IdleStateObject;
    public GameObject InteractiveStateObject;

    // The distance at which the swap should happen
    public float proximityThreshold = 5f;

    private void Start()
    {
        // Ensure the default state is IdleState
        if (IdleStateObject != null)
        {
            IdleStateObject.SetActive(true);
        }

        if (InteractiveStateObject != null)
        {
            InteractiveStateObject.SetActive(false);
        }

        if (TargetObject == null) TargetObject = Camera.main.transform;
    }

    private void Update()
    {
        // Check the distance between this object and the target object
        var transform1 = transform;
        var position = TargetObject.position;
        float distance = Vector2.Distance(new Vector2(transform1.position.x, transform1.position.z),
            new Vector2(position.x, position.z));

        // If the object is within the proximity threshold, swap to InteractiveState
        if (distance <= proximityThreshold)
        {
            SwapToInteractiveState();
        }
        else
        {
            SwapToIdleState();
        }
    }

    // Swap to the IdleState
    private void SwapToIdleState()
    {
        // Enable IdleState object, disable InteractiveState object
        if (IdleStateObject != null && !IdleStateObject.activeSelf)
        {
            IdleStateObject.SetActive(true);
        }

        if (InteractiveStateObject != null && InteractiveStateObject.activeSelf)
        {
            InteractiveStateObject.SetActive(false);
        }
    }

    // Swap to the InteractiveState
    private void SwapToInteractiveState()
    {
        // Enable InteractiveState object, disable IdleState object
        if (InteractiveStateObject != null && !InteractiveStateObject.activeSelf)
        {
            InteractiveStateObject.SetActive(true);
        }

        if (IdleStateObject != null && IdleStateObject.activeSelf)
        {
            IdleStateObject.SetActive(false);
        }
    }
}
