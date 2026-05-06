using UnityEngine;

public class PlayerKeys : MonoBehaviour
{
    public bool hasKey = false;

    public void GiveKey()
    {
        hasKey = true;
        Debug.Log("Key collected!");
    }
}
