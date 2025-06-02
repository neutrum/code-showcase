using UnityEngine;

public class CTWLookAtCamera : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get main camera position
        Vector3 camPosition = Camera.main.transform.position;

        // Flatten the Y axis (keep object’s current Y)
        camPosition.y = transform.position.y;

        // Look at camera only on XZ
        transform.LookAt(camPosition);
    }
}
