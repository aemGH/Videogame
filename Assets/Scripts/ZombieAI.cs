using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class ZombieAI : MonoBehaviour
{
    private enum State { Idle, Patrol, Chase_Direct, Chase_Pathfinding, Attack }

    [Header("Detection & Movement")]
    [SerializeField] private float _detectionRadius = 15f;
    [SerializeField] private float _attackRadius = 2f;
    
    [Header("Vision & Behavior")]
    [SerializeField] private float _fieldOfViewAngle = 140f;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private float _patrolSpeed = 1.5f;
    [SerializeField] private float _chaseSpeed = 4f;
    [SerializeField] private float _patrolRadius = 10f;
    [SerializeField] private float _patrolWaitTime = 3f;
    [Tooltip("How long the zombie remembers you after breaking line of sight")]
    [SerializeField] private float _memoryDuration = 6.0f;

    [Header("Zombie Characteristics")]
    [Tooltip("Distance at which the zombie starts reaching and slows down")]
    [SerializeField] private float _slowDownRadius = 5f;
    [Tooltip("Minimum speed when right next to the player")]
    [SerializeField] private float _minChaseSpeed = 1.5f;
    [Tooltip("How fast the zombie can turn. Lower = clunkier/zombie-like")]
    [SerializeField] private float _turnSpeed = 2.5f;
    [Tooltip("Adds a stumbling speed variation")]
    [SerializeField] private float _stumbleFrequency = 3f;
    [SerializeField] private float _stumbleMagnitude = 0.5f;
    [Tooltip("How fast the zombie accelerates to top speed")]
    [SerializeField] private float _acceleration = 0.5f;
    [Tooltip("How much momentum is lost when the zombie has to turn")]
    [SerializeField] private float _turnDeceleration = 2.0f;

    [Header("Animation")]
    [SerializeField] private string _velocityParamName = "velocity";
    [SerializeField] private float _dampTime = 0.1f;

    private NavMeshAgent _agent;
    private Animator _animator;
    private Transform _target;
    private State _currentState = State.Idle;
    
    [Header("Combat Settings")]
    public float attackDamage = 15f;

    private float _patrolTimer;
    private float _timeSinceLastSawPlayer = 999f;
    private float _repathTimer;
    private float _attackTimer = 0f;
    [SerializeField] private float _attackCooldown = 1.5f;
    private float _currentMoveSpeed = 0f;
    private bool _isDead = false;
    private bool _isCrawling = false;

    // Animator Hashes for performance
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int FakeDeathHash = Animator.StringToHash("FakeDeath");
    private static readonly int IsCrawlingHash = Animator.StringToHash("IsCrawling");

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        if (_animator != null) _animator.applyRootMotion = false;
        
        // Disable automatic NavMesh rotation so we can manually apply our custom clunky turning
        _agent.updateRotation = false;
    }

    private void Start()
    {
        FindTarget();
    }

    private void Update()
    {
        if (_isDead) return;

        if (_isCrawling)
        {
            // Update animator parameter
            _animator.SetBool(IsCrawlingHash, true);
        }

        if (_target == null)
        {
            FindTarget();
            UpdateState(State.Idle);
            return;
        }

        float distance = Vector3.Distance(transform.position, _target.position);
        bool hasLineOfSight = CanSeePlayer(distance);

        if (hasLineOfSight) 
            _timeSinceLastSawPlayer = 0f;
        else 
            _timeSinceLastSawPlayer += Time.deltaTime;

        bool hasMemoryOfPlayer = _timeSinceLastSawPlayer < _memoryDuration;

        // State Machine transitions
        State nextState = DetermineNextState(distance, hasLineOfSight, hasMemoryOfPlayer);
        UpdateState(nextState);

        HandleStateBehavior(distance);
        UpdateAnimation();
    }

    private State DetermineNextState(float distance, bool hasLineOfSight, bool hasMemory)
    {
        // Hysteresis buffer to prevent flicker at the exact edge of attack radius
        if (_currentState == State.Attack && distance <= _attackRadius + 0.5f) return State.Attack;
        if (distance <= _attackRadius) return State.Attack;
        
        // The core of the strong system: Separate direct pursuit from complex pathfinding
        if (hasLineOfSight) return State.Chase_Direct;
        if (hasMemory) return State.Chase_Pathfinding;
            
        return State.Patrol;
    }

    private void FindTarget()
    {
        CharacterController playerController = Object.FindFirstObjectByType<CharacterController>();
        if (playerController != null && playerController.gameObject != this.gameObject)
        {
            _target = playerController.transform;
        }
    }

    private bool CanSeePlayer(float distanceToTarget)
    {
        if (_target == null || distanceToTarget > _detectionRadius) return false;

        Vector3 rayStart = transform.position + Vector3.up;
        Vector3 targetCenter = _target.position + Vector3.up;
        Vector3 directionToTarget = (targetCenter - rayStart).normalized;

        Vector3 flatDir = new Vector3(directionToTarget.x, 0, directionToTarget.z);
        Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z);

        float currentFOV = (_currentState == State.Chase_Direct || _currentState == State.Chase_Pathfinding) ? 360f : _fieldOfViewAngle;
        if (Vector3.Angle(flatForward, flatDir) > currentFOV / 2f) 
            return false;

        if (Physics.Raycast(rayStart, directionToTarget, out RaycastHit hit, distanceToTarget, _obstacleMask))
        {
            return hit.transform == _target || hit.transform.IsChildOf(_target);
        }

        return true; 
    }

    private void UpdateState(State newState)
    {
        if (_currentState == newState) return;

        _currentState = newState;

        switch (_currentState)
        {
            case State.Patrol:
                _agent.ResetPath(); // Stop moving towards old targets
                _agent.speed = _patrolSpeed;
                _agent.stoppingDistance = 0f;
                _agent.autoBraking = true;
                break;

            case State.Chase_Direct:
            case State.Chase_Pathfinding:
                _agent.speed = _chaseSpeed;
                _agent.stoppingDistance = _attackRadius * 0.8f;
                _agent.autoBraking = false; 
                _repathTimer = 0f; // Force immediate path calculation!
                break;

            case State.Attack:
                _animator.SetTrigger(AttackHash); // Fire attack animation immediately
                _attackTimer = _attackCooldown;   // Reset cooldown so we don't double-attack
                _currentMoveSpeed = 0f; // Reset momentum
                _agent.ResetPath(); 
                _agent.velocity = Vector3.zero;
                _agent.autoBraking = true;
                break;

            case State.Idle:
                _currentMoveSpeed = 0f; // Reset momentum
                _agent.ResetPath(); 
                _agent.velocity = Vector3.zero;
                _agent.autoBraking = true;
                break;
        }
    }

    private void HandleStateBehavior(float distance)
    {
        if (!_agent.isOnNavMesh || _target == null) return;

        switch (_currentState)
        {
            case State.Chase_Direct:
                _agent.ResetPath();
                
                // Determine base target speed based on distance
                float targetSpeed = _chaseSpeed;
                if (distance <= _slowDownRadius && distance > _attackRadius)
                {
                    float t = (distance - _attackRadius) / (_slowDownRadius - _attackRadius);
                    targetSpeed = Mathf.Lerp(_minChaseSpeed, _chaseSpeed, t);
                }

                Vector3 directionToTarget = (_target.position - transform.position).normalized;

                // Zombie Behavior: Lose momentum if turning too sharply to face the player
                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    float turnAngle = Vector3.Angle(transform.forward, directionToTarget);
                    if (turnAngle > 5f)
                    {
                        // Deduct speed proportional to the severity of the turn
                        _currentMoveSpeed -= (turnAngle / 180f) * _turnDeceleration * Time.deltaTime;
                        _currentMoveSpeed = Mathf.Max(0f, _currentMoveSpeed);
                    }
                }

                // Zombie Behavior: Slow acceleration (effort to build up speed)
                _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, targetSpeed, _acceleration * Time.deltaTime);

                // Zombie Behavior: Stumbling (varying speed over time)
                float stumble = Mathf.Sin(Time.time * _stumbleFrequency) * _stumbleMagnitude;
                float finalSpeed = Mathf.Max(0.1f, _currentMoveSpeed + stumble); // Keep at least a tiny bit of speed if stumbling

                _agent.velocity = directionToTarget * finalSpeed;
                
                // Zombie Behavior: Clunky, slower turning
                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * _turnSpeed);
                }
                break;

            case State.Chase_Pathfinding:
                _repathTimer -= Time.deltaTime;
                if (_repathTimer <= 0f || !_agent.hasPath)
                {
                    _agent.SetDestination(_target.position);
                    _repathTimer = 0.5f;
                }

                // Apply same logic to pathfinding chase
                float pathTargetSpeed = _chaseSpeed;
                if (distance <= _slowDownRadius && distance > _attackRadius)
                {
                    float t = (distance - _attackRadius) / (_slowDownRadius - _attackRadius);
                    pathTargetSpeed = Mathf.Lerp(_minChaseSpeed, _chaseSpeed, t);
                }
                
                // Lose momentum on pathfinding corners
                Vector3 desiredDir = _agent.desiredVelocity.normalized;
                if (desiredDir.sqrMagnitude > 0.01f)
                {
                    float pathTurnAngle = Vector3.Angle(transform.forward, desiredDir);
                    if (pathTurnAngle > 5f)
                    {
                        _currentMoveSpeed -= (pathTurnAngle / 180f) * _turnDeceleration * Time.deltaTime;
                        _currentMoveSpeed = Mathf.Max(0f, _currentMoveSpeed);
                    }

                    // Manually turn toward path
                    Quaternion lookRot = Quaternion.LookRotation(new Vector3(desiredDir.x, 0, desiredDir.z));
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * _turnSpeed);
                }

                _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, pathTargetSpeed, _acceleration * Time.deltaTime);

                float pathStumble = Mathf.Sin(Time.time * _stumbleFrequency) * _stumbleMagnitude;
                _agent.speed = Mathf.Max(0.1f, _currentMoveSpeed + pathStumble);
                break;

            case State.Patrol:
                // Smooth acceleration for patrol as well to prevent snapping to speed
                _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, _patrolSpeed, _acceleration * Time.deltaTime);
                _agent.speed = _currentMoveSpeed;

                // Zombie Behavior: Clunky turning during patrol too
                Vector3 patrolDir = _agent.desiredVelocity.normalized;
                if (patrolDir.sqrMagnitude > 0.01f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(new Vector3(patrolDir.x, 0, patrolDir.z));
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * _turnSpeed);
                }

                if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.1f)
                {
                    _patrolTimer += Time.deltaTime;
                    if (_patrolTimer >= _patrolWaitTime)
                    {
                        Vector3 randomDirection = Random.insideUnitSphere * _patrolRadius;
                        randomDirection += transform.position;
                        
                        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, _patrolRadius, NavMesh.AllAreas))
                        {
                            _agent.SetDestination(hit.position);
                            _patrolTimer = 0f;
                        }
                    }
                }
                break;

            case State.Attack:
                _agent.velocity = Vector3.zero;
                Vector3 directionToFace = (_target.position - transform.position).normalized;
                if (directionToFace.sqrMagnitude > 0.01f)
                {
                    Quaternion attackRot = Quaternion.LookRotation(new Vector3(directionToFace.x, 0, directionToFace.z));
                    // Slowly turn during attack, allowing the player to dodge/juke the zombie
                    transform.rotation = Quaternion.Slerp(transform.rotation, attackRot, Time.deltaTime * (_turnSpeed * 1.5f));
                }

                // Handle repeating attacks if the player stays in range
                _attackTimer -= Time.deltaTime;
                if (_attackTimer <= 0f)
                {
                    _animator.SetTrigger(AttackHash);
                    _attackTimer = _attackCooldown;
                    
                    // Infliger les dégâts au joueur
                    if (distance <= _attackRadius + 1f)
                    {
                        PlayerController player = _target.GetComponent<PlayerController>();
                        if (player != null)
                        {
                            player.TakeDamage(attackDamage);
                        }
                    }
                }
                break;
        }
    }

    private void UpdateAnimation()
    {
        float currentSpeed = _agent.velocity.magnitude;
        float normalizedSpeed = _chaseSpeed > 0 ? currentSpeed / _chaseSpeed : 0f;
        _animator.SetFloat(_velocityParamName, normalizedSpeed, _dampTime, Time.deltaTime);
    }

    /// <summary>
    /// Call this from a health script or player combat script when the zombie's health hits 0.
    /// </summary>
    public void TriggerDeath()
    {
        if (_isDead) return;
        _isDead = true;

        // Stop the zombie immediately
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
        _currentMoveSpeed = 0f;
        
        // Trigger the Death animation state
        _animator.SetTrigger(DeathHash);

        // Disable AI logic so it doesn't try to get up and chase again
        this.enabled = false;
    }

    /// <summary>
    /// Call this to make the zombie fall and start crawling instead of dying completely.
    /// </summary>
    public void TriggerFakeDeath()
    {
        if (_isDead || _isCrawling) return;
        
        _isCrawling = true;
        _animator.SetTrigger(FakeDeathHash);
        _animator.SetBool(IsCrawlingHash, true);

        // ADJUST COLLIDER FOR CRAWLING
        CapsuleCollider cap = GetComponent<CapsuleCollider>();
        if (cap != null) {
            cap.direction = 2; // Z-Axis (Horizontal)
            cap.height = 2.0f;
            cap.center = new Vector3(0, 0.3f, 0); // Close to ground
        }
        
        // Reduce speed and attack properties for crawling state
        _chaseSpeed *= 0.35f;
        _patrolSpeed *= 0.5f;
        _minChaseSpeed *= 0.5f;
        _attackCooldown *= 1.5f;
        _turnSpeed *= 0.5f;
        
        // Reset attack timer so it doesn't instantly bite while falling
        _attackTimer = 1.5f; 
        
        // Temporarily stop agent to let the fall animation play
        _agent.velocity = Vector3.zero;
        _currentMoveSpeed = 0f;
    }

    /// <summary>
    /// Manually force an attack (if called from an external combat manager)
    /// </summary>
    public void TriggerAttack()
    {
        if (_isDead) return;
        _animator.SetTrigger(AttackHash);
    }
}