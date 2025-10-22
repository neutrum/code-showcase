using UnityEngine;
using CTW.Story;

/// <summary>
/// Example usage patterns for the CTW Story System.
/// This script shows how to trigger story events from code.
/// </summary>
public class CTWStoryExamples : MonoBehaviour
{
    [Header("Story Events")]
    public CTWStoryEvent introEvent;
    public CTWStoryEvent tutorialEvent;
    public CTWStoryEvent bossIntroEvent;

    [Header("Testing")]
    public bool playIntroOnStart = false;
    public KeyCode testKey = KeyCode.T;

    private void Start()
    {
        if (playIntroOnStart && introEvent)
        {
            PlayStory(introEvent);
        }
    }

    private void Update()
    {
        // Test trigger
        if (Input.GetKeyDown(testKey) && tutorialEvent)
        {
            PlayStory(tutorialEvent);
        }
    }

    /// <summary>
    /// Play a story event through the director
    /// </summary>
    public void PlayStory(CTWStoryEvent storyEvent)
    {
        if (CTWStoryDirector.Instance != null)
        {
            bool played = CTWStoryDirector.Instance.TryPlay(storyEvent);
            if (played)
            {
                Debug.Log($"[StoryExamples] Playing story: {storyEvent.eventId}");
            }
            else
            {
                Debug.LogWarning($"[StoryExamples] Story blocked by flags or already played: {storyEvent.eventId}");
            }
        }
        else
        {
            Debug.LogError("[StoryExamples] CTWStoryDirector not found in scene!");
        }
    }

    /// <summary>
    /// Example: Play story on trigger enter (alternative to CTWStoryTrigger)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && introEvent)
        {
            PlayStory(introEvent);
        }
    }

    /// <summary>
    /// Example: Conditional story based on game state
    /// </summary>
    public void CheckAndPlayTutorial()
    {
        // Check if player has flag
        if (CTWStoryDirector.Instance != null)
        {
            if (!CTWStoryDirector.Instance.HasFlag("tutorial_completed"))
            {
                PlayStory(tutorialEvent);
            }
            else
            {
                Debug.Log("[StoryExamples] Tutorial already completed, skipping");
            }
        }
    }

    /// <summary>
    /// Example: Set a flag manually (for branching logic)
    /// </summary>
    public void MarkTutorialComplete()
    {
        CTWStoryDirector.Instance?.SetFlag("tutorial_completed");
        Debug.Log("[StoryExamples] Set flag: tutorial_completed");
    }

    /// <summary>
    /// Example: Clear a flag (reset state)
    /// </summary>
    public void ResetTutorialFlag()
    {
        CTWStoryDirector.Instance?.ClearFlag("tutorial_completed");
        Debug.Log("[StoryExamples] Cleared flag: tutorial_completed");
    }

    /// <summary>
    /// Example: Integration with event system
    /// Wire this up to EventOnlyPresenter UnityEvent
    /// </summary>
    public void OnStoryEventTriggered(string eventName)
    {
        Debug.Log($"[StoryExamples] Story event received: {eventName}");

        switch (eventName)
        {
            case "SpawnEnemy":
                // Spawn enemy logic here
                Debug.Log("Spawning enemy...");
                break;

            case "UnlockDoor":
                // Unlock door logic here
                Debug.Log("Unlocking door...");
                break;

            case "StartBossFight":
                // Start boss fight
                if (bossIntroEvent)
                {
                    PlayStory(bossIntroEvent);
                }
                break;

            default:
                Debug.LogWarning($"Unknown story event: {eventName}");
                break;
        }
    }
}

