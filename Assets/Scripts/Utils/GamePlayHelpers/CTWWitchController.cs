using System;
using UnityEngine;
using Utils.GameObjectExt;

public class CTWWitchController : MonoBehaviour
{
    private static readonly string FRAMES = "_frames";

    public bool allowYTilt = false;
    public float baseXRotation = -88.89f;
    public bool reactToCamera = false;

    private Transform _cameraTransform;
    private Material _material;

    public bool destroyOnFirstLoop = false;

    void Start()
    {
        _material = GetComponent<MeshRenderer>().sharedMaterial;
        Stop();
        _cameraTransform = Camera.main.transform;
        if (reactToCamera)
        {
            var frustrumDetector = gameObject.AddComponent<CTWCameraFrustrumDetector>();
            frustrumDetector.targetCamera = Camera.main;
            frustrumDetector.onEnterFrustum = Run;
            frustrumDetector.onExitFrustum = Stop;
        }
    }

    // private void Update()
    // {
    //
    //      Vector3 targetPos = _cameraTransform.position;
    //     //
    //     // if (!allowYTilt)
    //     // {
    //     //     // Flatten target position to match object's Y (XZ plane only)
    //     //     targetPos = new Vector3(_cameraTransform.position.x, transform.position.y, targetPos.z);
    //     // }
    //     //
    //     // // Calculate look rotation
    //     // Vector3 direction = targetPos - transform.position;
    //     // float yAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
    //     //
    //     // // Final rotation: apply fixed X tilt, dynamic Y, and 0 Z
    //     // //Quaternion baseRot = Quaternion.Euler(baseXRotation, 0f, 0f);
    //     // Quaternion yawRot = Quaternion.Euler(0f, yAngle, 0f);
    //     //
    //     // transform.rotation = yawRot; //* baseRot;
    //     transform.LookAt(targetPos);
    // }

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
