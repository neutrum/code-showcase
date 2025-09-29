using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;

public class CTWEnemySpawner : MonoBehaviour
{
    private MRUKRoom _room;
    private Camera _camera;

    public GameObject ToSpawn;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _room = MRUK.Instance.GetCurrentRoom();
        _camera = Camera.main;
        SpawnObject();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SpawnObject(PositionType type = PositionType.OnTopOfWall)
    {
        switch (type)
        {
            case PositionType.OnTopOfWall:
                SpawnOnTopOfWall();
                break;
            case PositionType.OnWallWithDirection:
                SpawnOnWall(Vector3.back);
                break;
        }
    }


    private void SpawnOnTopOfWall()
    {
        Vector3? spawnPoint = GetRandomTopEdgePointFromWallsBehindCamera();

        if (spawnPoint.HasValue && ToSpawn != null)
        {
            Instantiate(ToSpawn, spawnPoint.Value, Quaternion.identity, transform);
            Debug.Log($"Spawned {ToSpawn.name} at {spawnPoint.Value}");
        }
        else
        {
            Debug.LogWarning("No valid spawn point found or ToSpawn is null.");
        }
    }

    private void SpawnOnWall(Vector3 direction)
    {
        var result = GetWallAlignedPose(direction);
        if (result.HasValue && ToSpawn != null)
        {
            ToSpawn.name = $"Enemy-{GetName(direction)}";
            Instantiate(ToSpawn, result.Value.position, result.Value.rotation, transform);
        }
    }

    public Vector3? GetRandomTopEdgePointFromWallsBehindCamera(int samplePerWall = 10)
    {
        var wallAnchors = _room.WallAnchors;
        List<Vector3> allCandidates = new();

        foreach (var anchor in wallAnchors)
        {
            if (anchor == null || anchor.PlaneBoundary2D == null || anchor.PlaneBoundary2D.Count < 2)
                continue;

            // Find top edge
            List<Vector2> sorted = new List<Vector2>(anchor.PlaneBoundary2D);
            sorted.Sort((a, b) => b.y.CompareTo(a.y)); // descending Y

            Vector2 topA = sorted[0];
            Vector2 topB = sorted[1];

            for (int i = 0; i <= samplePerWall; i++)
            {
                float t = i / (float)samplePerWall;
                Vector2 lerp2D = Vector2.Lerp(topA, topB, t);
                Vector3 localPoint = new Vector3(lerp2D.x, lerp2D.y, 0);
                Vector3 worldPoint = anchor.transform.TransformPoint(localPoint);

                Vector3 toPoint = worldPoint - _camera.transform.position;
                if (Vector3.Dot(_camera.transform.forward, toPoint) < 0f)
                {
                    allCandidates.Add(worldPoint);
                }
            }
        }

        if (allCandidates.Count == 0)
            return null;

        return allCandidates[Random.Range(0, allCandidates.Count)];
    }

    public (Vector3 position, Quaternion rotation)? GetWallAlignedPose(Vector3 localAxisToAlign)
    {
        var wallAnchors = _room?.WallAnchors;
        if (wallAnchors == null || wallAnchors.Count == 0)
            return null;

        MRUKAnchor wall = null;
        for (int i = 0; i < 10; i++)
        {
            var candidate = wallAnchors[Random.Range(0, wallAnchors.Count)];
            if (candidate != null && candidate.PlaneRect.HasValue)
            {
                wall = candidate;
                break;
            }
        }

        if (wall == null)
            return null;

        // Random point inside inner 70% of the wall
        Rect rect = wall.PlaneRect.Value;
        float scale = 0.7f;
        float dx = rect.width * (1f - scale) * 0.5f;
        float dy = rect.height * (1f - scale) * 0.5f;
        float x = Random.Range(rect.x + dx, rect.xMax - dx);
        float y = Random.Range(rect.y + dy, rect.yMax - dy);
        Vector3 localPoint = new Vector3(x, y, 0f);
        Vector3 worldPosition = wall.transform.TransformPoint(localPoint);

        // Determine world-aligned target vector for your object axis
        Vector3 wallNormal = wall.transform.forward;
        Vector3 targetDirection = -wallNormal; // we want the object's axis to point INTO the wall

        // Compute rotation that aligns 'localAxisToAlign' to 'targetDirection'
        Quaternion rotation = Quaternion.FromToRotation(localAxisToAlign, targetDirection);

        return (worldPosition, rotation);
    }




    private static readonly Dictionary<Vector3, string> VectorNames = new()
    {
        { Vector3.up, "Vector3.up" },
        { Vector3.down, "Vector3.down" },
        { Vector3.left, "Vector3.left" },
        { Vector3.right, "Vector3.right" },
        { Vector3.forward, "Vector3.forward" },
        { Vector3.back, "Vector3.back" },
        { Vector3.zero, "Vector3.zero" },
        { Vector3.one, "Vector3.one" }
    };

    public static string GetName(Vector3 vector)
    {
        return VectorNames.TryGetValue(vector, out var name) ? name : vector.ToString();
    }



    public enum PositionType
    {
        OnTopOfWall,
        OnWallWithDirection
    }
}
