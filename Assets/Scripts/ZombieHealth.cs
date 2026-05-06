using UnityEngine;

[RequireComponent(typeof(ZombieAI))]
public class ZombieHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;
    private ZombieAI aiScript;

    [Header("Rewards")]
    public int scoreValue = 50;
    public GameObject collectiblePrefabToDrop; // Optionnel : lâcher un objet
    public float dropChance = 0.3f; // 30% de chance de loot

    private bool isDead = false;

    void Awake()
    {
        // Automatically move zombie to the "Enemy" layer so bullets can hit it
        // and ignore environmental obstacles if necessary.
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
        {
            gameObject.layer = enemyLayer;
            // Also set children (body parts) to this layer
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = enemyLayer;
            }
        }
        else
        {
            Debug.LogWarning("[ZombieHealth] 'Enemy' layer not found in project. Please create it in 'Tags and Layers'.");
        }
    }

    void Start()
    {
        if (maxHealth <= 0) maxHealth = 100f;
        currentHealth = maxHealth;
        aiScript = GetComponent<ZombieAI>();
        if (aiScript == null) aiScript = GetComponentInParent<ZombieAI>();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) {
            Debug.Log("[ZombieHealth] Already dead, ignoring damage.");
            return;
        }

        currentHealth -= amount;
        Debug.Log("[ZombieHealth] Hit for " + amount + "! Remaining health: " + currentHealth);

        // Faux mort ? (optionnel, disons à 40% de vie)
        if (currentHealth > 0 && currentHealth <= maxHealth * 0.4f)
        {
            // 50% de chance de faire un faux mort
            if (Random.value > 0.5f)
            {
                Debug.Log("[ZombieHealth] Triggering Fake Death (Crawling)...");
                aiScript.TriggerFakeDeath();
            }
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("[ZombieHealth] Zombie Died! Triggering Real Death animation.");
        aiScript.TriggerDeath();

        // Ajouter du score
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue, "ZombieKill");
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnZombieDied();
        }

        // Système de Loot
        if (collectiblePrefabToDrop != null && Random.value <= dropChance)
        {
            Instantiate(collectiblePrefabToDrop, transform.position + Vector3.up, Quaternion.identity);
        }

        // Détruire le zombie après quelques secondes
        Destroy(gameObject, 10f);
    }
}
