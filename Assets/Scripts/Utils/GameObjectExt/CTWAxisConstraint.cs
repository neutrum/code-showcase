// AxisConstraint.cs
// Drop on any GameObject. Configure which position/rotation axes to lock.
// Works with or without a Rigidbody. If you enable "Use Rigidbody Constraints",
// it will mirror the toggles to Rigidbody.constraints instead of manually
// correcting transform values.

using UnityEngine;

[AddComponentMenu("Utils/Axis Constraint")]
public class CTWAxisConstraint : MonoBehaviour
{
    public enum SpaceMode { World, Local }
    public enum UpdatePhase { Update, FixedUpdate, LateUpdate }

    [Header("Position Locks")]
    public bool lockPosX = false;
    public bool lockPosY = true;   // <- default: keep on horizontal plane
    public bool lockPosZ = false;
    [Tooltip("Apply position constraints in World or Local space.")]
    public SpaceMode positionSpace = SpaceMode.World;

    [Header("Rotation Locks (Euler axes)")]
    public bool lockRotX = false;
    public bool lockRotY = false;
    public bool lockRotZ = true;   // <- default: prevent roll (Z rotation)
    [Tooltip("Apply rotation constraints in World or Local space.")]
    public SpaceMode rotationSpace = SpaceMode.World;

    [Header("Baseline (captured on Start)")]
    [Tooltip("Capture the current transform components on Start and hold locked axes at these values.")]
    public bool captureBaselineOnStart = true;

    [Tooltip("If true, copy toggles into Rigidbody.constraints instead of manual correction.")]
    public bool useRigidbodyConstraints = true;

    [Header("Update")]
    public UpdatePhase updatePhase = UpdatePhase.LateUpdate;

    Vector3 _baselinePosWorld, _baselinePosLocal;
    Vector3 _baselineEulerWorld, _baselineEulerLocal;
    Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (captureBaselineOnStart)
        {
            _baselinePosWorld = transform.position;
            _baselinePosLocal = transform.localPosition;
            _baselineEulerWorld = transform.eulerAngles;
            _baselineEulerLocal = transform.localEulerAngles;
        }

        TryApplyRigidbodyConstraints();
    }

    void Update()
    {
        if (updatePhase == UpdatePhase.Update) Apply();
    }

    void FixedUpdate()
    {
        if (updatePhase == UpdatePhase.FixedUpdate) Apply();
    }

    void LateUpdate()
    {
        if (updatePhase == UpdatePhase.LateUpdate) Apply();
    }

    [ContextMenu("Capture Baseline Now")]
    public void CaptureBaselineNow()
    {
        _baselinePosWorld = transform.position;
        _baselinePosLocal = transform.localPosition;
        _baselineEulerWorld = transform.eulerAngles;
        _baselineEulerLocal = transform.localEulerAngles;
    }

    [ContextMenu("Snap To Baseline Now")]
    public void SnapToBaselineNow()
    {
        // Force a one-time snap to the stored baseline on locked axes
        Apply(true);
    }

    void Apply(bool forceSnap = false)
    {
        // If using Rigidbody constraints, avoid double-fighting
        if (useRigidbodyConstraints && _rb != null && _rb.constraints != RigidbodyConstraints.None)
            return;

        // POSITION
        if (lockPosX || lockPosY || lockPosZ)
        {
            if (positionSpace == SpaceMode.World)
            {
                Vector3 p = transform.position;
                Vector3 baseline = captureBaselineOnStart ? _baselinePosWorld : transform.position;

                if (lockPosX) p.x = baseline.x;
                if (lockPosY) p.y = baseline.y;
                if (lockPosZ) p.z = baseline.z;

                MovePosition(p);
            }
            else
            {
                Vector3 p = transform.localPosition;
                Vector3 baseline = captureBaselineOnStart ? _baselinePosLocal : transform.localPosition;

                if (lockPosX) p.x = baseline.x;
                if (lockPosY) p.y = baseline.y;
                if (lockPosZ) p.z = baseline.z;

                SetLocalPosition(p);
            }
        }

        // ROTATION
        if (lockRotX || lockRotY || lockRotZ)
        {
            if (rotationSpace == SpaceMode.World)
            {
                Vector3 e = transform.eulerAngles;
                Vector3 baseline = captureBaselineOnStart ? _baselineEulerWorld : transform.eulerAngles;

                if (lockRotX) e.x = baseline.x;
                if (lockRotY) e.y = baseline.y;
                if (lockRotZ) e.z = baseline.z;

                RotateTo(Quaternion.Euler(e));
            }
            else
            {
                Vector3 e = transform.localEulerAngles;
                Vector3 baseline = captureBaselineOnStart ? _baselineEulerLocal : transform.localEulerAngles;

                if (lockRotX) e.x = baseline.x;
                if (lockRotY) e.y = baseline.y;
                if (lockRotZ) e.z = baseline.z;

                SetLocalRotation(Quaternion.Euler(e));
            }
        }
    }

    void MovePosition(Vector3 worldPos)
    {
        if (_rb != null && _rb.isKinematic == false)
            _rb.MovePosition(worldPos);
        else
            transform.position = worldPos;
    }

    void SetLocalPosition(Vector3 localPos)
    {
        // Rigidbody doesn't support MovePosition in local space directly
        transform.localPosition = localPos;
    }

    void RotateTo(Quaternion worldRot)
    {
        if (_rb != null && _rb.isKinematic == false)
            _rb.MoveRotation(worldRot);
        else
            transform.rotation = worldRot;
    }

    void SetLocalRotation(Quaternion localRot)
    {
        transform.localRotation = localRot;
    }

    void TryApplyRigidbodyConstraints()
    {
        if (!useRigidbodyConstraints || _rb == null) return;

        RigidbodyConstraints c = RigidbodyConstraints.None;

        // Position
        if (lockPosX && positionSpace == SpaceMode.World) c |= RigidbodyConstraints.FreezePositionX;
        if (lockPosY && positionSpace == SpaceMode.World) c |= RigidbodyConstraints.FreezePositionY;
        if (lockPosZ && positionSpace == SpaceMode.World) c |= RigidbodyConstraints.FreezePositionZ;

        // Rotation
        if (lockRotX && rotationSpace == SpaceMode.World) c |= RigidbodyConstraints.FreezeRotationX;
        if (lockRotY && rotationSpace == SpaceMode.World) c |= RigidbodyConstraints.FreezeRotationY;
        if (lockRotZ && rotationSpace == SpaceMode.World) c |= RigidbodyConstraints.FreezeRotationZ;

        _rb.constraints = c;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Keep Rigidbody constraints in sync if requested (Editor-time too)
        if (Application.isPlaying == false)
        {
            _rb = GetComponent<Rigidbody>();
            TryApplyRigidbodyConstraints();
        }
    }
#endif
}
