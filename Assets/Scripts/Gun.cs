using UnityEngine;
using TMPro;

public class Gun : MonoBehaviour
{
    // Gun stats
    public int damage = 25;
    public float timeBetweenShooting = 0.1f, spread = 0.02f, range = 100f, reloadTime = 1.5f;
    public int magazineSize = 30;
    public bool allowButtonHold = true;

    int bulletsLeft;
    bool shooting, readyToShoot, reloading;

    // References
    public Camera fpsCam;
    public LayerMask whatIsEnemy = ~0;

    // Graphics
    public ParticleSystem muzzleFlash;
    public GameObject bulletHoleGraphic;
    public CamShake camShake;
    public float camShakeMagnitude = 0.05f, camShakeDuration = 0.1f;

    // UI
    public TextMeshProUGUI text;

    // Audio
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip reloadSound;

    private void Awake()
    {
        // Force minimum usable stats if they are set too low in Inspector
        if (magazineSize <= 0) magazineSize = 30;
        if (damage < 20) damage = 25; 
        if (timeBetweenShooting <= 0f) timeBetweenShooting = 0.1f;
        if (range < 10f) range = 100f;

        // Auto-detect Enemy layer
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1) whatIsEnemy = (1 << enemyLayer) | (1 << 0); // Enemy + Default

        bulletsLeft = magazineSize;
        readyToShoot = true;

        if (fpsCam == null) fpsCam = Camera.main;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // AGGRESSIVE PINK FIX: Check every renderer in the gun hierarchy
        CleanPinkMaterials(gameObject);
    }

    private void CleanPinkMaterials(GameObject obj)
    {
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>(true))
        {
            if (r.sharedMaterial == null)
            {
                r.enabled = false;
                continue;
            }

            string shaderName = r.sharedMaterial.shader.name;
            bool isUnsupported = shaderName.Contains("Error") || 
                                 shaderName == "Standard" || 
                                 shaderName == "Standard (Specular setup)" ||
                                 shaderName.StartsWith("Particles/") || 
                                 shaderName.StartsWith("Legacy Shaders/") || 
                                 shaderName.StartsWith("Mobile/");

            if (isUnsupported && !shaderName.Contains("Universal Render Pipeline"))
            {
                Debug.Log("[Gun] Fixing broken (pink) renderer: " + r.gameObject.name + " from shader: " + shaderName);
                
                if (shaderName.StartsWith("Particles/")) 
                {
                    Shader urpParticle = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                    if (urpParticle != null) r.material.shader = urpParticle;
                    else r.enabled = false;
                }
                else 
                {
                    Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
                    if (urpLit != null) r.material.shader = urpLit;
                    else r.enabled = false;
                }
            }
        }
    }

    private void Update()
    {
        if (fpsCam == null) fpsCam = Camera.main;

        MyInput();

        if (text != null)
            text.SetText(bulletsLeft + " / " + magazineSize);
            
        // Occasional check to catch dynamically enabled effects
        if (Time.frameCount % 60 == 0) CleanPinkMaterials(gameObject);
    }

    private void MyInput()
    {
        if (allowButtonHold) shooting = Input.GetKey(KeyCode.Mouse0);
        else shooting = Input.GetKeyDown(KeyCode.Mouse0);

        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && !reloading)
            Reload();

        if (readyToShoot && shooting && !reloading && bulletsLeft > 0)
            Shoot();
    }

    private void Shoot()
    {
        if (GameManager.Instance != null && (GameManager.Instance.isGamePaused || GameManager.Instance.isGameOver))
            return;

        readyToShoot = false;
        bulletsLeft--;

        // Calculate Direction
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);
        Vector3 direction = fpsCam.transform.forward + fpsCam.transform.right * x + fpsCam.transform.up * y;

        // Visual Debug
        Debug.DrawRay(fpsCam.transform.position, direction * range, Color.red, 0.5f);

        // Targeted "Thick" Raycast (0.4f sphere is very forgiving)
        // Offset starting point slightly forward to avoid hitting internal camera colliders
        Vector3 rayStart = fpsCam.transform.position + fpsCam.transform.forward * 1.0f; 
        RaycastHit[] hits = Physics.SphereCastAll(rayStart, 0.5f, direction, range, whatIsEnemy, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        ZombieHealth zombieFound = null;
        RaycastHit bestHit = new RaycastHit();
        bool hitAnything = false;

        // PRIORITY LOOP: Find a zombie anywhere in the sphere before settling for a wall
        foreach (var hit in hits)
        {
            // Ignore anything part of the player or the gun itself
            if (hit.collider.CompareTag("Player") || hit.collider.name.Contains("Player") || hit.collider.transform.IsChildOf(transform.root))
                continue;

            // IGNORE THE ENVIRONMENT IF IT IS TOO CLOSE (likely clipping)
            if (hit.distance < 0.2f && !hit.collider.name.Contains("Zombie"))
                continue;

            ZombieHealth h = hit.collider.GetComponentInParent<ZombieHealth>();
            if (h == null) h = hit.collider.GetComponentInChildren<ZombieHealth>();

            if (h != null)
            {
                zombieFound = h;
                bestHit = hit;
                break; // Found a zombie, priority 1!
            }

            if (!hitAnything)
            {
                bestHit = hit;
                hitAnything = true;
            }
        }

        if (zombieFound != null)
        {
            Debug.Log("[Gun] ZOMBIE HIT CONFIRMED: " + zombieFound.gameObject.name);
            zombieFound.TakeDamage(damage);
        }
        else if (hitAnything)
        {
            Debug.Log("[Gun] Environment Hit: " + bestHit.collider.name + " | Layer: " + LayerMask.LayerToName(bestHit.collider.gameObject.layer));
            
            if (bestHit.collider.TryGetComponent(out Target t)) t.TakeDamage(damage);

            // Safer instantiation for bullet holes
            if (bulletHoleGraphic != null && !bestHit.collider.name.Contains("Player")) {
                GameObject hole = Instantiate(bulletHoleGraphic, bestHit.point + bestHit.normal * 0.01f, Quaternion.LookRotation(bestHit.normal));
                hole.transform.SetParent(bestHit.collider.transform);
                
                // Cleanup pink on hole
                CleanPinkMaterials(hole);
                Destroy(hole, 2f); 
            }
        }
        else
        {
            Debug.Log("[Gun] Total Miss - SphereCast hit nothing.");
        }

        // Effects
        if (camShake != null) camShake.Shake(camShakeDuration, camShakeMagnitude);
        
        if (muzzleFlash != null) {
            CleanPinkMaterials(muzzleFlash.gameObject);
            muzzleFlash.Play();
        }

        if (audioSource != null && shootSound != null) audioSource.PlayOneShot(shootSound);

        Invoke(nameof(ResetShot), timeBetweenShooting);
    }

    private void ResetShot() { readyToShoot = true; }

    private void Reload()
    {
        reloading = true;
        Debug.Log("[Gun] Reloading...");
        if (audioSource != null && reloadSound != null) audioSource.PlayOneShot(reloadSound);
        Invoke(nameof(ReloadFinished), reloadTime);
    }

    private void ReloadFinished()
    {
        bulletsLeft = magazineSize;
        reloading = false;
        Debug.Log("[Gun] Reload Complete.");
    }
}
