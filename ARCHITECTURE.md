# Cathedral: The Witch — Architecture Overview

Mixed Reality horror game for Meta Quest 3/3S built with Unity URP + Meta XR SDK.

---

## Application Lifecycle

```
Bootstrap scene
  └── CTWBootUp          — waits for XR loader init, fires OnBootComplete
  └── CTWSettings        — applies master volume and physics timestep once on Awake
  └── CTWQuestQuickPreset — applies display Hz, eye texture scale, and foveation via
                            both OpenXR and OVRPlugin reflection paths

Main scene
  └── CTWGameManager     — singleton state machine (Initializing → Tutorial → Playing → Paused)
                           drives which object groups are active; fires UnityEvents on transitions
  └── CTWStoryDirector   — story runner (see below)
  └── CTWTutorialManager — tutorial sequencer (see below)
```

---

## Story System (`CTW.Story` namespace)

**Pattern:** Director + Presenter. Data (what to show) is separated from presentation (how to show it).

### Data
- **`CTWStoryEvent`** (ScriptableObject) — a sequence of slides. Each slide carries: image, subtitle, audio clip, duration, fade time, wait-for-input flag. The event also holds `requireFlags`, `forbidFlags`, and `setFlags` for flag-gating.

### Director
- **`CTWStoryDirector`** — singleton. Maintains:
  - `Queue<CTWStoryEvent>` — events pending playback
  - `HashSet<string> _flags` — story state (persisted to `PlayerPrefs`)
  - `HashSet<string> _playedOnce` — once-only event guard (persisted to `PlayerPrefs`)
  - `ClearPersistedState()` — wipes persistence for testing (accessible via ContextMenu)

### Presenters
Abstract base: **`CTWStoryPresenter`** with `Setup()`, `Show(slide)` (coroutine), `Hide()` (coroutine), `CanHandleAudio()`.

| Presenter | Description |
|---|---|
| `CTWBillboardPresenter` | World-space canvas that follows the player head using damped-exponential lerp |
| `CTWDioramaPresenter` | Spawns 3D prefab "scenes" with Scale/Alpha/Instant fade modes |
| `CTWFullscreenPresenter` | Screen-space overlay |
| `CTWVideoPresenter` | Video-based slide |
| `CTWEventOnlyPresenter` | Fires events without any visual |
| `CTWHybridPresenter` | Routes to sub-presenters via `[tag]` prefixes in subtitle text |

### Triggers
- **`CTWStoryTrigger`** — collider-based; calls `CTWStoryDirector.TryPlay()` on enter.

---

## Tutorial System (`CTW.Tutorial` namespace)

**`CTWTutorialManager`** drives three sequential coroutine phases, each completing when a spatial condition is met:

| Phase | Mechanic taught | Completion condition |
|---|---|---|
| GoToLight | Movement | Player enters spawned light radius |
| LookAtHands | Hand awareness | Player gazes at either hand for `handGazeRequiredTime` |
| PickupDoll | Grab interaction | Doll moves > 0.3 m from spawn position |

Each phase sets flags in `CTWStoryDirector` which gate the corresponding narrative whispers. Tutorial completion is persisted — `skipTutorialIfCompleted` skips the sequence on subsequent launches.

---

## Message Bus (`Utils.MessageBus` namespace)

Type-safe pub/sub with optional sticky-message replay:

```csharp
CTWMessageBus<MyMessage>.Send(new MyMessage { ... });
CTWMessageBus<MyMessage>.Subscribe(OnMessage, receiveLast: true);
```

Inspector-wirable via **`CTWGenericMessageListener`** / **`CTWGenericMessageSender`**, backed by a custom Editor that discovers all `ICTWMessage` types at edit time via reflection.

**`CTWMessageChannel`** (ScriptableObject) decouples sender and receiver at the asset level.

---

## Scene Management (`CTWSceneManager`)

Task-based async scene manager with `CancellationToken` support.

```csharp
await CTWSceneManager.Instance.LoadAsync("gameplay");
await CTWSceneManager.Instance.SwapToAsync("credits");    // keep listed keys, unload rest
await CTWSceneManager.Instance.NextAsync(wrap: true);
```

State per slot: `IsLoaded`, `IsLoading`, `IsUnloading`. `AsyncOperation.AsTask()` extension bridges Unity's coroutine world to `async/await`.

---

## Mixed Reality Environment (`Utils.Occlusion`, `Utils.Mesh`)

### Room Setup
1. **`CTWEnvironmentSpawner`** — MRUK anchor-driven prefab spawner. Scores candidates by weighted aspect-ratio + volume similarity; supports Wall / Door / Window / General anchor types.
2. **`CTWRoofGenerator`** — procedural hipped-roof mesh from `PlaneBoundary2D` polygon.
3. **`CTWFloorSpaceFinder`** — Monte Carlo sampling on the floor anchor; rejects positions overlapping furniture volumes via `Physics.OverlapBox`.
4. **`CTWMeshOverlay`** — applies the MRUK global mesh with planar UV generation.
5. **`MeshUtils`** — mesh serialization, room-scan JSON persistence, `RemoveVerticesInsideAnchors`, `RemoveFlatSurfaces`, **`DBSCANClustering`** for spatial anomaly detection.

### Occlusion
**`CTWOcclusionManager`** / **`CTWMultiOcclusionCameraManager`** — custom frustum culling using three proxy cameras (centre + left/right offset by half-IPD) to correctly handle stereo VR geometry.

---

## Animation

**`CTWAnimationBlender`** — uses Unity's **Playables API** directly (no AnimatorController). Builds a `PlayableGraph` → `AnimationMixerPlayable` → per-state `AnimationClipPlayable`s, normalising weights each frame.

**`CTWWitchAnimationController`** bridges the blender to a VAT (Vertex Animation Texture) pipeline via `openVAT_decoder` shader materials.

---

## Editor Tools

| Tool | Location | Purpose |
|---|---|---|
| `CTWCollidersOverlapChecker` | Tools › Check Collider Overlaps | AABB overlap finder with selection shortcut |
| `FindMetaMaterials` | Tools › Find Meta Shaders | Scans all materials for `Meta/` shader references |
| `XRReloadReset` | `[InitializeOnLoad]` | Reinitialises XR loader on Play Mode enter/exit to prevent Quest Link instability |
| `CTWGenericMessageListenerEditor` | Inspector | Reflection-driven type-picker for `ICTWMessage` subscriptions |

---

## Logging Convention

`CTWLog.Verbose(message)` — stripped from Release builds via `[System.Diagnostics.Conditional]`.

`Debug.LogWarning` / `Debug.LogError` — retained in all builds; reserved for genuine problems.
