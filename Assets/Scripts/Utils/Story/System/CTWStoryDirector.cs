using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace CTW.Story
{
    /// <summary>
    /// Lightweight story runner with a queue, flags, and swappable presenter.
    /// Flags and once-only playback are persisted across sessions via PlayerPrefs.
    /// </summary>
    public class CTWStoryDirector : MonoBehaviour
    {
        public static CTWStoryDirector Instance { get; private set; }

        [Header("Presenter")]
        public CTWStoryPresenter presenterPrefab;
        public Transform playerHead;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        [Header("Input System")]
        public InputActionReference advanceAction;
        private InputAction _advance;
#else
        public KeyCode legacyAdvanceKey = KeyCode.Space;
#endif

        private readonly Queue<CTWStoryEvent> _queue = new();
        private readonly HashSet<string> _flags = new();
        private readonly HashSet<string> _playedOnce = new();

        private CTWStoryPresenter _presenter;
        private bool _isPlaying;
        private AudioSource _audioSource;

        // ─── Persistence ────────────────────────────────────────────────────────

        private const string FlagsKey      = "CTW_StoryFlags";
        private const string PlayedOnceKey = "CTW_StoryPlayedOnce";

        [System.Serializable]
        private class StringList { public List<string> items = new(); }

        private void LoadPersistedState()
        {
            if (PlayerPrefs.HasKey(FlagsKey))
            {
                var list = JsonUtility.FromJson<StringList>(PlayerPrefs.GetString(FlagsKey));
                if (list?.items != null) foreach (var f in list.items) _flags.Add(f);
            }
            if (PlayerPrefs.HasKey(PlayedOnceKey))
            {
                var list = JsonUtility.FromJson<StringList>(PlayerPrefs.GetString(PlayedOnceKey));
                if (list?.items != null) foreach (var f in list.items) _playedOnce.Add(f);
            }
        }

        private void SavePersistedState()
        {
            PlayerPrefs.SetString(FlagsKey,      JsonUtility.ToJson(new StringList { items = new List<string>(_flags) }));
            PlayerPrefs.SetString(PlayedOnceKey, JsonUtility.ToJson(new StringList { items = new List<string>(_playedOnce) }));
            PlayerPrefs.Save();
        }

        /// <summary>Wipe all persisted story state. Call from a debug menu to replay from scratch.</summary>
        [ContextMenu("Clear Persisted State")]
        public void ClearPersistedState()
        {
            PlayerPrefs.DeleteKey(FlagsKey);
            PlayerPrefs.DeleteKey(PlayedOnceKey);
            _flags.Clear();
            _playedOnce.Clear();
        }

        // ─── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPersistedState();
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            _advance = advanceAction ? advanceAction.action : new InputAction("CTW_StoryAdvance", InputActionType.Button);
            if (!advanceAction)
            {
                _advance.AddBinding("<Keyboard>/space");
                _advance.AddBinding("<Gamepad>/south");
                _advance.AddBinding("<XRController>{RightHand}/trigger");
            }
            _advance.Enable();
#endif
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (_advance != null)
            {
                _advance.Disable();
                if (!advanceAction) _advance.Dispose();
                _advance = null;
            }
#endif
        }

        // ─── Public API ─────────────────────────────────────────────────────────

        /// <summary>Swap presenters at runtime if desired.</summary>
        public void UsePresenter(CTWStoryPresenter prefab) => presenterPrefab = prefab;

        /// <summary>Try to enqueue a story event if it passes flags/once-only.</summary>
        public bool TryPlay(CTWStoryEvent ev)
        {
            if (!ev) return false;
            if (ev.onlyOnce && !string.IsNullOrEmpty(ev.eventId) && _playedOnce.Contains(ev.eventId)) return false;
            if (!PassesFlags(ev)) return false;

            _queue.Enqueue(ev);
            if (!_isPlaying) StartCoroutine(RunQueue());
            return true;
        }

        // Flags API for your story graph
        public bool HasFlag(string f) => _flags.Contains(f);

        public void SetFlag(string f)
        {
            if (string.IsNullOrEmpty(f)) return;
            _flags.Add(f);
            SavePersistedState();
        }

        public void ClearFlag(string f)
        {
            if (string.IsNullOrEmpty(f)) return;
            _flags.Remove(f);
            SavePersistedState();
        }

        // ─── Internal ───────────────────────────────────────────────────────────

        private bool PassesFlags(CTWStoryEvent ev)
        {
            if (ev.requireFlags != null)
                foreach (var f in ev.requireFlags)
                    if (!string.IsNullOrEmpty(f) && !_flags.Contains(f)) return false;

            if (ev.forbidFlags != null)
                foreach (var f in ev.forbidFlags)
                    if (!string.IsNullOrEmpty(f) && _flags.Contains(f)) return false;

            return true;
        }

        private IEnumerator RunQueue()
        {
            _isPlaying = true;

            if (!_presenter)
            {
                _presenter = Instantiate(presenterPrefab);
                var head = playerHead ? playerHead : (Camera.main ? Camera.main.transform : null);
                _presenter.Setup(head, Camera.main);
            }

            while (_queue.Count > 0)
            {
                var ev = _queue.Dequeue();

                if (ev.onlyOnce && !string.IsNullOrEmpty(ev.eventId))
                {
                    _playedOnce.Add(ev.eventId);
                    SavePersistedState();
                }

                foreach (var s in ev.slides)
                {
                    bool presenterHandledAudio = _presenter.CanHandleAudio();

                    if (s.audio && !presenterHandledAudio)
                    {
                        if (_audioSource == null)
                        {
                            _audioSource = GetComponent<AudioSource>();
                            if (_audioSource == null)
                            {
                                _audioSource = gameObject.AddComponent<AudioSource>();
                                _audioSource.spatialBlend = 0f;
                                _audioSource.playOnAwake = false;
                                _audioSource.volume = 1f;
                            }
                        }
                        _audioSource.clip = s.audio;
                        _audioSource.Play();
                        CTWLog.Verbose($"[StoryDirector] Playing fallback 2D audio: {s.audio.name}");
                    }

                    yield return _presenter.Show(s);

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                    if (s.waitForInput)
                        while (_advance == null || !_advance.WasPerformedThisFrame()) yield return null;
                    else
                        yield return new WaitForSeconds(Mathf.Max(0f, s.duration));
#else
                    if (s.waitForInput)
                        while (!Input.GetKeyDown(legacyAdvanceKey)) yield return null;
                    else
                        yield return new WaitForSeconds(Mathf.Max(0f, s.duration));
#endif

                    yield return _presenter.Hide(s.fade);
                }

                if (ev.setFlags != null)
                {
                    foreach (var f in ev.setFlags)
                        if (!string.IsNullOrEmpty(f)) _flags.Add(f);
                    SavePersistedState();
                }
            }

            _isPlaying = false;
        }
    }
}
