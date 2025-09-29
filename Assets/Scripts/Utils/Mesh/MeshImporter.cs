using System.IO;
using UnityEngine;
using Utils;

public class MeshImporter : MonoBehaviour
{
    public Transform anchorRoot;
    public string meshFileName = "roomscan.mesh";// Name of the binary file
    public string roomScanFileName = "roomscan.json";
    public Material cubeMaterial;
    private Mesh originalMesh;
    [Range(0, 100)]
    [SerializeField]
    public float flatnessThreshold = 0.1f;

    void Start()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, meshFileName);

        var roomscanFilePath =  Path.Combine(Application.streamingAssetsPath, roomScanFileName);
        var roomScanData = MeshUtils.LoadRoomScan(roomscanFilePath);
        var globalMeshAnchor = roomScanData.anchors.Find(anchor => anchor.name == "GLOBAL_MESH");
        Mesh loadedMesh = MeshUtils.LoadMeshFromFile(filePath, globalMeshAnchor);
        if (loadedMesh != null)
        {
            //   var filteredAnchors = roomScanData.anchors.FindAll(anchor => anchor.name == "COUCH");
            var reloadedMesh = MeshUtils.RemoveVerticesInsideAnchors(loadedMesh, roomScanData.anchors, 0.5f, 3.0f);
            //MeshUtils.RemoveVerticesInsideAnchors(loadedMesh, roomScanData.anchors, 0.5f);
            GetComponent<MeshFilter>().mesh = reloadedMesh;
            //MeshUtils.RenderAnchorsAsCubes(filteredAnchors, anchorRoot, 0.5f);
        }
    }

    // private void ReloadMesh()
    // {
    //     originalMesh.RecalculateNormals();
    //     var reloadedMesh = MeshUtils.RemoveFlatSurfaces(originalMesh, flatnessThreshold);
    //     GetComponent<MeshFilter>().mesh = reloadedMesh;
    // }
    //
    // private void OnValidate()
    // {
    //     if (flatnessThreshold < 0 || flatnessThreshold > 100)
    //     {
    //         flatnessThreshold = Mathf.Clamp(flatnessThreshold, 0, 100); // Ensure the value stays within range
    //     }
    //
    //     Debug.Log("Value changed: " + flatnessThreshold);
    //     ReloadMesh();
    // }
}
