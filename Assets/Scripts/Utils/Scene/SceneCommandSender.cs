using UnityEngine;
using Utils.Scene;

namespace Utils.Scene
{
    public class SceneCommandSender : MonoBehaviour
    {
        [SerializeField] private SceneCommandChannel channel;

        public void Next() => channel?.RaiseNext();
        public void Previous() => channel?.RaisePrevious();
        public void SwapTo(string key) => channel?.RaiseSwapTo(key);
        public void Load(string key) => channel?.RaiseLoad(key, true);
        public void LoadNoActive(string key) => channel?.RaiseLoad(key, false);
        public void Unload(string key) => channel?.RaiseUnload(key);
    }
}
