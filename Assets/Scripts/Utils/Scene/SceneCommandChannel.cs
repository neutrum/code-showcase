using System;
using UnityEngine;

namespace Utils.Scene
{
    [CreateAssetMenu(menuName = "Scenes/Scene Command Channel", fileName = "SceneCommandChannel")]
    public class SceneCommandChannel : ScriptableObject
    {
        public event Action OnNext;
        public event Action OnPrevious;
        public event Action<string> OnSwapTo;
        public event Action<string, bool> OnLoad;
        public event Action<string> OnUnload;

        public void RaiseNext() => OnNext?.Invoke();
        public void RaisePrevious() => OnPrevious?.Invoke();
        public void RaiseSwapTo(string key) => OnSwapTo?.Invoke(key);
        public void RaiseLoad(string key, bool setActive) => OnLoad?.Invoke(key, setActive);
        public void RaiseUnload(string key) => OnUnload?.Invoke(key);
    }
}
