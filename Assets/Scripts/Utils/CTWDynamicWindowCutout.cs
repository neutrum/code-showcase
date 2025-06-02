using UnityEngine;
using System.Collections.Generic;

public class CTWDynamicWindowCutout : MonoBehaviour
{
    public Material wallMaterial; // Reference to the wall material with the cutout shader
    public List<GameObject> windows; // List of window GameObjects
    public float windowRadius = 1.0f; // The radius of the cutout for each window
    private Texture2D windowDataTexture;

    void Start()
    {
        // Create a texture to store the window data (position and radius)
        int textureSize = Mathf.CeilToInt(Mathf.Sqrt(windows.Count));
        windowDataTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBAFloat, false);
        windowDataTexture.filterMode = FilterMode.Point; // Prevent any interpolation

        // Assign the texture to the material
        wallMaterial.SetTexture("_WindowDataTex", windowDataTexture);
    }

    void Update()
    {
        // Update the texture with window positions and radii
        for (int i = 0; i < windows.Count; i++)
        {
            Vector3 windowPos = windows[i].transform.position;
            float radius = windowRadius;

            // Store position (x, y, z) and radius (w) in the texture
            Color windowData = new Color(windowPos.x, windowPos.y, windowPos.z, radius);

            // Calculate the pixel index in the texture
            int x = i % windowDataTexture.width;
            int y = i / windowDataTexture.width;

            windowDataTexture.SetPixel(x, y, windowData);
        }

        // Apply the changes to the texture
        windowDataTexture.Apply();
    }
}