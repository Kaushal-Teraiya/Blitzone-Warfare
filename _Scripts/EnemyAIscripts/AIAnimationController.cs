using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class AIAnimationController : NetworkBehaviour
{
    private Animator animator;
    [HideInInspector]
    public float currentStrafe = 0f;
    private NavMeshAgent agent;

    // [SyncVar(hook = nameof(OnMoveSpeedChanged))]
    // private float syncedMoveSpeed;

    // [SyncVar(hook = nameof(OnStrafeSpeedChanged))]
    // private float syncedStrafeSpeed;

    // [SyncVar(hook = nameof(OnJumpStateChanged))]
    // private bool syncedIsJumping;

    void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

    }
    void Update()
    {
        if (!isServer)
            return;

        Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);

        float moveSpeed = 0f;

        if (localVelocity.z > 0.1f)
            moveSpeed = Mathf.Clamp(localVelocity.z / agent.speed, 0.6f, 1f);
        else if (localVelocity.z < -0.1f)
            moveSpeed = Mathf.Clamp(localVelocity.z / agent.speed, -1f, -0.6f);

        bool isJumping = false;

        RpcUpdateAnimation(moveSpeed, currentStrafe, isJumping);
    }

    [ClientRpc]
    void RpcUpdateAnimation(float moveSpeed, float strafe, bool isJumping)
    {
        animator.SetFloat("speed", moveSpeed);
        animator.SetFloat("Strafe", strafe);
        animator.SetBool("IsJumping", isJumping);
    }

    void OnMoveSpeedChanged(float oldValue, float newValue)
    {
        animator.SetFloat("speed", newValue);
    }

    void OnStrafeSpeedChanged(float oldValue, float newValue)
    {
        animator.SetFloat("Strafe", newValue);
    }

    void OnJumpStateChanged(bool oldValue, bool newValue)
    {
        animator.SetBool("IsJumping", newValue);
    }
}
