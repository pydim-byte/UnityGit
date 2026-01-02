using Cinemachine;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI;

public class TPSController : MonoBehaviour
{
    public AudioClip GunShoot;
    public Transform GunPosition;

    public GameModeManager gameModeManager;
    public ParticleSystem muzzleFlash;
    public GameObject bulletHole;
    [SerializeField] private ParticleSystem bloodImpactVFX;
    [SerializeField] private CinemachineVirtualCamera aimVirtualCamera;
    [SerializeField] private float normalSensetivity = 1f;
    [SerializeField] private float aimSensetivity = 0.5f;
    [SerializeField] private LayerMask aimColliderLayerMask;
    [SerializeField] private Transform hitpoint;
    [SerializeField] private float rotateSpeed = 20f;

    [Header("Weapon Stats")]
    [SerializeField] private float weaponDamage = 1f;
    [SerializeField] private float weaponCritChance = 0f;
    [SerializeField] private float weaponCritMultiplier = 3f;

    public float WeaponDamage => weaponDamage;
    public float WeaponCritChance => weaponCritChance;

    [Header("Auto-Aim on Shot")]
    [SerializeField] private float aimHoldDuration = 0.6f; // how long to stay in aim after last shot
    [SerializeField] private float aimTurnSpeedMultiplier = 4f; // how quickly to snap toward aim when shot
    private float aimTimer = 0f;

    private ThirdPersonController thirdPersonController;
    private StarterAssetsInputs starterAssetsInput;

    private RaycastHit lastHit;
    private bool hasLastHit;

    // rotation-on-shoot state (kept for backward compat, but we cancel it when auto-aiming)
    private bool rotateFromShoot;
    private Vector3 shootTargetForward;

    private void Awake()
    {
        thirdPersonController = GetComponent<ThirdPersonController>();
        starterAssetsInput = GetComponent<StarterAssetsInputs>();
    }

    private void Update()
    {
        // tick down the auto-aim timer
        if (aimTimer > 0f)
            aimTimer -= Time.deltaTime;

        HandleRaycast();
        HandleAimRotation();
        HandleShootRotation();
        HandleShooting();

        if (starterAssetsInput.shop)
        {
            gameModeManager.OpenShop();
            starterAssetsInput.shop = false;
        }
    }

    // Called by shop to upgrade damage
    public void UpgradeDamage(float amount)
    {
        weaponDamage += amount;
    }

    // Called by shop to upgrade crit chance
    public void UpgradeCritChance(float amount)
    {
        weaponCritChance += amount;
        weaponCritChance = Mathf.Clamp(weaponCritChance, 0f, 100f);
    }

    // ---------------- RAYCAST ----------------
    private void HandleRaycast()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        hasLastHit = false;

        if (Physics.Raycast(ray, out RaycastHit hit, 999f, aimColliderLayerMask))
        {
            lastHit = hit;
            hasLastHit = true;

            if (hitpoint != null)
                hitpoint.position = hit.point;
        }
    }

    // ---------------- AIM ----------------
    private void HandleAimRotation()
    {
        // treat either player-aim input OR aim-timer (auto-aim) as "aiming"
        bool isAiming = starterAssetsInput.aim || aimTimer > 0f;

        if (!isAiming)
        {
            if (aimVirtualCamera != null)
                aimVirtualCamera.gameObject.SetActive(false);

            thirdPersonController.SetSensitivity(normalSensetivity);
            thirdPersonController.SetRotateOnMove(true);
            return;
        }

        // cancel shoot-rotation while aiming
        rotateFromShoot = false;

        if (aimVirtualCamera != null)
            aimVirtualCamera.gameObject.SetActive(true);

        thirdPersonController.SetSensitivity(aimSensetivity);
        thirdPersonController.SetRotateOnMove(false);

        // rotate toward camera forward (flattened)
        Vector3 camForward = Camera.main.transform.forward;
        camForward.y = 0f;

        if (camForward.sqrMagnitude > 0.001f)
        {
            transform.forward = Vector3.Slerp(
                transform.forward,
                camForward.normalized,
                Time.deltaTime * rotateSpeed
            );
        }
    }

    // ---------------- SHOOT ROTATION (continuous) ----------------
    private void HandleShootRotation()
    {
        // keep old rotateFromShoot behavior only when not auto-aiming or manually aiming
        if (!rotateFromShoot || starterAssetsInput.aim || aimTimer > 0f)
            return;

        Vector3 current = transform.forward;
        current.y = 0f;

        Vector3 target = shootTargetForward;
        target.y = 0f;

        transform.forward = Vector3.Slerp(
            current,
            target.normalized,
            Time.deltaTime * rotateSpeed
        );

        // stop rotating when aligned
        if (Vector3.Angle(current, target) < 1f)
        {
            transform.forward = target.normalized;
            rotateFromShoot = false;
        }
    }

    // ---------------- SHOOT ----------------
    private void HandleShooting()
    {
        if (!starterAssetsInput.shoot)
            return;

        DoShoot();

        // --- AUTO-ENTER AIM MODE on shooting ---
        aimTimer = aimHoldDuration;
        rotateFromShoot = false; // cancel the previous rotate-from-shoot since we're aiming now

        // Quick immediate turn toward camera/shoot direction so first shot feels like aim
        Vector3 quickDir;
        if (hasLastHit)
            quickDir = lastHit.point - transform.position;
        else
            quickDir = Camera.main.transform.forward;

        quickDir.y = 0f;
        if (quickDir.sqrMagnitude > 0.001f)
        {
            // do a faster slerp (multiplied speed) to make it feel snappy
            transform.forward = Vector3.Slerp(
                transform.forward,
                quickDir.normalized,
                Time.deltaTime * rotateSpeed * aimTurnSpeedMultiplier
            );
        }

        // reset shoot input
        starterAssetsInput.shoot = false;
    }

    private Vector3 GetShootDirection()
    {
        Vector3 dir;

        if (hasLastHit)
            dir = lastHit.point - transform.position;
        else
            dir = Camera.main.transform.forward;

        dir.y = 0f;
        return dir.sqrMagnitude > 0.001f ? dir.normalized : transform.forward;
    }

    // ---------------- DAMAGE / FX ----------------
    private void DoShoot()
    {
        AudioSource.PlayClipAtPoint(GunShoot, transform.position);

        if (muzzleFlash != null)
            muzzleFlash.Play();

        if (!hasLastHit || lastHit.transform == null)
            return;

        BulletTarget bulletTarget = lastHit.transform.GetComponent<BulletTarget>();

        if (bulletTarget != null)
        {
            EnemyHealth enemyHealth = lastHit.transform.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                float finalDamage = weaponDamage;

                bool isCrit = Random.value < (weaponCritChance / 100f);
                if (isCrit)
                {
                    finalDamage *= weaponCritMultiplier;
                }

                enemyHealth.TakeDamage(finalDamage);
            }

            if (bloodImpactVFX != null)
            {
                ParticleSystem blood = Instantiate(
                    bloodImpactVFX,
                    lastHit.point,
                    Quaternion.LookRotation(-lastHit.normal)
                );
                Destroy(blood.gameObject, 3f);
            }
        }
        else
        {
            float zFightOffset = 0.005f;
            Vector3 spawnPos = lastHit.point + lastHit.normal * zFightOffset;

            Quaternion rot = Quaternion.LookRotation(-lastHit.normal);
            rot *= Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            GameObject hole = Instantiate(bulletHole, spawnPos, rot);
            hole.transform.SetParent(lastHit.transform, true);
            Destroy(hole, 5f);
        }
    }
}