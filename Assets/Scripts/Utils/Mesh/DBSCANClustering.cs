using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class DBSCANClustering
{
    public float epsilon = 0.1f;      // Maximum distance between two points to consider them neighbors
    public int minPoints = 3;         // Minimum number of points to form a cluster
    public float cubeSize = 0.1f;     // Size of cubes representing anomalies
    public Material cubeMaterial;     // Material for cubes
    public float normalThreshold = 0.3f;  // Threshold to determine if a surface is flat

    public void Run(Mesh mesh, Transform parent)
    {
        mesh.RecalculateNormals();
        
        List<Vector3> vertices = new List<Vector3>(mesh.vertices);
        List<Vector3> normals = new List<Vector3>(mesh.normals);

        // Ensure we have the same number of normals and vertices
        if (vertices.Count != normals.Count)
        {
            Debug.LogError("Mismatch between number of vertices and normals!");
            return;
        }

        // Filter out flat surfaces
        List<int> nonFlatVertices = FilterOutFlatSurfaces(vertices, normals, normalThreshold);

        // Run DBSCAN clustering only on non-flat vertices
        List<Vector3> nonFlatPoints = GetVerticesFromIndices(vertices, nonFlatVertices);
        List<List<Vector3>> clusters = DBSCAN(nonFlatPoints, epsilon, minPoints);

        // Place cubes at the centroid of each cluster
        foreach (List<Vector3> cluster in clusters)
        {
            if (cluster.Count > 0)
            {
                // Calculate the centroid of the cluster
                Vector3 centroid = CalculateCentroid(cluster);

                // Create and position a cube at the centroid
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = centroid;
                cube.transform.localScale = Vector3.one * cubeSize;
                cube.transform.parent = parent;

                // Assign material to the cube
                if (cubeMaterial != null)
                {
                    cube.GetComponent<Renderer>().material = cubeMaterial;
                }
            }
        }
    }

    // DBSCAN Algorithm
    List<List<Vector3>> DBSCAN(List<Vector3> points, float epsilon, int minPoints)
    {
        int[] clusterLabels = new int[points.Count];
        List<List<Vector3>> clusters = new List<List<Vector3>>();
        int clusterId = 0;

        for (int i = 0; i < points.Count; i++)
        {
            if (clusterLabels[i] != 0)  // Already processed
                continue;

            List<int> neighbors = GetNeighbors(points, i, epsilon);

            if (neighbors.Count < minPoints)
            {
                // Mark as noise, but we are focusing on clusters
                clusterLabels[i] = -1;
            }
            else
            {
                // Create a new cluster
                clusterId++;
                List<Vector3> newCluster = new List<Vector3>();
                clusters.Add(newCluster);
                ExpandCluster(points, clusterLabels, i, neighbors, clusterId, epsilon, minPoints, newCluster);
            }
        }

        return clusters;
    }

    // Expands a cluster by checking all neighbors of the points
    void ExpandCluster(List<Vector3> points, int[] clusterLabels, int pointIndex, List<int> neighbors, int clusterId, float epsilon, int minPoints, List<Vector3> cluster)
    {
        clusterLabels[pointIndex] = clusterId;
        cluster.Add(points[pointIndex]);

        int i = 0;
        while (i < neighbors.Count)
        {
            int neighborIndex = neighbors[i];

            // Safety check before accessing the neighbor
            if (neighborIndex >= 0 && neighborIndex < points.Count)
            {
                if (clusterLabels[neighborIndex] == -1)
                {
                    // Change noise point to part of the cluster
                    clusterLabels[neighborIndex] = clusterId;
                    cluster.Add(points[neighborIndex]);
                }
                else if (clusterLabels[neighborIndex] == 0)
                {
                    // Add this point to the cluster
                    clusterLabels[neighborIndex] = clusterId;
                    cluster.Add(points[neighborIndex]);

                    // Find neighbors of the new point
                    List<int> newNeighbors = GetNeighbors(points, neighborIndex, epsilon);
                    if (newNeighbors.Count >= minPoints)
                    {
                        neighbors.AddRange(newNeighbors);
                    }
                }
            }

            i++;
        }
    }

    // Finds all neighbors of a point within the epsilon distance
    List<int> GetNeighbors(List<Vector3> points, int pointIndex, float epsilon)
    {
        List<int> neighbors = new List<int>();

        for (int i = 0; i < points.Count; i++)
        {
            if (i != pointIndex && Vector3.Distance(points[pointIndex], points[i]) < epsilon)
            {
                neighbors.Add(i);
            }
        }

        return neighbors;
    }

    // Filters out flat surfaces based on normals
    List<int> FilterOutFlatSurfaces(List<Vector3> vertices, List<Vector3> normals, float threshold)
    {
        List<int> nonFlatIndices = new List<int>();

        for (int i = 0; i < vertices.Count; i++)
        {
            bool isFlat = false;

            // Make sure not to go out of range
            for (int j = i + 1; j < vertices.Count && j < normals.Count; j++)
            {
                if (Vector3.Distance(vertices[i], vertices[j]) < epsilon)
                {
                    float normalDiff = Vector3.Angle(normals[i], normals[j]);
                    if (normalDiff < threshold)
                    {
                        isFlat = true;
                        break;
                    }
                }
            }

            if (!isFlat)
            {
                nonFlatIndices.Add(i);
            }
        }

        return nonFlatIndices;
    }

    // Utility function to get vertices from a list of indices
    List<Vector3> GetVerticesFromIndices(List<Vector3> vertices, List<int> indices)
    {
        List<Vector3> result = new List<Vector3>();
        foreach (int index in indices)
        {
            if (index >= 0 && index < vertices.Count) // Safety check
            {
                result.Add(vertices[index]);
            }
        }
        return result;
    }

    // Calculate the centroid of a cluster
    Vector3 CalculateCentroid(List<Vector3> cluster)
    {
        Vector3 sum = Vector3.zero;
        foreach (Vector3 point in cluster)
        {
            sum += point;
        }
        return sum / cluster.Count;
    }
}
