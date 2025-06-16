using System;
using UnityEngine;

public class CTWWitchController : MonoBehaviour
{
    private static readonly string FRAMES = "_frames";

    public bool allowYTilt = false;
    public float baseXRotation = -88.89f;

    private Transform _cameraTransform;
    private Material _material;

    public bool destroyOnFirstLoop = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _material = GetComponent<MeshRenderer>().sharedMaterial;
        _material.SetFloat(FRAMES, 1);
        _cameraTransform = Camera.main.transform;
        transform.rotation = Quaternion.Euler(-88.89f, 0 , 0);
        Run();
    }

    private void Update()
    {
//     {
// // Direction to camera on XZ plane
//         Vector3 toCamera = _cameraTransform.position - transform.position;
//         toCamera.y = 0f;
//         toCamera.Normalize();
//
// // Compute Y-axis rotation only
//         float yAngle = Mathf.Atan2(toCamera.x, toCamera.z) * Mathf.Rad2Deg;
//
// // Combine base rotation and Y-axis rotation
//         Quaternion baseRotation = Quaternion.Euler(-88.89f, 0f, 0f); // aligns model to floor
//         Quaternion yawRotation = Quaternion.Euler(0f, yAngle, 0f);    // horizontal camera look
//
// // Final rotation: yaw * base
//         transform.rotation = yawRotation * baseRotation;

        Vector3 targetPos = _cameraTransform.position;

        if (!allowYTilt)
        {
            // Flatten target position to match object's Y (XZ plane only)
            targetPos = new Vector3(_cameraTransform.position.x, transform.position.y, _cameraTransform.position.z);
        }

        // Calculate look rotation
        Vector3 direction = targetPos - transform.position;
        float yAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        // Final rotation: apply fixed X tilt, dynamic Y, and 0 Z
        Quaternion baseRot = Quaternion.Euler(baseXRotation, 0f, 0f);
        Quaternion yawRot = Quaternion.Euler(0f, yAngle, 0f);

        transform.rotation = yawRot * baseRot;
    }

    public void Run()
    {
        _material.SetFloat(FRAMES, 16);
        if (destroyOnFirstLoop) gameObject.AddComponent<CTWDestroyOnFinish>();
    }

    public void Stop()
    {
        _material.SetFloat(FRAMES, 1);
    }
}
