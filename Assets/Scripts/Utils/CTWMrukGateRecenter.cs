#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Meta.XR.MRUtilityKit; // MRUK

[DefaultExecutionOrder(-10000)] // run before most things
public class CTWMrukGateRecenter : MonoBehaviour
{
    [Header("Assign the GameObject that has the MRUK component")]
    public GameObject mrukRoot = null!;        // e.g., "MRUK" in the scene

    [Header("Optional: your content root (NOT the MRUK root) if you want to place it after")]
    public Transform? contentRoot;

    [Header("Tracking Origin")]
    public bool setFloorOrigin = true;
    public bool recenter = true;
    [Range(0, 5)] public int settleFrames = 1; // let HMD pose update

    void Awake()
    {
        // Prevent MRUK from building anchors/rooms in the "old" tracking space
        if (mrukRoot) mrukRoot.SetActive(false);
    }

    IEnumerator Start()
    {
        // 1) Normalize tracking origin BEFORE MRUK starts
        var inputs = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(inputs);
        foreach (var i in inputs)
        {
            if (setFloorOrigin) i.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
            if (recenter)       i.TryRecenter();
        }

        // 2) Let the XR pose settle
        for (int f = 0; f < Mathf.Max(1, settleFrames); f++) yield return null;

        // 3) Now allow MRUK to initialize in the correct space
        if (mrukRoot) mrukRoot.SetActive(true);

        // 4) Wait until MRUK is ready (rooms/anchors built)
        yield return WaitForMRUKReady();

        // 5) (Optional) place your content AFTER MRUK is ready
        if (contentRoot)
        {
            // Simple example: keep content where authored; or align to current room center/anchor if you prefer.
            // You can fetch current room via MRUK.Instance.GetCurrentRoom() (if available in your MRUK version).
        }
    }

    static IEnumerator WaitForMRUKReady()
    {
        // Works across MRUK versions: wait for Instance & at least one room/anchor
        while (MRUK.Instance == null) yield return null;

        // If your MRUK has a "Rooms" list, wait for it:
        var tries = 0;
        while (tries++ < 600) // ~10s at 60fps
        {
            var rooms = MRUK.Instance.Rooms; // in most versions this exists
            if (rooms != null && rooms.Count > 0) break;
            yield return null;
        }
    }
}
