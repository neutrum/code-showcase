using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;

public class CTWDrawAnchorsWithVolumeRuntime: MonoBehaviour
{
    // Prefab or primitive GameObject to represent the volume
    public GameObject volumeRepresentationPrefab;

    // Dictionary to store references to drawn objects
    private Dictionary<Meta.XR.MRUtilityKit.MRUKAnchor, GameObject> drawnVolumes = new Dictionary<Meta.XR.MRUtilityKit.MRUKAnchor, GameObject>();

    public void Run(List<MRUKAnchor> anchorList)
    {
        if (anchorList == null) return;

        foreach (var anchor in anchorList)
        {
            // Check if the anchor has a volume
            if (anchor.VolumeBounds.HasValue)
            {
                var bounds = anchor.VolumeBounds.Value;

                // Create a visual representation at runtime
                CreateVolumeRepresentation(anchor, bounds);
            }
        }
    }

    void CreateVolumeRepresentation(Meta.XR.MRUtilityKit.MRUKAnchor anchor, Bounds bounds)
    {
        // If we have a prefab to represent the volume
        if (volumeRepresentationPrefab != null)
        {
            // Instantiate the volume representation prefab
            GameObject volumeObject = (volumeRepresentationPrefab);

            // Set the size and position based on the anchor's VolumeBounds
            volumeObject.transform.position = anchor.transform.TransformPoint(bounds.center);
            volumeObject.transform.localScale = bounds.size;

            // Optionally parent to the anchor for easier management
            volumeObject.transform.SetParent(anchor.transform);

            // Store reference to the created object
            drawnVolumes[anchor] = volumeObject;
        }
        else
        {
            // If no prefab is provided, use a simple cube as a fallback
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            // Set position and size
            Transform transform1;
            cube.transform.position = (transform1 = anchor.transform).TransformPoint(bounds.center);
            cube.transform.localScale = bounds.size;
            cube.transform.rotation = transform1.localRotation;
            
            Material material = Resources.Load<Material>("Materials/CTWQuadOutline");
            // Optionally, set the cube's color or material (e.g., transparent or wireframe)
            cube.GetComponent<Renderer>().material = material;// Green, semi-transparent
            cube.transform.parent = transform1;
            // Optionally, disable collider since this is for visualization
            Destroy(cube.GetComponent<Collider>());

            // Store reference
            drawnVolumes[anchor] = cube;
        }
    }

    // Optionally, a method to clear visualizations
    public void ClearVisualizations()
    {
        foreach (var kvp in drawnVolumes)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }

        drawnVolumes.Clear();
    }
}
