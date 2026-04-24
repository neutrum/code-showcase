using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.MRUtilityKit;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CTWRoofGenerator : MonoBehaviour
{ 
    [SerializeField] private float roofHeight = 2f;       // The height of the roof peak
    [SerializeField] private float offsetAmount = 0.5f;   // Offset for the hipped roof

    private void Start()
    {
        var ceilingAnchor = MRUK.Instance.GetCurrentRoom().Anchors
            .First(anchor => anchor.Label == MRUKAnchor.SceneLabels.CEILING);       
        Mesh roofMesh = CreateHippedRoof(ceilingAnchor.PlaneBoundary2D, roofHeight, offsetAmount);
        GetComponent<MeshFilter>().mesh = roofMesh;
        transform.position = ceilingAnchor.transform.position;
        transform.rotation =  Quaternion.Euler(transform.rotation.eulerAngles.x, 180 - ceilingAnchor.transform.rotation.y, transform.rotation.eulerAngles.z);
    }

    private Mesh CreateHippedRoof(List<Vector2> basePolygon2D, float roofHeight, float offsetAmount)
    {
        Mesh mesh = new Mesh();

        // Convert 2D points to 3D for base polygon
        Vector3[] basePolygon = new Vector3[basePolygon2D.Count];
        for (int i = 0; i < basePolygon2D.Count; i++)
        {
            basePolygon[i] = new Vector3(basePolygon2D[i].x, 0, basePolygon2D[i].y);
        }

        // Step 1: Generate inner offset points
        Vector3[] offsetPolygon = new Vector3[basePolygon.Length];
        for (int i = 0; i < basePolygon.Length; i++)
        {
            Vector3 direction = (basePolygon[i] - GetPolygonCenter(basePolygon)).normalized;
            offsetPolygon[i] = basePolygon[i] - direction * offsetAmount;
        }

        // Step 2: Define vertices
        List<Vector3> vertices = new List<Vector3>();
        vertices.AddRange(basePolygon);                 // Add outer base vertices
        vertices.AddRange(offsetPolygon);               // Add inner offset vertices

        Vector3 roofCenter = GetPolygonCenter(offsetPolygon) + Vector3.up * roofHeight;
        vertices.Add(roofCenter);                       // Roof peak vertex

        // Step 3: Define triangles
        List<int> triangles = new List<int>();

        // Create triangles for outer base to inner offset
        int baseCount = basePolygon.Length;
        for (int i = 0; i < baseCount; i++)
        {
            int next = (i + 1) % baseCount;
            triangles.Add(i);                           // Outer vertex
            triangles.Add(baseCount + next);            // Inner offset next vertex
            triangles.Add(baseCount + i);               // Inner offset current vertex

            triangles.Add(i);                           // Outer vertex
            triangles.Add(next);                        // Outer next vertex
            triangles.Add(baseCount + next);            // Inner offset next vertex
        }

        // Create triangles from inner offset vertices to the roof peak
        int roofPeakIndex = vertices.Count - 1;
        for (int i = 0; i < baseCount; i++)
        {
            int next = (i + 1) % baseCount;
            triangles.Add(baseCount + i);               // Inner offset vertex
            triangles.Add(roofPeakIndex);               // Roof peak
            triangles.Add(baseCount + next);            // Inner offset next vertex
        }

        // Step 4: Assign vertices, triangles to mesh
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    private Vector3 GetPolygonCenter(Vector3[] polygon)
    {
        Vector3 center = Vector3.zero;
        foreach (var point in polygon)
            center += point;
        return center / polygon.Length;
    }
}
