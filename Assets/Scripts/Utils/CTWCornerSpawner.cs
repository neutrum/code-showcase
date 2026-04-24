using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Utils
{
    public class CTWCornerSpawner : MonoBehaviour
    {
        private MRUKRoom _currentRoom;
        
        [Header("Corner Settings")] 
        public List<GameObject> CornersToSpawn;

        public void Awake()
        {
           // MRUK.Instance.RegisterSceneLoadedCallback(SpawnCorners);
        }

        public void SpawnCorners()
        {
            if (CornersToSpawn.Count == 0) return;
            var floor = MRUK.Instance.GetCurrentRoom().Anchors
                .Find(anchor => anchor.Label == MRUKAnchor.SceneLabels.FLOOR);
            var corners = floor.PlaneBoundary2D;
            corners = FilterSharpCorners(corners);
            
         
            
            foreach (var corner in corners)
            {
                var cornerPrefab = CornersToSpawn[Random.Range(0, CornersToSpawn.Count)];
                var cornerXYZ = new Vector3( corner.x, corner.y, 0 );
                var spawnedCorner = Instantiate(cornerPrefab);
                spawnedCorner.transform.parent = transform;
                spawnedCorner.transform.rotation = UnityEngine.Quaternion.Euler(-90, -180, 0);
                spawnedCorner.transform.position = cornerXYZ;
                spawnedCorner.GetOrAddComponent<CTWOcludee>();
                //spawnedCorner.SetActive(true);
            }

            transform.position = floor.transform.position;
            transform.rotation = floor.transform.rotation;
        }

        private List<Vector2> FilterSharpCorners(List<Vector2> points)
        {
            int count = points.Count;
            List<Vector2> isOutward = new List<Vector2>();

            for (int i = 0; i < count; i++)
            {
                // Get the previous, current, and next points (wrapping around)
                Vector2 prev = points[(i - 1 + count) % count];
                Vector2 curr = points[i];
                Vector2 next = points[(i + 1) % count];

                // Calculate vectors
                Vector2 v1 = curr - prev;
                Vector2 v2 = next - curr;

                // Compute cross product (z-component of 3D cross product)
                float cross = v1.x * v2.y - v1.y * v2.x;

                // Check if the corner is outward (positive cross product for clockwise winding)
                if (cross > 0) isOutward.Add(curr);
            }

            return isOutward;
        }
    }
}