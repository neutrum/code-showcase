using System.IO;
using UnityEngine;

public class CTWCubemapSaver : MonoBehaviour
{
    public Camera captureCamera;
    public int cubemapSize = 512;
    public string savePath = "CubemapFaces";

    private RenderTexture cubemapRenderTexture;

    void Start()
    {
        // Create the cubemap RenderTexture
        cubemapRenderTexture = new RenderTexture(cubemapSize, cubemapSize, 16);
        cubemapRenderTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;
        cubemapRenderTexture.hideFlags = HideFlags.HideAndDontSave;
        cubemapRenderTexture.Create();

        // Render to cubemap
        captureCamera.RenderToCubemap(cubemapRenderTexture);

        // Save faces to disk
        SaveCubemapFaces(cubemapRenderTexture);
    }

    void SaveCubemapFaces(RenderTexture cubemap)
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, savePath));

        CubemapFace[] faces = new[]
        {
            CubemapFace.PositiveX,
            CubemapFace.NegativeX,
            CubemapFace.PositiveY,
            CubemapFace.NegativeY,
            CubemapFace.PositiveZ,
            CubemapFace.NegativeZ
        };

        string[] faceNames = { "PosX", "NegX", "PosY", "NegY", "PosZ", "NegZ" };

        RenderTexture.active = cubemap;

        for (int i = 0; i < faces.Length; i++)
        {
            Texture2D tex = new Texture2D(cubemap.width, cubemap.height, TextureFormat.RGB24, false);
            // Read from each face
            Graphics.SetRenderTarget(cubemap, 0, faces[i]);
            tex.ReadPixels(new Rect(0, 0, cubemap.width, cubemap.height), 0, 0);
            tex.Apply();

            // Encode to PNG
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(Application.dataPath, savePath, faceNames[i] + ".png"), bytes);

            Destroy(tex);
        }

        RenderTexture.active = null;
        Debug.Log("Cubemap faces saved to " + Path.Combine(Application.dataPath, savePath));
    }
}

