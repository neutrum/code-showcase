// GateMRUKUntilRecenter.cs
#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;
using Meta.XR.MRUtilityKit;

[DefaultExecutionOrder(-32000)] // earlier than almost anything
public class CTWXRRecenter : MonoBehaviour
{
    [Tooltip("Root object(s) that contain MRUK components. If empty, the script will find them.")]
    public GameObject[] mrukRoots;

    readonly List<Behaviour> _disabled = new();

    void Awake()
    {
        // Disable any MRUK behaviours so they don't build rooms/anchors yet
        var targets = (mrukRoots != null && mrukRoots.Length > 0)
            ? mrukRoots.SelectMany(r => r.GetComponentsInChildren<Behaviour>(true))
            : FindObjectsOfType<Behaviour>(true);

        foreach (var b in targets)
        {
            if (!b) continue;
            var t = b.GetType();
            // A coarse filter: anything in MRUK namespace, or known MRUK behaviours
            if (t.Namespace != null && t.Namespace.Contains("Meta.XR.MRUtilityKit"))
            {
                if (b.enabled) { b.enabled = false; _disabled.Add(b); }
            }
        }
    }

    IEnumerator Start()
    {
        // Normalize tracking origin BEFORE letting MRUK run
        var subs = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subs);
        foreach (var s in subs) { s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor); s.TryRecenter(); }

        // Let pose settle one frame
        yield return null;

        // Re-enable MRUK behaviours
        foreach (var b in _disabled) if (b) b.enabled = true;
        _disabled.Clear();

        // If your MRUK version exposes a refresh/build method, call it now
        // (names vary by version; examples shown below—pick the one your API has)
        var mruk = MRUK.Instance;
        if (mruk != null)
        {
            // Uncomment whichever exists in your version:
            // mruk.RebuildRooms();
            // mruk.RequestSceneRefresh();
            // mruk.LoadFromDevice(); // etc.
        }

        // (Optional) wait until MRUK reports rooms ready
        yield return WaitForMRUKReady();
    }

    static IEnumerator WaitForMRUKReady()
    {
        while (MRUK.Instance == null) yield return null;
        // Many MRUK versions expose Rooms; if not, just skip this loop.
        var tries = 0;
        while (tries++ < 600)
        {
            var roomsProp = MRUK.Instance.GetType().GetProperty("Rooms");
            if (roomsProp != null)
            {
                var rooms = roomsProp.GetValue(MRUK.Instance) as System.Collections.ICollection;
                if (rooms != null && rooms.Count > 0) break;
            }
            yield return null;
        }
    }
}
