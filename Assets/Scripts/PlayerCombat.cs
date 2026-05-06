using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Combat Settings")]
    public float damage = 25f;
    public float range = 50f;
    public float fireRate = 0.5f;
    
    private float nextTimeToFire = 0f;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        // Ne pas tirer si le jeu est en pause ou terminAc
        if (GameManager.Instance != null && (GameManager.Instance.isGamePaused || GameManager.Instance.isGameOver))
            return;

        if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        // Simple Raycast
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // Feedback visuel / sonore (optionnel, utiliser SoundManager si dispo)
        if (SoundManager.Instance != null && null != null)
        {
            SoundManager.Instance.PlaySFX(null);
        }

        if (Physics.Raycast(ray, out hit, range))
        {
            // VAcifier si on touche un zombie
            ZombieHealth target = hit.transform.GetComponent<ZombieHealth>();
            if (target == null) target = hit.transform.GetComponentInParent<ZombieHealth>();
            
            if (target != null)
            {
                target.TakeDamage(damage);
            }
        }
    }
}

