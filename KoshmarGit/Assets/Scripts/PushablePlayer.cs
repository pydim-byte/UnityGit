using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerPushable : MonoBehaviour
{
    private CharacterController cc;
    private Vector3 pushVelocity;

    [Header("Push Settings")]
    public float pushRecoverySpeed = 5f; // how fast player recovers from push

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Smoothly decay push velocity
        if (pushVelocity.magnitude > 0.01f)
        {
            cc.Move(pushVelocity * Time.deltaTime);
            pushVelocity = Vector3.Lerp(pushVelocity, Vector3.zero, pushRecoverySpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Called by enemy to push the player
    /// </summary>
    /// <param name="direction">Normalized push direction</param>
    /// <param name="force">Push force/speed</param>
    public void Push(Vector3 direction, float force)
    {
        direction.y = 0f;
        pushVelocity += direction.normalized * force;
    }
}
