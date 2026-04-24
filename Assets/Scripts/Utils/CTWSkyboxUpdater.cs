using UnityEngine;

public class CTWSkyboxUpdater : MonoBehaviour
{
    [Header("Camera that captures the cubemap")]
    public Camera captureCam;

    [Header("Target cubemap RenderTexture (must be Cubemap type)")]
    public RenderTexture cubemapTexture;

    [Header("Skybox material using Skybox/Cubemap shader")]
    public Material skyboxMaterial;

    [Header("Update interval in seconds (set 0 for one-time capture)")]
    public float updateInterval = 30f;

    private float timer;

    void Start()
    {
        // Assign skybox material to the scene
        if (skyboxMaterial != null)
        {
            RenderSettings.skybox = skyboxMaterial;

            // Set the cubemap texture to the material
            if (cubemapTexture != null)
            {
                skyboxMaterial.SetTexture("_Tex", cubemapTexture);
            }
        }

        // Initial capture
        if (captureCam != null && cubemapTexture != null)
        {
            captureCam.RenderToCubemap(cubemapTexture);
        }
    }

    void Update()
    {
        if (updateInterval > 0f)
        {
            timer += Time.deltaTime;
            if (timer >= updateInterval)
            {
                if (captureCam != null && cubemapTexture != null)
                {
                    captureCam.RenderToCubemap(cubemapTexture);
                    skyboxMaterial.SetTexture("_Tex", cubemapTexture);
                }
                timer = 0f;
            }
        }
    }
}
