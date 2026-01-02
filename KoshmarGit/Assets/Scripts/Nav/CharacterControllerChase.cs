using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterControllerChase : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public string playerTag = "Player";

    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float acceleration = 8f;
    public float rotationSpeed = 10f;
    public float stoppingDistance = 1.5f;
    public float gravity = -9.81f;

    [Header("Detection")]
    public float detectionRadius = 12f;
    public LayerMask obstacleMask;                // solids that block movement (do NOT include Player)
    [Tooltip("Distance in front of the CharacterController to check for blocking obstacles")]
    public float blockCheckDistance = 0.22f;
    [Tooltip("How long (seconds) enemy remains blocked after hit to avoid instant re-trying")]
    public float blockedCooldown = 0.35f;

    [Header("Combat")]
    public float pushForce = 5f;
    public float pushCooldown = 1f;
    public float damageAmount = 10f;
    public float damageCooldown = 1f;

    [Header("Model Fix")]
    public float modelRotationOffsetY = 180f;

    [Header("Animation")]
    public Animator animator;
    public string moveFloat = "moveSpeed";
    public string attackTrigger = "attack";

    // Internal
    private CharacterController controller;
    private float currentSpeed;
    private float verticalVelocity;
    private float lastPushTime;
    private float lastDamageTime;

    // Hard block state
    private bool hardBlocked;
    private float blockedUntilTime;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) target = p.transform;
        }
    }

    void Update()
    {
        if (!target) return;

        // Clear hardBlocked when cooldown expired
        if (Time.time >= blockedUntilTime) hardBlocked = false;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        Vector3 direction = (distance > 0.0001f) ? toTarget.normalized : Vector3.zero;

        bool canSeeTarget = HasLineOfSight();
        bool inDetectionRange = distance <= detectionRadius && canSeeTarget;
        bool inAttackRange = distance <= stoppingDistance && canSeeTarget;

        // ROTATION (always try to face)
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRot =
                Quaternion.LookRotation(direction) *
                Quaternion.Euler(0f, modelRotationOffsetY, 0f);

            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
        }

        // HARD BLOCK CHECK (CapsuleCast ahead)
        // Do not perform check if already hard-blocked (we'll wait the cooldown)
        if (!hardBlocked)
        {
            if (IsBlockedAhead())
            {
                // Immediately stop forward movement and mark blocked
                hardBlocked = true;
                blockedUntilTime = Time.time + blockedCooldown;
                currentSpeed = 0f;
            }
        }

        // MOVEMENT: only move forward when not hard-blocked and inside detection range (and not attacking)
        if (!hardBlocked && inDetectionRange && !inAttackRange)
        {
            float targetSpeed = Mathf.Lerp(walkSpeed, runSpeed, Mathf.Clamp01(distance / detectionRadius));
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, acceleration * Time.deltaTime);
        }

        // GRAVITY: always apply so we don't float
        if (controller.isGrounded)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        // Compose move vector
        Vector3 move = direction * currentSpeed;
        move.y = verticalVelocity;

        // If hard-blocked, prevent horizontal movement completely (still apply vertical)
        if (hardBlocked)
        {
            move.x = 0f;
            move.z = 0f;
        }

        controller.Move(move * Time.deltaTime);

        // ANIMATION
        if (animator)
            animator.SetFloat(moveFloat, currentSpeed / runSpeed);

        // ATTACK / PUSH / DAMAGE — only if not hard-blocked
        if (!hardBlocked && inAttackRange && canSeeTarget)
        {
            if (animator && !string.IsNullOrEmpty(attackTrigger))
                animator.SetTrigger(attackTrigger);

            Collider[] hits = Physics.OverlapSphere(transform.position, stoppingDistance);
            foreach (var hit in hits)
            {
                // PUSH
                PlayerPushable pushable = hit.GetComponent<PlayerPushable>();
                if (pushable && Time.time - lastPushTime >= pushCooldown)
                {
                    Vector3 pushDir = (hit.transform.position - transform.position).normalized;
                    pushable.Push(pushDir, pushForce);
                    lastPushTime = Time.time;
                }

                // DAMAGE
                HealthBar health = hit.GetComponent<HealthBar>();
                if (health && Time.time - lastDamageTime >= damageCooldown)
                {
                    health.TakeDamage(damageAmount);
                    lastDamageTime = Time.time;
                }
            }
        }
    }

    /// <summary>
    /// Uses the CharacterController's capsule to capsule-cast forward a short distance to detect blocking obstacles
    /// and return true if something solid (in obstacleMask) will block immediate forward motion.
    /// </summary>
    private bool IsBlockedAhead()
    {
        if (controller == null) return false;

        // CharacterController geometry
        float radius = controller.radius;
        float halfHeight = Mathf.Max(0.01f, controller.height * 0.5f);
        Vector3 center = transform.position + controller.center;

        // bottom and top points for capsule (slightly inset by radius)
        Vector3 p1 = center + Vector3.up * (-halfHeight + radius);
        Vector3 p2 = center + Vector3.up * (halfHeight - radius);

        // Direction and distance to test
        Vector3 dir = transform.forward;
        float castDist = Mathf.Max(0.01f, blockCheckDistance);

        // Perform capsulecast
        if (Physics.CapsuleCast(p1, p2, radius, dir, out RaycastHit hit, castDist, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            // Avoid considering the player as an obstacle
            if (hit.transform == target) return false;

            // If hit normal is ground-like, ignore
            if (hit.normal.y > 0.5f) return false;

            return true;
        }

        return false;
    }

    // Keep OnControllerColliderHit as a safety (it will set hardBlocked if a frontal collision occurs during Move).
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y > 0.5f) return; // ignore ground hits

        // If we hit something in front → hard block
        float dot = Vector3.Dot(hit.normal, -transform.forward);
        if (dot > 0.5f)
        {
            hardBlocked = true;
            blockedUntilTime = Time.time + blockedCooldown;
            currentSpeed = 0f;
        }
    }

    // Raycast LOS (unchanged)
    bool HasLineOfSight()
    {
        if (target == null) return false;

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 targetPos = target.position + Vector3.up * 0.5f;
        Vector3 dir = (targetPos - origin).normalized;
        float dist = Vector3.Distance(origin, targetPos);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != target)
                return false;
        }

        return true;
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            // attempt to fetch controller for drawing if in editor
            if (controller == null) controller = GetComponent<CharacterController>();
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);

        // Draw capsule cast preview
        if (controller != null)
        {
            float radius = controller.radius;
            float halfHeight = Mathf.Max(0.01f, controller.height * 0.5f);
            Vector3 center = transform.position + controller.center;
            Vector3 p1 = center + Vector3.up * (-halfHeight + radius);
            Vector3 p2 = center + Vector3.up * (halfHeight - radius);

            Gizmos.color = hardBlocked ? Color.magenta : Color.cyan;
            Gizmos.DrawLine(p1, p1 + transform.forward * blockCheckDistance);
            Gizmos.DrawLine(p2, p2 + transform.forward * blockCheckDistance);
            // small spheres at ends
            Gizmos.DrawSphere(p1 + transform.forward * blockCheckDistance, radius * 0.05f);
            Gizmos.DrawSphere(p2 + transform.forward * blockCheckDistance, radius * 0.05f);
        }
    }
}
