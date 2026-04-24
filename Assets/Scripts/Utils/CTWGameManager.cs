using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Application-level state machine. Drives which objects are active and fires
/// lifecycle events as the game progresses through Initializing → Tutorial → Playing.
/// </summary>
public class CTWGameManager : MonoBehaviour
{
    public static CTWGameManager Instance { get; private set; }

    public enum GameState { Initializing, Tutorial, Playing, Paused }

    [Header("Initial State")]
    public GameState initialState = GameState.Initializing;

    [Header("State-Activated Objects")]
    [Tooltip("Enabled while in Tutorial state, disabled otherwise")]
    public List<GameObject> tutorialObjects = new();
    [Tooltip("Enabled while in Playing state, disabled otherwise")]
    public List<GameObject> gameplayObjects = new();

    [Header("Lifecycle Events")]
    public UnityEvent onTutorialStart;
    public UnityEvent onGameplayStart;
    public UnityEvent onGamePaused;
    public UnityEvent onGameResumed;

    /// <summary>Fired whenever state changes. Carries the new state.</summary>
    public event Action<GameState> OnStateChanged;

    public GameState CurrentState { get; private set; } = GameState.Initializing;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (initialState != GameState.Initializing)
            TransitionTo(initialState);
    }

    // ─── State Machine ──────────────────────────────────────────────────────────

    public void TransitionTo(GameState next)
    {
        if (CurrentState == next) return;
        CurrentState = next;
        ApplyState(next);
        OnStateChanged?.Invoke(next);
        CTWLog.Verbose($"[GameManager] → {next}");
    }

    private void ApplyState(GameState state)
    {
        SetGroupActive(tutorialObjects, state == GameState.Tutorial);
        SetGroupActive(gameplayObjects, state == GameState.Playing);

        switch (state)
        {
            case GameState.Tutorial: onTutorialStart.Invoke();  break;
            case GameState.Playing:  onGameplayStart.Invoke();  break;
            case GameState.Paused:   onGamePaused.Invoke();     break;
        }
    }

    // ─── Public API ─────────────────────────────────────────────────────────────

    [ContextMenu("Begin Tutorial")]
    public void BeginTutorial() => TransitionTo(GameState.Tutorial);

    [ContextMenu("Begin Gameplay")]
    public void BeginGameplay() => TransitionTo(GameState.Playing);

    [ContextMenu("Pause")]
    public void PauseGame()
    {
        if (CurrentState == GameState.Playing) TransitionTo(GameState.Paused);
    }

    [ContextMenu("Resume")]
    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;
        TransitionTo(GameState.Playing);
        onGameResumed.Invoke();
    }

    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static void SetGroupActive(List<GameObject> group, bool active)
    {
        foreach (var go in group) if (go) go.SetActive(active);
    }
}
