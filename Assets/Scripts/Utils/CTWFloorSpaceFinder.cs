using System.Collections.Generic;
using System.Text;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace Utils
{
    public class CTWFloorSpaceFinder : MonoBehaviour
    {
        [Header("Search Params")]
        public float minWidth = 1.0f;
        public float minDepth = 1.0f;
        public float checkHeight = 2.0f;
        public float padding = 0.1f;

        [Header("Overlap")]
        public LayerMask overlapLayers = -1;
        public bool checkAnchors = true;
        public bool checkColliders = true;
        public float boundaryMargin = 0.2f;

        [Header("Sampling")]
        public int maxAttempts = 50;

        [Header("Debug")]
        public bool drawDebugGizmos = true;
        public Color candidateGood = Color.green;
        public Color candidateBad = Color.red;
        public float gizmoPointSize = 0.05f;

        private MRUKRoom _currentRoom;
        private MRUKAnchor _floorAnchor;
        private readonly List<Bounds> _anchorBounds = new();
        private Bounds _floorBounds;
        private readonly List<(Vector3 pos, bool ok)> _candidateDebugPoints = new();

        public struct FloorSpaceResult
        {
            public bool found;
            public Vector3 position;
            public Vector3 normal;
            public Quaternion rotation;
            public Bounds bounds;
            public float score;
        }

        // ======================================================
        // SIMULATOR DETECTION (SAFE FOR ALL SDK VERSIONS)
        // ======================================================

        private bool IsSimulator()
        {
            // Safest & universal check
            return Application.isEditor;
        }

        // ======================================================
        // INIT
        // ======================================================

        void Start()
        {
            if (MRUK.Instance != null)
                MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
        }

        void OnSceneLoaded()
        {
            CacheRoomData();
        }

        // ======================================================
        // CACHE ROOM
        // ======================================================

        public void CacheRoomData()
        {
            _currentRoom = MRUK.Instance?.GetCurrentRoom();
            if (_currentRoom == null)
            {
                Debug.LogWarning("[FSF] No current room");
                return;
            }

            _floorAnchor = _currentRoom.FloorAnchor;
            if (_floorAnchor == null)
            {
                Debug.LogWarning("[FSF] No floor anchor found");
                return;
            }

            _floorBounds = CalculateFloorBounds(_floorAnchor);
            _anchorBounds.Clear();

            bool sim = IsSimulator();

            foreach (var anchor in _currentRoom.Anchors)
            {
                if (anchor == _floorAnchor)
                    continue;

                // True spatial bounds (real MR scan)
                if (anchor.VolumeBounds.HasValue)
                {
                    _anchorBounds.Add(anchor.VolumeBounds.Value);
                    continue;
                }

                // Simulator → skip fake anchors entirely
                if (sim)
                    continue;

                // Fallback for real rooms with plane rect only
                if (anchor.PlaneRect.HasValue)
                {
                    var r = anchor.PlaneRect.Value;
                    _anchorBounds.Add(new Bounds(
                        anchor.transform.position,
                        new Vector3(r.size.x, 0.1f, r.size.y)
                    ));
                }
            }

            Debug.Log($"[FSF] Cached {_anchorBounds.Count} anchors. FloorRect={_floorBounds.size}");
        }

        // ======================================================
        // FIND EMPTY SPOT
        // ======================================================

        public FloorSpaceResult FindEmptyFloorSpace(float width, float depth)
        {
            if (_currentRoom == null || _floorAnchor == null)
                CacheRoomData();

            if (_currentRoom == null || _floorAnchor == null)
                return new FloorSpaceResult();

            _candidateDebugPoints.Clear();

            Vector3 areaSize = new Vector3(width + padding * 2f, checkHeight, depth + padding * 2f);

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                bool ok = _currentRoom.GenerateRandomPositionOnSurface(
                    MRUK.SurfaceType.FACING_UP,
                    Mathf.Max(width, depth) / 2f,
                    new LabelFilter(),     // FIX: null not allowed
                    out Vector3 pos,
                    out Vector3 normal
                );

                if (!ok)
                    continue;

                bool clear = IsAreaClear(pos, areaSize, normal);
                _candidateDebugPoints.Add((pos, clear));

                if (!clear)
                    continue;

                return new FloorSpaceResult
                {
                    found = true,
                    position = pos,
                    normal = normal,
                    rotation = Quaternion.FromToRotation(Vector3.up, normal),
                    bounds = new Bounds(pos, areaSize),
                    score = CalculatePositionScore(pos)
                };
            }

            return new FloorSpaceResult { found = false };
        }

        // ======================================================
        // BEST SPOT
        // ======================================================

        public FloorSpaceResult FindBestEmptyFloorSpace(float width, float depth, int samples = 10)
        {
            FloorSpaceResult best = new FloorSpaceResult { found = false, score = -999 };

            for (int i = 0; i < samples; i++)
            {
                var r = FindEmptyFloorSpace(width, depth);
                if (r.found && r.score > best.score)
                    best = r;
            }

            return best;
        }

        // ======================================================
        // MANUAL CHECK
        // ======================================================

        public bool HasSpaceAt(Vector3 position, float width, float depth)
        {
            Vector3 size = new Vector3(width + padding * 2f, checkHeight, depth + padding * 2f);
            Vector3 normal = _floorAnchor ? _floorAnchor.transform.up : Vector3.up;

            return IsAreaClear(position, size, normal);
        }

        // ======================================================
        // AREA CHECK
        // ======================================================

        private bool IsAreaClear(Vector3 center, Vector3 size, Vector3 normal)
        {
            bool sim = IsSimulator();
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, normal);

            // Physics overlap
            if (checkColliders)
            {
                var overlaps = Physics.OverlapBox(
                    center + normal * (checkHeight / 2f),
                    size / 2f,
                    rot,
                    overlapLayers
                );

                foreach (var c in overlaps)
                {
                    if (c.transform == _floorAnchor.transform)
                        continue;

                    if (sim && (c.name.Contains("MRUK") || c.name.Contains("Simulator")))
                        continue;

                    return false;
                }
            }

            // MR spatial anchors
            if (checkAnchors)
            {
                var test = new Bounds(center, size);
                foreach (var b in _anchorBounds)
                    if (test.Intersects(b))
                        return false;
            }

            // Edges
            if (!IsWithinFloorBounds(center, size.x, size.z))
                return false;

            return true;
        }

        private bool IsWithinFloorBounds(Vector3 pos, float width, float depth)
        {
            Vector3 local = _floorAnchor.transform.InverseTransformPoint(pos);

            float hw = width / 2f + boundaryMargin;
            float hd = depth / 2f + boundaryMargin;

            return Mathf.Abs(local.x) < (_floorBounds.extents.x - hw) &&
                   Mathf.Abs(local.z) < (_floorBounds.extents.z - hd);
        }

        // ======================================================
        // BOUNDS / SCORE
        // ======================================================

        private Bounds CalculateFloorBounds(MRUKAnchor anchor)
        {
            if (anchor.PlaneRect.HasValue)
            {
                var r = anchor.PlaneRect.Value;
                return new Bounds(anchor.transform.position,
                    new Vector3(r.size.x, 0.1f, r.size.y));
            }

            return new Bounds(anchor.transform.position, Vector3.one * 5f);
        }

        private float CalculatePositionScore(Vector3 pos)
        {
            float score = 0f;

            float distToCenter = Vector3.Distance(pos, _floorBounds.center);
            float max = _floorBounds.extents.magnitude;

            score += (1 - distToCenter / max) * 0.5f;

            float minDist = float.MaxValue;
            foreach (var b in _anchorBounds)
                minDist = Mathf.Min(minDist, Vector3.Distance(pos, b.center));

            score += Mathf.Clamp01(minDist / 2f) * 0.5f;

            return score;
        }

        // ======================================================
        // GIZMOS
        // ======================================================

        private void OnDrawGizmos()
        {
            if (!drawDebugGizmos)
                return;

            if (_floorAnchor != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(_floorBounds.center, _floorBounds.size);
            }

            foreach (var b in _anchorBounds)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(b.center, b.size);
            }

            foreach (var (pos, ok) in _candidateDebugPoints)
            {
                Gizmos.color = ok ? candidateGood : candidateBad;
                Gizmos.DrawSphere(pos, gizmoPointSize);
            }
        }

        // ======================================================
        // SINGLE DEBUG LOG
        // ======================================================

        [ContextMenu("DEBUG_FloorSpace")]
        public void DebugFloorSpace()
        {
            StringBuilder sb = new();

            sb.AppendLine("=========== CTW FLOOR SPACE DEBUG ===========");
            sb.AppendLine($"Simulator: {IsSimulator()}");
            sb.AppendLine($"Room OK: {_currentRoom != null}");
            sb.AppendLine($"FloorAnchor OK: {_floorAnchor != null}");

            if (_floorAnchor != null)
            {
                sb.AppendLine($"Label: {_floorAnchor.Label}");
                sb.AppendLine($"PlaneRect: {_floorAnchor.PlaneRect?.size}");
                sb.AppendLine($"Boundary2D: {_floorAnchor.PlaneBoundary2D?.Count}");
                sb.AppendLine($"VolumeBounds Exists: {_floorAnchor.VolumeBounds.HasValue}");
            }

            sb.AppendLine("\n--- Anchors ---");
            foreach (var a in _currentRoom?.Anchors ?? new List<MRUKAnchor>())
            {
                sb.AppendLine(
                    $"• {a.name} | Label={a.Label} | PR={a.PlaneRect?.size} | VB={a.VolumeBounds}"
                );
            }

            sb.AppendLine($"\nFloorBounds: center={_floorBounds.center}, size={_floorBounds.size}");
            sb.AppendLine("=============================================");

            Debug.Log(sb.ToString());
        }
    }
}
