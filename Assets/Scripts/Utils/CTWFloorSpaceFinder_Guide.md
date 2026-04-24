# CTW Floor Space Finder - Complete Guide

Find empty rectangular areas on the floor that don't overlap with furniture, walls, or other objects in your MR scene.

---

## Quick Start

### Setup (3 steps)

1. **Add to Scene:**
```
Create Empty GameObject → Add Component → CTWFloorSpaceFinder
```

2. **Configure Settings:**
```
Min Width: 1.0m
Min Depth: 1.0m
Check Height: 2.0m  (how high to check for obstacles)
Check Anchors: ✅
Check Colliders: ✅
```

3. **Use in Code:**
```csharp
public CTWFloorSpaceFinder floorFinder;

void Start()
{
    var result = floorFinder.FindEmptyFloorSpace(1.0f, 1.5f);
    if (result.found)
    {
        Instantiate(myObject, result.position, result.rotation);
    }
}
```

---

## Core Methods

### 1. Find Single Empty Space

```csharp
FloorSpaceResult FindEmptyFloorSpace(float width, float depth)
```

**Parameters:**
- `width` - Width of area in meters (X axis)
- `depth` - Depth of area in meters (Z axis)

**Returns:**
- `FloorSpaceResult` with position, rotation, bounds, and quality score

**Example:**
```csharp
// Find 2m x 1.5m area for table
var result = floorFinder.FindEmptyFloorSpace(2.0f, 1.5f);

if (result.found)
{
    Debug.Log($"Found space at {result.position}");
    Debug.Log($"Quality score: {result.score}");
    
    // Spawn table
    Instantiate(tablePrefab, result.position, result.rotation);
}
```

---

### 2. Find Multiple Empty Spaces

```csharp
List<FloorSpaceResult> FindMultipleEmptySpaces(float width, float depth, int count)
```

**Perfect for:** Spawning multiple enemies, furniture, collectibles

**Example:**
```csharp
// Find 5 spots for enemy spawns (0.5m x 0.5m each)
var spawns = floorFinder.FindMultipleEmptySpaces(0.5f, 0.5f, 5);

foreach (var spawn in spawns)
{
    Instantiate(enemyPrefab, spawn.position, spawn.rotation);
}
```

---

### 3. Find Best Quality Space

```csharp
FloorSpaceResult FindBestEmptyFloorSpace(float width, float depth, int samples = 10)
```

**Tries multiple positions and returns the best one**

Quality Score considers:
- ✅ Distance from center (prefers center)
- ✅ Distance from obstacles (prefers open areas)
- ✅ Distance from walls (prefers away from edges)

**Example:**
```csharp
// Find the best spot for player spawn
var result = floorFinder.FindBestEmptyFloorSpace(1.0f, 1.0f, samples: 20);

if (result.found)
{
    player.transform.position = result.position;
    Debug.Log($"Spawn quality: {result.score:F2}");
}
```

---

### 4. Check Specific Position

```csharp
bool HasSpaceAt(Vector3 position, float width, float depth)
```

**Check if a specific position has enough space**

**Example:**
```csharp
// Check if player's current position has space for furniture
if (floorFinder.HasSpaceAt(player.position, 1.5f, 1.5f))
{
    Debug.Log("You can place furniture here!");
}
else
{
    Debug.Log("Not enough space!");
}
```

---

### 5. Get Floor Position

```csharp
bool GetFloorPositionAt(Vector3 worldPoint, out Vector3 floorPosition, out Vector3 floorNormal)
```

**Snap world position to floor**

**Example:**
```csharp
// Snap raycast hit to floor
if (floorFinder.GetFloorPositionAt(hitPoint, out Vector3 floorPos, out Vector3 normal))
{
    marker.transform.position = floorPos;
    marker.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
}
```

---

## FloorSpaceResult Structure

```csharp
public struct FloorSpaceResult
{
    public bool found;          // Was a valid space found?
    public Vector3 position;    // Center position of the area
    public Vector3 normal;      // Floor surface normal
    public Quaternion rotation; // Recommended rotation (aligned with floor)
    public Bounds bounds;       // Full 3D bounds of the area
    public float score;         // Quality score (0-1, higher = better)
}
```

**Usage:**
```csharp
var result = floorFinder.FindEmptyFloorSpace(1f, 1f);

if (result.found)
{
    transform.position = result.position;
    transform.rotation = result.rotation;
    
    // Access bounds if needed
    Debug.Log($"Area size: {result.bounds.size}");
}
```

---

## Configuration Options

### Search Parameters

| Setting | Default | Description |
|---------|---------|-------------|
| `minWidth` | 1.0m | Minimum width to search for |
| `minDepth` | 1.0m | Minimum depth to search for |
| `checkHeight` | 2.0m | How high to check for obstacles |
| `padding` | 0.1m | Extra space around area |

### Overlap Detection

| Setting | Default | Description |
|---------|---------|-------------|
| `checkAnchors` | ✅ true | Check MRUK anchors (furniture, walls) |
| `checkColliders` | ✅ true | Check Unity colliders |
| `overlapLayers` | All | Which layers to check |
| `boundaryMargin` | 0.2m | Distance from room edges |

### Search Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `maxAttempts` | 50 | Max tries before giving up |
| `gridResolution` | 0.1m | Search precision (lower = slower but more accurate) |

---

## Common Use Cases

### Use Case 1: Enemy Spawner

```csharp
public class EnemySpawner : MonoBehaviour
{
    public CTWFloorSpaceFinder floorFinder;
    public GameObject enemyPrefab;
    public int enemyCount = 5;
    
    public void SpawnEnemies()
    {
        var spawns = floorFinder.FindMultipleEmptySpaces(0.5f, 0.5f, enemyCount);
        
        foreach (var spawn in spawns)
        {
            var enemy = Instantiate(enemyPrefab, spawn.position, spawn.rotation);
            Debug.Log($"Spawned enemy with score: {spawn.score}");
        }
    }
}
```

---

### Use Case 2: Furniture Placement

```csharp
public class FurniturePlacer : MonoBehaviour
{
    public CTWFloorSpaceFinder floorFinder;
    
    public void PlaceTable(GameObject tablePrefab)
    {
        // Tables need more space
        var result = floorFinder.FindBestEmptyFloorSpace(1.5f, 2.0f, samples: 15);
        
        if (result.found)
        {
            Instantiate(tablePrefab, result.position, result.rotation);
        }
    }
    
    public void PlaceChair(GameObject chairPrefab)
    {
        // Chairs need less space
        var result = floorFinder.FindEmptyFloorSpace(0.6f, 0.6f);
        
        if (result.found)
        {
            Instantiate(chairPrefab, result.position, result.rotation);
        }
    }
}
```

---

### Use Case 3: Player Safe Zone

```csharp
public class SafeZoneCreator : MonoBehaviour
{
    public CTWFloorSpaceFinder floorFinder;
    public GameObject safeZoneVisualPrefab;
    public float safeZoneRadius = 2.0f;
    
    public void CreateSafeZone()
    {
        // Find circular area
        float diameter = safeZoneRadius * 2f;
        var result = floorFinder.FindBestEmptyFloorSpace(diameter, diameter, samples: 20);
        
        if (result.found)
        {
            var safeZone = Instantiate(safeZoneVisualPrefab, result.position, Quaternion.identity);
            safeZone.transform.localScale = Vector3.one * diameter;
            Debug.Log($"Safe zone created at {result.position}");
        }
    }
}
```

---

### Use Case 4: Collectible Scatter

```csharp
public class CollectibleScatterer : MonoBehaviour
{
    public CTWFloorSpaceFinder floorFinder;
    public GameObject collectiblePrefab;
    
    public void ScatterCollectibles(int count)
    {
        // Small collectibles (0.3m x 0.3m)
        var positions = floorFinder.FindMultipleEmptySpaces(0.3f, 0.3f, count);
        
        foreach (var pos in positions)
        {
            var collectible = Instantiate(collectiblePrefab, pos.position, pos.rotation);
            
            // Add random rotation
            collectible.transform.Rotate(0, Random.Range(0f, 360f), 0);
        }
        
        Debug.Log($"Scattered {positions.Count} collectibles");
    }
}
```

---

### Use Case 5: Interactive Marker Placement

```csharp
public class MarkerPlacer : MonoBehaviour
{
    public CTWFloorSpaceFinder floorFinder;
    public GameObject markerPrefab;
    public float markerSize = 0.5f;
    
    void Update()
    {
        // Raycast from controller
        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger))
        {
            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Snap to floor
                if (floorFinder.GetFloorPositionAt(hit.point, out Vector3 floorPos, out Vector3 normal))
                {
                    // Check if there's space
                    if (floorFinder.HasSpaceAt(floorPos, markerSize, markerSize))
                    {
                        Instantiate(markerPrefab, floorPos, Quaternion.FromToRotation(Vector3.up, normal));
                    }
                    else
                    {
                        Debug.Log("Not enough space here!");
                    }
                }
            }
        }
    }
}
```

---

## Optimization Tips

### ✅ Cache Room Data

The finder automatically caches room data on scene load, but you can force a refresh:

```csharp
floorFinder.CacheRoomData();
```

Call this if:
- Room changes
- New anchors are added
- Objects are moved/removed

### ✅ Adjust Max Attempts

For faster searches (less precise):
```csharp
floorFinder.maxAttempts = 20;  // Default: 50
```

For more thorough searches:
```csharp
floorFinder.maxAttempts = 100;
```

### ✅ Use Appropriate Dimensions

```csharp
// ❌ Don't search for huge areas
FindEmptyFloorSpace(10f, 10f);  // Will likely fail

// ✅ Use reasonable sizes
FindEmptyFloorSpace(2f, 2f);   // More likely to succeed
```

---

## Debugging

### Enable Debug Visualization

```csharp
floorFinder.drawDebugGizmos = true;
```

**Shows in Scene View:**
- Blue wireframe = Floor bounds
- Red wireframes = Occupied areas (anchors)
- Green = Found empty spaces (when searching)

### Debug Logs

The finder logs useful information:
```
[FloorSpaceFinder] Cached 15 anchor bounds
[FloorSpaceFinder] Found empty area at (1.2, 0, 0.5)
[FloorSpaceFinder] Could not find empty area after 50 attempts
```

---

## Troubleshooting

### ❌ "No current room found"

**Solution:** Make sure MRUK is loaded:
```csharp
MRUK.Instance.RegisterSceneLoadedCallback(() => {
    floorFinder.CacheRoomData();
});
```

### ❌ Always returns `found = false`

**Check:**
1. Is the requested area too large?
2. Is `maxAttempts` too low?
3. Are there too many obstacles in the room?
4. Is `checkHeight` catching overhead obstacles?

**Try:**
```csharp
// Reduce required space
FindEmptyFloorSpace(0.5f, 0.5f);

// Increase attempts
floorFinder.maxAttempts = 100;

// Reduce check height if ceiling is low
floorFinder.checkHeight = 1.0f;
```

### ❌ Objects spawn inside furniture

**Check:**
- `checkAnchors` is enabled
- `checkColliders` is enabled
- Furniture has colliders
- Furniture is on the correct layer

---

## Performance

### Typical Performance:

| Operation | Time | Notes |
|-----------|------|-------|
| `FindEmptyFloorSpace()` | 1-10ms | Depends on room complexity |
| `FindMultipleEmptySpaces(5)` | 5-50ms | Linear with count |
| `FindBestEmptyFloorSpace(20)` | 20-200ms | 20 samples × search time |
| `HasSpaceAt()` | <1ms | Just checks, no search |

### Best Practices:

✅ **DO:**
- Cache the finder reference
- Call `FindEmptyFloorSpace()` once when needed
- Use `HasSpaceAt()` for quick checks
- Spread multiple searches over frames

❌ **DON'T:**
- Call `FindEmptyFloorSpace()` every frame
- Search for unnecessarily large areas
- Use `FindBestEmptyFloorSpace()` with 100+ samples

---

## Integration with Existing Systems

### With CTWSpawnObject

```csharp
// Replace random spawn with smart placement
var result = floorFinder.FindEmptyFloorSpace(objectWidth, objectDepth);
if (result.found)
{
    spawnObject.transform.position = result.position;
}
```

### With Story System

```csharp
// Place diorama in clear space
var result = floorFinder.FindBestEmptyFloorSpace(1.5f, 1.5f);
if (result.found)
{
    dioramaPresenter.dioramaRoot.position = result.position;
}
```

---

## Summary

**CTWFloorSpaceFinder** gives you:
✅ Easy way to find empty floor areas  
✅ Collision-free object placement  
✅ Smart positioning (avoids walls, prefers center)  
✅ Works with MRUK room data  
✅ Perfect for dynamic MR gameplay  

**Key Methods:**
- `FindEmptyFloorSpace()` - Find one spot
- `FindMultipleEmptySpaces()` - Find many spots
- `FindBestEmptyFloorSpace()` - Find the best spot
- `HasSpaceAt()` - Check specific position

Now you can confidently place enemies, furniture, collectibles, and gameplay elements without worrying about overlaps! 🎮✨


