using Meta.XR.MRUtilityKit;
using UnityEngine;

public class SaveSceneToJson : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (MRUK.Instance != null)
        {
            // CoordinateSystem can be Device or Local. IncludeGlobalMesh is a boolean.
            string jsonString = MRUK.Instance.SaveSceneToJsonString(true);

            // Save to file
            string path = System.IO.Path.Combine(Application.persistentDataPath, "scene.json");
            System.IO.File.WriteAllText(path, jsonString);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
