using System;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.Serialization;
using Utils.GameObjectExt;

namespace Utils
{
    public class CTWAnchorPrefabSpawner : AnchorPrefabSpawner
    {
        
        [Header("Pedestal Settings")]
        [FormerlySerializedAs("_include")]
        [SerializeField, Tooltip("Anchors to include.")]
        public MRUKAnchor.SceneLabels PedestalIncludeAnchors;
        public List<GameObject> PedestalsToSpawn;

        [Header("Wall Settings")] 
        public bool CustomWallHeight = true;
        public float WallHeight = 2.0f;
        
        [Header("Roof Settings")] 
        public RoofType RoofType;
        public GameObject RoofToSpawn;
        private bool _roofSpawned = false; 
            
        [Header("Other Settings")]
        public bool ShowAnchorOutline = true;

        protected override void SpawnPrefab(MRUKAnchor anchorInfo)
        {
            if (CustomWallHeight && anchorInfo.Label == MRUKAnchor.SceneLabels.WALL_FACE)
            {
                anchorInfo = AdjustWallAnchorInfo(anchorInfo);
            }
            
            base.SpawnPrefab(anchorInfo);
            
            // Spawn Pedestal
            if (AnchorPrefabSpawnerObjects.ContainsKey(anchorInfo) && IsLabelSelected(anchorInfo.Label))
            {
                var prefab = AnchorPrefabSpawnerObjects[anchorInfo];
                var prefabBounds = Utilities.GetPrefabBounds(prefab);
                if (!prefabBounds.HasValue) return;
                SpawnPedestal(anchorInfo, prefab);
            }
            // Spawn Roof
            SpawnRoof(anchorInfo);
            
            // Make occludee
            if (AnchorPrefabSpawnerObjects.TryGetValue(anchorInfo, out var o))
            {
                o.GetOrAddComponent<CTWOcludee>();
            }
        }

        
        protected override void SpawnPrefabs(MRUKRoom room, bool clearPrefabs = true)
        {
            base.SpawnPrefabs(room, clearPrefabs);
            if (ShowAnchorOutline) ShowAnchorOutlines();
        }

        protected override void SpawnPrefabs(bool clearPrefabs = true)
        {
            base.SpawnPrefabs(clearPrefabs);
            if (ShowAnchorOutline) ShowAnchorOutlines();
        }
        
        public override GameObject CustomPrefabSelection(MRUKAnchor anchor, List<GameObject> prefabList)
        {
            GameObject sizeMatchingPrefab = null;
            float closestSizeDifference = Mathf.Infinity;

            Vector3 anchorSize = Vector3.zero;
            bool hasVolumeBounds = anchor.VolumeBounds.HasValue;

            // Determine anchor size based on VolumeBounds or PlaneBoundary2D
            if (hasVolumeBounds)
            {
                // Use VolumeBounds size if available
                anchorSize = anchor.VolumeBounds.Value.size;
                Debug.Log($"Anchor has VolumeBounds: {anchorSize}");
            }
            else if (anchor.PlaneBoundary2D != null && anchor.PlaneBoundary2D.Count > 2)
            {
                // Calculate the 2D boundary's extents (min/max X and Y values)
                var minX = anchor.PlaneBoundary2D.Min(v => v.x);
                var maxX = anchor.PlaneBoundary2D.Max(v => v.x);
                var minY = anchor.PlaneBoundary2D.Min(v => v.y);
                var maxY = anchor.PlaneBoundary2D.Max(v => v.y);

                // Use the size on the X and Y axes for comparison
                anchorSize = new Vector3(maxX - minX, maxY - minY, 0);
                Debug.Log($"Anchor has 2D boundary with size: {anchorSize}");
            }
            else
            {
                throw new InvalidOperationException(
                    "Cannot match a prefab as the anchor has neither volume bounds nor a valid 2D boundary.");
            }

            // Iterate over prefab list to find the best match
            foreach (var prefab in prefabList)
            {
                var bounds = Utilities.GetPrefabBounds(prefab);
                if (!bounds.HasValue)
                {
                    continue;
                }

                var prefabSize = bounds.Value.size;
                float totalSizeDifference;

                if (hasVolumeBounds)
                {
                    // 3D Volume comparison
                    var xDifference = Mathf.Abs(anchorSize.x - prefabSize.x);
                    var yDifference = Mathf.Abs(anchorSize.y - prefabSize.y);
                    var zDifference = Mathf.Abs(anchorSize.z - prefabSize.z);
                    totalSizeDifference = xDifference + yDifference + zDifference;

                    Debug.Log(
                        $"Comparing prefab '{prefab.name}' with VolumeBounds: Size = {prefabSize}, Total Difference = {totalSizeDifference}");
                }
                else
                {
                    // 2D X and Y axis comparison
                    var xDifference = Mathf.Abs(anchorSize.x - prefabSize.x);
                    var yDifference = Mathf.Abs(anchorSize.y - prefabSize.y);
                    totalSizeDifference = xDifference + yDifference;

                    Debug.Log(
                        $"Comparing prefab '{prefab.name}' with 2D boundary: X Difference = {xDifference}, Y Difference = {yDifference}, Total Difference = {totalSizeDifference}");
                }

                // Only update if the total size difference is smaller than the closest found so far
                if (totalSizeDifference < closestSizeDifference)
                {
                    closestSizeDifference = totalSizeDifference;
                    sizeMatchingPrefab = prefab;
                }
            }

            return sizeMatchingPrefab;
        }

        public override Vector3 CustomPrefabAlignment(Bounds anchorVolumeBounds, Bounds? prefabBounds)
        {
                var pivots = (prefabPivot: new Vector3(), anchorVolumePivot: new Vector3());
                if (prefabBounds.HasValue)
                {
                    var center = prefabBounds.Value.center;
                    var max = prefabBounds.Value.max;
                    pivots.prefabPivot = new Vector3(center.x, center.z, max.y);
                }

                pivots.anchorVolumePivot = anchorVolumeBounds.center;
                pivots.anchorVolumePivot.z = anchorVolumeBounds.max.z;

                var prefabSize = prefabBounds?.size ?? Vector3.one;
                var volumeBounds = RotateVolumeBounds(anchorVolumeBounds,
                    0);
                var volumeSize = volumeBounds.size;
                var scale = new Vector3(volumeSize.x / prefabSize.x, volumeSize.z / prefabSize.y,
                    volumeSize.y / prefabSize.z);

                var localScale = AnchorPrefabSpawnerUtilities.ScalePrefab(scale, ScalingMode.NoScaling);

                pivots.prefabPivot.x *= localScale.x;
                pivots.prefabPivot.y *= localScale.z;
                pivots.prefabPivot.z *= localScale.y;
                var localPosition = pivots.anchorVolumePivot;//- pivots.prefabPivot;
            return localPosition;
        }

        private void SpawnPedestal(MRUKAnchor anchor, GameObject originalPrefab)
        {
            if (PedestalsToSpawn.Count == 0) return;
            if (anchor.VolumeBounds.HasValue)
            {
                var anchorBounds = anchor.VolumeBounds.Value;
                var closestPedestal = CustomPrefabSelection(anchor, PedestalsToSpawn);
                var prefab = Instantiate(closestPedestal, anchor.transform, true);
                var prefabBounds = Utilities.GetPrefabBounds(prefab);
                var prefabSize = prefabBounds?.size ?? Vector3.one;
                var scale = new Vector3(anchorBounds.size.x / prefabSize.x, anchorBounds.size.z / prefabSize.y,
                    anchorBounds.size.y / prefabSize.z);

                var position = originalPrefab.transform.position;
                var sizeAdjustment = (position.y / anchorBounds.size.z);//OverTheTop ? 1 : (position.y / anchorBounds.size.z);
                prefab.transform.localScale = new Vector3(scale.x, scale.y * sizeAdjustment, scale.z);
                prefab.transform.position = new Vector3(position.x, 0,
                    position.z);
                prefab.transform.rotation = originalPrefab.transform.rotation;
                prefab.GetOrAddComponent<CTWDynamicOcclusionObject>();
                // TODO: if min y is below zero adjust pivot
            }
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

        private void SpawnDebug(Transform reftransform)
        {
            var debugCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debugCube.transform.rotation = reftransform.rotation;
            debugCube.transform.position = reftransform.position;
        }
        
        private void ShowAnchorOutlines()
        {
            var visualize = gameObject.AddComponent<CTWDrawAnchorsWithVolumeRuntime>();
            visualize.Run(MRUK.Instance.GetCurrentRoom().Anchors);
        }

        private Bounds RotateVolumeBounds(Bounds bounds,
            int rotation = 0)
        {
            var center = bounds.center;
            var size = bounds.size;
            return rotation switch
            {
                1 => new Bounds(new Vector3(-center.y, center.x, center.z), new Vector3(size.y, size.x, size.z)),
                2 => new Bounds(new Vector3(-center.x, -center.x, center.z), size),
                3 => new Bounds(new Vector3(center.y, -center.x, center.z), new Vector3(size.y, size.x, size.z)),
                _ => bounds
            };
        }
        
        private bool IsLabelSelected(MRUKAnchor.SceneLabels label)
        {
            return (PedestalIncludeAnchors & label) == label;
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
    }
    
    // public enum RoofType
    // {
    //     NONE,
    //     CUSTOM,
    //     PROCEDURAL
    // };
    
}