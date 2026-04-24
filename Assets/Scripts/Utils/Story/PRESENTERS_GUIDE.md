# CTW Story Presenters Guide

Complete guide to using the flexible presenter system in your CTW Story framework.

---

## Overview

The story system now supports **5 different presenters** that can be mixed and matched:

1. **CTWBillboardPresenter** - World-space billboard UI (original)
2. **CTWEventOnlyPresenter** - Invisible, fires UnityEvents/MessageBus (NEW)
3. **CTWDioramaPresenter** - 3D miniature scenes (NEW)
4. **CTWFullscreenPresenter** - Dramatic fullscreen overlays (NEW)
5. **CTWVideoPresenter** - Video playback (NEW)
6. **CTWHybridPresenter** - Mix multiple presenters in one sequence (NEW)

---

## 1. Billboard Presenter (Original)

**Best for:** Standard VR storytelling, floating UI panels

### Setup
```
1. Create empty GameObject with CTWBillboardPresenter
2. Assign to CTWStoryDirector.presenterPrefab
3. Assign playerHead (XR camera)
4. Done! UI auto-generates
```

### Features
- Follows player head at customizable distance
- Image + subtitle display
- Smooth billboard rotation
- Auto-fading

---

## 2. Event-Only Presenter

**Best for:** Triggering gameplay without showing UI

### Setup
```
1. Create empty GameObject with CTWEventOnlyPresenter
2. Add Event Mappings in inspector:
   - Event Key: "SpawnEnemy" (matches slide.subtitle)
   - OnShow: Wire up UnityEvent to spawn function
3. Assign to CTWStoryDirector.presenterPrefab
```

### Usage Example

**Story Event Setup:**
- Slide 1: subtitle = "SpawnEnemy"
- Slide 2: subtitle = "UnlockDoor"
- Slide 3: subtitle = "PlaySound"

**In Inspector:**
```
Event Mappings:
  [0] eventKey: "SpawnEnemy"
      onShow: EnemyManager.SpawnWave()
  [1] eventKey: "UnlockDoor"
      onShow: DoorController.Unlock()
  [2] eventKey: "PlaySound"
      onShow: AudioManager.PlayDramaticSound()
```

### MessageBus Integration
```csharp
// Enable in inspector: sendToMessageBus = true
// Then customize the Show() method to send your message types:

public override IEnumerator Show(CTWStoryEvent.Slide slide)
{
    if (slide.subtitle == "WitchAppears")
    {
        CTWMessageBus<CTWWitchStartAnimationMessage>.Send(
            new CTWWitchStartAnimationMessage()
        );
    }
    yield return null;
}
```

---

## 3. Diorama Presenter

**Best for:** Miniature 3D scenes, tactical displays, world previews

### Setup
```
1. Create prefabs for each diorama scene
2. Create empty GameObject with CTWDioramaPresenter
3. Configure scenes:
   - Slide Index: 0
   - Prefab: YourDioramaPrefab
   - Local Position/Rotation/Scale
   - Optional: Particle systems, animators
4. Assign to CTWStoryDirector.presenterPrefab
```

### Example Diorama Flow
**Story Event with 3 slides:**

**Slide 0:** "The Cathedral Approach"
- Diorama Scene 0: Cathedral exterior miniature
- Particles: Fog effects
- Animator: Camera pan

**Slide 1:** "The Inner Sanctum"
- Diorama Scene 1: Interior room miniature
- Particles: Candle flames
- Spawn Sound: Ambient choir

**Slide 2:** "The Witch Awakens"
- Diorama Scene 2: Witch character model
- Animator: Wake up animation
- Particles: Magic swirls

### Settings
- **Follow Player:** Keep diorama in front of player
- **Spawn Distance:** How far from player (default 2.5m)
- **Fade Mode:** Scale (grow/shrink) or Alpha (material fade)

---

## 4. Fullscreen Presenter

**Best for:** Dramatic cutscenes, visual novel style, title cards

### Setup
```
1. Create empty GameObject with CTWFullscreenPresenter
2. Configure visual style:
   - Background Color/Alpha
   - Image Area (normalized rect)
   - Text Area (normalized rect)
3. Enable effects:
   - Typewriter Effect: true
   - Typewriter Speed: 30 chars/sec
4. Assign to CTWStoryDirector.presenterPrefab
```

### Features
- **Screen Space Overlay** - Always visible, covers entire screen
- **Typewriter Effect** - Animated text reveal
- **Custom Layout** - Position image and text independently
- **Fade Modes:**
  - `WithText` - Fade image and text together
  - `ImageFirst` - Show image, then text
  - `TextFirst` - Show text, then image

### Perfect For
- Chapter titles
- Dramatic reveals
- Character introductions
- End credits

---

## 5. Video Presenter

**Best for:** Cutscenes, trailers, recorded gameplay

### Setup
```
1. Create empty GameObject with CTWVideoPresenter
2. Choose display mode:
   - Fullscreen (screen overlay)
   - Billboard (world space)
   - Projection (requires target surface)
3. Configure render texture size
4. Assign to CTWStoryDirector.presenterPrefab
```

### Usage
**In your Story Event Slide:**
- **subtitle:** Path to video file
  - Example: `"Assets/Videos/Intro.mp4"`
  - Example: `"file:///C:/Videos/Cutscene.mp4"`

### Display Modes
- **Fullscreen:** Screen space overlay, like a traditional cutscene
- **Billboard:** Floating screen in VR space (like diorama but flat)
- **Projection:** Project onto a wall/surface (future enhancement)

### Settings
- **Wait for Video End:** Auto-advance when video finishes
- **Loop Video:** Repeat playback
- **Follow Player:** (Billboard mode) Track player movement

---

## 6. Hybrid Presenter (Mix & Match!)

**Best for:** Complex story sequences with varied presentation styles

### Setup
```
1. Create presenter prefabs for each type you want
2. Create empty GameObject with CTWHybridPresenter
3. Add Presenter Mappings:
   - Tag: "billboard"    → Billboard prefab
   - Tag: "fullscreen"   → Fullscreen prefab
   - Tag: "diorama"      → Diorama prefab
   - Tag: "event"        → EventOnly prefab
4. Set Default Presenter (fallback)
5. Assign to CTWStoryDirector.presenterPrefab
```

### Usage - Tag-Based Routing
**In your Story Event Slides, use tags in subtitle:**

**Slide 0:**
- subtitle: `"[fullscreen] Chapter 1: The Cathedral"`
- → Uses fullscreen presenter

**Slide 1:**
- subtitle: `"[diorama] The Cathedral appears in the distance"`
- → Uses diorama presenter to show miniature

**Slide 2:**
- subtitle: `"[billboard] Your journey begins..."`
- → Uses billboard presenter for normal dialog

**Slide 3:**
- subtitle: `"[event] SpawnEnemies"`
- → Uses event-only presenter, triggers enemy spawn

### Tag Format
- Default: `[tagname]` at start of subtitle
- Tags are case-insensitive
- Tag is stripped from display if `stripTagFromSubtitle = true`

### Example Sequence
```
Story Event: "Opening Sequence"
  Slide 0: "[fullscreen] Cathedral: The Witch"
           → Dramatic title card
  
  Slide 1: "[video] Assets/Videos/FlyThrough.mp4"
           → Cinematic fly-through
  
  Slide 2: "[diorama] A miniature cathedral materializes"
           → 3D preview of the level
  
  Slide 3: "[billboard] The witch is trapped inside..."
           → Floating story text
  
  Slide 4: "[event] BeginGameplay"
           → Triggers game start, no visual
```

---

## Advanced: Extending the Slide Class

If you need presenter-specific data, extend `CTWStoryEvent.Slide`:

```csharp
[System.Serializable]
public class Slide
{
    // Existing
    public Sprite image;
    [TextArea] public string subtitle;
    public AudioClip audio;
    public float duration = 3f;
    public float fade = 0.25f;
    public bool waitForInput;
    
    // NEW: Diorama-specific
    public GameObject dioramaPrefab;
    public Vector3 dioramaOffset;
    
    // NEW: Event-specific
    public UnityEvent customEvent;
    
    // NEW: Video-specific
    public VideoClip videoClip;
    
    // NEW: Generic data (JSON)
    [TextArea] public string customData;
}
```

---

## Integration Examples

### Example 1: Tutorial Sequence
```
Mix billboard (instructions) + event (spawn objects)

Slide 0: "[billboard] Pick up the wand"
Slide 1: "[event] SpawnWand"
Slide 2: "[billboard] Cast your first spell!"
Slide 3: "[event] EnableMagicSystem"
```

### Example 2: Boss Introduction
```
Dramatic buildup with multiple presenters

Slide 0: "[fullscreen] The Ancient One Awakens..."
         duration: 2s, fade: 0.5s
         
Slide 1: "[video] Assets/Videos/BossAwakening.mp4"
         waitForInput: false
         
Slide 2: "[diorama] (Boss miniature appears)"
         slideIndex: 0, prefab: BossDiorama
         
Slide 3: "[billboard] Defeat her to escape the cathedral"
         waitForInput: true
```

### Example 3: Environmental Storytelling
```
Use event-only for ambient narrative

Slide 0: "[event] PlayThunder"
Slide 1: "[event] FlickerLights"
Slide 2: "[billboard] Something approaches..."
Slide 3: "[event] SpawnGhost"
```

---

## Tips & Best Practices

### Performance
- **Diorama:** Use low-poly prefabs, limit particles
- **Video:** Use compressed formats (H.264), reasonable resolution
- **Fullscreen:** Use TextMeshPro for better text rendering

### VR Comfort
- **Billboard:** Keep distance 1.5-2.5m, avoid rapid movement
- **Fullscreen:** Use sparingly, can be disorienting in VR
- **Diorama:** Great for VR, feels natural in 3D space

### Story Flow
- Use **flags** for branching (requireFlags, forbidFlags)
- Use **eventId + onlyOnce** for one-time events
- Chain events with **setFlags** at end of sequences

### Debugging
- Enable `logEvents = true` on EventOnlyPresenter
- Use `CTWStoryTrigger.triggerOnStart = true` for testing
- Test individual presenters before combining in Hybrid

---

## Quick Reference

| Presenter | Use Case | Setup Complexity | VR Friendly |
|-----------|----------|------------------|-------------|
| Billboard | General dialog | Easy | ✅ Yes |
| EventOnly | Gameplay triggers | Medium | ✅ Yes |
| Diorama | 3D previews | Medium | ✅ Perfect |
| Fullscreen | Dramatic moments | Easy | ⚠️ Use sparingly |
| Video | Cutscenes | Medium | ⚠️ Test comfort |
| Hybrid | Complex sequences | Advanced | ✅ Yes |

---

## Common Patterns

### Pattern: Tutorial
```
Hybrid → billboard (instructions) + event (triggers)
```

### Pattern: Cinematic
```
Hybrid → fullscreen (title) + video (cutscene) + billboard (epilogue)
```

### Pattern: Exploration Reward
```
Trigger → diorama (discovery) + event (unlock items)
```

### Pattern: Boss Fight
```
EventOnly → spawn boss + enable combat + play music
```

---

## Troubleshooting

**Presenter not showing?**
- Check CTWStoryDirector has presenter assigned
- Verify playerHead is assigned (for billboard/diorama)
- Check Canvas sorting order (for fullscreen)

**Tags not working in Hybrid?**
- Verify tag spelling matches exactly
- Check tagPrefix/tagSuffix settings
- Enable stripTagFromSubtitle if you don't want to see `[tag]`

**Events not firing?**
- Check EventMapping eventKey matches slide.subtitle exactly
- Verify UnityEvents are wired up in inspector
- Enable logEvents = true for debugging

**Video not playing?**
- Check video file path is correct (use forward slashes)
- Verify video codec is supported (H.264 recommended)
- Check VideoPlayer.isPrepared before playing

---

## Next Steps

1. **Try the examples** in this guide
2. **Create presenter prefabs** for your project
3. **Test individual presenters** before combining
4. **Use Hybrid presenter** for complex sequences
5. **Extend Slide class** if you need custom data

**Have fun storytelling! 🎭✨**


