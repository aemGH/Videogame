using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5.0f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Look Settings")]
    public Transform playerCamera; // Réf vers la caméra principale
    public float mouseSensitivity = 2.0f;
    private float xRotation = 0f;

    [Header("Survival Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float hunger = 100f;
    public float thirst = 100f;
    
    [Header("Decay Rates")]
    public float hungerDecayRate = 0.5f;
    public float thirstDecayRate = 0.7f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    // References to managers
    private GameManager gameManager;
    private UIManager uiManager;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        currentHealth = maxHealth;
        
        // Verrouiller et cacher le curseur
        Cursor.lockState = CursorLockMode.Locked;

        // Assuming managers exist in the scene
        gameManager = FindFirstObjectByType<GameManager>();
        uiManager = FindFirstObjectByType<UIManager>();
    }

    void Update()
    {
        // Gérer le curseur et bloquer les mouvements si le jeu est en pause ou terminé
        if (GameManager.Instance != null && GameManager.Instance.isGamePaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return; // Bloque les entrées caméra/mouvement
        }
        else if (GameManager.Instance != null && GameManager.Instance.isGameOver)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        HandleMovement();
        HandleSurvivalDecay();
    }

    void HandleMovement()
    {
        // --- LOOK (Souris) ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotation horizontale du personnage (tourner le corps entier)
        transform.Rotate(Vector3.up * mouseX);

        // Rotation verticale de la caméra seule
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        // --- MOVE (Clavier) ---
        // Check if player is on the ground
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep player grounded
        }

        // Get Input
        float x = Input.GetAxis("Horizontal"); // A/D ou Flèches Gauche/Droite
        float z = Input.GetAxis("Vertical");   // W/S ou Flèches Haut/Bas

        // Déplacement du personnage relatif à sa direction horizontale uniquement
        // Pour éviter que le joueur ne "vole" si la caméra regarde en l'air, on utilise un vecteur qui ignore le Y
        Vector3 forward = playerCamera.forward;
        forward.y = 0;
        forward.Normalize();
        
        Vector3 right = playerCamera.right;
        right.y = 0;
        right.Normalize();

        Vector3 move = right * x + forward * z;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Jumping
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleSurvivalDecay()
    {
        // Decrease stats over time
        hunger -= hungerDecayRate * Time.deltaTime;
        thirst -= thirstDecayRate * Time.deltaTime;

        // Health drops if starving or dehydrated
        if (hunger <= 0 || thirst <= 0)
        {
            currentHealth -= 2 * Time.deltaTime; // Reduce health over time
        }

        // Clamp stats
        hunger = Mathf.Clamp(hunger, 0, 100);
        thirst = Mathf.Clamp(thirst, 0, 100);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Update UI
        if (uiManager != null)
        {
            uiManager.UpdateHealthUI(currentHealth, maxHealth);
        }

        // Check for Game Over
        if (currentHealth <= 0)
        {
            if (gameManager != null)
            {
                gameManager.GameOver();
            }
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        if (uiManager != null)
        {
            uiManager.UpdateHealthUI(currentHealth, maxHealth);
        }

        if (currentHealth <= 0 && gameManager != null)
        {
            gameManager.GameOver();
        }
    }

    // Exemple de script de collision (pour le barème "Scripts de collisions")
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Hazard"))
        {
            currentHealth -= 10f; // Prendre des dégâts
            Debug.Log("Collision avec un danger ! Santé -10");
        }
    }
}
