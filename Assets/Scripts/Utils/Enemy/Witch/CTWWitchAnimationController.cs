using UnityEngine;
using Utils.Animation;
using Utils.Enemy.Witch;

public class CTWWitchAnimationController : MonoBehaviour
{
    public CTWAnimationBlender blender;
    public CTWWitchTestMover mover;

    [Header("Speed Thresholds")]
    public float floatThreshold = 0.2f;
    public float flyThreshold = 1.5f;

    [Header("Blend Speeds")]
    public float blendIn = 4f;
    public float blendOut = 3f;

    [Header("Rotation")]
    public float turnSpeed = 6f;

    void Update()
    {
        float speed = mover.velocity.magnitude;
        Vector3 dir = mover.direction;

        // -------------------------------
        // FLY MODE
        // -------------------------------
        if (speed > flyThreshold)
        {
            blender.weights[(int)CTWBlendState.Fly] =
                Mathf.Lerp(blender.weights[(int)CTWBlendState.Fly], 1f, blendIn * Time.deltaTime);

            blender.weights[(int)CTWBlendState.Float] =
                Mathf.Lerp(blender.weights[(int)CTWBlendState.Float], 0f, blendOut * Time.deltaTime);

            blender.weights[(int)CTWBlendState.IdleFloat] =
                Mathf.Lerp(blender.weights[(int)CTWBlendState.IdleFloat], 0f, blendOut * Time.deltaTime);

            // Orientation DURING FLIGHT
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Lerp(transform.rotation, look, turnSpeed * Time.deltaTime);
            }

            return; // <─ avoid blending interference
        }

        // -------------------------------
        // FLOAT MODE
        // -------------------------------
        if (speed > floatThreshold)
        {
            blender.weights[(int)CTWBlendState.Fly] =
                Mathf.Lerp(blender.weights[(int)CTWBlendState.Fly], 0f, blendOut * Time.deltaTime);

            blender.weights[(int)CTWBlendState.Float] =
                Mathf.Lerp(blender.weights[(int)CTWBlendState.Float], 1f, blendIn * Time.deltaTime);

            blender.weights[(int)CTWBlendState.IdleFloat] =
                Mathf.Lerp(blender.weights[(int)CTWBlendState.IdleFloat], 0f, blendOut * Time.deltaTime);

            // face the player while floating
            LookAtPlayer();
            return;
        }

        // -------------------------------
        // IDLE FLOAT
        // -------------------------------
        blender.weights[(int)CTWBlendState.Fly] =
            Mathf.Lerp(blender.weights[(int)CTWBlendState.Fly], 0f, blendOut * Time.deltaTime);

        blender.weights[(int)CTWBlendState.Float] =
            Mathf.Lerp(blender.weights[(int)CTWBlendState.Float], 0f, blendOut * Time.deltaTime);

        blender.weights[(int)CTWBlendState.IdleFloat] =
            Mathf.Lerp(blender.weights[(int)CTWBlendState.IdleFloat], 1f, blendIn * Time.deltaTime);

        LookAtPlayer();
    }

    private void LookAtPlayer()
    {
        if (Camera.main == null) return;

        Vector3 dir = (Camera.main.transform.position - transform.position);
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Lerp(transform.rotation, look, turnSpeed * Time.deltaTime);
        }
    }
}
