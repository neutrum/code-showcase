using UnityEngine;

public class CTWCoffingHelper : MonoBehaviour
{
    public GameObject TrueEye;
    public GameObject Skull;
    public GameObject Eye;

    public void Start()
    {
        Debug.Log($@"--- CTW: Coffin spawned at {transform.position}");
        TrueEye.SetActive(false);
    }

    public void Finish()
    {
        Eye.SetActive(false);
        TrueEye.SetActive(true);
        Skull.transform.SetParent(null);
    }
}
