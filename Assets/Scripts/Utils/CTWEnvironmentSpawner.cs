/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.Serialization;
using Utils.GameObjectExt;
using Random = System.Random;

namespace Utils
{
    /// <summary>
    /// Manages the spawning of prefabs based on anchor data within a scene, providing various customization options
    /// for scaling, alignment, and selection modes.
    /// </summary>
    public class CTWEnvironmentSpawner : MonoBehaviour 
    {
        
        [Header("Wall Settings")] 
        public bool CustomWallHeight = true;
        public float WallHeight = 2.0f;
        
        [Header("Roof Settings")] 
        public RoofType RoofType;
        public GameObject RoofToSpawn;
        private bool _roofSpawned = false; 
            
        [Header("Other Settings")]
        public bool ShowAnchorOutline = true;
        
        /// <summary>
        /// Defines the scaling modes available for adjusting prefab sizes to fit the anchor's dimensions.
        /// </summary>

        /// <summary>
        /// Represents a group of prefabs associated with specific scene labels, along with settings for how they should be spawned.
        /// </summary>

        [Tooltip("Whens the scene data is loaded, this controls what room(s) the prefabs will spawn in.")]
        public MRUK.RoomFilter SpawnOnStart = MRUK.RoomFilter.CurrentRoomOnly;

        [Tooltip("If enabled, updates on scene elements such as rooms and anchors will be handled by this class")]
        internal bool TrackUpdates = true;

        [Tooltip("Specify a seed value for consistent prefab selection (0 = Random).")]
        public int SeedValue;

        /// <summary>
        /// Gets a dictionary that maps MRUKAnchor instances to their corresponding spawned GameObjects.
        /// This should be treated as read-only, do not modify the contents.
        /// </summary>
        public Dictionary<MRUKAnchor, GameObject> AnchorPrefabSpawnerObjects { get; } = new();
        
        [Header("Prefabs to spawn")]
        public List<GameObject> Walls;
        public List<GameObject> Doors;
        public List<GameObject> Windows;
        /// <summary>
        /// The list of AnchorPrefabGroup configurations that determine how prefabs are spawned based on anchor data.
        /// </summary>
        [FormerlySerializedAs("PrefabsToSpawn")] public List<GameObject> General = new List<GameObject>();

        private Dictionary<MRUKAnchor.SceneLabels, AnchorSetup> anchorSetups = new Dictionary<MRUKAnchor.SceneLabels, AnchorSetup>
        {
            { MRUKAnchor.SceneLabels.WALL_FACE , new AnchorSetup(false, true)}
        };
        
        protected Random _random; // An instance of the Random class used to generate random numbers.
        private SceneTrackingSettings SceneTrackingSettings;
        private static readonly string Suffix = "(PrefabSpawner Clone)";
        private Func<Vector3, Vector3> _customPrefabScalingVolume;
        private Func<Bounds, Bounds?, (Vector3, Vector3)> _customPrefabAlignmentVolume;
        private Func<Vector2, Vector2> _customPrefabScalingPlaneRect;
        private Func<Rect, Bounds?, (Vector3, Vector2)> _customPrefabAlignmentPlaneRect;
        private Func<MRUKAnchor, List<GameObject>, GameObject> _customPrefabSelection;

        protected virtual void Start()
        {
            if (MRUK.Instance is null)
            {
                return;
            }

            SceneTrackingSettings.UnTrackedRooms = new();
            SceneTrackingSettings.UnTrackedAnchors = new();

            MRUK.Instance.RegisterSceneLoadedCallback(() =>
            {
                if (SpawnOnStart == MRUK.RoomFilter.None)
                {
                    return;
                }

                switch (SpawnOnStart)
                {
                    case MRUK.RoomFilter.CurrentRoomOnly:
                        SpawnPrefabs(MRUK.Instance.GetCurrentRoom());
                        break;
                    case MRUK.RoomFilter.AllRooms:
                        SpawnPrefabs();
                        break;
                    case MRUK.RoomFilter.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });

            if (!TrackUpdates)
            {
                return;
            }
        }


        protected virtual void OnEnable()
        {
            if (MRUK.Instance)
            {
                MRUK.Instance.RoomCreatedEvent.AddListener(ReceiveCreatedRoom);
                MRUK.Instance.RoomRemovedEvent.AddListener(ReceiveRemovedRoom);
            }
        }

        protected virtual void OnDisable()
        {
            if (MRUK.Instance)
            {
                MRUK.Instance.RoomCreatedEvent.RemoveListener(ReceiveCreatedRoom);
                MRUK.Instance.RoomRemovedEvent.RemoveListener(ReceiveRemovedRoom);
            }
        }

        /// <summary>
        /// Handles the event when a room is removed from the scene. This method clears all prefabs associated with the room
        /// and unregisters updates for anchors within the room.
        /// </summary>
        /// <param name="room">The room that has been removed.</param>
        protected virtual void ReceiveRemovedRoom(MRUKRoom room)
        {
            ClearPrefabs(room);
            UnRegisterAnchorUpdates(room);
        }

        /// <summary>
        /// Unregisters the anchor update events for a specific room. This method stops listening for anchor creation, removal,
        /// and update events within the specified room.
        /// </summary>
        /// <param name="room">The room for which to unregister anchor update events.</param>
        protected virtual void UnRegisterAnchorUpdates(MRUKRoom room)
        {
            room.AnchorCreatedEvent.RemoveListener(ReceiveAnchorCreatedEvent);
            room.AnchorRemovedEvent.RemoveListener(ReceiveAnchorRemovedCallback);
            room.AnchorUpdatedEvent.RemoveListener(ReceiveAnchorUpdatedCallback);
        }

        /// <summary>
        /// Registers the anchor update events for a specific room. This method starts listening for anchor creation, removal,
        /// and update events within the specified room.
        /// </summary>
        /// <param name="room">The room for which to register anchor update events.</param>
        protected virtual void RegisterAnchorUpdates(MRUKRoom room)
        {
            room.AnchorCreatedEvent.AddListener(ReceiveAnchorCreatedEvent);
            room.AnchorRemovedEvent.AddListener(ReceiveAnchorRemovedCallback);
            room.AnchorUpdatedEvent.AddListener(ReceiveAnchorUpdatedCallback);
        }

        /// <summary>
        /// Responds to the event of an anchor being updated within the scene. This method clears existing prefabs and triggers
        /// the spawning of new prefabs based on the updated anchor information, provided that updates are being tracked and
        /// the anchor or its parent room is not marked as untracked.
        /// </summary>
        /// <param name="anchorInfo">The anchor that has been updated.</param>
        protected virtual void ReceiveAnchorUpdatedCallback(MRUKAnchor anchorInfo)
        {
            // only update the anchor when we track updates
            // &
            // only create when the anchor or parent room is tracked
            if (SceneTrackingSettings.UnTrackedRooms.Contains(anchorInfo.Room) ||
                SceneTrackingSettings.UnTrackedAnchors.Contains(anchorInfo) ||
                !TrackUpdates)
            {
                return;
            }

            ClearPrefabs();
            SpawnPrefabs(anchorInfo);
        }

        /// <summary>
        /// Responds to the event of an anchor being removed from the scene. This method clears all prefabs spawned by this spawner.
        /// </summary>
        /// <param name="anchorInfo">The anchor that has been removed.</param>
        protected virtual void ReceiveAnchorRemovedCallback(MRUKAnchor anchorInfo)
        {
            ClearPrefabs();
        }

        /// <summary>
        /// Responds to the event of a new anchor being created within the scene. This method triggers the spawning of prefabs
        /// if the anchor's room is being tracked and updates are enabled.
        /// </summary>
        /// <param name="anchorInfo">The anchor that has been created.</param>
        protected virtual void ReceiveAnchorCreatedEvent(MRUKAnchor anchorInfo)
        {
            // only create the anchor when we track updates
            // &
            // only create when the parent room is tracked
            if (SceneTrackingSettings.UnTrackedRooms.Contains(anchorInfo.Room) ||
                !TrackUpdates)
            {
                return;
            }

            SpawnPrefabs();
        }

        /// <summary>
        /// Responds to the event of a new room being created within the scene. This method triggers the spawning of prefabs
        /// and registers for anchor updates within the room, provided that room updates are being tracked and the configuration
        /// is set to handle all rooms.
        /// </summary>
        /// <param name="room">The room that has been created.</param>
        protected virtual void ReceiveCreatedRoom(MRUKRoom room)
        {
            //only create the room when we track room updates
            if (TrackUpdates &&
                SpawnOnStart == MRUK.RoomFilter.AllRooms)
            {
                SpawnPrefabs(room);
                RegisterAnchorUpdates(room);
            }
        }

        /// <summary>
        ///  Clears all the spawned gameobjects from this AnchorPrefabSpawner in the given room
        /// </summary>
        /// <param name="room">The room from where to remove all the spawned objects.</param>
        protected virtual void ClearPrefabs(MRUKRoom room)
        {
            List<MRUKAnchor> anchorsToRemove = new();
            foreach (var kv in AnchorPrefabSpawnerObjects)
            {
                if (kv.Key.Room != room)
                {
                    continue;
                }

                ClearPrefab(kv.Value);
                anchorsToRemove.Add(kv.Key);
            }

            foreach (var anchor in anchorsToRemove)
            {
                AnchorPrefabSpawnerObjects.Remove(anchor);
            }

            SceneTrackingSettings.UnTrackedRooms.Add(room);
        }

        /// <summary>
        /// Destroys the specified GameObject, effectively removing the prefab from the scene.
        /// </summary>
        /// <param name="go">The GameObject to be destroyed.</param>
        protected virtual void ClearPrefab(GameObject go)
        {
            Destroy(go);
        }

        /// <summary>
        /// Clears the gameobject associated with the anchor. Useful when receiving an event that a
        /// specific anchor has been removed
        /// </summary>
        /// <param name="anchorInfo">The anchor reference</param>
        protected virtual void ClearPrefab(MRUKAnchor anchorInfo)
        {
            if (!AnchorPrefabSpawnerObjects.ContainsKey(anchorInfo))
            {
                return;
            }

            ClearPrefab(AnchorPrefabSpawnerObjects[anchorInfo]);
            AnchorPrefabSpawnerObjects.Remove(anchorInfo);
            SceneTrackingSettings.UnTrackedAnchors.Add(anchorInfo);
        }

        /// <summary>
        /// Clears all the gameobjects created with the PrefabSpawner
        /// </summary>
        protected virtual void ClearPrefabs()
        {
            foreach (var kv in AnchorPrefabSpawnerObjects)
            {
                ClearPrefab(kv.Value);
            }

            AnchorPrefabSpawnerObjects.Clear();
        }


        /// <summary>
        /// Spawns prefabs according to the settings
        /// </summary>
        /// <param name="clearPrefabs">Clear already existing prefabs before.</param>
        protected virtual void SpawnPrefabs(bool clearPrefabs = true)
        {
            // Perform a cleanup if necessary
            if (clearPrefabs)
            {
                ClearPrefabs();
            }

            foreach (var room in MRUK.Instance.Rooms)
            {
                SpawnPrefabsInternal(room);
            }
        }

        /// <summary>
        /// Creates gameobjects for the given room.
        /// </summary>
        /// <param name="room">The room reference</param>
        /// <param name="clearPrefabs">clear all before adding them again</param>
        protected virtual void SpawnPrefabs(MRUKRoom room, bool clearPrefabs = true)
        {
            // Perform a cleanup if necessary
            if (clearPrefabs)
            {
                ClearPrefabs();
            }

            SpawnPrefabsInternal(room);
        }

        private void SpawnPrefabsInternal(MRUKRoom room)
        {
            InitializeRandom(ref SeedValue);
            foreach (var anchor in room.Anchors)
            {
                SpawnPrefab(anchor);
            }
            
            if (ShowAnchorOutline) ShowAnchorOutlines();
        }

        private bool IsRelevantLabel(MRUKAnchor anchorInfo)
        {
            return (anchorInfo.Label != MRUKAnchor.SceneLabels.WALL_FACE && 
                    anchorInfo.Label != MRUKAnchor.SceneLabels.FLOOR &&
                    anchorInfo.Label != MRUKAnchor.SceneLabels.WALL_ART &&
                    anchorInfo.Label != MRUKAnchor.SceneLabels.DOOR_FRAME && 
                    anchorInfo.Label != MRUKAnchor.SceneLabels.CEILING);
        }
        
        /// <summary>
        /// Spawns a prefab based on the provided anchor information. This method determines the appropriate prefab to spawn
        /// based on the anchor's label, checks for existing instances, and configures the spawned prefab's position, scale,
        /// and orientation according to the anchor's properties and predefined settings.
        /// </summary>
        /// <param name="anchorInfo">The anchor based on which the prefab will be spawned.</param>
        protected void SpawnPrefab(MRUKAnchor anchorInfo)
        { 
            if (CustomWallHeight && anchorInfo.Label == MRUKAnchor.SceneLabels.WALL_FACE)
            {
                 anchorInfo = AdjustWallAnchorInfo(anchorInfo);
            }
            
            var prefabToCreate = LabelToPrefab(anchorInfo.Label, anchorInfo);
            
            if (prefabToCreate == null)
            {
                return;
            }

            if (AnchorPrefabSpawnerObjects.ContainsKey(anchorInfo))
            {
                Debug.LogWarning("Anchor already associated with a gameobject spawned from this AnchorPrefabSpawner");
                return;
            }

            var anchorSetup = GetAnchorSetup(anchorInfo);

            // Create a new instance of the prefab
            // We will translate location and scale differently depending on the label.
            var prefab = Instantiate(prefabToCreate, anchorInfo.transform);
            prefab.name = string.Concat(prefabToCreate.name, Suffix);
            prefab.name = prefabToCreate.name + Suffix;
            prefab.transform.parent = anchorInfo.transform;

            var prefabBounds = Utilities.GetPrefabBounds(prefabToCreate);

            var prefabSize = prefabBounds?.size ?? Vector3.one;

            if (anchorInfo.VolumeBounds.HasValue)
            {
                var cardinalAxisIndex = 0;
                if (anchorSetup.CalculateFacingDirection && !anchorSetup.MatchAspectRatio)
                {
                    CTWAnchorPrefabSpawnerUtilities.GetDirectionAwayFromClosestWall(anchorInfo, out cardinalAxisIndex);
                }

                var volumeBounds = CTWAnchorPrefabSpawnerUtilities.RotateVolumeBounds(anchorInfo.VolumeBounds.Value,
                    cardinalAxisIndex);

                var volumeSize = volumeBounds.size;
                var scale = new Vector3(volumeSize.x / prefabSize.x, volumeSize.z / prefabSize.y,
                    volumeSize.y / prefabSize.z); // flipped z and y to correct orientation

                if (anchorSetup.MatchAspectRatio)
                {
                    CTWAnchorPrefabSpawnerUtilities.MatchAspectRatio(anchorInfo, anchorSetup.CalculateFacingDirection,
                        prefabSize, volumeSize, ref cardinalAxisIndex, ref volumeBounds, ref scale);
                }

                scale = AnchorPrefabSpawnerUtilities.ScalePrefab(scale, AnchorPrefabSpawner.ScalingMode.Stretch);

                var localPosition = AnchorPrefabSpawnerUtilities.AlignPrefabPivot(volumeBounds, prefabBounds, scale, AnchorPrefabSpawner.AlignMode.Automatic);

                prefab.transform.localPosition = Quaternion.AngleAxis(cardinalAxisIndex * 90, Vector3.forward) * localPosition;

                // scene geometry is unusual, we need to swap Y/Z for a more standard prefab structure
                prefab.transform.localRotation = Quaternion.Euler((cardinalAxisIndex - 1) * 90, -90, -90);
                prefab.transform.localScale = scale;
            }

            else if (anchorInfo.PlaneRect.HasValue)
            {
                var planeSize = anchorInfo.PlaneRect.Value.size;
                var scale = new Vector2(planeSize.x / prefabSize.x, planeSize.y / prefabSize.y);

                prefab.transform.localScale = AnchorPrefabSpawnerUtilities.ScalePrefab(scale, AnchorPrefabSpawner.ScalingMode.Stretch);
                prefab.transform.localPosition = AnchorPrefabSpawnerUtilities.AlignPrefabPivot(anchorInfo.PlaneRect.Value, prefabBounds, scale,
                        AnchorPrefabSpawner.AlignMode.Automatic);
            }

            AnchorPrefabSpawnerObjects.Add(anchorInfo, prefab);
            
            // Spawn Roof
            SpawnRoof(anchorInfo);
            
            // Make occludee
            if (AnchorPrefabSpawnerObjects.TryGetValue(anchorInfo, out var o))
            {
                o.GetOrAddComponent<CTWOcludee>();
            }
        }

        private AnchorSetup GetAnchorSetup(MRUKAnchor anchorInfo)
        {
            if (anchorSetups.ContainsKey(anchorInfo.Label)) return anchorSetups[anchorInfo.Label];
            return new AnchorSetup(true, true);
        }
        
        private GameObject LabelToPrefab(MRUKAnchor.SceneLabels labels, MRUKAnchor anchor)
        {
            return labels switch
            {
                MRUKAnchor.SceneLabels.WALL_FACE => PrefabSelection(anchor, Walls),
                MRUKAnchor.SceneLabels.DOOR_FRAME => PrefabSelection(anchor, Doors),
                MRUKAnchor.SceneLabels.WINDOW_FRAME => PrefabSelection(anchor, Windows),
                MRUKAnchor.SceneLabels.CEILING => null,
                MRUKAnchor.SceneLabels.FLOOR => null,
                MRUKAnchor.SceneLabels.GLOBAL_MESH => null,
                MRUKAnchor.SceneLabels.WALL_ART => null,
                _ => PrefabSelection(anchor, General)
            };
        }
        
        private void SpawnRoof(MRUKAnchor anchor)
         {
             if (_roofSpawned) return;
             
             var meshCeiling = GameObject.Find("EffectMeshCeiling");
             if (meshCeiling != null) meshCeiling.SetActive(false);
             
             if (null != RoofToSpawn && anchor.Label == MRUKAnchor.SceneLabels.CEILING)
             {
                 switch (RoofType)
                 {
                     case RoofType.NONE:
                         if (meshCeiling != null) meshCeiling.SetActive(true);           
                         break;
                     case RoofType.CUSTOM:
                         RoofToSpawn.transform.position = anchor.transform.position;
                         var wallAnchor = MRUK.Instance.GetCurrentRoom().GetKeyWall(out Vector2 wallScale, 0.1f);
                         RoofToSpawn.transform.rotation = wallAnchor.transform.rotation;
                         _roofSpawned = true;
                         break;
                     case RoofType.PROCEDURAL:
                         break;
                 }
             }
         }

        public GameObject PrefabSelection(MRUKAnchor anchor, List<GameObject> originalPrefabList)
        {
            if (originalPrefabList == null || originalPrefabList.Count == 0)
            {
                Debug.LogWarning($"No prefabs available for anchor {anchor.name} with label {anchor.Label}");
                return null;
            }

            List<GameObject> prefabList = new List<GameObject>(originalPrefabList);
            GameObject bestMatchPrefab = null;
            float bestMatchScore = float.MaxValue;

            Vector3 anchorSize = GetAnchorSize(anchor, out bool hasVolumeBounds);
            if (anchorSize == Vector3.zero)
            {
                Debug.LogError($"Could not determine size for anchor {anchor.name}");
                return prefabList[UnityEngine.Random.Range(0, prefabList.Count)];
            }

            foreach (var prefab in prefabList)
            {
                var bounds = Utilities.GetPrefabBounds(prefab);
                if (!bounds.HasValue)
                {
                    Debug.LogWarning($"Could not get bounds for prefab {prefab.name}, skipping...");
                    continue;
                }

                var prefabSize = bounds.Value.size;
                float matchScore = CalculateMatchScore(anchorSize, prefabSize, hasVolumeBounds);
                float rotatedMatchScore = CalculateMatchScore(anchorSize, new Vector3(prefabSize.z, prefabSize.y, prefabSize.x), hasVolumeBounds);

                // Use the better of the two orientations
                float finalScore = Mathf.Min(matchScore, rotatedMatchScore);

                Debug.Log($"Prefab '{prefab.name}' scores: Normal={matchScore:F3}, Rotated={rotatedMatchScore:F3}");

                if (finalScore < bestMatchScore)
                {
                    bestMatchScore = finalScore;
                    bestMatchPrefab = prefab;
                    Debug.Log($"New best match: '{prefab.name}' with score {finalScore:F3}");
                }
            }

            if (bestMatchPrefab != null)
            {
                Debug.Log($"Selected prefab '{bestMatchPrefab.name}' for anchor {anchor.name} with final score {bestMatchScore:F3}");
            }
            else
            {
                Debug.LogWarning($"No suitable prefab found for anchor {anchor.name}, using random selection");
                bestMatchPrefab = prefabList[UnityEngine.Random.Range(0, prefabList.Count)];
            }

            return bestMatchPrefab;
        }

        private Vector3 GetAnchorSize(MRUKAnchor anchor, out bool hasVolumeBounds)
        {
            hasVolumeBounds = anchor.VolumeBounds.HasValue;
            
            if (hasVolumeBounds)
            {
                return anchor.VolumeBounds.Value.size;
            }
            
            if (anchor.PlaneBoundary2D != null && anchor.PlaneBoundary2D.Count > 2)
            {
                var minX = anchor.PlaneBoundary2D.Min(v => v.x);
                var maxX = anchor.PlaneBoundary2D.Max(v => v.x);
                var minY = anchor.PlaneBoundary2D.Min(v => v.y);
                var maxY = anchor.PlaneBoundary2D.Max(v => v.y);
                
                return new Vector3(maxX - minX, maxY - minY, 0);
            }

            return Vector3.zero;
        }

        private float CalculateMatchScore(Vector3 anchorSize, Vector3 prefabSize, bool hasVolumeBounds)
        {
            if (hasVolumeBounds)
            {
                // For 3D volumes, consider all dimensions with aspect ratio preservation
                float scaleX = anchorSize.x / prefabSize.x;
                float scaleY = anchorSize.y / prefabSize.y;
                float scaleZ = anchorSize.z / prefabSize.z;

                // Calculate aspect ratio differences
                float xyAspectDiff = Mathf.Abs(scaleX - scaleY);
                float yzAspectDiff = Mathf.Abs(scaleY - scaleZ);
                float xzAspectDiff = Mathf.Abs(scaleX - scaleZ);

                // Calculate volume difference
                float volumeDiff = Mathf.Abs((anchorSize.x * anchorSize.y * anchorSize.z) - 
                                           (prefabSize.x * prefabSize.y * prefabSize.z));

                // Combine scores with weights
                return (xyAspectDiff + yzAspectDiff + xzAspectDiff) * 0.4f + volumeDiff * 0.6f;
            }
            else
            {
                // For 2D surfaces, focus on X and Y dimensions
                float areaRatio = (anchorSize.x * anchorSize.y) / (prefabSize.x * prefabSize.y);
                float aspectRatioDiff = Mathf.Abs((anchorSize.x / anchorSize.y) - (prefabSize.x / prefabSize.y));
                
                return aspectRatioDiff * 0.7f + Mathf.Abs(1 - areaRatio) * 0.3f;
            }
        }

        private MRUKAnchor AdjustWallAnchorInfo(MRUKAnchor anchorInfo)
         {
             if (anchorInfo.PlaneRect.HasValue)
             {
                 Rect planeRect = anchorInfo.PlaneRect.Value;
                 planeRect.height = WallHeight;
                 //anchorInfo.PlaneRect = planeRect;
             }

             return anchorInfo;
         }

        /// <summary>
        /// Initializes a new instance of the Random class using the specified seed.
        /// </summary>
        /// <param name="seed">The seed value to initialize the random number generator.
        /// If zero, the seed will be set to the current system tick count.
        /// </param>
        public void InitializeRandom(ref int seed)
        {
            if (seed == 0)
            {
                seed = Environment.TickCount;
            }

            _random = new Random(seed);
        }
        
        private void ShowAnchorOutlines()
        {
            var visualize = gameObject.AddComponent<CTWDrawAnchorsWithVolumeRuntime>();
            visualize.Run(MRUK.Instance.GetCurrentRoom().Anchors);
        }
    }
    
    public struct SceneTrackingSettings
    {
        public HashSet<MRUKRoom> UnTrackedRooms;
        public HashSet<MRUKAnchor> UnTrackedAnchors;
    }
    
    public enum RoofType
    {
        NONE,
        CUSTOM,
        PROCEDURAL
    };

    public struct AnchorSetup
    {
        public bool MatchAspectRatio;
        public bool CalculateFacingDirection;
        
        public AnchorSetup(bool matchAspectRatio, bool calculateFacingDirection)
        {
            this.MatchAspectRatio = matchAspectRatio;
            this.CalculateFacingDirection = calculateFacingDirection;
        }
    }
}
