# Cathedral: The Witch

A mixed-reality horror experience for **Meta Quest 3 / 3S**, built with **Unity 6 LTS**, **Universal Render Pipeline**, and the **Meta XR SDK**. The game blends environmental storytelling with spatial computing — scanning the player's real room, integrating it into the scene, and using it as the stage for a narrative-driven horror encounter.

> Demo video coming soon.

---

## Platform & Stack

| | |
|---|---|
| **Target hardware** | Meta Quest 3 / 3S |
| **Engine** | Unity 6.0 LTS |
| **Render pipeline** | Universal Render Pipeline (URP) |
| **XR integration** | Meta XR SDK · MRUK (Mixed Reality Utility Kit) |
| **Input** | Unity Input System + legacy fallback |
| **Animation** | Unity Playables API · Vertex Animation Textures (VAT) |

---

## Architecture Overview

The codebase is organized around a few well-separated systems that communicate through a typed message bus rather than direct references. No monolithic managers — each system owns a clear responsibility.

```
Assets/Scripts/Utils/
├── Story/
│   ├── System/        # Director, Story Events (ScriptableObjects), Presenters
│   └── Tutorial/      # Phase-based spatial tutorial
├── MessageBus/        # Typed pub/sub with sticky-message replay
├── Scene/             # Async scene manager (Task-based)
├── Animation/         # Playables graph + VAT controller
├── Occlusion/         # Multi-camera stereo frustum culling
├── Mesh/              # MRUK mesh utilities, DBSCAN clustering, serialization
├── Logging/           # Conditional diagnostics (stripped from Release)
└── Editor/            # Custom tooling (overlap checker, material scanner, XR reload)
```

---

## Systems

### Story System

A **Director / Presenter** pattern that fully separates narrative data from display logic.

- **`CTWStoryEvent`** (ScriptableObject) — a sequence of slides, each carrying image, subtitle, audio clip, duration, fade time, and an input-wait flag. Events declare `requireFlags`, `forbidFlags`, and `setFlags` for branching narrative flow.
- **`CTWStoryDirector`** — singleton queue runner. Manages a `HashSet`-backed flag store persisted to PlayerPrefs, enforces once-only playback guards, and supports both InputSystem and legacy input.
- **`CTWStoryPresenter`** — abstract base with `Show(slide)` / `Hide(fadeSeconds)` coroutines.

Five concrete presenters are provided:

| Presenter | Description |
|---|---|
| `CTWBillboardPresenter` | World-space canvas that follows the player with damped exponential lerp |
| `CTWFullscreenPresenter` | Screen-space overlay |
| `CTWDioramaPresenter` | Spawns a 3D prefab scene (scale / alpha / instant fade modes) |
| `CTWVideoPresenter` | Video clip playback |
| `CTWHybridPresenter` | Routes slides to sub-presenters via `[tag]` prefixes in subtitle text, allowing mixed styles within a single event |

Adding a new display mode requires implementing one abstract class — no changes to the director or event data.

---

### Message Bus

A lightweight, type-safe pub/sub system with sticky-message replay.

```csharp
// Send from anywhere
CTWMessageBus<GameStateChanged>.Send(new GameStateChanged { State = GameState.Playing });

// Subscribe — receiveLast: true replays the most recent message immediately on subscribe
CTWMessageBus<GameStateChanged>.Subscribe(OnStateChanged, receiveLast: true);
```

- Inspector-wirable via `CTWGenericMessageListener` / `CTWGenericMessageSender`, which use reflection to discover all `ICTWMessage` types at edit time.
- `CTWMessageChannel` ScriptableObject enables asset-level sender/receiver decoupling across scenes.

---

### Scene Manager

Task-based async loading with `CancellationToken` support. Wraps Unity's `AsyncOperation` via an `AsTask()` extension:

```csharp
await CTWSceneManager.Instance.LoadAsync("gameplay");
await CTWSceneManager.Instance.SwapToAsync("credits");   // keep listed, unload rest
await CTWSceneManager.Instance.NextAsync(wrap: true);
```

Per-slot state (`IsLoaded`, `IsLoading`, `IsUnloading`) and events (`SceneLoaded`, `SceneUnloaded`, `ActiveSceneChanged`) give the rest of the codebase clean hooks without polling.

---

### Tutorial System

Three sequential spatial phases, each completing by observable world interaction rather than button presses:

1. **GoToLight** — spawns a light beacon; completes when the player enters its radius
2. **LookAtHands** — completes after the player gazes at their own hands for a configurable duration
3. **PickupDoll** — completes when a grabbable object moves more than 0.3 m from its spawn point

Completion state is persisted so the tutorial is skipped on subsequent sessions. Story flags are set at each phase boundary to gate narrative whispers.

---

### Mixed Reality Environment

**Anchor-Driven Spawning** (`CTWEnvironmentSpawner`): scores MRUK anchors by weighted aspect-ratio and volume similarity to a target, then uses a seeded RNG for consistent spawning across sessions.

**Procedural Roof** (`CTWRoofGenerator`): builds a hipped-roof mesh from a `PlaneBoundary2D` polygon extracted from MRUK wall anchors.

**Floor Space Finder** (`CTWFloorSpaceFinder`): Monte Carlo sampling on the floor anchor. Candidate positions are rejected if `Physics.OverlapBox` finds furniture volumes within a configurable padding. Visualized with debug gizmos in-editor.

**Mesh Processing** (`MeshUtils`, `DBSCANClustering`): MRUK global mesh is filtered to remove flat surfaces and clustered with DBSCAN to detect spatial anomalies. Centroids are visualized as scene annotations. Room scan data can be serialized to JSON for offline debugging.

---

### Multi-Camera Occlusion

Standard Unity frustum culling misses geometry visible to one eye but not the other in stereo VR. `CTWOcclusionManager` solves this with three proxy cameras:

- One center camera
- Two offset cameras at ±half-IPD (0.032 m default)

Each has configurable FOV. `CTWMultiOcclusionCameraManager` coordinates them and ensures correct culling across the full stereo view volume.

---

### Animation

`CTWAnimationBlender` builds a `PlayableGraph` → `AnimationMixerPlayable` → per-state `AnimationClipPlayable` pipeline directly, without an AnimatorController. Weights are normalized each frame. All clips are forced-looping at the graph level.

`CTWWitchAnimationController` bridges this to a Vertex Animation Texture pipeline, blending between Idle / Float / Fly states based on speed thresholds, with smooth rotation applied during flight.

---

### Performance

- Per-device quality tiers with LOD bias, MSAA, and anisotropic texture control
- Application Space Warp (ASW) auto-enabled via `CTWLogger` startup diagnostics
- Verbose logging wrapped in `CTWLog.Verbose()` — stripped from Release builds via `#if UNITY_EDITOR || DEVELOPMENT_BUILD`
- Playables API chosen over AnimatorController to minimize overhead on Quest hardware

---

### Editor Tooling

| Tool | Purpose |
|---|---|
| `CTWCollidersOverlapChecker` | AABB overlap finder across selected GameObjects (Tools menu) |
| `FindMetaMaterials` | Scans all project materials for `Meta/` shader references |
| `XRReloadReset` | `[InitializeOnLoad]` hook that reinitializes the XR loader on Play Mode transitions to prevent Quest Link instability |
| `CTWGenericMessageListenerEditor` | Reflection-driven type-picker for `ICTWMessage` subscriptions in the Inspector |

---

## Application Lifecycle

```
Bootstrap scene (XR loader init, 5s timeout)
  │
  └─► CTWBootUp.OnBootComplete
        │
        └─► CTWGameManager state machine
              Initializing → Tutorial → Playing → Paused
```

`CTWSettings` applies runtime configuration (master volume, physics timestep) on Awake. `CTWInitXR` configures camera tags, URP base/overlay modes, skybox, and non-XR camera fallback.

---

## Scene Structure

```
Scenes/
  Bootstrap.unity      XR subsystem initialization
  CTWScene.unity       Main gameplay
  Intro.unity
  SceneTest.unity
```

---

## Development

Built and tested against **Unity 6.0 LTS**. The Meta XR SDK and MRUK are vendored under `Assets/Oculus/` and `Assets/MRUKSample/`. Quest Link is the primary development workflow; the `XRReloadReset` editor script stabilizes Play Mode transitions over Link.

To reset story state during testing, use the context menu on `CTWStoryDirector` → *Clear All Persistent Flags*.
