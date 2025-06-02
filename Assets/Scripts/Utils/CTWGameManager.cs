using System.Collections.Generic;
using UnityEngine;

public class CTWGameManager : MonoBehaviour
{
    public List<GameObject> gameObjects = new List<GameObject>();

    public void Activate()
    {
        foreach (var go in gameObjects)
        {
            go.SetActive(true);
        } 
    }
}
