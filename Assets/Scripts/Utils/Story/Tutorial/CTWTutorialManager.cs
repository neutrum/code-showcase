using System.Collections;
using UnityEngine;
using Utils;
using CTW.Story;

namespace CTW.Tutorial
{
    /// <summary>
    /// Manages tutorial progression and teaches game mechanics.
    /// Works in tandem with CTWStoryDirector for narrative presentation.
    ///
    /// Architecture:
    /// - TutorialManager = Gameplay mechanics and progression
    /// - StoryDirector   = Narrative presentation and whispers
    /// - Integration via flags and message bus
    /// </summary>
    public class CTWTutorialManager : MonoBehaviour
    {
        public static CTWTutorialManager Instance { get; private set; }

        [Header("Dependencies")]
        public CTWFloorSpaceFinder floorSpaceFinder;
        public Transform playerHead;
        public Transform leftHand;
        public Transform rightHand;

        [Header("Tutorial Prefabs")]
        public GameObject lightPrefab;
        public GameObject dollPrefab;
        public GameObject tutorialMarkerPrefab;

        [Header("Tutorial Settings")]
        public float handGazeRequiredTime = 2f;
        public float lightRadius = 1.5f;
        public bool skipTutorialIfCompleted = true;

        [Header("Story Events")]
        public CTWStoryEvent introWhisper;
        public CTWStoryEvent handsWhisper;
        public CTWStoryEvent pickupWhisper;
        public CTWStoryEvent tutorialCompleteEvent;

        private enum TutorialPhase { NotStarted, GoToLight, LookAtHands, PickupDoll, Complete }

        private TutorialPhase _currentPhase = TutorialPhase.NotStarted;
        private GameObject _spawnedLight;
        private GameObject _spawnedDoll;
        private Vector3 _lightPosition;
        private bool _isWatchingForAction;

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (skipTutorialIfCompleted && CTWStoryDirector.Instance.HasFlag("tutorial_complete"))
            {
                CTWLog.Verbose("[Tutorial] Tutorial already completed, skipping");
                StartMainGame();
                return;
            }
            Invoke(nameof(StartTutorial), 1f);
        }

        // ─── Phase 1: Go Into The Light ─────────────────────────────────────────

        public void StartTutorial()
        {
            CTWLog.Verbose("[Tutorial] Starting tutorial sequence");
            _currentPhase = TutorialPhase.GoToLight;

            var result = floorSpaceFinder.FindBestEmptyFloorSpace(lightRadius * 2, lightRadius * 2, samples: 10);
            if (!result.found)
            {
                Debug.LogError("[Tutorial] Could not find space for light! Skipping tutorial.");
                StartMainGame();
                return;
            }

            _lightPosition = result.position;
            _spawnedLight = Instantiate(lightPrefab, _lightPosition, Quaternion.identity);
            CTWLog.Verbose($"[Tutorial] Light spawned at {_lightPosition}");

            CTWStoryDirector.Instance.SetFlag("tutorial_started");
            if (introWhisper) CTWStoryDirector.Instance.TryPlay(introWhisper);

            _isWatchingForAction = true;
            StartCoroutine(WatchForPlayerEnterLight());
        }

        private IEnumerator WatchForPlayerEnterLight()
        {
            CTWLog.Verbose("[Tutorial] Waiting for player to enter light...");

            while (_isWatchingForAction)
            {
                if (playerHead == null)
                {
                    var rig = Object.FindAnyObjectByType<OVRCameraRig>();
                    if (rig != null) playerHead = rig.centerEyeAnchor;
                }

                if (playerHead != null && Vector3.Distance(playerHead.position, _lightPosition) < lightRadius)
                {
                    CTWLog.Verbose("[Tutorial] Player entered light");
                    OnPhase1Complete();
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private void OnPhase1Complete()
        {
            _isWatchingForAction = false;
            CTWStoryDirector.Instance.SetFlag("entered_light");
            CTWStoryDirector.Instance.SetFlag("learned_movement");
            CTWLog.Verbose("[Tutorial] Phase 1 complete — Learned movement");
            StartPhase2();
        }

        // ─── Phase 2: Look At Your Hands ────────────────────────────────────────

        private void StartPhase2()
        {
            _currentPhase = TutorialPhase.LookAtHands;
            CTWLog.Verbose("[Tutorial] Phase 2: Look at hands");
            CTWStoryDirector.Instance.SetFlag("phase_2_started");
            if (handsWhisper) CTWStoryDirector.Instance.TryPlay(handsWhisper);

            _isWatchingForAction = true;
            StartCoroutine(WatchForHandGaze());
        }

        private IEnumerator WatchForHandGaze()
        {
            CTWLog.Verbose("[Tutorial] Waiting for player to look at hands...");
            float gazeTime = 0f;

            while (_isWatchingForAction)
            {
                if (IsPlayerLookingAtHands())
                {
                    gazeTime += Time.deltaTime;
                    if (gazeTime >= handGazeRequiredTime)
                    {
                        CTWLog.Verbose("[Tutorial] Player looked at hands");
                        OnPhase2Complete();
                        yield break;
                    }
                }
                else
                {
                    gazeTime = 0f;
                }
                yield return null;
            }
        }

        private bool IsPlayerLookingAtHands() =>
            playerHead != null && (IsLookingAt(leftHand) || IsLookingAt(rightHand));

        private bool IsLookingAt(Transform target, float angleThreshold = 50f)
        {
            if (target == null || playerHead == null) return false;
            return Vector3.Angle(playerHead.forward, (target.position - playerHead.position).normalized) < angleThreshold;
        }

        private void OnPhase2Complete()
        {
            _isWatchingForAction = false;
            CTWStoryDirector.Instance.SetFlag("learned_hands");
            CTWLog.Verbose("[Tutorial] Phase 2 complete — Learned hands");
            StartPhase3();
        }

        // ─── Phase 3: Pick Up The Doll ──────────────────────────────────────────

        private void StartPhase3()
        {
            _currentPhase = TutorialPhase.PickupDoll;
            CTWLog.Verbose("[Tutorial] Phase 3: Pick up doll");

            _spawnedDoll = Instantiate(dollPrefab, _lightPosition + Vector3.up * 0.5f, Quaternion.identity);
            CTWStoryDirector.Instance.SetFlag("doll_spawned");
            if (pickupWhisper) CTWStoryDirector.Instance.TryPlay(pickupWhisper);

            var grabbable = _spawnedDoll.GetComponent<OVRGrabbable>();
            if (grabbable == null)
                Debug.LogWarning("[Tutorial] Doll has no OVRGrabbable component!");

            StartCoroutine(WatchForDollPickup());
        }

        private IEnumerator WatchForDollPickup()
        {
            CTWLog.Verbose("[Tutorial] Waiting for doll pickup...");
            Vector3 initialPosition = _spawnedDoll.transform.position;

            while (_spawnedDoll != null)
            {
                if (Vector3.Distance(_spawnedDoll.transform.position, initialPosition) > 0.3f)
                {
                    CTWLog.Verbose("[Tutorial] Doll picked up");
                    OnPhase3Complete();
                    yield break;
                }
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void OnDollGrabbed()
        {
            CTWLog.Verbose("[Tutorial] Doll grabbed via event");
            OnPhase3Complete();
        }

        private void OnPhase3Complete()
        {
            CTWStoryDirector.Instance.SetFlag("learned_pickup");
            CTWStoryDirector.Instance.SetFlag("tutorial_complete");
            CTWLog.Verbose("[Tutorial] Phase 3 complete — Learned pickup");

            _currentPhase = TutorialPhase.Complete;
            if (tutorialCompleteEvent) CTWStoryDirector.Instance.TryPlay(tutorialCompleteEvent);
            Invoke(nameof(StartMainGame), 3f);
        }

        // ─── Completion ─────────────────────────────────────────────────────────

        private void StartMainGame()
        {
            CTWLog.Verbose("[Tutorial] Tutorial complete — Starting main game");

            if (_spawnedLight) Destroy(_spawnedLight);
            if (_spawnedDoll)  Destroy(_spawnedDoll);

            CTWStoryDirector.Instance.SetFlag("main_game_started");
        }

        // ─── Public API ─────────────────────────────────────────────────────────

        [ContextMenu("Skip Tutorial")]
        public void SkipTutorial()
        {
            CTWLog.Verbose("[Tutorial] Skipping tutorial");
            CTWStoryDirector.Instance.SetFlag("tutorial_started");
            CTWStoryDirector.Instance.SetFlag("entered_light");
            CTWStoryDirector.Instance.SetFlag("learned_movement");
            CTWStoryDirector.Instance.SetFlag("learned_hands");
            CTWStoryDirector.Instance.SetFlag("learned_pickup");
            CTWStoryDirector.Instance.SetFlag("tutorial_complete");
            StartMainGame();
        }

        [ContextMenu("Reset Tutorial")]
        public void ResetTutorial()
        {
            CTWLog.Verbose("[Tutorial] Resetting tutorial");
            CTWStoryDirector.Instance.ClearFlag("tutorial_started");
            CTWStoryDirector.Instance.ClearFlag("entered_light");
            CTWStoryDirector.Instance.ClearFlag("learned_movement");
            CTWStoryDirector.Instance.ClearFlag("learned_hands");
            CTWStoryDirector.Instance.ClearFlag("learned_pickup");
            CTWStoryDirector.Instance.ClearFlag("tutorial_complete");
            CTWStoryDirector.Instance.ClearFlag("main_game_started");
            _currentPhase = TutorialPhase.NotStarted;
            StartTutorial();
        }

        private void OnDrawGizmos()
        {
            if (_currentPhase == TutorialPhase.GoToLight && _spawnedLight != null)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawSphere(_lightPosition, lightRadius);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_lightPosition, lightRadius);
            }
        }
    }
}
