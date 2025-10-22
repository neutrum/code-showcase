# Story Flags System - Complete Guide with Examples

The flag system allows you to create **branching narratives** and **conditional story events** based on player actions and game state.

---

## How Flags Work

### Three Flag Types on Each Story Event:

1. **requireFlags** - Event will ONLY play if ALL these flags are set
2. **forbidFlags** - Event will NOT play if ANY of these flags are set
3. **setFlags** - These flags are SET when the event finishes playing

### Director Flag Management:

The `CTWStoryDirector` singleton maintains a set of flags that persist during the game session.

```csharp
// Check if flag exists
bool hasKey = CTWStoryDirector.Instance.HasFlag("has_key");

// Set a flag
CTWStoryDirector.Instance.SetFlag("door_unlocked");

// Clear a flag
CTWStoryDirector.Instance.ClearFlag("enemies_alive");
```

---

## Example 1: Linear Story Progression

**Scenario:** Tutorial sequence that plays in order

### Story Event 1: "Tutorial_Intro"
```
eventId: "tutorial_intro"
onlyOnce: true
requireFlags: (empty)
forbidFlags: (empty)
setFlags: ["tutorial_started"]

Slides:
- "Welcome to the Cathedral..."
- "Use your hands to interact"
```

### Story Event 2: "Tutorial_Combat"
```
eventId: "tutorial_combat"
onlyOnce: true
requireFlags: ["tutorial_started"]    ← Must have completed intro
forbidFlags: ["tutorial_complete"]
setFlags: ["learned_combat"]

Slides:
- "Now let's learn combat..."
```

### Story Event 3: "Tutorial_Complete"
```
eventId: "tutorial_done"
onlyOnce: true
requireFlags: ["tutorial_started", "learned_combat"]  ← Needs both
forbidFlags: (empty)
setFlags: ["tutorial_complete"]

Slides:
- "Tutorial complete! Good luck!"
```

**Result:** Events play in strict order. Can't skip ahead.

---

## Example 2: Branching Paths

**Scenario:** Player can choose to help or ignore an NPC

### Story Event: "NPC_Asks_For_Help"
```
eventId: "npc_plea"
requireFlags: (empty)
forbidFlags: ["npc_helped", "npc_ignored"]  ← Only if haven't chosen yet
setFlags: (empty)  ← Player makes choice via gameplay

Slides:
- "[billboard] Please, help me escape!"
- "[billboard] Press A to help, B to ignore"
```

### Story Event: "NPC_Helped_Thanks"
```
eventId: "npc_thanks"
requireFlags: ["npc_helped"]     ← Only if player helped
forbidFlags: (empty)
setFlags: ["npc_is_ally"]

Slides:
- "Thank you! I'll help you later."
```

### Story Event: "NPC_Ignored_Angry"
```
eventId: "npc_angry"
requireFlags: ["npc_ignored"]    ← Only if player ignored
forbidFlags: (empty)
setFlags: ["npc_is_enemy"]

Slides:
- "You'll regret abandoning me!"
```

### Story Event: "Boss_Fight_Ally_Appears"
```
eventId: "boss_ally"
requireFlags: ["npc_is_ally"]    ← Only if you helped NPC earlier
forbidFlags: (empty)
setFlags: (empty)

Slides:
- "[event] SpawnNPCAlly"
- "[billboard] I've got your back!"
```

**Code to set player choice:**
```csharp
// When player presses A
if (Input.GetKeyDown(KeyCode.A))
{
    CTWStoryDirector.Instance.SetFlag("npc_helped");
}

// When player presses B
if (Input.GetKeyDown(KeyCode.B))
{
    CTWStoryDirector.Instance.SetFlag("npc_ignored");
}
```

---

## Example 3: Collectibles / Discovery

**Scenario:** Secret ending unlocked by finding 3 artifacts

### Story Event: "Artifact_1_Found"
```
eventId: "artifact1"
onlyOnce: true
requireFlags: (empty)
forbidFlags: (empty)
setFlags: ["artifact_1"]

Slides:
- "[diorama] The Crystal of Light"
- "1 of 3 artifacts collected"
```

### Story Event: "Artifact_2_Found"
```
eventId: "artifact2"
onlyOnce: true
requireFlags: (empty)
forbidFlags: (empty)
setFlags: ["artifact_2"]

Slides:
- "[diorama] The Tome of Shadows"
- "2 of 3 artifacts collected"
```

### Story Event: "Artifact_3_Found"
```
eventId: "artifact3"
onlyOnce: true
requireFlags: (empty)
forbidFlags: (empty)
setFlags: ["artifact_3"]

Slides:
- "[diorama] The Ring of Souls"
- "3 of 3 artifacts collected!"
```

### Story Event: "Secret_Ending"
```
eventId: "secret_ending"
onlyOnce: true
requireFlags: ["artifact_1", "artifact_2", "artifact_3"]  ← All three!
forbidFlags: (empty)
setFlags: ["true_ending_unlocked"]

Slides:
- "[fullscreen] Secret Ending Unlocked"
- "[video] Assets/Videos/TrueEnding.mp4"
```

**Trigger code:**
```csharp
void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player"))
    {
        // Play artifact found event
        CTWStoryDirector.Instance.TryPlay(artifact1Event);
        
        // Check if all artifacts collected
        if (CTWStoryDirector.Instance.HasFlag("artifact_1") &&
            CTWStoryDirector.Instance.HasFlag("artifact_2") &&
            CTWStoryDirector.Instance.HasFlag("artifact_3"))
        {
            CTWStoryDirector.Instance.TryPlay(secretEndingEvent);
        }
    }
}
```

---

## Example 4: State-Based Events

**Scenario:** Different dialog based on game state

### Story Event: "Witch_First_Meeting"
```
eventId: "witch_intro"
requireFlags: (empty)
forbidFlags: ["met_witch"]
setFlags: ["met_witch"]

Slides:
- "Who dares enter my domain?"
```

### Story Event: "Witch_Friendly"
```
eventId: "witch_friend"
requireFlags: ["met_witch", "gave_gift"]
forbidFlags: ["witch_angry"]
setFlags: (empty)

Slides:
- "Ah, my friend returns!"
```

### Story Event: "Witch_Hostile"
```
eventId: "witch_hostile"
requireFlags: ["met_witch"]
forbidFlags: (empty)
requireFlags: ["witch_angry"]  ← Set by combat system
setFlags: (empty)

Slides:
- "You DARE attack me?!"
- "[event] StartBossFight"
```

---

## Example 5: Complex Conditions (AND/OR Logic)

### AND Logic (Require ALL flags)
```
requireFlags: ["has_key", "door_unlocked", "witch_defeated"]
```
= Player must have key AND door unlocked AND witch defeated

### OR Logic (Use multiple events)
```
Event A:
  requireFlags: ["path_left_chosen"]
  
Event B:
  requireFlags: ["path_right_chosen"]
```
= Different events for different choices

### NOT Logic (Forbid flags)
```
forbidFlags: ["player_dead", "game_over"]
```
= Won't play if player is dead OR game is over

---

## Example 6: Reset/Replay Mechanics

**Scenario:** Player can replay tutorial

```csharp
public void ResetTutorial()
{
    // Clear all tutorial flags
    CTWStoryDirector.Instance.ClearFlag("tutorial_started");
    CTWStoryDirector.Instance.ClearFlag("learned_combat");
    CTWStoryDirector.Instance.ClearFlag("tutorial_complete");
    
    // Play tutorial again
    CTWStoryDirector.Instance.TryPlay(tutorialIntroEvent);
}
```

---

## Example 7: Time-Limited Events

**Scenario:** Warning appears if player takes too long

```csharp
public class TimedStoryEvent : MonoBehaviour
{
    public CTWStoryEvent warningEvent;
    public float warningTime = 60f;
    
    void Start()
    {
        Invoke(nameof(ShowWarning), warningTime);
    }
    
    void ShowWarning()
    {
        // Only show if player hasn't completed objective
        if (!CTWStoryDirector.Instance.HasFlag("objective_complete"))
        {
            CTWStoryDirector.Instance.TryPlay(warningEvent);
        }
    }
}
```

**Warning Event:**
```
eventId: "time_warning"
requireFlags: (empty)
forbidFlags: ["objective_complete"]
setFlags: ["was_warned"]

Slides:
- "Hurry! Time is running out!"
```

---

## Example 8: Achievement System

**Integration with your MessageBus:**

```csharp
public class StoryAchievements : MonoBehaviour
{
    void Start()
    {
        // Subscribe to story flag changes
        CTWMessageBus<StoryFlagSetMessage>.Subscribe(OnFlagSet);
    }
    
    void OnFlagSet(StoryFlagSetMessage msg)
    {
        switch (msg.flagName)
        {
            case "all_artifacts_collected":
                UnlockAchievement("Collector");
                break;
                
            case "pacifist_run":
                UnlockAchievement("Peaceful Soul");
                break;
                
            case "speedrun_complete":
                UnlockAchievement("Speed Runner");
                break;
        }
    }
}
```

---

## Best Practices

### ✅ DO:
- Use descriptive flag names: `"witch_defeated"` not `"flag1"`
- Set flags after events finish (in `setFlags`)
- Check flags before showing conditional content
- Clear flags for replay systems
- Use `onlyOnce: true` for one-time story beats

### ❌ DON'T:
- Create circular dependencies (Event A requires B, B requires A)
- Forget to set flags after important events
- Use too many flags (keep it simple)
- Hardcode flag names (use constants)

---

## Flag Management Tips

### Use Constants
```csharp
public static class StoryFlags
{
    public const string TUTORIAL_COMPLETE = "tutorial_complete";
    public const string WITCH_DEFEATED = "witch_defeated";
    public const string HAS_KEY = "has_key";
    public const string ARTIFACT_1 = "artifact_1";
}

// Usage
CTWStoryDirector.Instance.SetFlag(StoryFlags.TUTORIAL_COMPLETE);
```

### Debug Helper
```csharp
public class StoryDebugger : MonoBehaviour
{
    [ContextMenu("Print All Flags")]
    void PrintFlags()
    {
        // You'd need to expose the flags HashSet in director
        // Or add a debug method there
        Debug.Log("Current story flags:");
        // foreach flag, print it
    }
    
    [ContextMenu("Clear All Flags")]
    void ClearAllFlags()
    {
        CTWStoryDirector.Instance.ClearFlag("tutorial_complete");
        CTWStoryDirector.Instance.ClearFlag("witch_defeated");
        // etc...
    }
}
```

---

## Complete Example: Cathedral Witch Game

### Story Flow
```
1. Player enters cathedral
   → "cathedral_entered" flag set
   
2. Player meets witch
   requireFlags: ["cathedral_entered"]
   forbidFlags: ["met_witch"]
   setFlags: ["met_witch"]
   
3. Player chooses path:
   A) Give gift → setFlags: ["gave_gift", "witch_friendly"]
   B) Attack → setFlags: ["attacked_witch", "witch_hostile"]
   
4A. Friendly path:
   requireFlags: ["witch_friendly"]
   → Witch gives quest
   → setFlags: ["quest_active"]
   
4B. Hostile path:
   requireFlags: ["witch_hostile"]
   → Boss fight
   → After win: setFlags: ["witch_defeated"]
   
5. Ending:
   requireFlags: ["witch_defeated"] OR ["quest_complete"]
   setFlags: ["game_complete"]
```

This creates a complete branching narrative! 🎮✨

---

**Remember:** Flags persist during the game session but are cleared when you restart. If you need permanent persistence, you'd integrate with a save system.

