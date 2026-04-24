# Tutorial Story Events - Quick Setup Guide

Quick reference for creating the story events for your tutorial sequence.

---

## Story Events to Create

### 1. Tutorial_Intro_Whisper

```
Right-click in Project → Create → CTW → Story Event
Name: Tutorial_Intro_Whisper
```

**Settings:**
- **Event ID:** `tutorial_intro`
- **Only Once:** ✅ true
- **Require Flags:** `tutorial_started`
- **Forbid Flags:** `entered_light`
- **Set Flags:** (empty)

**Slides:**
- **Slide 1:**
  - Audio: [Your whisper audio clip: "Go into the light..."]
  - Subtitle: `"Go into the light..."`
  - Duration: `4` seconds
  - Wait for Input: ❌ false

---

### 2. Tutorial_Hands_Whisper

```
Right-click in Project → Create → CTW → Story Event
Name: Tutorial_Hands_Whisper
```

**Settings:**
- **Event ID:** `tutorial_hands`
- **Only Once:** ✅ true
- **Require Flags:** `phase_2_started`
- **Forbid Flags:** `learned_hands`
- **Set Flags:** (empty)

**Slides:**
- **Slide 1:**
  - Audio: [Whisper: "Look at your hands..."]
  - Subtitle: `"Look at your hands..."`
  - Duration: `3` seconds
  - Wait for Input: ❌ false

---

### 3. Tutorial_Pickup_Whisper

```
Right-click in Project → Create → CTW → Story Event
Name: Tutorial_Pickup_Whisper
```

**Settings:**
- **Event ID:** `tutorial_pickup`
- **Only Once:** ✅ true
- **Require Flags:** `doll_spawned`
- **Forbid Flags:** `learned_pickup`
- **Set Flags:** (empty)

**Slides:**
- **Slide 1:**
  - Audio: [Whisper: "Pick up the doll..."]
  - Subtitle: `"Pick up the doll..."`
  - Duration: `3` seconds
  - Wait for Input: ❌ false

---

### 4. Tutorial_Complete

```
Right-click in Project → Create → CTW → Story Event
Name: Tutorial_Complete
```

**Settings:**
- **Event ID:** `tutorial_complete_celebration`
- **Only Once:** ✅ true
- **Require Flags:** `tutorial_complete`
- **Forbid Flags:** `main_game_started`
- **Set Flags:** (empty)

**Slides:**
- **Slide 1:**
  - Audio: [Whisper: "You learn quickly... The cathedral awaits..."]
  - Subtitle: `"You learn quickly..."`
  - Duration: `3` seconds
  
- **Slide 2:**
  - Audio: (optional continuation)
  - Subtitle: `"The cathedral awaits..."`
  - Duration: `2` seconds

---

## Presenter Setup

Create a presenter for the whispers:

### Option A: Event-Only (No Visuals)

```
GameObject → Create Empty → "WhisperPresenter"
Add Component → CTWEventOnlyPresenter
```

**Inspector:**
- Log Events: ✅ true (for debugging)
- Send To Message Bus: ❌ false (optional)

**Pros:** Simple, no UI clutter
**Cons:** No subtitles shown

---

### Option B: Billboard (With Subtitles)

```
GameObject → Create Empty → "WhisperBillboard"
Add Component → CTWBillboardPresenter
```

**Inspector:**
- Distance: `1.2` (closer for whispers)
- Height Offset: `0` (eye level)
- Always On Top: ✅ true
- Sorting Order: `100`

**Customize appearance:**
- Small, subtle
- Dark background
- White text
- Maybe eerie glow effect

**Pros:** Shows subtitles, accessible
**Cons:** Visual element in VR

---

### Option C: Spatial Audio Only

Just use audio from Tutorial Manager:

```csharp
// In TutorialManager
AudioSource audioSource;

void PlayWhisper(AudioClip clip)
{
    audioSource.clip = clip;
    audioSource.spatialBlend = 0.5f; // Slightly spatial
    audioSource.Play();
}
```

Then story events are just markers, audio plays separately.

**Pros:** Full control over audio positioning
**Cons:** Audio not in story system

---

## Wiring it All Together

### In Story Director

```
Scene → Find "StoryDirector" (or create new empty GameObject)
Add Component → CTWStoryDirector
```

**Inspector:**
- **Presenter Prefab:** Drag your presenter (WhisperPresenter or WhisperBillboard)
- **Player Head:** OVRCameraRig → CenterEyeAnchor

---

### In Tutorial Manager

```
Scene → Find "TutorialManager" (or create new empty GameObject)
Add Component → CTWTutorialManager
```

**Inspector:**
- **Floor Space Finder:** Drag your FloorSpaceFinder object
- **Player Head:** OVRCameraRig → CenterEyeAnchor
- **Left Hand:** OVRCameraRig → LeftHandAnchor
- **Right Hand:** OVRCameraRig → RightHandAnchor
- **Light Prefab:** Your glowing light prefab
- **Doll Prefab:** Your grabbable doll prefab
- **Intro Whisper:** Tutorial_Intro_Whisper (asset)
- **Hands Whisper:** Tutorial_Hands_Whisper (asset)
- **Pickup Whisper:** Tutorial_Pickup_Whisper (asset)
- **Tutorial Complete Event:** Tutorial_Complete (asset)

---

## Testing

### Test Individual Events

```csharp
// In Unity Console or Inspector
CTWStoryDirector.Instance.SetFlag("tutorial_started");
CTWStoryDirector.Instance.TryPlay(introWhisper);
```

### Skip Tutorial (for testing main game)

```
TutorialManager → Right-click → Skip Tutorial
```

### Reset Tutorial

```
TutorialManager → Right-click → Reset Tutorial
```

---

## Audio Tips

### Whisper Audio Creation:

**Processing:**
1. Record voice normally
2. Lower pitch slightly (makes it creepier)
3. Add reverb (cathedral echo)
4. Add subtle distortion/grain
5. Lower volume (whisper level)
6. Add spatial audio component in Unity

**Unity Audio Source Settings:**
- Spatial Blend: `0.5` (semi-spatial)
- Doppler Level: `0`
- Volume Rolloff: `Linear`
- Min Distance: `0.5`
- Max Distance: `5`
- Priority: `128` (normal)

### Example Processing in Audacity:

1. Effects → Change Pitch (-10%)
2. Effects → Reverb (Cathedral preset)
3. Effects → Normalize
4. Effects → Noise Gate (remove silence)
5. Export as .wav or .ogg

---

## Flag Flow Diagram

```
Tutorial Starts
    ↓
[tutorial_started] ← Set by TutorialManager
    ↓
Story plays "Intro Whisper"
    ↓
Player moves to light
    ↓
[entered_light] ← Set by TutorialManager
[learned_movement] ← Set by TutorialManager
    ↓
[phase_2_started] ← Set by TutorialManager
    ↓
Story plays "Hands Whisper"
    ↓
Player looks at hands
    ↓
[learned_hands] ← Set by TutorialManager
    ↓
[doll_spawned] ← Set by TutorialManager
    ↓
Story plays "Pickup Whisper"
    ↓
Player grabs doll
    ↓
[learned_pickup] ← Set by TutorialManager
[tutorial_complete] ← Set by TutorialManager
    ↓
Story plays "Tutorial Complete"
    ↓
[main_game_started] ← Set by TutorialManager
    ↓
Main Game Begins!
```

---

## Troubleshooting

### ❌ Whisper doesn't play

**Check:**
1. Is flag set correctly?
   ```csharp
   Debug.Log(CTWStoryDirector.Instance.HasFlag("tutorial_started"));
   ```
2. Is story event assigned in TutorialManager?
3. Is presenter assigned in StoryDirector?
4. Check console for `[StoryDirector]` logs

---

### ❌ Tutorial Manager doesn't detect player

**Check:**
1. Is `playerHead` assigned?
2. Is OVRCameraRig in scene?
3. Check console for `[Tutorial]` logs
4. Use Gizmos to visualize light radius

---

### ❌ Audio cuts off too early

**Fix:**
- Increase `duration` in slide
- Or set `waitForInput = true` and advance manually

---

## Next Steps

After tutorial works:

1. **Polish audio** - Add music, ambient sounds
2. **Add visual effects** - Light glow, particle effects, doll shimmer
3. **Expand story** - More whispers, environmental storytelling
4. **Build quest system** - Use same pattern for main game objectives
5. **Create save system** - Remember tutorial completion

The foundation is now in place for your entire game's narrative structure! 🎮✨


