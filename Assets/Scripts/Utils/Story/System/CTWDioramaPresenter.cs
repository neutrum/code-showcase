using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CTW.Story
{
    /// <summary>
    /// Spawns 3D diorama prefabs in world space. Each slide = a scene configuration.
    /// Perfect for miniature theater-style storytelling in XR.
    /// Map slide index to prefab configurations.
    /// </summary>
    public class CTWDioramaPresenter : CTWStoryPresenter
    {
        [System.Serializable]
        public class DioramaScene
        {
            [Tooltip("Which slide index this scene is for")]
            public int slideIndex;
            
            [Header("Prefab")]
            public GameObject prefab;           // The diorama prefab to spawn
            
            [Header("Placement (local to diorama root)")]
            public Vector3 localPosition = Vector3.zero;
            public Vector3 localRotation = Vector3.zero;
            public float scale = 1f;
            
            [Header("Effects (optional)")]
            public ParticleSystem[] particlesToPlay;
            public Animator[] animatorsToTrigger;
            public string animatorTrigger = "Play";
            public AudioClip spawnSound;
        }

        [Header("Placement")]
        public Transform dioramaRoot;
        public float spawnDistance = 2.5f;
        public Vector3 spawnOffset = new Vector3(0, -0.2f, 0);
        public bool followPlayer = true;
        public float followSpeed = 5f;

        [Header("Diorama Scenes")]
        public List<DioramaScene> scenes = new();

        [Header("Transitions")]
        public FadeMode fadeMode = FadeMode.Scale;
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public enum FadeMode
        {
            Scale,      // Grow/shrink from zero
            Alpha,      // Fade materials (requires shader support)
            Instant     // No transition
        }

        private Transform _head;
        private GameObject _currentDiorama;
        private int _slideIndex = 0;
        private AudioSource _audioSource;
        private Vector3 _targetScale;

        public override void Setup(Transform playerHead, Camera worldCamera)
        {
            _head = playerHead;
            
            if (!dioramaRoot)
            {
                dioramaRoot = new GameObject("DioramaRoot").transform;
                dioramaRoot.SetParent(transform);
                dioramaRoot.localPosition = Vector3.zero;
            }

            _audioSource = GetComponent<AudioSource>();
            if (!_audioSource && scenes.Exists(s => s.spawnSound != null))
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1f; // 3D sound
            }

            Debug.Log($"[DioramaPresenter] Setup complete with {scenes.Count} scenes");
        }

        private void LateUpdate()
        {
            if (followPlayer && _head != null && dioramaRoot != null)
            {
                var forward = Vector3.ProjectOnPlane(_head.forward, Vector3.up).normalized;
                if (forward.sqrMagnitude < 1e-4f) forward = _head.forward;

                var targetPos = _head.position + forward * spawnDistance + spawnOffset;
                dioramaRoot.position = Vector3.Lerp(dioramaRoot.position, targetPos, 
                    1 - Mathf.Exp(-followSpeed * Time.deltaTime));

                var targetRot = Quaternion.LookRotation(forward, Vector3.up);
                dioramaRoot.rotation = Quaternion.Slerp(dioramaRoot.rotation, targetRot, 
                    1 - Mathf.Exp(-followSpeed * Time.deltaTime));
            }
        }

        public override IEnumerator Show(CTWStoryEvent.Slide slide)
        {
            // Find matching scene
            var scene = scenes.Find(s => s.slideIndex == _slideIndex);
            
            if (scene != null && scene.prefab != null)
            {
                Debug.Log($"[DioramaPresenter] Showing scene {_slideIndex}: {scene.prefab.name}");

                // Position diorama root if first spawn
                if (followPlayer && _head != null)
                {
                    var forward = Vector3.ProjectOnPlane(_head.forward, Vector3.up).normalized;
                    if (forward.sqrMagnitude < 1e-4f) forward = _head.forward;
                    
                    dioramaRoot.position = _head.position + forward * spawnDistance + spawnOffset;
                    dioramaRoot.rotation = Quaternion.LookRotation(forward, Vector3.up);
                }

                // Spawn prefab
                _currentDiorama = Instantiate(scene.prefab, dioramaRoot);
                _currentDiorama.transform.localPosition = scene.localPosition;
                _currentDiorama.transform.localEulerAngles = scene.localRotation;
                _targetScale = Vector3.one * scene.scale;

                // Play spawn sound
                if (scene.spawnSound && _audioSource)
                {
                    _audioSource.transform.position = _currentDiorama.transform.position;
                    _audioSource.PlayOneShot(scene.spawnSound);
                }

                // Trigger effects
                foreach (var ps in scene.particlesToPlay)
                {
                    if (ps) ps.Play();
                }

                foreach (var anim in scene.animatorsToTrigger)
                {
                    if (anim) anim.SetTrigger(scene.animatorTrigger);
                }

                // Fade in
                yield return FadeIn(slide.fade);
            }
            else
            {
                Debug.LogWarning($"[DioramaPresenter] No diorama scene configured for slide index {_slideIndex}");
                yield return null;
            }

            _slideIndex++;
        }

        public override IEnumerator Hide(float fadeSeconds)
        {
            if (_currentDiorama)
            {
                Debug.Log("[DioramaPresenter] Hiding current diorama");
                yield return FadeOut(fadeSeconds);
                Destroy(_currentDiorama);
                _currentDiorama = null;
            }
        }

        private IEnumerator FadeIn(float time)
        {
            if (!_currentDiorama || fadeMode == FadeMode.Instant)
            {
                if (_currentDiorama)
                    _currentDiorama.transform.localScale = _targetScale;
                yield break;
            }

            switch (fadeMode)
            {
                case FadeMode.Scale:
                    yield return AnimateScale(Vector3.zero, _targetScale, time);
                    break;
                
                case FadeMode.Alpha:
                    _currentDiorama.transform.localScale = _targetScale;
                    yield return AnimateAlpha(0f, 1f, time);
                    break;
            }
        }

        private IEnumerator FadeOut(float time)
        {
            if (!_currentDiorama || fadeMode == FadeMode.Instant)
                yield break;

            switch (fadeMode)
            {
                case FadeMode.Scale:
                    yield return AnimateScale(_currentDiorama.transform.localScale, Vector3.zero, time);
                    break;
                
                case FadeMode.Alpha:
                    yield return AnimateAlpha(1f, 0f, time);
                    break;
            }
        }

        private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = fadeCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
                _currentDiorama.transform.localScale = Vector3.Lerp(from, to, t);
                yield return null;
            }
            _currentDiorama.transform.localScale = to;
        }

        private IEnumerator AnimateAlpha(float from, float to, float duration)
        {
            // Get all renderers
            var renderers = _currentDiorama.GetComponentsInChildren<Renderer>();
            var materialProperties = new List<MaterialPropertyBlock>();
            
            foreach (var renderer in renderers)
            {
                var props = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(props);
                materialProperties.Add(props);
            }

            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = fadeCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
                float alpha = Mathf.Lerp(from, to, t);

                for (int i = 0; i < renderers.Length; i++)
                {
                    materialProperties[i].SetFloat("_Alpha", alpha);
                    renderers[i].SetPropertyBlock(materialProperties[i]);
                }
                
                yield return null;
            }
        }

        /// <summary>
        /// Reset slide index (useful if reusing presenter)
        /// </summary>
        public void ResetSlideIndex()
        {
            _slideIndex = 0;
        }
    }
}

