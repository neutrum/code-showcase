as# Tutorial & Story Architecture Guide

This document explains the architectural approach for building the Cathedral Witch tutorial and story progression system.

---

## 🎯 Core Philosophy: Separation of Concerns

```
MECHANICS (Tutorial Manager)
    ↕️ (Flags & Events)
NARRATIVE (Story Director)
```

**Tutorial Manager** = "WHAT happens" (gameplay, progression, mechanics)
**Story Director** = "HOW it's presented" (audio, visuals, atmosphere)

---

## 📐 Architecture Overview

```
┌─────────────────────────────────────────────────┐
│           CTWTutorialManager                    │
│  ┌───────────────────────────────────────────┐  │
│  │ Phase 1: Go To Light                      │  │
│  │ Phase 2: Look At Hands                    │  │
│  │ Phase 3: Pick Up Doll                     │  │
│  └───────────────────────────────────────────┘  │
│                                                 │
│  • Detects player actions                      │
│  • Spawns tutorial objects                     │
│  • Manages progression                         │
│  • Sets flags when objectives complete         │
└──────────────┬──────────────────────────────────┘
               │
               │ Sets Flags
               │ ("tutorial_started",
               │  "learned_movement", etc.)
               ↓
┌─────────────────────────────────────────────────┐
│           CTWStoryDirector                      │
│  ┌───────────────────────────────────────────┐  │
│  │ Story Event: "Intro Whisper"              │  │
│  │   requireFlags: ["tutorial_started"]      │  │
│  │   Plays: "Go into the light..."           │  │
│  └───────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────┐  │
│  │ Story Event: "Hands Whisper"              │  │
│  │   requireFlags: ["phase_2_started"]       │  │
│  │   Plays: "Look at your hands..."          │  │
│  └───────────────────────────────────────────┘  │
│                                                 │
│  • Plays whispers & narration                  │
│  • Shows visual story moments                  │
│  • Responds to flag changes                    │
└─────────────────────────────────────────────────┘
               ↑
               │ (Optional)
               │ Sends Messages
               ↓
┌─────────────────────────────────────────────────┐
│           CTWMessageBus                         │
│                                                 │
│  • Decouples systems                           │
│  • Event-driven communication                  │
│  • Example: TutorialPhaseChanged               │
└─────────────────────────────────────────────────┘
```

---

## ✅ Why This Architecture?

### Advantages:

1. **Separation of Concerns**
   - Tutorial logic ≠ Story presentation
   - Can change whispers without touching code
   - Can change tutorial flow without touching story events

2. **Flexibility**
   - Swap audio easily (different languages, voices)
   - Skip tutorial but keep story
   - Test mechanics without story
   - Test story without mechanics

3. **Reusability**
   - StoryDirector used for tutorial AND main game
   - Tutorial patterns reusable for quests
   - Flag system works everywhere

4. **Designer-Friendly**
   - Story events = ScriptableObjects (easy to edit)
   - No code changes for story tweaks
   - Clear progression visibility

---

## 🎮 Example Flow: "Go Into The Light"

### Step-by-Step:

**1. Tutorial Manager Starts**
```csharp
StartTutorial()
{
    // Find space, spawn light
    lightPosition = floorSpaceFinder.FindEmptyFloorSpace(...);
    SpawnLight(lightPosition);

    // Tell story system: tutorial started
    CTWStoryDirector.Instance.SetFlag("tutorial_started");

    // Play whisper
    CTWStoryDirector.Instance.TryPlay(introWhisper);

    // Start watching for player
    StartCoroutine(WatchForPlayerEnterLight());
}
```

**2. Story Director Responds**
```
Story Event: "Tutorial_Intro_Whisper"
  requireFlags: ["tutorial_started"]
  forbidFlags: ["entered_light"]

  Slides:
    - Audio: "Go into the light..." (spooky whisper)
    - Duration: 4 seconds
    - Presenter: EventOnly (invisible)
```

**3. Player Moves**
```
(Player walks around)
```

**4. Tutorial Manager Detects**
```csharp
WatchForPlayerEnterLight()
{
    if (distance < lightRadius)
    {
        // Player succeeded!
        CTWStoryDirector.Instance.SetFlag("entered_light");
        CTWStoryDirector.Instance.SetFlag("learned_movement");

        StartPhase2();
    }
}
```

**5. Story Director Updates**
```
Story Event: "Tutorial_Light_Success"
  requireFlags: ["entered_light"]
  onlyOnce: true

  Slides:
    - Audio: "Good... now look at your hands..."
```

---

## 🔧 How to Set This Up

### 1. Create Tutorial Manager

```
GameObject → Create Empty → "TutorialManager"
Add Component → CTWTutorialManager
```

### 2. Create Story Events

```
Assets → Create → CTW → Story Event
```

**Event: "Tutorial_Intro_Whisper"**
- eventId: `"tutorial_intro"`
- requireFlags: `["tutorial_started"]`
- forbidFlags: `["entered_light"]`
- Slides:
  - Audio: Your whisper audio clip
  - subtitle: `"Go into the light..."`
  - duration: 4

**Event: "Tutorial_Hands_Whisper"**
- eventId: `"tutorial_hands"`
- requireFlags: `["phase_2_started"]`
- forbidFlags: `["learned_hands"]`
- Slides:
  - Audio: Hands whisper clip
  - subtitle: `"Look at your hands..."`

**Event: "Tutorial_Pickup_Whisper"**
- eventId: `"tutorial_pickup"`
- requireFlags: `["doll_spawned"]`
- forbidFlags: `["learned_pickup"]`
- Slides:
  - Audio: Pickup whisper clip
  - subtitle: `"Pick up the doll..."`

### 3. Wire Everything Together

In **TutorialManager Inspector**:
- Assign Floor Space Finder
- Assign Player Head (OVRCameraRig → CenterEyeAnchor)
- Assign Hands (if tracked)
- Assign Light Prefab
- Assign Doll Prefab
- Assign Story Events (the ones you created)

### 4. Create Presenter for Whispers

You probably want EventOnly or invisible billboard for whispers:

**Option A: EventOnly Presenter** (no visuals, just audio)
```
Create GameObject → "WhisperPresenter"
Add Component → CTWEventOnlyPresenter
```

**Option B: Billboard with Audio** (if you want subtitle text)
```
Create GameObject → "WhisperBillboard"
Add Component → CTWBillboardPresenter
Configure: Small, subtle, shows subtitle text
```

Assign presenter to StoryDirector.presenterPrefab

---

## 🎨 Visual Scripting / Editor Idea (Future)

You mentioned a storyline editor - **great idea!** Here's what it could look like:

```
┌───────────────────────────────────────────────────┐
│  Cathedral Witch - Story Editor                  │
├───────────────────────────────────────────────────┤
│                                                   │
│  [Tutorial Sequence]                              │
│                                                   │
│  ┌─────┐      ┌──────────┐      ┌─────────┐     │
│  │START│─────▶│Go to Light│─────▶│Look Hands│    │
│  └─────┘      └──────────┘      └─────────┘     │
│                    │                   │          │
│                    │                   │          │
│              ┌─────▼────┐        ┌────▼─────┐    │
│              │Whisper:  │        │Whisper:  │    │
│              │"Go into  │        │"Look at  │    │
│              │the light"│        │hands"    │    │
│              └──────────┘        └──────────┘    │
│                                                   │
│  [Properties]                                     │
│  ┌────────────────────────────────────────────┐  │
│  │ Phase: Go To Light                         │  │
│  │ Objective: Enter light radius (1.5m)       │  │
│  │ On Complete: Set flag "learned_movement"   │  │
│  │ Whisper Audio: [select clip]               │  │
│  │ Light Prefab: [select prefab]              │  │
│  └────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────┘
```

This would be similar to:
- **Yarn Spinner** (dialog editor)
- **Articy Draft** (narrative design tool)
- **Unity Timeline** (but for story/tutorial flow)

### Features it could have:
- ✅ Visual node-based editing
- ✅ Drag-drop story events
- ✅ Flag condition visualization
- ✅ Branching paths
- ✅ Preview/test flow
- ✅ Export to ScriptableObjects

**Implementation approach:**
- Unity Editor Window
- Custom node graph (similar to Shader Graph)
- SerializedObject for data
- Runtime interpreter

---

## 🎓 Best Practices

### DO ✅

**1. Use Flags for State**
```csharp
// Good
CTWStoryDirector.Instance.SetFlag("learned_movement");

// Bad
public bool hasLearnedMovement = true; // Hard to track
```

**2. Keep Tutorial Manager Focused**
```csharp
// Good - just mechanics
void OnPlayerEnterLight()
{
    SetFlag("entered_light");
    NextPhase();
}

// Bad - mixing presentation
void OnPlayerEnterLight()
{
    SetFlag("entered_light");
    PlayWhisper("good job");  // Let StoryDirector handle this!
    ShowUI("Movement learned");
}
```

**3. Make Story Events Reusable**
```
Story Event: "Generic_Whisper_Positive"
  Slides:
    - Audio: "Good..."
    - Can trigger from anywhere!
```

### DON'T ❌

**1. Don't Put Story Logic in Tutorial Manager**
```csharp
// Bad
void OnPickupDoll()
{
    AudioSource.PlayOneShot(whisperClip);  // Use StoryDirector!
    ShowSubtitle("Well done!");             // Use StoryDirector!
}
```

**2. Don't Put Tutorial Logic in Story Events**
```
// Bad
Story Event with:
  - Spawn light
  - Track player position
  - Check if player entered

// Story events should be PASSIVE (respond to flags)
```

**3. Don't Tightly Couple Systems**
```csharp
// Bad
TutorialManager.Instance.storyDirector.PlayEvent(...)

// Good
CTWStoryDirector.Instance.SetFlag("event_happened");
// StoryDirector responds automatically
```

---

## 📊 Flag Naming Convention

Use clear, descriptive flag names:

```csharp
// Tutorial progression
"tutorial_started"
"learned_movement"
"learned_hands"
"learned_pickup"
"tutorial_complete"

// Tutorial phases
"phase_1_active"
"phase_2_started"
"doll_spawned"

// Game state
"main_game_started"
"first_enemy_defeated"
"witch_encountered"

// Player abilities
"has_spell_fireball"
"has_spell_shield"
"has_artifact_crystal"
```

---

## 🔄 Extending to Main Game

The same pattern works for quests, objectives, game progression:

### Quest Example:

**Quest Manager:**
```csharp
void OnEnemyDefeated()
{
    enemiesDefeated++;

    if (enemiesDefeated >= 5)
    {
        CTWStoryDirector.Instance.SetFlag("quest_kill_5_enemies_complete");
    }
}
```

**Story Director:**
```
Story Event: "Quest_Complete_Witch_Appears"
  requireFlags: ["quest_kill_5_enemies_complete"]
  forbidFlags: ["witch_defeated"]

  Slides:
    - "[fullscreen] The Witch Emerges"
    - "[diorama] Witch appears in cathedral
    - "[billboard] Defeat her to escape!"
    - "[event] SpawnWitchBoss"
```

Same architecture scales to entire game! 🎮

---

## 🚀 Summary

**Use:**
- ✅ **Tutorial Manager** for mechanics (movement, pickup, gaze tracking)
- ✅ **Story Director** for narrative (whispers, visuals, atmosphere)
- ✅ **Flags** for communication (decouple systems)
- ✅ **Message Bus** for events (optional, advanced)

**This gives you:**
- Clean separation
- Easy iteration
- Designer-friendly
- Scalable to full game
- Foundation for visual story editor (future)

**Next Steps:**
1. Create TutorialManager GameObject
2. Create 3-4 story events (whispers)
3. Test tutorial flow
4. Iterate on timing/audio
5. Later: Build visual editor if needed

Your tutorial sequence is a **perfect fit** for this hybrid approach! 🏰🧙‍♀️✨


