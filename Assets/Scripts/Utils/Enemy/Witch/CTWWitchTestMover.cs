using System.Collections;
using UnityEngine;

namespace Utils.Enemy.Witch
{
    public class CTWWitchTestMover : MonoBehaviour
    {
        [Header("Movement")]
        public float floatSpeed = 2f;
        public float flySpeed = 6f;
        public float orbitRadius = 2f;
        public float turningSpeed = 1f;
        public float stoppingDistance = 0.25f;
        public float delayBetweenMoves = 5f;

        [Header("Test Area")]
        public Vector3 areaSize = new Vector3(10f, 3f, 10f);

        // Exposed for animation controller
        public Vector3 velocity { get; private set; } = Vector3.zero;
        public Vector3 direction { get; private set; } = Vector3.zero;
        public bool isMoving { get; private set; } = false;

        private Vector3 lastPosition;
        private Vector3 anchorPoint;
        private float orbitAngle;

        private void Start()
        {
            lastPosition = transform.position;
            StartCoroutine(TestRoutine());
        }

        private IEnumerator TestRoutine()
        {
            while (true)
            {
                GenerateRandomAnchor();
                yield return OrbitAroundAnchor(anchorPoint);

                yield return new WaitForSeconds(delayBetweenMoves);
            }
        }

        private void GenerateRandomAnchor()
        {
            anchorPoint = transform.position + new Vector3(
                Random.Range(-areaSize.x, areaSize.x),
                Random.Range(-areaSize.y, areaSize.y),
                Random.Range(-areaSize.z, areaSize.z)
            );
        }

        private IEnumerator OrbitAroundAnchor(Vector3 anchor)
        {
            isMoving = true;

            // Orbit for a few seconds
            float orbitTime = Random.Range(3f, 6f);
            float t = 0f;

            // Start orbit angle based on witch's current orientation
            orbitAngle = Random.Range(0f, 360f);

            while (t < orbitTime)
            {
                t += Time.deltaTime;

                // Orbit movement
                orbitAngle += turningSpeed * Time.deltaTime;

                Vector3 orbitOffset = new Vector3(
                    Mathf.Cos(orbitAngle),
                    Random.Range(-0.1f, 0.1f),     // slight vertical drift
                    Mathf.Sin(orbitAngle)
                ) * orbitRadius;

                Vector3 targetPos = anchor + orbitOffset;

                float dist = Vector3.Distance(transform.position, targetPos);
                float speed = (dist > 2f) ? flySpeed : floatSpeed;

                Vector3 newPos = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
                transform.position = newPos;

                // Direction + velocity for animation blending
                velocity = (transform.position - lastPosition) / Time.deltaTime;
                direction = velocity.normalized;
                lastPosition = transform.position;

                // Rotate toward direction
                if (direction.sqrMagnitude > 0.01f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(direction, Vector3.up);
                    transform.rotation = Quaternion.Lerp(transform.rotation, lookRot, 8f * Time.deltaTime);
                }

                yield return null;
            }

            isMoving = false;
            velocity = Vector3.zero;
            direction = Vector3.zero;
        }
    }
}
