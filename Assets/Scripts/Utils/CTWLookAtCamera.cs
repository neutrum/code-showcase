using UnityEngine;
using UnityEngine.Serialization;

public class CTWLookAtCamera : MonoBehaviour
{
    public Camera TargetCamera;
    public bool OnlyOnce = false;
    public float YOffset = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (TargetCamera == null)
            TargetCamera = Camera.main;

        // Get main camera position
        Vector3 camPosition = TargetCamera.transform.position;

        // Flatten the Y axis (keep object’s current Y)
        camPosition.y = transform.position.y;

        // Look at camera only on XZ
        transform.LookAt(camPosition);
    }

    void LateUpdate()
    {
        if (OnlyOnce) return;
        if (TargetCamera != null)
        {
            Vector3 offset = new Vector3(0f, -YOffset, 0f); // adjust here
            Vector3 targetPos = TargetCamera.transform.position + offset;
            transform.LookAt(targetPos);
        }
    }
}
