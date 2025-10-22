# CTW Story System (lightweight, pluggable)

A tiny story/event runner for Unity 6 that shows images + subtitles in front of the player (or any custom presentation).

## Files

### Core System
- `CTWStoryEvent.cs` – ScriptableObject describing slides (image, subtitle, optional audio).
- `CTWStoryPresenter.cs` – Abstract base for any visual presentation.
- `CTWStoryDirector.cs` – Runner/queue/flags, uses a presenter prefab.
- `CTWStoryTrigger.cs` – Generic trigger to fire story events.

### Presenters (Built-in)
- `CTWBillboardPresenter.cs` – World-space billboard UI (default, good for VR dialog).
- `CTWEventOnlyPresenter.cs` – Invisible presenter that fires UnityEvents/MessageBus.
- `CTWDioramaPresenter.cs` – Spawns 3D miniature scenes in world space.
- `CTWFullscreenPresenter.cs` – Dramatic fullscreen overlay with typewriter effect.
- `CTWVideoPresenter.cs` – Video playback (fullscreen or billboard mode).
- `CTWHybridPresenter.cs` – Meta-presenter that routes to different presenters via tags.

All classes are under the `CTW.Story` namespace and prefixed with **CTW**.

📖 **See [PRESENTERS_GUIDE.md](./PRESENTERS_GUIDE.md) for detailed usage examples!**

## Quick Setup
1. **Import scripts** into `Assets/CTW/Story/`.
2. Create an empty `CTW_StoryDirector` in your scene and add **CTWStoryDirector**.
3. Create a prefab with **CTWBillboardPresenter** (or another presenter) and assign it to **CTWStoryDirector.presenterPrefab**.
   - Assign **playerHead** (your XR camera) for billboard tracking.
4. Create a **Story Event** via `Assets → Create → CTW → Story Event` and fill in slides.
5. Add **CTWStoryTrigger** to a trigger collider (set `isTrigger = true`) and assign the Story Event.
6. **Play**. Walk into the trigger (or call `CTWStoryDirector.Instance.TryPlay(event)` from code).

## Input
- If using the **Input System**, the director listens for **Space / Gamepad South / XR Trigger** by default when a slide has `waitForInput = true`.
- If using **Legacy Input**, Space is used (can be changed on the director).

## Swapping Presentation
Create your own presenter by inheriting from `CTWStoryPresenter` and implementing:
```csharp
public override void Setup(Transform head, Camera worldCam) { ... }
public override IEnumerator Show(CTWStoryEvent.Slide slide) { ... }
public override IEnumerator Hide(float fadeSeconds) { ... }
```
Assign the new presenter prefab to the **CTWStoryDirector**. No trigger/event changes needed.

## Flags & Once-Only
- Use `requireFlags`, `forbidFlags`, `setFlags` on events to chain logic.
- Use `eventId + onlyOnce` to ensure an event plays a single time.

## Notes
- Uses `UnityEngine.UI` (built-in) for simplicity; swap to TextMeshPro if you prefer.
- Presenter prefab can be anything (fullscreen canvas, projector/Decal, Timeline, etc.).
