using UnityEngine;

public class StationaryRangedEnemy : MonoBehaviour
{
    [Header("Target")]
    public string playerTag = "Player";

    private Transform target;
    private Transform targetAimPoint;

    [Header("Rotation")]
    public float modelRotationOffsetY = 180f;
    public float rotationSpeed = 5f;

    [Header("Attack")]
    public float detectionRadius = 15f;
    public float attackRange = 10f;
    public float attackCooldown = 2f;
    public GameObject projectilePrefab;
    public float projectileSpeed = 15f;
    public Transform projectileSpawnPoint;

    [Header("Animation")]
    public Animator animator;
    public string attackTrigger = "attack";

    private float lastAttackTime;

    void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            target = player.transform;

            // Try to find an AimPoint child
            Transform aim = player.transform.Find("AimPoint");
            if (aim != null)
                targetAimPoint = aim;
        }
    }

    void Update()
    {
        if (!target) return;

        Vector3 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;

        bool inDetectionRange = distance <= detectionRadius;
        bool inAttackRange = distance <= attackRange;

        // ROTATION
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRot =
                Quaternion.LookRotation(toTarget.normalized) *
                Quaternion.Euler(0f, modelRotationOffsetY, 0f);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                lookRot,
                rotationSpeed * Time.deltaTime
            );
        }

        if (inDetectionRange && inAttackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            ShootProjectile();
            lastAttackTime = Time.time;

            if (animator && !string.IsNullOrEmpty(attackTrigger))
                animator.SetTrigger(attackTrigger);
        }
    }

    void ShootProjectile()
    {
        if (!projectilePrefab || !projectileSpawnPoint) return;

        Vector3 aimPoint =
            targetAimPoint != null
            ? targetAimPoint.position
            : target.position + Vector3.up * 1.5f;

        Vector3 dir = (aimPoint - projectileSpawnPoint.position).normalized;

        GameObject proj = Instantiate(
            projectilePrefab,
            projectileSpawnPoint.position,
            Quaternion.LookRotation(dir)
        );

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.velocity = dir * projectileSpeed;
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
