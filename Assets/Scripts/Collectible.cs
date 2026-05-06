using UnityEngine;

public class Collectible : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Le type de l'objet (ex: Food, Water, Health)")]
    public string itemType = "Food"; 
    
    [Tooltip("Score ajouté lors de la collecte")]
    public int scoreAmount = 10;
    
    [Tooltip("Quantité de statistique à restaurer")]
    public float restoreAmount = 20f; 

    private void OnTriggerEnter(Collider other)
    {
        // Vérifier si c'est le joueur en utilisant son tag
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                // Restaurer la statistique appropriée selon le type d'objet
                switch (itemType)
                {
                    case "Food":
                        player.hunger += restoreAmount;
                        break;
                    case "Water":
                        player.thirst += restoreAmount;
                        break;
                    case "Health":
                        player.currentHealth += restoreAmount;
                        break;
                    case "Key":
                        // Ajouté à collectedItemTypes via GameManager
                        break;
                }
            }

            // Ajouter le score via le GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(scoreAmount, itemType);
            }

            // AJOUT: Jouer un son via le SoundManager
            if (SoundManager.Instance != null && SoundManager.Instance.collectClip != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.collectClip);
            }

            // Détruire l'objet après collecte
            Destroy(gameObject);
        }
    }
}
