using Meta.XR.MRUtilityKit;
using UnityEngine;
using Utils;

public class CTWMeshOverlay : MonoBehaviour
{
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");

    public void Run()
    {
        var material = Resources.Load<Material>("Materials/Dirt");
        var texture = Resources.Load<Texture2D>("Materials/tangled_cobweb_texture_alpha_v2");
        var roomScanData = MRUK.Instance.GetCurrentRoom();
        var globalMeshAnchor = MRUK.Instance.GetCurrentRoom().GlobalMeshAnchor.GlobalMesh;
        var reducedMesh = MeshUtils.RemoveVerticesInsideAnchors(globalMeshAnchor, MeshUtils.ConvertAllAnchors(roomScanData.Anchors), 0.5f, 0.1f, 1.8f);
        reducedMesh.RecalculateNormals();
        reducedMesh.RecalculateTangents();
        GeneratePlanarUVs(reducedMesh);
        reducedMesh.UploadMeshData(true);
        GetComponent<MeshFilter>().mesh = reducedMesh;
        transform.position = MRUK.Instance.GetCurrentRoom().GlobalMeshAnchor.transform.position;
        transform.rotation = MRUK.Instance.GetCurrentRoom().GlobalMeshAnchor.transform.rotation;
        material.SetTexture(BaseMap, texture);
        GetComponent<Renderer>().sharedMaterial = material;
        var tex = material.GetTexture(BaseMap);
    }

    private void GeneratePlanarUVs(Mesh mesh)
    {
        if (mesh == null || mesh.vertexCount == 0)
        {
            Debug.LogWarning("Mesh is null or empty — cannot generate UVs.");
            return;
        }

        Vector3[] vertices = mesh.vertices;
        // Simple planar projection on XZ (horizontal floor/ceiling) or XY (walls)
        Vector2[] uvs = new Vector2[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];

            // Get both projections
            Vector2 uvXZ = new Vector2(v.x, v.z);
            Vector2 uvXY = new Vector2(v.x, v.y);

            // Blend them — e.g. 50/50
            uvs[i] = Vector2.Lerp(uvXZ, uvXY, 0.5f);
        }

        mesh.uv = uvs;
    }
}
