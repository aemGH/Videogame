using UnityEngine;
using TMPro;

public class GunSystem : MonoBehaviour
{
    // Gun stats
    public int damage = 25;
    public float timeBetweenShooting = 0.1f, spread = 0.02f, range = 100f, reloadTime = 1.5f, timeBetweenShots = 0.05f;
    public int magazineSize = 30, bulletsPerTap = 1;
    public bool allowButtonHold = true;

    int bulletsLeft, bulletsShot;

    // state
    bool shooting, readyToShoot, reloading;

    // References
    public Camera fpsCam;
    public Transform attackPoint;
    public RaycastHit rayHit;
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
        // FORCE VALID VALUES if inspector left them at 0
        if (magazineSize <= 0) magazineSize = 30;
        if (damage <= 0) damage = 25;
        if (bulletsPerTap <= 0) bulletsPerTap = 1;
        if (timeBetweenShooting <= 0f) timeBetweenShooting = 0.1f;
        if (range <= 0f) range = 100f;
        if (reloadTime <= 0f) reloadTime = 1.5f;

        bulletsLeft = magazineSize;
        readyToShoot = true;

        if (fpsCam == null) fpsCam = Camera.main;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // KILL THE PINK ARTEFACT
        foreach (var r in GetComponentsInChildren<Renderer>(true)) {
            if (r.sharedMaterial != null && (r.sharedMaterial.name.Contains("Internal-Error") || r.sharedMaterial.name.Contains("Hidden"))) {
                Debug.Log("[GunSystem] Disabled pink renderer: " + r.gameObject.name);
                r.enabled = false;
            }
        }
    }

    private void Update()
    {
        if (fpsCam == null) fpsCam = Camera.main;

        MyInput();

        if (text != null)
            text.SetText(bulletsLeft + " / " + magazineSize);
    }

    private void MyInput()
    {
        if (allowButtonHold)
            shooting = Input.GetKey(KeyCode.Mouse0);
        else
            shooting = Input.GetKeyDown(KeyCode.Mouse0);

        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && !reloading)
            Reload();

        if (readyToShoot && shooting && !reloading && bulletsLeft > 0)
        {
            bulletsShot = bulletsPerTap;
            Shoot();
        }
    }

    private void Shoot()
    {
        readyToShoot = false;

        bulletsLeft--;

        // spread
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);

        Vector3 direction = fpsCam.transform.forward + fpsCam.transform.right * x + fpsCam.transform.up * y;

        // SphereCast with thickness to make hitting zombies much easier
        if (Physics.SphereCast(fpsCam.transform.position, 0.15f, direction, out rayHit, range, whatIsEnemy))
        {
            // Ignore Player
            if (rayHit.collider.CompareTag("Player") || rayHit.collider.name.Contains("Player"))
            {
                // Restart raycast from the point of impact moving forward
                if (!Physics.Raycast(rayHit.point + direction * 0.1f, direction, out rayHit, range - rayHit.distance, whatIsEnemy))
                {
                    ResetShot();
                    return;
                }
            }

            Debug.Log("[GunSystem] Hit: " + rayHit.collider.name + " on Layer: " + rayHit.collider.gameObject.layer);

            // Damage Zombie
            ZombieHealth target = rayHit.collider.GetComponent<ZombieHealth>();
            if (target == null) target = rayHit.collider.GetComponentInParent<ZombieHealth>();
            
            if (target != null)
            {
                Debug.Log("[GunSystem] HIT CONFIRMED on Zombie!");
                target.TakeDamage(damage);
            }
            else
            {
                // If we didn't hit a zombie directly, check if we hit a body part child
                ZombieHealth childTarget = rayHit.collider.GetComponentInChildren<ZombieHealth>();
                if (childTarget != null) {
                    childTarget.TakeDamage(damage);
                }
            }

            if (bulletHoleGraphic != null)
                Instantiate(bulletHoleGraphic, rayHit.point, Quaternion.LookRotation(rayHit.normal));
        }

        // Effects
        if (camShake != null) camShake.Shake(camShakeDuration, camShakeMagnitude);

        if (muzzleFlash != null) {
            muzzleFlash.Play();
        }

        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        bulletsShot--;
        Invoke(nameof(ResetShot), timeBetweenShooting);
    }

    private void ResetShot()
    {
        readyToShoot = true;
    }

    private void Reload()
    {
        reloading = true;
        Debug.Log("[GunSystem] Reloading...");

        if (audioSource != null && reloadSound != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }

        Invoke(nameof(ReloadFinished), reloadTime);
    }

    private void ReloadFinished()
    {
        bulletsLeft = magazineSize;
        reloading = false;
        Debug.Log("[GunSystem] Reload Complete. Bullets: " + bulletsLeft);
    }
}
