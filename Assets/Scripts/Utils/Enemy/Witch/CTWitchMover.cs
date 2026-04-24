using System.Collections;
using UnityEngine;

namespace Utils.Enemy.Witch
{
    public class CTWWitchMover : MonoBehaviour
    {
        [Header("Movement")]
        public float floatSpeed = 2f;
        public float flySpeed = 6f;
        public float hoverHeight = 1.8f;
        public float stoppingDistance = 0.25f;

        [HideInInspector] public bool isFlying;
        [HideInInspector] public bool isSlowingDown;

        private Vector3 target;
        private bool moving;

        public void MoveTo(Vector3 targetPosition)
        {
            target = targetPosition;
            target.y = hoverHeight;           // lock target height
            if (!moving) StartCoroutine(MoveRoutine());
        }

        IEnumerator MoveRoutine()
        {
            moving = true;

            while (true)
            {
                Vector3 pos = transform.position;
                pos.y = hoverHeight;
                transform.position = pos;     // always keep float height

                float dist = Vector3.Distance(transform.position, target);

                if (dist < stoppingDistance)
                {
                    isFlying = false;
                    isSlowingDown = true;
                    break;
                }

                // Far = accelerate → FLY
                if (dist > 4f)
                {
                    isFlying = true;
                    isSlowingDown = false;
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        target,
                        flySpeed * Time.deltaTime
                    );
                }
                else
                {
                    // Close = slow → FLOAT
                    isFlying = false;
                    isSlowingDown = true;
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        target,
                        floatSpeed * Time.deltaTime
                    );
                }

                yield return null;
            }

            moving = false;
        }

        public bool IsMoving => moving;
    }
}
