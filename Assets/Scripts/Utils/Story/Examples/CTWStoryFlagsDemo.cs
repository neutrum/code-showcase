using UnityEngine;
using CTW.Story;

/// <summary>
/// Practical demonstration of the Story Flags system.
/// Shows how to use flags for branching narratives.
/// </summary>
public class CTWStoryFlagsDemo : MonoBehaviour
{
    [Header("Story Events - Linear Progression")]
    public CTWStoryEvent tutorialIntro;
    public CTWStoryEvent tutorialCombat;
    public CTWStoryEvent tutorialComplete;

    [Header("Story Events - Branching Path")]
    public CTWStoryEvent npcAsksForHelp;
    public CTWStoryEvent npcThanks;
    public CTWStoryEvent npcAngry;
    public CTWStoryEvent bossAllyAppears;

    [Header("Story Events - Collectibles")]
    public CTWStoryEvent artifact1Found;
    public CTWStoryEvent artifact2Found;
    public CTWStoryEvent artifact3Found;
    public CTWStoryEvent secretEnding;

    [Header("Testing")]
    public KeyCode testTutorialKey = KeyCode.T;
    public KeyCode testBranchingKey = KeyCode.B;
    public KeyCode testCollectiblesKey = KeyCode.C;

    void Update()
    {
        // Test linear progression
        if (Input.GetKeyDown(testTutorialKey))
        {
            TestLinearProgression();
        }

        // Test branching paths
        if (Input.GetKeyDown(testBranchingKey))
        {
            TestBranchingPath();
        }

        // Test collectibles
        if (Input.GetKeyDown(testCollectiblesKey))
        {
            TestCollectibles();
        }
    }

    // ============================================================
    // EXAMPLE 1: Linear Story Progression
    // ============================================================
    
    /// <summary>
    /// Configure your Story Events like this:
    /// 
    /// tutorialIntro:
    ///   setFlags: ["tutorial_started"]
    ///   
    /// tutorialCombat:
    ///   requireFlags: ["tutorial_started"]
    ///   setFlags: ["learned_combat"]
    ///   
    /// tutorialComplete:
    ///   requireFlags: ["tutorial_started", "learned_combat"]
    ///   setFlags: ["tutorial_complete"]
    /// </summary>
    void TestLinearProgression()
    {
        Debug.Log("=== Testing Linear Progression ===");

        // Try to play tutorial intro
        bool played = CTWStoryDirector.Instance.TryPlay(tutorialIntro);
        Debug.Log($"Tutorial Intro played: {played}");
        // ✅ Will play (no requirements)
        // Sets flag: "tutorial_started"

        // Try to play combat tutorial
        played = CTWStoryDirector.Instance.TryPlay(tutorialCombat);
        Debug.Log($"Tutorial Combat played: {played}");
        // ✅ Will play (has "tutorial_started")
        // Sets flag: "learned_combat"

        // Try to play completion
        played = CTWStoryDirector.Instance.TryPlay(tutorialComplete);
        Debug.Log($"Tutorial Complete played: {played}");
        // ✅ Will play (has both flags)
        // Sets flag: "tutorial_complete"
    }

    // ============================================================
    // EXAMPLE 2: Branching Paths Based on Player Choice
    // ============================================================

    /// <summary>
    /// Configure your Story Events:
    /// 
    /// npcAsksForHelp:
    ///   forbidFlags: ["npc_helped", "npc_ignored"]
    ///   
    /// npcThanks:
    ///   requireFlags: ["npc_helped"]
    ///   setFlags: ["npc_is_ally"]
    ///   
    /// npcAngry:
    ///   requireFlags: ["npc_ignored"]
    ///   setFlags: ["npc_is_enemy"]
    ///   
    /// bossAllyAppears:
    ///   requireFlags: ["npc_is_ally"]
    /// </summary>
    void TestBranchingPath()
    {
        Debug.Log("=== Testing Branching Path ===");

        // Show NPC asking for help
        CTWStoryDirector.Instance.TryPlay(npcAsksForHelp);

        // Simulate player choice (you'd do this based on actual input)
        SimulatePlayerChoice();
    }

    void SimulatePlayerChoice()
    {
        // Random choice for demo
        bool playerHelps = Random.value > 0.5f;

        if (playerHelps)
        {
            Debug.Log("Player chose to HELP");
            CTWStoryDirector.Instance.SetFlag("npc_helped");
            
            // This will now play (has "npc_helped" flag)
            CTWStoryDirector.Instance.TryPlay(npcThanks);
            // Sets "npc_is_ally"
        }
        else
        {
            Debug.Log("Player chose to IGNORE");
            CTWStoryDirector.Instance.SetFlag("npc_ignored");
            
            // This will now play (has "npc_ignored" flag)
            CTWStoryDirector.Instance.TryPlay(npcAngry);
            // Sets "npc_is_enemy"
        }

        // Later in the game, during boss fight:
        CheckForAlly();
    }

    void CheckForAlly()
    {
        // Only plays if player helped NPC earlier
        if (CTWStoryDirector.Instance.HasFlag("npc_is_ally"))
        {
            Debug.Log("NPC appears as ally!");
            CTWStoryDirector.Instance.TryPlay(bossAllyAppears);
        }
        else
        {
            Debug.Log("No ally - you're on your own!");
        }
    }

    // ============================================================
    // EXAMPLE 3: Collectibles System
    // ============================================================

    /// <summary>
    /// Configure your Story Events:
    /// 
    /// artifact1Found:
    ///   setFlags: ["artifact_1"]
    ///   
    /// artifact2Found:
    ///   setFlags: ["artifact_2"]
    ///   
    /// artifact3Found:
    ///   setFlags: ["artifact_3"]
    ///   
    /// secretEnding:
    ///   requireFlags: ["artifact_1", "artifact_2", "artifact_3"]
    ///   setFlags: ["true_ending_unlocked"]
    /// </summary>
    void TestCollectibles()
    {
        Debug.Log("=== Testing Collectibles ===");

        // Collect artifact 1
        CollectArtifact(1);
        
        // Collect artifact 2
        CollectArtifact(2);
        
        // Check secret ending (should fail - missing artifact 3)
        CheckSecretEnding();
        
        // Collect artifact 3
        CollectArtifact(3);
        
        // Check secret ending again (should succeed!)
        CheckSecretEnding();
    }

    void CollectArtifact(int artifactNumber)
    {
        Debug.Log($"Collecting artifact {artifactNumber}...");

        CTWStoryEvent artifactEvent = artifactNumber switch
        {
            1 => artifact1Found,
            2 => artifact2Found,
            3 => artifact3Found,
            _ => null
        };

        if (artifactEvent != null)
        {
            CTWStoryDirector.Instance.TryPlay(artifactEvent);
            // This sets the corresponding "artifact_X" flag
        }
    }

    void CheckSecretEnding()
    {
        // Check if all artifacts collected
        bool hasAll = CTWStoryDirector.Instance.HasFlag("artifact_1") &&
                      CTWStoryDirector.Instance.HasFlag("artifact_2") &&
                      CTWStoryDirector.Instance.HasFlag("artifact_3");

        Debug.Log($"Has all artifacts: {hasAll}");

        if (hasAll)
        {
            Debug.Log("🎉 Secret ending unlocked!");
            CTWStoryDirector.Instance.TryPlay(secretEnding);
        }
        else
        {
            Debug.Log("❌ Still missing artifacts");
        }
    }

    // ============================================================
    // EXAMPLE 4: Practical Integration - Trigger Zones
    // ============================================================

    /// <summary>
    /// Example: Use this on a trigger collider to play story when player enters
    /// </summary>
    public CTWStoryEvent triggerEvent;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && triggerEvent != null)
        {
            // TryPlay automatically checks flags
            bool played = CTWStoryDirector.Instance.TryPlay(triggerEvent);
            
            if (!played)
            {
                Debug.Log($"Story event '{triggerEvent.eventId}' blocked by flags");
            }
        }
    }

    // ============================================================
    // EXAMPLE 5: Manual Flag Management
    // ============================================================

    [ContextMenu("Test Manual Flags")]
    void TestManualFlags()
    {
        Debug.Log("=== Testing Manual Flag Management ===");

        // Set a flag
        CTWStoryDirector.Instance.SetFlag("player_has_key");
        Debug.Log("Set flag: player_has_key");

        // Check if flag exists
        bool hasKey = CTWStoryDirector.Instance.HasFlag("player_has_key");
        Debug.Log($"Has key: {hasKey}"); // true

        // Clear a flag
        CTWStoryDirector.Instance.ClearFlag("player_has_key");
        Debug.Log("Cleared flag: player_has_key");

        // Check again
        hasKey = CTWStoryDirector.Instance.HasFlag("player_has_key");
        Debug.Log($"Has key: {hasKey}"); // false
    }

    // ============================================================
    // EXAMPLE 6: Reset System
    // ============================================================

    [ContextMenu("Reset All Tutorial Flags")]
    public void ResetTutorial()
    {
        Debug.Log("Resetting tutorial flags...");
        
        CTWStoryDirector.Instance.ClearFlag("tutorial_started");
        CTWStoryDirector.Instance.ClearFlag("learned_combat");
        CTWStoryDirector.Instance.ClearFlag("tutorial_complete");
        
        Debug.Log("Tutorial reset! You can replay it now.");
    }

    [ContextMenu("Reset All Collectible Flags")]
    public void ResetCollectibles()
    {
        Debug.Log("Resetting collectible flags...");
        
        CTWStoryDirector.Instance.ClearFlag("artifact_1");
        CTWStoryDirector.Instance.ClearFlag("artifact_2");
        CTWStoryDirector.Instance.ClearFlag("artifact_3");
        CTWStoryDirector.Instance.ClearFlag("true_ending_unlocked");
        
        Debug.Log("Collectibles reset!");
    }

    // ============================================================
    // EXAMPLE 7: Complex Conditions
    // ============================================================

    public CTWStoryEvent specialEvent;

    void CheckComplexConditions()
    {
        // Check multiple conditions manually
        bool canPlaySpecialEvent = 
            CTWStoryDirector.Instance.HasFlag("witch_defeated") &&
            CTWStoryDirector.Instance.HasFlag("all_artifacts_collected") &&
            !CTWStoryDirector.Instance.HasFlag("already_seen_special");

        if (canPlaySpecialEvent)
        {
            CTWStoryDirector.Instance.TryPlay(specialEvent);
        }
    }

    // ============================================================
    // Helper: Debug Current Flags
    // ============================================================

    [ContextMenu("Debug - Show Help")]
    void ShowHelp()
    {
        Debug.Log(@"
=== Story Flags System Demo ===

KEYBOARD SHORTCUTS:
  T - Test Linear Tutorial Progression
  B - Test Branching Path (NPC choice)
  C - Test Collectibles System

RIGHT-CLICK MENU:
  Reset All Tutorial Flags
  Reset All Collectible Flags
  Test Manual Flags

SETUP:
1. Create Story Event assets (Right-click → Create → CTW → Story Event)
2. Configure their flags in inspector:
   - requireFlags: flags needed to play
   - forbidFlags: flags that prevent playing
   - setFlags: flags set after playing
3. Assign events to this script
4. Press keys to test!

See CTWStoryFlagsExample.md for detailed examples.
        ");
    }
}


