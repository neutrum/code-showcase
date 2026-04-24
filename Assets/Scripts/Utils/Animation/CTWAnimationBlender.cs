using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Utils.Animation
{
    [RequireComponent(typeof(Animator))]
    public class CTWAnimationBlender : MonoBehaviour
    {
        [Header("Animation Clips (order must match CTWBlendState enum)")]
        public AnimationClip[] clips;

        [HideInInspector]
        public float[] weights;

        private PlayableGraph graph;
        private AnimationMixerPlayable mixer;
        private Animator animator;

        void Awake()
        {
            animator = EnsureAnimatorExists();

            if (clips == null || clips.Length == 0)
            {
                Debug.LogError("CTWAnimationBlender: No animation clips assigned!");
                enabled = false;
                return;
            }

            // initialize weight array
            weights = new float[clips.Length];
            weights[0] = 1f;

            // --- Build Playable Graph ---
            graph = PlayableGraph.Create("CTWAnimationBlender");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            var output = AnimationPlayableOutput.Create(graph, "CTWOutput", animator);

            mixer = AnimationMixerPlayable.Create(graph, clips.Length);

            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clips[i]);

                // 🔥 FORCE LOOPING
                clips[i].wrapMode = WrapMode.Loop;
                playable.SetDuration(double.PositiveInfinity);
                playable.SetTime(0);

                graph.Connect(playable, 0, mixer, i);
                mixer.SetInputWeight(i, weights[i]);
            }

            output.SetSourcePlayable(mixer);
            graph.Play();
        }

        private Animator EnsureAnimatorExists()
        {
            Animator anim = GetComponent<Animator>();

            if (anim == null)
                anim = gameObject.AddComponent<Animator>();

            // 🎯 AnimatorController NOT REQUIRED for Playables
            anim.runtimeAnimatorController = null;
            return anim;
        }

        void Update()
        {
            float sum = 0f;
            for (int i = 0; i < weights.Length; i++)
                sum += Mathf.Max(0, weights[i]);

            if (sum < 0.0001f) sum = 1;

            for (int i = 0; i < weights.Length; i++)
                mixer.SetInputWeight(i, weights[i] / sum);
        }

        void OnDestroy()
        {
            if (graph.IsValid())
                graph.Destroy();
        }
    }
}
