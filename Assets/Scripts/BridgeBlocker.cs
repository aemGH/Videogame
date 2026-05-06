using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerKeys playerKeys = other.GetComponent<PlayerKeys>();

            if (playerKeys != null)
            {
                playerKeys.GiveKey();
                Destroy(gameObject); // remove key after pickup
            }
        }
    }
}