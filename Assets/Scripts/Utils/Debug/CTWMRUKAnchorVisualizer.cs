// MRUKAnchorVisualizer.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit; // MRUK

public class CTWMRUKAnchorVisualizer : MonoBehaviour
{
    public float cubeSize = 0.1f;
    public Material overrideMat;        // optional; otherwise we'll tint per label
    public Transform parent;            // leave null to keep at world root (recommended!)

    IEnumerator Start()
    {
        // wait until MRUK is ready
        while (MRUK.Instance == null) yield return null;
        while (MRUK.Instance.Rooms == null || MRUK.Instance.Rooms.Count == 0) yield return null;

        foreach (var room in MRUK.Instance.Rooms)
        {
            var anchors = room.GetComponentsInChildren<MRUKAnchor>(true);
            foreach (var a in anchors)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"MRUK_{a.Label}";
                if (parent != null) go.transform.SetParent(parent, false); // if null → stays at world root
                go.transform.position = a.transform.position;   // world-space
                go.transform.rotation = a.transform.rotation;
                go.transform.localScale = Vector3.one * cubeSize;

                var r = go.GetComponent<Renderer>();
                if (overrideMat) r.sharedMaterial = overrideMat;
                else
                {
                    // quick per-label tint for sanity
                    var mpb = new MaterialPropertyBlock();
                    mpb.SetColor("_BaseColor", LabelColor(a.Label));
                    r.SetPropertyBlock(mpb);
                }
            }
        }
    }

    static Color LabelColor(MRUKAnchor.SceneLabels label)
    {
        switch (label)
        {
            case MRUKAnchor.SceneLabels.FLOOR:     return new Color(0.2f, 0.8f, 0.2f);
            case MRUKAnchor.SceneLabels.CEILING:   return new Color(0.2f, 0.6f, 1f);
            case MRUKAnchor.SceneLabels.WALL_ART:
            case MRUKAnchor.SceneLabels.WALL_FACE:  return new Color(1f, 0.8f, 0.2f);
            case MRUKAnchor.SceneLabels.TABLE:     return new Color(0.9f, 0.4f, 0.2f);
            default:                                return new Color(0.9f, 0.9f, 0.9f);
        }
    }
}
