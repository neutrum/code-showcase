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
    /// - TutorialManager = Gameplay mechanics & progression
    /// - StoryDirector = Narrative presentation & whispers
    /// - Integration via flags & message bus
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

        // Tutorial state
        private enum TutorialPhase
        {
            NotStarted,
            GoToLight,
            LookAtHands,
            PickupDoll,
            Complete
        }

        private TutorialPhase currentPhase = TutorialPhase.NotStarted;
        private GameObject spawnedLight;
        private GameObject spawnedDoll;
        private Vector3 lightPosition;
        private bool isWatchingForAction = false;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Check if tutorial was already completed
            if (skipTutorialIfCompleted && CTWStoryDirector.Instance.HasFlag("tutorial_complete"))
            {
                Debug.Log("[Tutorial] Tutorial already completed, skipping");
                StartMainGame();
                return;
            }

            // Wait a moment before starting
            Invoke(nameof(StartTutorial), 1f);
        }

        // ============================================================
        // PHASE 1: Go Into The Light (Teach Movement)
        // ============================================================

        public void StartTutorial()
        {
            Debug.Log("[Tutorial] Starting tutorial sequence");

            currentPhase = TutorialPhase.GoToLight;

            // Find empty space for the light
            var result = floorSpaceFinder.FindBestEmptyFloorSpace(lightRadius * 2, lightRadius * 2, samples: 10);

            if (!result.found)
            {
                Debug.LogError("[Tutorial] Could not find space for light! Skipping tutorial.");
                StartMainGame();
                return;
            }

            lightPosition = result.position;

            // Spawn the guiding light
            spawnedLight = Instantiate(lightPrefab, lightPosition, Quaternion.identity);
            Debug.Log($"[Tutorial] Light spawned at {lightPosition}");

            // Set flag to trigger intro whisper via StoryDirector
            CTWStoryDirector.Instance.SetFlag("tutorial_started");

            // Play the intro whisper
            if (introWhisper)
            {
                CTWStoryDirector.Instance.TryPlay(introWhisper);
            }

            // Start watching for player entering light
            isWatchingForAction = true;
            StartCoroutine(WatchForPlayerEnterLight());
        }

        private IEnumerator WatchForPlayerEnterLight()
        {
            Debug.Log("[Tutorial] Waiting for player to enter light...");

            while (isWatchingForAction)
            {
                if (playerHead == null)
                {
                    // Try to find player head
                    var ovrCameraRig = Object.FindAnyObjectByType<OVRCameraRig>();// FindObjectOfType<OVRCameraRig>();
                    if (ovrCameraRig != null)
                    {
                        playerHead = ovrCameraRig.centerEyeAnchor;
                    }
                }

                if (playerHead != null)
                {
                    float distance = Vector3.Distance(playerHead.position, lightPosition);

                    if (distance < lightRadius)
                    {
                        Debug.Log("[Tutorial] ✅ Player entered light!");
                        OnPhase1Complete();
                        yield break;
                    }
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private void OnPhase1Complete()
        {
            isWatchingForAction = false;

            // Set flags
            CTWStoryDirector.Instance.SetFlag("entered_light");
            CTWStoryDirector.Instance.SetFlag("learned_movement");

            Debug.Log("[Tutorial] Phase 1 complete - Learned movement");

            // Move to next phase
            StartPhase2();
        }

        // ============================================================
        // PHASE 2: Look At Your Hands (Teach Hand Awareness)
        // ============================================================

        private void StartPhase2()
        {
            currentPhase = TutorialPhase.LookAtHands;

            Debug.Log("[Tutorial] Phase 2: Look at hands");

            // Set flag for story event
            CTWStoryDirector.Instance.SetFlag("phase_2_started");

            // Play hands whisper
            if (handsWhisper)
            {
                CTWStoryDirector.Instance.TryPlay(handsWhisper);
            }

            // Optional: Enable hand highlighting or effects here
            // EnableHandHighlights();

            // Start watching for hand gaze
            isWatchingForAction = true;
            StartCoroutine(WatchForHandGaze());
        }

        private IEnumerator WatchForHandGaze()
        {
            Debug.Log("[Tutorial] Waiting for player to look at hands...");

            float gazeTime = 0f;

            while (isWatchingForAction)
            {
                if (IsPlayerLookingAtHands())
                {
                    gazeTime += Time.deltaTime;

                    if (gazeTime >= handGazeRequiredTime)
                    {
                        Debug.Log("[Tutorial] ✅ Player looked at hands!");
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

        private bool IsPlayerLookingAtHands()
        {
            if (playerHead == null || (leftHand == null && rightHand == null))
                return false;

            // Check if player is looking at either hand
            return IsLookingAt(leftHand) || IsLookingAt(rightHand);
        }

        private bool IsLookingAt(Transform target, float angleThreshold = 50f)
        {
            if (target == null || playerHead == null)
                return false;

            Vector3 directionToTarget = (target.position - playerHead.position).normalized;
            float angle = Vector3.Angle(playerHead.forward, directionToTarget);

            return angle < angleThreshold;
        }

        private void OnPhase2Complete()
        {
            isWatchingForAction = false;

            // Set flags
            CTWStoryDirector.Instance.SetFlag("learned_hands");

            Debug.Log("[Tutorial] Phase 2 complete - Learned hands");

            // Move to next phase
            StartPhase3();
        }

        // ============================================================
        // PHASE 3: Pick Up The Doll (Teach Grabbing)
        // ============================================================

        private void StartPhase3()
        {
            currentPhase = TutorialPhase.PickupDoll;

            Debug.Log("[Tutorial] Phase 3: Pick up doll");

            // Spawn doll at light position (slightly above ground)
            Vector3 dollSpawnPos = lightPosition + Vector3.up * 0.5f;
            spawnedDoll = Instantiate(dollPrefab, dollSpawnPos, Quaternion.identity);

            // Set flag for story event
            CTWStoryDirector.Instance.SetFlag("doll_spawned");

            // Play pickup whisper
            if (pickupWhisper)
            {
                CTWStoryDirector.Instance.TryPlay(pickupWhisper);
            }

            // Subscribe to doll pickup event
            // Assuming doll has OVRGrabbable or similar
            var grabbable = spawnedDoll.GetComponent<OVRGrabbable>();
            if (grabbable != null)
            {
                // You'd hook into the grab event here
                // grabbable.OnGrabbed += OnDollGrabbed;

                // For now, start polling for pickup
                StartCoroutine(WatchForDollPickup());
            }
            else
            {
                Debug.LogWarning("[Tutorial] Doll has no OVRGrabbable component!");
                StartCoroutine(WatchForDollPickup());
            }
        }

        private IEnumerator WatchForDollPickup()
        {
            Debug.Log("[Tutorial] Waiting for doll pickup...");

            Vector3 initialPosition = spawnedDoll.transform.position;
            float pickupDistanceThreshold = 0.3f;

            while (spawnedDoll != null)
            {
                float distance = Vector3.Distance(spawnedDoll.transform.position, initialPosition);

                // Simple check: if doll moved significantly, assume it was picked up
                if (distance > pickupDistanceThreshold)
                {
                    Debug.Log("[Tutorial] ✅ Doll picked up!");
                    OnPhase3Complete();
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private void OnDollGrabbed()
        {
            Debug.Log("[Tutorial] Doll grabbed via event!");
            OnPhase3Complete();
        }

        private void OnPhase3Complete()
        {
            // Set flags
            CTWStoryDirector.Instance.SetFlag("learned_pickup");
            CTWStoryDirector.Instance.SetFlag("tutorial_complete");

            Debug.Log("[Tutorial] Phase 3 complete - Learned pickup");

            currentPhase = TutorialPhase.Complete;

            // Play tutorial complete event
            if (tutorialCompleteEvent)
            {
                CTWStoryDirector.Instance.TryPlay(tutorialCompleteEvent);
            }

            // Wait a moment then start main game
            Invoke(nameof(StartMainGame), 3f);
        }

        // ============================================================
        // Completion & Main Game
        // ============================================================

        private void StartMainGame()
        {
            Debug.Log("[Tutorial] Tutorial complete! Starting main game...");

            // Clean up tutorial objects
            if (spawnedLight) Destroy(spawnedLight);
            if (spawnedDoll) Destroy(spawnedDoll);

            // Trigger main game start
            // You could load a scene, enable game managers, etc.
            CTWStoryDirector.Instance.SetFlag("main_game_started");

            // Example: Enable your game manager
            // FindObjectOfType<CTWGameManager>()?.Activate();
        }

        // ============================================================
        // Public API for Manual Control
        // ============================================================

        /// <summary>
        /// Skip tutorial and go straight to main game
        /// </summary>
        [ContextMenu("Skip Tutorial")]
        public void SkipTutorial()
        {
            Debug.Log("[Tutorial] Skipping tutorial");

            // Set all tutorial flags as complete
            CTWStoryDirector.Instance.SetFlag("tutorial_started");
            CTWStoryDirector.Instance.SetFlag("entered_light");
            CTWStoryDirector.Instance.SetFlag("learned_movement");
            CTWStoryDirector.Instance.SetFlag("learned_hands");
            CTWStoryDirector.Instance.SetFlag("learned_pickup");
            CTWStoryDirector.Instance.SetFlag("tutorial_complete");

            StartMainGame();
        }

        /// <summary>
        /// Reset tutorial (for testing)
        /// </summary>
        [ContextMenu("Reset Tutorial")]
        public void ResetTutorial()
        {
            Debug.Log("[Tutorial] Resetting tutorial");

            CTWStoryDirector.Instance.ClearFlag("tutorial_started");
            CTWStoryDirector.Instance.ClearFlag("entered_light");
            CTWStoryDirector.Instance.ClearFlag("learned_movement");
            CTWStoryDirector.Instance.ClearFlag("learned_hands");
            CTWStoryDirector.Instance.ClearFlag("learned_pickup");
            CTWStoryDirector.Instance.ClearFlag("tutorial_complete");
            CTWStoryDirector.Instance.ClearFlag("main_game_started");

            currentPhase = TutorialPhase.NotStarted;

            // Restart
            StartTutorial();
        }

        private void OnDrawGizmos()
        {
            // Visualize light position and radius
            if (currentPhase == TutorialPhase.GoToLight && spawnedLight != null)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawSphere(lightPosition, lightRadius);

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(lightPosition, lightRadius);
            }
        }
    }
}


