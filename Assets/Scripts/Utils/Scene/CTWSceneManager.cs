#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Drop this on a bootstrap GameObject in your startup scene.
/// Define your scenes in the inspector and load/unload them by key.
///
/// • Additive loading by default
/// • Optional set-active on load
/// • Simple events for load/unload
/// • "UnloadAllExcept" helper for quick swaps
/// • Next/Previous helpers callable from *any* script (via singleton Instance)
/// • No Addressables, no dependencies
/// </summary>
[DefaultExecutionOrder(-5000)]
public class CTWSceneManager : MonoBehaviour
{
    public static CTWSceneManager? Instance { get; private set; }

    [SerializeField]
    private List<SceneSlot> scenes = new List<SceneSlot>();

    [Tooltip("If set, this key will be loaded & set active on Start (optional).")]
    [SerializeField] private string initialActiveKey = string.Empty;

    private readonly Dictionary<string, SceneSlot> _byKey = new();
    private readonly Dictionary<string, int> _indexByKey = new();
    private int _activeIndex = -1;

    public event Action<string>? SceneLoaded;    // key
    public event Action<string>? SceneUnloaded;  // key
    public event Action<string>? ActiveSceneChanged; // key

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[LightweightSceneManager] Duplicate instance destroyed.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _byKey.Clear();
        _indexByKey.Clear();
        for (int i = 0; i < scenes.Count; i++)
        {
            var s = scenes[i];
            if (string.IsNullOrWhiteSpace(s.Key)) continue;
            if (_byKey.ContainsKey(s.Key))
            {
                Debug.LogWarning($"[LightweightSceneManager] Duplicate key '{s.Key}' ignored.");
                continue;
            }
            _byKey.Add(s.Key, s);
            _indexByKey[s.Key] = i;
        }

        SceneManager.activeSceneChanged += OnUnityActiveSceneChanged;
    }

    void Start()
    {
        if (!string.IsNullOrEmpty(initialActiveKey) && _byKey.ContainsKey(initialActiveKey))
        {
            // Fire and forget
            _ = LoadAsync(initialActiveKey, setActive: true);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.activeSceneChanged -= OnUnityActiveSceneChanged;
    }

    private void OnUnityActiveSceneChanged(Scene prev, Scene next)
    {
        if (!next.IsValid()) return;
        var key = _byKey.FirstOrDefault(kv => kv.Value.SceneName == next.name).Key;
        if (!string.IsNullOrEmpty(key) && _indexByKey.TryGetValue(key, out var idx))
        {
            _activeIndex = idx;
            ActiveSceneChanged?.Invoke(key);
        }
    }

    #region Public API

    /// <summary>
    /// Load a scene by key (additive). Returns true if newly loaded or already loaded.
    /// </summary>
    public async Task<bool> LoadAsync(string key, bool setActive = false, CancellationToken ct = default)
    {
        if (!_byKey.TryGetValue(key, out var slot))
        {
            Debug.LogError($"[LightweightSceneManager] Unknown key '{key}'.");
            return false;
        }

        if (slot.IsLoaded)
        {
            if (setActive) TrySetActive(slot.SceneName);
            if (_indexByKey.TryGetValue(key, out var idx) && setActive) _activeIndex = idx;
            return true;
        }

        if (slot.IsLoading)
        {
            // Wait for ongoing load
            await slot.WaitUntilNotLoadingAsync(ct);
            if (setActive) TrySetActive(slot.SceneName);
            if (_indexByKey.TryGetValue(key, out var idx2) && setActive) _activeIndex = idx2;
            return slot.IsLoaded;
        }

        slot.IsLoading = true;
        try
        {
            var op = SceneManager.LoadSceneAsync(slot.SceneName, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError($"[LightweightSceneManager] Failed to start loading '{slot.SceneName}'.");
                slot.IsLoading = false;
                return false;
            }
            op.allowSceneActivation = true;
            await op.AsTask(ct);

            slot.IsLoaded = true;
            if (setActive)
            {
                TrySetActive(slot.SceneName);
                if (_indexByKey.TryGetValue(key, out var idx3)) _activeIndex = idx3;
            }
            SceneLoaded?.Invoke(key);
            return true;
        }
        finally
        {
            slot.IsLoading = false;
        }
    }

    /// <summary>
    /// Unload a scene by key. Returns true if it was loaded and is now unloaded (or already not loaded).
    /// </summary>
    public async Task<bool> UnloadAsync(string key, CancellationToken ct = default)
    {
        if (!_byKey.TryGetValue(key, out var slot))
        {
            Debug.LogError($"[LightweightSceneManager] Unknown key '{key}'.");
            return false;
        }

        if (!slot.IsLoaded)
            return true; // already unloaded

        if (slot.IsUnloading)
        {
            await slot.WaitUntilNotUnloadingAsync(ct);
            return !slot.IsLoaded;
        }

        slot.IsUnloading = true;
        try
        {
            var op = SceneManager.UnloadSceneAsync(slot.SceneName);
            if (op == null)
            {
                Debug.LogError($"[LightweightSceneManager] Failed to start unloading '{slot.SceneName}'.");
                slot.IsUnloading = false;
                return false;
            }
            await op.AsTask(ct);

            slot.IsLoaded = false;
            SceneUnloaded?.Invoke(key);
            return true;
        }
        finally
        {
            slot.IsUnloading = false;
        }
    }

    /// <summary>
    /// Loads target keys and unloads everything else.
    /// </summary>
    public async Task SwapToAsync(IEnumerable<string> keepKeys, bool setActiveFirstKeep = true, CancellationToken ct = default)
    {
        var keep = new HashSet<string>(keepKeys ?? Enumerable.Empty<string>());
        // Load missing
        bool firstSetActiveDone = false;
        foreach (var k in keep)
        {
            if (_byKey.TryGetValue(k, out var slot))
            {
                await LoadAsync(k, setActive: setActiveFirstKeep && !firstSetActiveDone, ct: ct);
                if (setActiveFirstKeep && !firstSetActiveDone)
                {
                    firstSetActiveDone = true;
                    if (_indexByKey.TryGetValue(k, out var idx)) _activeIndex = idx;
                }
            }
            else Debug.LogWarning($"[LightweightSceneManager] SwapTo: unknown key '{k}'.");
        }
        // Unload others
        foreach (var kv in _byKey)
        {
            if (!keep.Contains(kv.Key))
                await UnloadAsync(kv.Key, ct);
        }
    }

    /// <summary>
    /// Convenience overload: keep a single key.
    /// </summary>
    public Task SwapToAsync(string keepKey, bool setActive = true, CancellationToken ct = default)
        => SwapToAsync(new[] { keepKey }, setActive, ct);

    /// <summary>
    /// Returns true if the scene with key is currently loaded.
    /// </summary>
    public bool IsLoaded(string key) => _byKey.TryGetValue(key, out var s) && s.IsLoaded;

    /// <summary>
    /// Returns the currently active key (or empty if unknown).
    /// </summary>
    public string CurrentActiveKey => (_activeIndex >= 0 && _activeIndex < scenes.Count) ? scenes[_activeIndex].Key : string.Empty;

    /// <summary>
    /// Advance to the next scene in the inspector order. Wraps if wrap==true.
    /// Loads the next and unloads everything else. Returns the next key or empty if none.
    /// </summary>
    public async Task<string> NextAsync(bool wrap = true, CancellationToken ct = default)
    {
        if (scenes.Count == 0) return string.Empty;
        int nextIdx = (_activeIndex >= 0) ? _activeIndex + 1 : 0;
        if (nextIdx >= scenes.Count)
        {
            if (!wrap) return string.Empty;
            nextIdx = 0;
        }
        var key = scenes[nextIdx].Key;
        await SwapToAsync(key, setActive: true, ct: ct);
        _activeIndex = nextIdx;
        return key;
    }

    /// <summary>
    /// Go to previous scene in the inspector order. Wraps if wrap==true.
    /// </summary>
    public async Task<string> PreviousAsync(bool wrap = true, CancellationToken ct = default)
    {
        if (scenes.Count == 0) return string.Empty;
        int prevIdx = (_activeIndex >= 0) ? _activeIndex - 1 : scenes.Count - 1;
        if (prevIdx < 0)
        {
            if (!wrap) return string.Empty;
            prevIdx = scenes.Count - 1;
        }
        var key = scenes[prevIdx].Key;
        await SwapToAsync(key, setActive: true, ct: ct);
        _activeIndex = prevIdx;
        return key;
    }

    /// <summary>
    /// Non-async convenience wrappers for UI Buttons / UnityEvents.
    /// </summary>
    public void Next() => _ = NextAsync();
    public void Previous() => _ = PreviousAsync();

    /// <summary>
    /// Try to set a scene as the active scene by name.
    /// </summary>
    public static bool TrySetActive(string sceneName)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded) return false;
        return SceneManager.SetActiveScene(scene);
    }

    #endregion

    [Serializable]
    public class SceneSlot
    {
        [Tooltip("Unique key you will use to load/unload, e.g. 'Main', 'UI', 'Level1'.")]
        public string Key = "";

        [Tooltip("Scene name as in Build Settings.")]
        public string SceneName = "";

#if UNITY_EDITOR
        [SerializeField, Tooltip("Assign a SceneAsset for convenience in the editor. Will auto-fill SceneName.")]
        private UnityEditor.SceneAsset? sceneAsset;
        void OnValidate()
        {
            if (sceneAsset != null)
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(sceneAsset);
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrEmpty(name)) SceneName = name;
            }
        }
#endif

        [NonSerialized] public bool IsLoaded;
        [NonSerialized] public bool IsLoading;
        [NonSerialized] public bool IsUnloading;

        internal async Task WaitUntilNotLoadingAsync(CancellationToken ct)
        {
            while (IsLoading)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }
        internal async Task WaitUntilNotUnloadingAsync(CancellationToken ct)
        {
            while (IsUnloading)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }
    }
}

public static class AsyncOperationExtensions
{
    /// <summary>
    /// Await an AsyncOperation. Cancels by throwing if ct is cancelled.
    /// </summary>
    public static Task AsTask(this AsyncOperation op, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<bool>();
        void Completed(AsyncOperation _) => tcs.TrySetResult(true);
        op.completed += Completed;
        if (ct.CanBeCanceled)
        {
            ct.Register(() => tcs.TrySetCanceled(ct));
        }
        return tcs.Task;
    }
}

/* =============================
USAGE
-----
1) Put this script in your project as LightweightSceneManager.cs
2) Add it to a GameObject in your boot scene (e.g., "SceneHub").
3) In the inspector, add entries to the Scenes list (order matters for Next/Previous):
   - Key: e.g., "Main", "UI", "Level1"
   - SceneName: must match Build Settings scene name
   - (Editor only) optionally assign a SceneAsset to auto-fill SceneName
4) Ensure all scenes are added to File → Build Settings → Scenes in Build.

Examples:

// Load UI and Level1
await LightweightSceneManager.Instance!.LoadAsync("UI");
await LightweightSceneManager.Instance!.LoadAsync("Level1", setActive:true);

// Unload Level1
await LightweightSceneManager.Instance!.UnloadAsync("Level1");

// Swap to just Main (loads Main if needed, unloads everything else)
await LightweightSceneManager.Instance!.SwapToAsync("Main", setActive:true);

// Advance to next (by inspector order), from any other script on an event
LightweightSceneManager.Instance!.Next();

// Or await it
await LightweightSceneManager.Instance!.NextAsync();

// Subscribe to active scene changes
LightweightSceneManager.Instance!.ActiveSceneChanged += key => Debug.Log($"Active now: {key}");

============================= */
