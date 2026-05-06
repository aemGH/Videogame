using UnityEngine;

public class TrainInteract : MonoBehaviour
{
    public Transform player;
    public Transform trainSeat; // where player stands on train
    public KeyCode interactKey = KeyCode.E;

    private bool playerInRange = false;
    private bool isRiding = false;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            if (!isRiding)
                BoardTrain();
            else
                ExitTrain();
        }
    }

    void BoardTrain()
    {
        isRiding = true;

        // Move player to seat
        player.position = trainSeat.position;

        // Parent player to train (IMPORTANT)
        player.SetParent(transform);

        Debug.Log("Player boarded train");
    }

    void ExitTrain()
    {
        isRiding = false;

        // Unparent player
        player.SetParent(null);

        Debug.Log("Player exited train");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }
}
