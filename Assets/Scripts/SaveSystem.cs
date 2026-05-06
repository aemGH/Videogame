using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    // Sauvegarde les données de session en utilisant PlayerPrefs
    // PlayerPrefs est une méthode simple pour stocker des petites données
    public void SaveGame()
    {
        if (GameManager.Instance != null)
        {
            // Sauvegarde du score et du temps de survie
            PlayerPrefs.SetFloat("Score", (float)GameManager.Instance.totalScore);
            PlayerPrefs.SetFloat("SessionTime", GameManager.Instance.sessionTime);
            
            // Accéder au PlayerController pour sauvegarder la santé
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                PlayerPrefs.SetFloat("PlayerHealth", player.currentHealth);
            }
            
            PlayerPrefs.Save(); // Force l'écriture sur le disque
            Debug.Log("Partie sauvegardée avec succès !");
        }
    }

    // Charge les données de session
    public void LoadGame()
    {
        // Vérifier si une sauvegarde existe
        if (PlayerPrefs.HasKey("Score"))
        {
            // Récupérer les données
            GameManager.Instance.totalScore = (int)PlayerPrefs.GetFloat("Score");
            GameManager.Instance.sessionTime = PlayerPrefs.GetFloat("SessionTime");
            
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                player.currentHealth = PlayerPrefs.GetFloat("PlayerHealth");
            }
            
            Debug.Log("Partie chargée !");
        }
        else
        {
            Debug.Log("Aucune sauvegarde trouvée.");
        }
    }
}
