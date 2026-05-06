using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("Objectifs de progression")]
    [Tooltip("Score requis pour débloquer la zone")]
    public int scoreToUnlock = 150; 
    
    [Tooltip("La barricade ou porte à désactiver")]
    public GameObject barricade; 

    private bool isUnlocked = false;

    void Update()
    {
        // Vérifier si la condition est remplie et si ce n'est pas déjà débloqué
        if (!isUnlocked && GameManager.Instance != null)
        {
            if (GameManager.Instance.totalScore >= scoreToUnlock)
            {
                UnlockZone();
            }
        }
    }

    void UnlockZone()
    {
        isUnlocked = true;
        
        // Désactiver la barricade pour ouvrir le passage
        if (barricade != null)
        {
            barricade.SetActive(false);
            Debug.Log("Zone débloquée ! Félicitations.");
        }
    }
}
