using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class NetworkedGroundSlam : NetworkBehaviour
{
    public float slamRadius = 5f;
    public float throwForce = 10f;
    private playerMotor PlayerMotor;
    private playerController playerController;

    // [SerializeField]
    // private float delayPerUnit = 20f;
    public LayerMask playerLayer;
    public GameObject slamEffectPrefab;
    public GameObject ShockWaveVFX;

    public Animator animator;
    public Rig handRig;

    private bool isFlyingKneeActive = false;
    private float jumpStartY;
    public float offset;

    private void Start()
    {
        PlayerMotor = gameObject.GetComponent<playerMotor>();
        playerController = gameObject.GetComponent<playerController>();
    }

    private void Update()
    {
        if (!isOwned)
            return;

        if (Input.GetKeyDown(KeyCode.J) && !isFlyingKneeActive)
        {
            StartCoroutine(PerformSlamWithDelay());
        }
    }

    IEnumerator PerformSlamWithDelay()
    {
        Debug.Log("[Slam] Starting Ground Slam");

        jumpStartY = transform.position.y;
        isFlyingKneeActive = true;

        // Disable movement
        // if (PlayerMotor != null)
        //     PlayerMotor.enabled = false;
        if (playerController != null)
            playerController.isLocked = true;

        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        rb.isKinematic = true; // stop physics movement
        animator.applyRootMotion = true; // let animation control motion

        if (isServer)
            RpcPlaySlamAnimation();
        else if (isOwned)
            CmdPlaySlamAnimation();

        Debug.Log($"[Slam] Triggered animation 'GroundSlam', start Y={jumpStartY}");

        // Wait until animation starts
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("GroundSlam"))
            yield return null;

        Debug.Log("[Slam] Animation has started");

        // Wait for animation to finish
        float animationLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animationLength);

        Debug.Log("[Slam] Animation finished playing");

        // Re-enable movement
        // if (PlayerMotor != null)
        //     PlayerMotor.enabled = true;
        if (playerController != null)
            playerController.isLocked = false;

        rb.isKinematic = false;
        animator.applyRootMotion = false;
        isFlyingKneeActive = false;

        Vector3 fixedPosition = transform.position;
        fixedPosition.y = jumpStartY;
        transform.position = fixedPosition;

        if (isServer)
            RpcSetRigWeight();
        else if (isOwned)
            CmdSetRigWeight();

        Debug.Log("[Slam] Ground Slam coroutine completed");
    }

    IEnumerator LerpHandRigWeight(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            handRig.weight = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            Vector3 fixedPosition = transform.position;
            fixedPosition.y = jumpStartY;
            transform.position = fixedPosition;
            yield return null;
        }
        handRig.weight = to;
    }

    IEnumerator LerpHandRigWeightOtherPlayers(float from, float to, float duration, Rig rig)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            rig.weight = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rig.weight = to;
    }

    [Command]
    void CmdPlaySlamAnimation()
    {
        RpcPlaySlamAnimation();
    }

    [ClientRpc]
    void RpcPlaySlamAnimation()
    {
        handRig.weight = 0f;
        animator.SetTrigger("GroundSlam");
    }

    [Command]
    void CmdSetRigWeight()
    {
        RpcSetRigWeight();
    }

    [ClientRpc]
    void RpcSetRigWeight()
    {
        StartCoroutine(LerpHandRigWeight(handRig.weight, 1f, 0.5f));
    }

    [Command]
    void CmdRequestServerToPerformSlam()
    {
        PerformSlamOnServer();
    }

    [Server]
    void PerformSlamOnServer()
    {
        Debug.Log("[Slam] Performing slam on server...");

        RpcSpawnSlamEffect(transform.position + Vector3.up * offset);

        Collider[] hitPlayers = Physics.OverlapSphere(transform.position, slamRadius);
        Debug.Log($"[Slam] Players in radius: {hitPlayers.Length}");

        foreach (Collider player in hitPlayers)
        {
            Debug.Log($"[Slam] Checking collider: {player.name}");

            if (!player.CompareTag("Player"))
            {
                Debug.Log($"[Slam] Skipping {player.name}, not a player");
                continue;
            }

            if (player.gameObject == gameObject)
            {
                Debug.Log($"[Slam] Skipping self: {player.name}");
                continue;
            }

            if (player.TryGetComponent<NetworkIdentity>(out var identity))
            {
                Debug.Log($"[Slam] Found NetworkIdentity on {player.name}");
                TargetApplyKnockback(identity.connectionToClient, player.gameObject, slamRadius);
            }
            else
            {
                Debug.Log($"[Slam] No NetworkIdentity found on {player.name}");
            }
        }
    }

    [ClientRpc]
    void RpcSpawnSlamEffect(Vector3 position)
    {
        if (slamEffectPrefab != null)
        {
            GameObject effect = Instantiate(slamEffectPrefab, position, Quaternion.Euler(90, 0, 0));
            GameObject shockWave = Instantiate(ShockWaveVFX, position, Quaternion.identity);
            Destroy(effect, 3f);
            Destroy(shockWave, 3f);
        }

        if (CameraShake.instance != null)
            CameraShake.instance.ShakeOnPower();
    }

    public void OnSlamImpact()
    {
        if (isOwned)
        {
            CmdRequestServerToPerformSlam();
        }
    }

    [TargetRpc]
    void TargetApplyKnockback(NetworkConnection target, GameObject playerObject, float slamRadius)
    {
        Debug.Log($"[Knockback] TargetApplyKnockback called for {playerObject.name}");

        if (!playerObject.CompareTag("Player"))
        {
            Debug.Log($"[Knockback] {playerObject.name} is not a player, skipping");
            return;
        }

        if (playerObject.TryGetComponent<NetworkIdentity>(out var identity))
        {
            Debug.Log($"[Knockback] Found NetworkIdentity on {playerObject.name}");
            Vector3 direction = (playerObject.transform.position - transform.position).normalized;
            float distanceFromPlayerToEnemies = Vector3.Distance(
                transform.position,
                playerObject.transform.position
            );
            if (direction == Vector3.zero)
            {
                direction = playerObject.transform.forward;
            }

            float jumpStartY1 = playerObject.transform.position.y;
            float droppedForce = ForceDropOff(slamRadius, distanceFromPlayerToEnemies);
            CmdApplyKnockback(identity.netId, direction, jumpStartY1, droppedForce);
        }
        else
        {
            Debug.Log($"[Knockback] No NetworkIdentity on {playerObject.name}");
        }
    }

    [Command(requiresAuthority = false)]
    void CmdApplyKnockback(uint netid, Vector3 direction, float jumpStartY, float droppedForce)
    {
        RpcPlayKnockbackCombo(netid, direction, jumpStartY, droppedForce);
    }

    [ClientRpc]
    void RpcPlayKnockbackCombo(
        uint playerNetId,
        Vector3 direction,
        float jumpStartY,
        float droppedForce
    )
    {
        StartCoroutine(DelayedKnockbackSetup(playerNetId, direction, jumpStartY, droppedForce));
    }

    IEnumerator DelayedKnockbackSetup(
        uint playerNetId,
        Vector3 direction,
        float jumpStartY,
        float droppedForce
    )
    {
        // Wait a few frames so that the object is fully initialized on the client
        yield return new WaitForSeconds(0.1f);

        if (NetworkClient.spawned.TryGetValue(playerNetId, out NetworkIdentity identity))
        {
            var playerObj = identity.gameObject;
            Debug.Log($"[Knockback] RpcPlayKnockbackCombo executing for {playerObj.name}");

            if (
                playerObj.TryGetComponent<Rigidbody>(out var rb)
                && playerObj.TryGetComponent<Animator>(out var anim)
                && playerObj.TryGetComponent<RigBuilder>(out var rigBuild)
            )
            {
                float distance = Vector3.Distance(playerObj.transform.position, transform.position);

                // yield return new WaitForSeconds(distance * delayPerUnit);
                rb.AddForce(direction * droppedForce, ForceMode.Impulse);
                StartCoroutine(
                    SetupKnockbackAnim(
                        rb,
                        anim,
                        rigBuild.layers[0].rig,
                        direction,
                        jumpStartY,
                        droppedForce
                    )
                );
            }
            else
            {
                Debug.LogError(
                    $"[Knockback] Missing Rigidbody, Animator, or Rig on {playerObj.name}"
                );
            }
        }
        else
        {
            Debug.LogError($"[Knockback] Player with netId {playerNetId} not found on client");
        }
    }

    IEnumerator SetupKnockbackAnim(
        Rigidbody rb,
        Animator anim,
        Rig rig,
        Vector3 direction,
        float jumpStartY,
        float droppedForce
    )
    {
        var motor = rb.gameObject.GetComponent<playerMotor>();
        var controller = rb.gameObject.GetComponent<playerController>();
        if (motor != null && controller != null)
        {
            controller.isLocked = true;
        }
        Debug.Log("[Knockback] Starting knockback animation sequence");

        // Step 1: Apply physical knockback force
        rb.isKinematic = false;
       
        rb.AddForce(direction * droppedForce, ForceMode.Impulse);
        Debug.Log($"[Knockback] Force applied: {direction * droppedForce}");

        yield return new WaitForSeconds(0.25f); // Allow physics to apply force

        // Step 2: Start animation control
        // rb.isKinematic = true; // Freeze physics to allow root motion
        anim.applyRootMotion = true;
        rig.weight = 0f;
        anim.SetTrigger("Knockback");
        Debug.Log("[Knockback] Knockback animation triggered with root motion");

        yield return new WaitForSeconds(8f); // Wait for animation to complete

        // Step 3: Restore rig and state
        yield return LerpHandRigWeightOtherPlayers(0f, 1f, 0.5f, rig);
        anim.applyRootMotion = false;
        rig.weight = 1f;

        // IMPORTANT: Restore physics before restoring position
        // rb.isKinematic = false;

        // Don’t forcibly reset Y position unless sinking or glitching
        // (Removing fixedPosition logic unless absolutely necessary)
        Debug.Log("[Knockback] Physics restored, ready for normal movement");

        Debug.Log("[Knockback] Knockback sequence finished");
        if (motor != null && controller != null)
        {
            //motor.enabled = true;
            controller.isLocked = false;
        }
    }

    private float ForceDropOff(float slamRadius, float Distance)
    {
        float t = Mathf.InverseLerp(0f, slamRadius, Distance);
        float droppedOffForce = Mathf.Lerp(throwForce, 0, t);
        return droppedOffForce;
    }
}
