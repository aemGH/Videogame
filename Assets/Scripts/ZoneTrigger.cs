using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    [Header("Configuration")]
    public GameObject objectToEnable;
    public GameObject objectToDisable;
    public string requiredItem = "Key";
    public int requiredScore = 100;
    
    [Header("Feedback")]
    public string successMessage = "Zone Unlocked!";
    public string failMessage = "Not enough score or missing item!";

    private bool isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;

        if (other.CompareTag("Player"))
        {
            // Vérifier les conditions (ex: score ou possession d'un objet via GameManager)
            bool hasScore = (GameManager.Instance != null && GameManager.Instance.totalScore >= requiredScore);
            bool hasItem = (GameManager.Instance != null && GameManager.Instance.collectedItemTypes.Contains(requiredItem));
            
            // Si le trigger ne demande pas d'item ("None" ou vide), on valide
            if (string.IsNullOrEmpty(requiredItem) || requiredItem == "None") hasItem = true;

            if (hasScore && hasItem)
            {
                UnlockZone();
            }
            else
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowMessage(failMessage, 3f);
                }
            }
        }
    }

    void UnlockZone()
    {
        isTriggered = true;

        if (objectToEnable != null) objectToEnable.SetActive(true);
        if (objectToDisable != null) objectToDisable.SetActive(false);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMessage(successMessage, 3f);
        }

        // Optionnel : Jouer un son
        if (SoundManager.Instance != null && SoundManager.Instance.collectClip != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.collectClip); // Ou un son de porte
        }
    }
}
