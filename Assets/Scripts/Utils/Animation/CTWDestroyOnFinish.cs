using UnityEngine;

public class CTWDestroyOnFinish : MonoBehaviour
{
    public Material vatMaterial; // assign in Inspector or fetch via Renderer

    void Start()
    {
        if (vatMaterial == null)
            vatMaterial = GetComponent<Renderer>()?.sharedMaterial;

        if (vatMaterial != null &&
            vatMaterial.HasProperty("_frames") &&
            vatMaterial.HasProperty("_speed"))
        {
            float frames = vatMaterial.GetFloat("_frames");
            float speed = vatMaterial.GetFloat("_speed");

            if (speed > 0f)
            {
                float duration = frames / 30;
                Destroy(gameObject, duration);
            }
            else
            {
                Debug.LogWarning("Speed must be greater than zero to auto-destroy.");
            }
        }
        else
        {
            Debug.LogWarning("VAT material missing _frames or _speed properties.");
        }
    }
}
