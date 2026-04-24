using System;
using System.Collections.Generic;
using System.IO;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Utils
{
    public static class MeshUtils
    {
        // Method to save a Mesh to a file
        public static void SaveMeshToFile(string filePath, Mesh mesh)
        {
            if (mesh == null)
            {
                Debug.LogError("No mesh to save.");
                return;
            }

            using (BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
            {
                // Write vertices
                Vector3[] vertices = mesh.vertices;
                writer.Write(vertices.Length);
                foreach (Vector3 vertex in vertices)
                {
                    writer.Write(vertex.x);
                    writer.Write(vertex.y);
                    writer.Write(vertex.z);
                }

                // Write normals
                Vector3[] normals = mesh.normals;
                writer.Write(normals.Length);
                foreach (Vector3 normal in normals)
                {
                    writer.Write(normal.x);
                    writer.Write(normal.y);
                    writer.Write(normal.z);
                }

                // Write triangles (indices)
                int[] triangles = mesh.triangles;
                writer.Write(triangles.Length);
                foreach (int triangle in triangles)
                {
                    writer.Write(triangle);
                }

                // Write UVs
                Vector2[] uvs = mesh.uv;
                writer.Write(uvs.Length);
                foreach (Vector2 uv in uvs)
                {
                    writer.Write(uv.x);
                    writer.Write(uv.y);
                }

                Debug.Log($"Mesh saved to {filePath}");
            }
        }

        // Method to load a Mesh from a file
        public static Mesh LoadMeshFromFile(string filePath, RoomScanData.AnchorData correction = null)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError("File not found: " + filePath);
                return null;
            }

            Mesh mesh = new Mesh();

            using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                // Read vertices
                int vertexCount = reader.ReadInt32();
                Vector3[] vertices = new Vector3[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    float z = reader.ReadSingle();
                    vertices[i] = new Vector3(x, y, z);
                }

                // Apply anchor correction if provided
                if (correction != null)
                {
                    Quaternion rotationCorrection = correction.rotation;
                    Vector3 positionCorrection = correction.position;

                    for (int i = 0; i < vertices.Length; i++)
                    {
                        // Apply rotation
                        vertices[i] = rotationCorrection * vertices[i];
                        // Apply translation
                        vertices[i] += positionCorrection;
                    }
                }

                mesh.vertices = vertices;

                // Read normals
                int normalCount = reader.ReadInt32();
                Vector3[] normals = new Vector3[normalCount];
                for (int i = 0; i < normalCount; i++)
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    float z = reader.ReadSingle();
                    normals[i] = new Vector3(x, y, z);
                }

                mesh.normals = normals;

                // Read triangles (indices)
                int triangleCount = reader.ReadInt32();
                int[] triangles = new int[triangleCount];
                for (int i = 0; i < triangleCount; i++)
                {
                    triangles[i] = reader.ReadInt32();
                }

                mesh.triangles = triangles;

                // Read UVs
                int uvCount = reader.ReadInt32();
                Vector2[] uvs = new Vector2[uvCount];
                for (int i = 0; i < uvCount; i++)
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    uvs[i] = new Vector2(x, y);
                }

                mesh.uv = uvs;
            }

            Debug.Log($"Mesh loaded from {filePath}");
            return mesh;
        }

        public static List<RoomScanData.AnchorData> ConvertAllAnchors(List<MRUKAnchor> anchors)
        {
            var anchorList = new List<RoomScanData.AnchorData>();
            foreach (var anchor in anchors)
            {
                var anchorData = ConvertAnchor(anchor);
                anchorList.Add(anchorData);
            }

            return anchorList;
        }

        public static RoomScanData.AnchorData ConvertAnchor(MRUKAnchor anchor)
        {
            var transform = anchor.transform;
            Rect planeRect = (Rect)((anchor.PlaneRect == null) ? new Rect(0, 0, 0, 0) : anchor.PlaneRect);
            Bounds volumeBounds = (Bounds)((anchor.VolumeBounds == null) ? new Bounds() : anchor.VolumeBounds);

            RoomScanData.AnchorData anchorData = new RoomScanData.AnchorData
            {
                name = anchor.Label.ToString(),
                position = transform.position,
                rotation = transform.rotation,
                VolumeBounds = new SerializableBounds(volumeBounds),
                PlaneRect = new SerializableRect(planeRect),
                PlaneBoundary2D = anchor.PlaneBoundary2D
            };
            return anchorData;
        }

        public static Mesh RemoveFlatSurfaces(Mesh mesh, float flatnessThreshold)
        {
            // Get vertices, normals, and triangles
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;

            List<int> newTriangles = new List<int>();

            // Loop through triangles (each triangle has 3 indices)
            for (int i = 0; i < triangles.Length; i += 3)
            {
                // Get the indices of the triangle's vertices
                int index0 = triangles[i];
                int index1 = triangles[i + 1];
                int index2 = triangles[i + 2];

                // Get the normals of the triangle's vertices
                Vector3 normal0 = normals[index0];
                Vector3 normal1 = normals[index1];
                Vector3 normal2 = normals[index2];

                // Check if the normals are "similar" (i.e., if the surface is flat)
                if (IsFlatSurface(normal0, normal1, normal2, flatnessThreshold))
                {
                    // Skip this triangle (flat surface)
                    continue;
                }

                // If not flat, add the triangle to the new mesh
                newTriangles.Add(index0);
                newTriangles.Add(index1);
                newTriangles.Add(index2);
            }

            // Create a new mesh with only the non-flat triangles
            Mesh newMesh = new Mesh();
            newMesh.vertices = vertices;
            newMesh.normals = normals;
            newMesh.triangles = newTriangles.ToArray();

            // Assign the new mesh to the mesh filter
            return newMesh;
        }

        // Helper function to check if a surface is flat
        static bool IsFlatSurface(Vector3 normal0, Vector3 normal1, Vector3 normal2, float flatnessThreshold)
        {
            // Compare the angles between the normals
            float angle01 = Vector3.Angle(normal0, normal1);
            float angle12 = Vector3.Angle(normal1, normal2);
            float angle20 = Vector3.Angle(normal2, normal0);

            // If all angles between normals are smaller than the flatness threshold, it's a flat surface
            return (angle01 < flatnessThreshold && angle12 < flatnessThreshold && angle20 < flatnessThreshold);
        }

        // public static void SaveRoomScan()
        // {
        //     string roomScanFilepath = Application.persistentDataPath + $"/roomscan.json";
        //     string meshFilePath = Application.persistentDataPath + $"/roommesh.mesh";
        //     var currentRoom = MRUK.Instance.GetCurrentRoom();
        //     SaveMeshToFile(meshFilePath, currentRoom.GlobalMeshAnchor.GlobalMesh);
        //     string jsonData = JsonUtility.ToJson(currentRoom, true);
        //     File.WriteAllText(roomScanFilepath, jsonData);
        //     Debug.Log($"Room scan saved to {roomScanFilepath}");
        // }

        public static void SaveRoomScan()
        {
            string filePath = Application.persistentDataPath + $"/roomscan.json";
            string meshPath = Application.persistentDataPath + $"/roomscan.mesh";
            // Get the current room
            var currentRoom = MRUK.Instance.GetCurrentRoom();
            if (currentRoom == null || currentRoom.GlobalMeshAnchor == null)
            {
                Debug.LogError("No room or global mesh anchor found.");
                return;
            }

            SaveMeshToFile(meshPath, MRUK.Instance.GetCurrentRoom().GlobalMeshAnchor.GlobalMesh);
            // Create the RoomScanData object
            RoomScanData roomScanData = new RoomScanData();

            // Serialize global mesh data
            Mesh globalMesh = currentRoom.GlobalMeshAnchor.GlobalMesh;
            if (globalMesh != null)
            {
                roomScanData.globalMesh = new RoomScanData.MeshData
                {
                    vertices = globalMesh.vertices,
                    triangles = globalMesh.triangles,
                    normals = globalMesh.normals,
                    uvs = globalMesh.uv
                };
            }

            // Serialize anchor data
            // roomScanData.anchors = new List<RoomScanData.AnchorData>();
            // foreach (var anchor in currentRoom.Anchors)
            // {
            //     var transform = anchor.transform;
            //     Rect planeRect = (Rect)((anchor.PlaneRect == null) ? new Rect(0, 0, 0, 0) : anchor.PlaneRect);
            //     Bounds volumeBounds = (Bounds)((anchor.VolumeBounds == null) ? new Bounds() : anchor.VolumeBounds);
            //
            //     RoomScanData.AnchorData anchorData = new RoomScanData.AnchorData
            //     {
            //         name = anchor.Label.ToString(),
            //         position = transform.position,
            //         rotation = transform.rotation,
            //         VolumeBounds = new SerializableBounds(volumeBounds),
            //         PlaneRect = new SerializableRect(planeRect),
            //         PlaneBoundary2D = anchor.PlaneBoundary2D
            //     };
            //     roomScanData.anchors.Add(anchorData);
            // }
            roomScanData.anchors = ConvertAllAnchors(currentRoom.Anchors);

            // Convert RoomScanData to JSON
            string jsonData = JsonUtility.ToJson(roomScanData, true);

            // Save the JSON data to file
            File.WriteAllText(filePath, jsonData);

            Debug.Log($"Room scan saved to {filePath}");
        }

        public static RoomScanData LoadRoomScan(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError("File not found: " + filePath);
                return null;
            }

            // Read JSON data from the file
            string jsonData = File.ReadAllText(filePath);
            var scannedRoom = JsonUtility.FromJson<RoomScanData>(jsonData);

            Debug.Log($"Room scan loaded from {filePath}");
            return scannedRoom;
        }

        public static Mesh RemoveVerticesInsideAnchors(Mesh originalMesh, List<RoomScanData.AnchorData> anchors,
            float epsilon = 0.01f, float maxY = float.MaxValue, Transform meshTransform = null)
        {
            if (originalMesh == null)
            {
                Debug.LogError("Mesh is null.");
                return null;
            }

            Vector3[] vertices = originalMesh.vertices;
            int[] triangles = originalMesh.triangles;
            List<Vector3> newVertices = new List<Vector3>();
            List<int> newTriangles = new List<int>();

            Dictionary<int, int> vertexMapping = new Dictionary<int, int>();
            int removedVertices = 0;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                Vector3 worldVertex = meshTransform != null ? meshTransform.TransformPoint(vertex) : vertex;

                // Step 1: Skip vertex if it's above the maxY height (NO epsilon here)
                if (worldVertex.y > maxY)
                {
                    removedVertices++;
                    continue;
                }

                // Step 2: Check if the vertex is inside any anchor volume (WITH epsilon)
                bool insideAnchor = false;
                foreach (RoomScanData.AnchorData anchor in anchors)
                {
                    Bounds bounds;
                    Vector3 anchorPosition = anchor.position;
                    Quaternion anchorRotation = anchor.rotation;
                    bool hasBounds = false;

                    if (anchor.VolumeBounds != null)
                    {
                        bounds = anchor.VolumeBounds.ToBounds();
                        hasBounds = true;
                    }
                    else if (anchor.PlaneRect != null)
                    {
                        var rect = anchor.PlaneRect;
                        Vector3 center = new Vector3(rect.x + rect.width / 2f, 0, rect.y + rect.height / 2f);
                        Vector3 size = new Vector3(rect.width, 0.05f, rect.height);
                        bounds = new Bounds(center, size);
                        hasBounds = true;
                    }
                    else if (anchor.PlaneBoundary2D != null && anchor.PlaneBoundary2D.Count > 0)
                    {
                        float minX = float.MaxValue, maxX = float.MinValue;
                        float minY = float.MaxValue, maxY2 = float.MinValue;
                        foreach (var pt in anchor.PlaneBoundary2D)
                        {
                            if (pt.x < minX) minX = pt.x;
                            if (pt.x > maxX) maxX = pt.x;
                            if (pt.y < minY) minY = pt.y;
                            if (pt.y > maxY2) maxY2 = pt.y;
                        }
                        Vector3 center = new Vector3((minX + maxX) / 2f, 0, (minY + maxY2) / 2f);
                        Vector3 size = new Vector3(maxX - minX, 0.5f, maxY2 - minY); // 0.5f Y thickness
                        bounds = new Bounds(center, size);
                        hasBounds = true;
                    }
                    else
                    {
                        continue;
                    }

                    // Transform vertex to anchor local space
                    Vector3 localVertex = Quaternion.Inverse(anchorRotation) * (worldVertex - anchorPosition);
                    bounds.Expand(epsilon);

                    if (hasBounds && bounds.Contains(localVertex))
                    {
                        insideAnchor = true;
                        removedVertices++;
                        break;
                    }
                }

                if (!insideAnchor)
                {
                    vertexMapping[i] = newVertices.Count;
                    newVertices.Add(vertex);
                }
            }

            // Preserve only triangles whose vertices all remain
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];

                if (vertexMapping.ContainsKey(v0) && vertexMapping.ContainsKey(v1) && vertexMapping.ContainsKey(v2))
                {
                    newTriangles.Add(vertexMapping[v0]);
                    newTriangles.Add(vertexMapping[v1]);
                    newTriangles.Add(vertexMapping[v2]);
                }
            }

            Mesh newMesh = new Mesh
            {
                vertices = newVertices.ToArray(),
                triangles = newTriangles.ToArray()
            };

            newMesh.RecalculateNormals();
            newMesh.RecalculateBounds();

            Debug.Log($"Removed {removedVertices} vertices (above Y={maxY} or inside anchors). Final mesh: {newVertices.Count} vertices, {newTriangles.Count / 3} triangles.");

            return newMesh;
        }

        public static void RenderAnchorsAsCubes(List<RoomScanData.AnchorData> anchors, Transform parent,
            float epsilon = 0.0f)
        {
            // Loop through each anchor and render its bounds as a cube
            foreach (RoomScanData.AnchorData anchor in anchors)
            {
                if (anchor.VolumeBounds == null) continue; // Skip if no VolumeBounds

                // Create the cube
                RenderCube(anchor, parent, epsilon);
            }
        }

        public static void RenderCube(RoomScanData.AnchorData anchor, Transform parent = null, float epsilon = 0.01f)
        {
            GameObject anchorCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            anchorCube.transform.parent = parent;
            var volumeBounds = anchor.VolumeBounds.ToBounds();
            volumeBounds.Expand(epsilon);
            // Set cube position, rotation, and size
            anchorCube.transform.position = anchor.position;
            anchorCube.transform.rotation = anchor.rotation;
            anchorCube.transform.localScale = volumeBounds.size;


            // Optionally, set the cube's name and color for easier identification
            anchorCube.name = anchor.name;
            Renderer cubeRenderer = anchorCube.GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                cubeRenderer.material.color =
                    Color.Lerp(Color.red, Color.green, Random.value); // Random color for visibility
            }
        }


        [System.Serializable]
        public class RoomScanData
        {
            public MeshData globalMesh;
            public List<AnchorData> anchors;

            [System.Serializable]
            public class MeshData
            {
                public Vector3[] vertices;
                public int[] triangles;
                public Vector3[] normals;
                public Vector2[] uvs;
            }

            [System.Serializable]
            public class AnchorData
            {
                public string name;
                public Vector3 position;
                public Quaternion rotation;
                public SerializableRect PlaneRect;
                public SerializableBounds VolumeBounds;
                public List<Vector2> PlaneBoundary2D;
            }
        }

        [System.Serializable]
        public class SerializableRect
        {
            public float x = 0;
            public float y = 0;
            public float width = 0;
            public float height = 0;

            public SerializableRect(Rect rect)
            {
                x = rect.x;
                y = rect.y;
                width = rect.width;
                height = rect.height;
            }

            // Convert back to Rect
            public Rect ToRect()
            {
                return new Rect(x, y, width, height);
            }
        }

        [System.Serializable]
        public class SerializableBounds
        {
            public Vector3 center;
            public Vector3 size;

            // Constructor to convert from Bounds to SerializableBounds
            public SerializableBounds(Bounds bounds)
            {
                center = bounds.center;
                size = bounds.size;
            }

            // Convert back to Unity's Bounds object
            public Bounds ToBounds()
            {
                return new Bounds(center, size);
            }
        }
    }
}


// {
//     "name": "TABLE",
//     "position": {
//         "x": -0.07181096076965332,
//         "y": 0.35494256019592287,
//         "z": -0.5949490666389465
//     },
//     "rotation": {
//         "x": -0.005182529799640179,
//         "y": -0.7070878148078919,
//         "z": -0.7070878148078919,
//         "w": 0.005182529799640179
//     },
//     "PlaneRect": {
//         "x": -0.7430300712585449,
//         "y": -0.2857152223587036,
//         "width": 1.486060380935669,
//         "height": 0.571430504322052
//     },
//     "VolumeBounds": {
//         "center": {
//             "x": 0.0,
//             "y": 2.9802322387695315e-8,
//             "z": -0.17958557605743409
//         },
//         "size": {
//             "x": 1.4860601425170899,
//             "y": 0.571430504322052,
//             "z": 0.35917162895202639
//         }
//     },
//     "PlaneBoundary2D": [
//     {
//         "x": 0.743030309677124,
//         "y": 0.28571516275405886
//     },
//     {
//         "x": -0.7430298328399658,
//         "y": 0.2857152819633484
//     },
//     {
//         "x": -0.7430300712585449,
//         "y": -0.28571510314941409
//     },
//     {
//         "x": 0.7430300712585449,
//         "y": -0.2857152223587036
//     }
//     ]
// },
