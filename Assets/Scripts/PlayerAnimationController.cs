using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerAnimationController : MonoBehaviour
{
    private Animator animator;
    private CharacterController controller;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (animator == null) return;

        // "IsMoving" : vérifie si le contrôleur bouge
        bool isMoving = controller.velocity.magnitude > 0.1f;
        animator.SetBool("IsMoving", isMoving);

        // "Jump" : déclenché quand le joueur ne touche plus le sol
        if (!controller.isGrounded)
        {
            animator.SetTrigger("Jump");
        }
    }
}