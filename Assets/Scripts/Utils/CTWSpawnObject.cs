using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.MRUtilityKit;
using Meta.XR.Util;
using UnityEngine;
using UnityEngine.Serialization;

namespace Utils
{
    [System.Serializable]
    public class SpawnBoundarySettings
    {
        [Tooltip("Minimum distance from walls/obstacles")]
        public float MinClearance = 0.5f;
        
        [Tooltip("Maximum distance from walls/obstacles")]
        public float MaxClearance = 2.0f;
        
        [Tooltip("Height offset from ground when spawning")]
        public float HeightOffset = 0.1f;
        
        [Tooltip("Whether to respect room boundaries strictly")]
        public bool StrictBoundaries = true;
    }

    [System.Serializable]
    public class SpawnLocationSettings
    {
        [Tooltip("Minimum distance between spawned objects")]
        public float MinDistanceBetweenObjects = 1.0f;
        
        [Tooltip("Whether to maintain minimum distance from other spawned objects")]
        public bool MaintainObjectSpacing = true;
        
        [Tooltip("Preferred spawn height range")]
        public Vector2 HeightRange = new Vector2(0.5f, 2.0f);
    }

    /// <summary>
    /// Enhanced spawner for placing objects in MR environments with improved boundary handling and spawn controls.
    /// </summary>
    public class CTWSpawnObject : MonoBehaviour
    {
        [Header("Basic Settings")]
        [Tooltip("When the scene data is loaded, this controls what room(s) the prefabs will spawn in.")]
        public MRUK.RoomFilter SpawnOnStart = MRUK.RoomFilter.CurrentRoomOnly;

        [SerializeField, Tooltip("Prefab to be placed into the scene, or object in the scene to be moved around.")]
        public GameObject SpawnObject;

        [SerializeField, Tooltip("Number of SpawnObject(s) to place into the scene per room, only applies to Prefabs.")]
        public int SpawnAmount = 8;

        [SerializeField, Tooltip("Maximum number of times to attempt spawning/moving an object before giving up.")]
        public int MaxIterations = 1000;

        [Header("Spawn Location Settings")]
        [SerializeField] private SpawnLocationSettings locationSettings = new SpawnLocationSettings();
        
        [Header("Boundary Settings")]
        [SerializeField] private SpawnBoundarySettings boundarySettings = new SpawnBoundarySettings();

        [Header("Debug Settings")]
        [SerializeField, Tooltip("Enable to visualize spawn bounds")]
        private bool showDebugBounds = false;
        
        [SerializeField, Tooltip("Color for debug bounds visualization")]
        private Color debugBoundsColor = Color.cyan;
        
        [SerializeField, Tooltip("Duration for debug bounds visualization")]
        private float debugBoundsDuration = 0.1f;

        public enum SpawnLocation
        {
            Floating,
            AnySurface,
            VerticalSurfaces,
            OnTopOfSurfaces,
            HangingDown
        }

        [FormerlySerializedAs("selectedSnapOption")]
        [SerializeField, Tooltip("Attach content to scene surfaces.")]
        public SpawnLocation SpawnLocations = SpawnLocation.Floating;

        [SerializeField, Tooltip("When using surface spawning, use this to filter which anchor labels should be included.")]
        public MRUKAnchor.SceneLabels Labels = ~(MRUKAnchor.SceneLabels)0;

        [SerializeField, Tooltip("If enabled then the spawn position will be checked to make sure there is no overlap with physics colliders including themselves.")]
        public bool CheckOverlaps = true;

        [SerializeField, Tooltip("Required free space for the object (Set negative to auto-detect using GetPrefabBounds)")]
        public float OverrideBounds = -1;

        [FormerlySerializedAs("layerMask")]
        [SerializeField, Tooltip("Set the layer(s) for the physics bounding box checks, collisions will be avoided with these layers.")]
        public LayerMask LayerMask = -1;

        [SerializeField, Tooltip("The clearance distance required in front of the surface in order for it to be considered a valid spawn position")]
        public float SurfaceClearanceDistance = 0.1f;

        [SerializeField, Tooltip("If enabled, objects will rotate to face the camera horizontally at spawn time.")]
        private bool RotateToCamera = false;

        [SerializeField, Tooltip("If enabled, the object will spawn using its original center position without applying offset based on bounds.")]
        private bool UseOriginalCenter = false;

        [SerializeField, Tooltip("If enabled, bounds will be calculated based in the widest bounds in animation sequence")]
        private bool IsAnimation = false;

        private List<GameObject> spawnedObjects = new List<GameObject>();
        private Bounds? lastSpawnedBounds;
        private float lastSpawnTime;
        private const float MIN_SPAWN_INTERVAL = 0.1f; // Minimum time between spawns to prevent frame drops

        private void Start()
        {
            if (MRUK.Instance && SpawnOnStart != MRUK.RoomFilter.None)
            {
                MRUK.Instance.RegisterSceneLoadedCallback(() =>
                {
                    switch (SpawnOnStart)
                    {
                        case MRUK.RoomFilter.AllRooms:
                            StartSpawn();
                            break;
                        case MRUK.RoomFilter.CurrentRoomOnly:
                            StartSpawn(MRUK.Instance.GetCurrentRoom());
                            break;
                    }
                });
            }
        }

        public void StartSpawn()
        {
            foreach (var room in MRUK.Instance.Rooms)
            {
                StartSpawn(room);
            }
        }

        private bool IsValidSpawnPosition(Vector3 position, Bounds bounds, MRUKRoom room)
        {
            if (boundarySettings.StrictBoundaries && !room.IsPositionInRoom(position))
                return false;

            if (room.IsPositionInSceneVolume(position))
                return false;

            if (locationSettings.MaintainObjectSpacing)
            {
                foreach (var spawned in spawnedObjects)
                {
                    if (Vector3.Distance(position, spawned.transform.position) < locationSettings.MinDistanceBetweenObjects)
                        return false;
                }
            }

            return true;
        }

        private void DrawDebugBounds(Bounds bounds, Vector3 position, Quaternion rotation)
        {
            if (!showDebugBounds) return;
            
            if (Time.time - lastSpawnTime < MIN_SPAWN_INTERVAL) return;
            
            CTWUtilities.DrawWireBounds(bounds, position, rotation, debugBoundsColor, debugBoundsDuration);
            lastSpawnTime = Time.time;
        }

        public void StartSpawn(MRUKRoom room)
        {
            var prefabBounds = IsAnimation ? CTWUtilities.GetMaxBoundsOverFirstAnimationV2(SpawnObject) : Utilities.GetPrefabBounds(SpawnObject);
            if (!prefabBounds.HasValue)
            {
                Debug.LogError("Failed to get bounds for spawn object!");
                return;
            }

            float minRadius = Mathf.Max(boundarySettings.MinClearance, 
                Mathf.Min(-prefabBounds.Value.min.x, -prefabBounds.Value.min.z, 
                         prefabBounds.Value.max.x, prefabBounds.Value.max.z));

            Bounds adjustedBounds = new Bounds();
            float baseOffset = UseOriginalCenter ? 0f : -prefabBounds.Value.min.y;
            float centerOffset = UseOriginalCenter ? 0f : prefabBounds.Value.center.y;

            if (!UseOriginalCenter)
            {
                var min = prefabBounds.Value.min;
                var max = prefabBounds.Value.max;
                min.y += boundarySettings.HeightOffset;
                if (max.y < min.y) max.y = min.y;

                adjustedBounds.SetMinMax(min, max);

                if (OverrideBounds > 0)
                {
                    Vector3 center = new Vector3(0f, boundarySettings.HeightOffset, 0f);
                    Vector3 size = new Vector3(OverrideBounds * 2f, boundarySettings.HeightOffset * 2f, OverrideBounds * 2f);
                    adjustedBounds = new Bounds(center, size);
                }
            }
            else
            {
                adjustedBounds = new Bounds(Vector3.zero, prefabBounds.Value.size);
            }

            for (int i = 0; i < SpawnAmount; ++i)
            {
                bool foundValidSpawnPosition = false;
                for (int j = 0; j < MaxIterations; ++j)
                {
                    Vector3 spawnPosition = Vector3.zero;
                    Vector3 spawnNormal = Vector3.zero;

                    if (SpawnLocations == SpawnLocation.Floating)
                    {
                        var randomPos = room.GenerateRandomPositionInRoom(minRadius, true);
                        if (!randomPos.HasValue) break;

                        spawnPosition = randomPos.Value;
                        spawnPosition.y = Mathf.Clamp(spawnPosition.y, 
                            locationSettings.HeightRange.x, 
                            locationSettings.HeightRange.y);
                    }
                    else
                    {
                        MRUK.SurfaceType surfaceType = 0;
                        switch (SpawnLocations)
                        {
                            case SpawnLocation.AnySurface:
                                surfaceType |= MRUK.SurfaceType.FACING_UP | MRUK.SurfaceType.VERTICAL | MRUK.SurfaceType.FACING_DOWN;
                                break;
                            case SpawnLocation.VerticalSurfaces:
                                surfaceType |= MRUK.SurfaceType.VERTICAL;
                                break;
                            case SpawnLocation.OnTopOfSurfaces:
                                surfaceType |= MRUK.SurfaceType.FACING_UP;
                                break;
                            case SpawnLocation.HangingDown:
                                surfaceType |= MRUK.SurfaceType.FACING_DOWN;
                                break;
                        }

                        if (room.GenerateRandomPositionOnSurface(surfaceType, minRadius, new LabelFilter(Labels), out var pos, out var normal))
                        {
                            spawnPosition = UseOriginalCenter ? pos : pos + normal * baseOffset;
                            spawnNormal = normal;
                            var center = spawnPosition + (UseOriginalCenter ? Vector3.zero : normal * centerOffset);

                            if (!IsValidSpawnPosition(center, adjustedBounds, room)) continue;
                            if (room.Raycast(new Ray(pos, normal), SurfaceClearanceDistance, out _)) continue;
                        }
                    }

                    Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, spawnNormal);
                    Quaternion finalRotation = spawnRotation;

                    if (RotateToCamera && Camera.main != null)
                    {
                        Vector3 toCamera = Camera.main.transform.position - spawnPosition;
                        toCamera.y = 0;
                        if (toCamera != Vector3.zero)
                        {
                            Quaternion faceCameraRotation = Quaternion.LookRotation(toCamera);
                            finalRotation = Quaternion.LookRotation(faceCameraRotation * Vector3.forward, spawnNormal);
                        }
                    }

                    if (CheckOverlaps)
                    {
                        Vector3 overlapCenter = spawnPosition + finalRotation * adjustedBounds.center;
                        if (Physics.CheckBox(overlapCenter, adjustedBounds.extents, finalRotation, LayerMask, QueryTriggerInteraction.Ignore))
                        {
                            continue;
                        }
                        
                        DrawDebugBounds(adjustedBounds, spawnPosition, finalRotation);
                    }

                    foundValidSpawnPosition = true;

                    if (SpawnObject.gameObject.scene.path == null)
                    {
                        var spawned = Instantiate(SpawnObject, spawnPosition, finalRotation, transform);
                        if (IsAnimation) spawned.transform.position += Vector3.down * adjustedBounds.extents.y;
                        spawnedObjects.Add(spawned);
                        lastSpawnedBounds = adjustedBounds;
                    }
                    else
                    {
                        SpawnObject.transform.position = spawnPosition;
                        SpawnObject.transform.rotation = finalRotation;
                        return;
                    }
                    break;
                }

                if (!foundValidSpawnPosition)
                {
                    Debug.LogWarning($"Failed to find valid spawn position after {MaxIterations} iterations. Only spawned {i} prefabs instead of {SpawnAmount}.");
                    break;
                }
            }
        }

        public void ClearSpawnedObjects()
        {
            foreach (var obj in spawnedObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            spawnedObjects.Clear();
        }

        private void OnDestroy()
        {
            ClearSpawnedObjects();
        }
    }
}