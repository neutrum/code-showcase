using UnityEngine;

namespace Utils.GameObjectExt
{
    [RequireComponent(typeof(AudioSource))]
    public class CTWAudioPlayer : MonoBehaviour
    {
        private AudioSource audioSource;

        [Tooltip("Assign the audio clip to play here")]
        public AudioClip clip;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;

            if (clip != null)
            {
                audioSource.clip = clip;
            }
        }

        public void Play()
        {
            if (clip == null)
            {
                Debug.LogWarning("CTWAudioPlayer: No AudioClip assigned.");
                return;
            }

            if (audioSource.clip != clip)
            {
                audioSource.clip = clip;
            }

            audioSource.Play();
        }

        public void Stop()
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
}
