using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace CTW.Story
{
    /// <summary>
    /// World-space billboard presenter (image + subtitle) that sits in front of the player's head.
    /// </summary>
    public class CTWBillboardPresenter : CTWStoryPresenter
    {
        [Header("Placement")]
        public float distance = 1.8f;
        public float heightOffset = -0.05f;
        public float followLerp = 12f;

        [Header("UI (auto-wired if empty)")]
        public Canvas canvas;
        public CanvasGroup cg;
        public Image image;
        public Text subtitle;

        private Transform _head;

        public override void Setup(Transform playerHead, Camera worldCamera)
        {
            _head = playerHead;

            if (!canvas) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = worldCamera;

            if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1200, 700);
            canvas.transform.localScale = Vector3.one * 0.0012f;

            if (!image)
            {
                var go = new GameObject("Image", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                image = go.GetComponent<Image>();
                image.preserveAspect = true;
                var irt = image.rectTransform;
                irt.anchorMin = new Vector2(0.05f, 0.35f);
                irt.anchorMax = new Vector2(0.95f, 0.98f);
                irt.offsetMin = irt.offsetMax = Vector2.zero;
                image.color = Color.white;
            }

            if (!subtitle)
            {
                var go = new GameObject("Subtitle", typeof(RectTransform), typeof(Image), typeof(Text));
                go.transform.SetParent(transform, false);
                var bg = go.GetComponent<Image>(); bg.color = new Color(0, 0, 0, 0.55f);
                subtitle = go.GetComponent<Text>();
                subtitle.color = Color.white;
                subtitle.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                subtitle.alignment = TextAnchor.MiddleCenter;
                subtitle.resizeTextForBestFit = true;
                subtitle.resizeTextMinSize = 14;
                subtitle.resizeTextMaxSize = 42;
                var srt = subtitle.rectTransform;
                srt.anchorMin = new Vector2(0.05f, 0.02f);
                srt.anchorMax = new Vector2(0.95f, 0.30f);
                srt.offsetMin = srt.offsetMax = Vector2.zero;
            }

            cg.alpha = 0f;
        }

        private void LateUpdate()
        {
            if (!_head) return;

            var forward = Vector3.ProjectOnPlane(_head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 1e-4f) forward = _head.forward;

            var targetPos = _head.position + forward * distance + Vector3.up * heightOffset;
            transform.position = Vector3.Lerp(transform.position, targetPos, 1 - Mathf.Exp(-followLerp * Time.deltaTime));

            var toHead = (_head.position - transform.position);
            var look = Quaternion.LookRotation(toHead.normalized * -1f, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 1 - Mathf.Exp(-followLerp * Time.deltaTime));
        }

        public override IEnumerator Show(CTWStoryEvent.Slide slide)
        {
            image.sprite = slide.image;
            subtitle.text = slide.subtitle ?? "";
            yield return FadeTo(1f, slide.fade);
        }

        public override IEnumerator Hide(float fadeSeconds) => FadeTo(0f, fadeSeconds);

        private IEnumerator FadeTo(float target, float time)
        {
            float start = cg.alpha, t = 0f;
            while (t < time)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(start, target, t / Mathf.Max(0.0001f, time));
                yield return null;
            }
            cg.alpha = target;
        }
    }
}
