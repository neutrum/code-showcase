using UnityEngine;

public class Rotate : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var transform1 = transform;
        var rotation = transform1.rotation;
        rotation.y += 0.1f * Time.deltaTime;
        transform1.rotation = rotation;
    }
}
