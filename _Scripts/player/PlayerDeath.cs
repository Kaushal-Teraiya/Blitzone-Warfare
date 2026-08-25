using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerDeath : NetworkBehaviour
{
    [SyncVar]
    private bool _isDead = false;

    public bool isDead
    {
        get { return _isDead; }
        protected set { _isDead = value; }
    }

    [SerializeField]
    private GameObject PlayerRagdoll;

    [SerializeField]
    private GameObject deathEffect;

    [SerializeField]
    private GameObject spawnEffect;

    [SerializeField]
    private Behaviour[] disableOnDeath;

    [SerializeField]
    private GameObject[] disableGameObjectsOnDeath;

    [SerializeField]
    private GameObject healthDropPrefab;

    [SerializeField]
    private GameObject PlayerNameHolder;

    [SerializeField]
    private GameObject HealthBarUIobj;

    [SerializeField]
    private CapsuleCollider mainCollider;

    private bool[] wasEnabled;
    private bool firstSetup = true;
    private RagdollManager ragdollManager;
    private FlagHandler flagHandler;
    private playerShoot playerShoot;
    private Rigidbody playerRigidbody;
    private CharacterSpawner spawner;
    private FlagHandler FHD;

    public Vector3 storedHitPoint;
    public Vector3 storedHitDirection;
    public string storedHitBodyPartName;

    private NetworkMatchPlayer MatchPlayer
    {
        get
        {
            NetworkIdentity identity = null;

            if (NetworkServer.active)
                NetworkServer.spawned.TryGetValue(GetComponent<player>().matchPlayerNetId, out identity);
            else
                NetworkClient.spawned.TryGetValue(GetComponent<player>().matchPlayerNetId, out identity);

            return identity != null
                ? identity.GetComponent<NetworkMatchPlayer>()
                : null;
        }
    }

    private void Start()
    {
        ragdollManager = GetComponent<RagdollManager>();
        flagHandler = GetComponent<FlagHandler>();
        playerShoot = GetComponent<playerShoot>();
        mainCollider = GetComponent<CapsuleCollider>();
        playerRigidbody = GetComponent<Rigidbody>();
        FHD = GetComponent<FlagHandler>();
        spawner = FindAnyObjectByType<CharacterSpawner>();
    }

    [Server]
    public void ServerDie(
        NetworkIdentity killerId,
        Vector3 hitPoint,
        Vector3 hitDirection,
        string hitBodyPartName)
    {
        if (isDead)
            return;

        isDead = true;
        LogPlayerState("DIE START");
        playerRigidbody.isKinematic = true;

        Debug.Log(
            $"<color=yellow>[Die()]</color> <color=green>killerId:</color> <color=cyan>{killerId?.netId} ({killerId?.name})</color> | <color=red>This Player:</color> <color=cyan>{GetComponent<NetworkIdentity>()?.netId}</color>"
        );

        // Resolve killer
        player killerPlayer = null;
        NetworkMatchPlayer killerMatchPlayer = null;

        if (killerId != null && killerId != GetComponent<NetworkIdentity>())
        {
            killerPlayer = killerId.GetComponent<player>();

            if (killerPlayer != null)
            {
                killerMatchPlayer = killerPlayer.MatchPlayer;
            }
        }

        // Update K/D stats
        UpdateKillDeathStats(killerMatchPlayer);

        // Handle kill feed and colored names
        HandleKillFeed(killerMatchPlayer);

        // Disable animations and rigging
        DisableAnimationAndRigging();

        // Drop flag if holding
        DropFlagOnDeath();

        // Disable colliders and gameplay
        DisableGameplay();

        // Instantiate death effect
        if (deathEffect != null)
        {
            GameObject _gfxInstance = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(_gfxInstance, 3f);
        }

        // Hide UI
        PlayerNameHolder.SetActive(false);
        HealthBarUIobj.SetActive(false);

        // Enable ragdoll
        if (ragdollManager != null)
        {
            ragdollManager.EnableRagdoll();
        }

        // Notify victim's client
        if (isServer && connectionToClient != null)
        {
            TargetOnDeath(connectionToClient);
        }

        // Store hit info for ragdoll physics
        storedHitPoint = hitPoint;
        storedHitDirection = hitDirection;
        storedHitBodyPartName = hitBodyPartName;

        // Update health UI
        GetComponent<PlayerHealth>()?.UpdateHealth(0);

        // Clear weapon ammo
        player playerComponent = GetComponent<player>();
        if (playerComponent != null && playerComponent.currentWeapon != null)
        {
            playerComponent.currentWeapon.currentAmmo = 0;
        }

        // Cancel shooting
        if (playerShoot != null)
        {
            playerShoot.CancelInvoke("Shoot");
        }

        // Spawn health drop
        if (isServer)
            CmdSpawnHealthDrop();

        // Rotate player
        transform.rotation = Quaternion.Euler(
            90f,
            transform.rotation.eulerAngles.y,
            transform.rotation.eulerAngles.z
        );

        StartCoroutine(Respawn());
    }

    [Command]
    public void CmdSuicide()
    {
        if (isDead)
            return;

        PlayerHealth health = GetComponent<PlayerHealth>();

        if (health != null)
            health.currentHealth = 0;

        ServerDie(null, transform.position, Vector3.zero, "Suicide");
    }

    // Update kill/death stats on NetworkMatchPlayer
    private void UpdateKillDeathStats(NetworkMatchPlayer killerMatchPlayer)
    {
        if (isServer)
        {
            Debug.Log("===== K/D UPDATE =====");

            if (killerMatchPlayer != null)
            {
                Debug.Log("Calling AddKill()");
                killerMatchPlayer.AddKill();
            }
            else
            {
                Debug.LogError("killerMatchPlayer is NULL");
            }

            if (MatchPlayer != null)
            {
                Debug.Log("Calling AddDeath()");
                MatchPlayer.AddDeath();
            }
            else
            {
                Debug.LogError("Victim MatchPlayer is NULL");
            }
        }
    }

    // Victim color
    private void HandleKillFeed(NetworkMatchPlayer killerMatchPlayer)
    {
        string victimColor = "white";

        if (MatchPlayer != null)
        {
            victimColor = MatchPlayer.Team == "Blue"
                ? "#00BFFF"
                : "#FF3E3E";
        }

        // Killer name/color
        string killerName = "Environment";
        string killerColor = "white";

        if (killerMatchPlayer != null)
        {
            killerName = killerMatchPlayer.PlayerName;
            killerColor = killerMatchPlayer.Team == "Blue"
                ? "#00BFFF"
                : "#FF3E3E";
        }

        // Victim name
        string victimName = MatchPlayer != null
            ? MatchPlayer.PlayerName
            : GetComponent<player>().nameOfPlayer;

        // Build colored strings
        string coloredKiller = $"<color={killerColor}>{killerName}</color>";
        string coloredVictim = $"<color={victimColor}>{victimName}</color>";

        // Send kill feed
        if (isServer)
        {
            RpcAddKillFeed(coloredKiller, coloredVictim);
        }
    }

    // Disable animator and rigging
    private void DisableAnimationAndRigging()
    {
        Animator animator = GetComponent<Animator>();

        if (animator != null)
            animator.enabled = false;

        RigBuilder rigBuilder = GetComponent<RigBuilder>();

        if (rigBuilder != null)
            rigBuilder.enabled = false;
    }

    // Drop held flag on death
    private void DropFlagOnDeath()
    {
        if (flagHandler.heldFlag != null)
        {
            Debug.Log($"Player died while holding flag: {flagHandler.heldFlag.name}");
            flagHandler.heldFlag.transform.SetParent(null);
            flagHandler.ServerDropFlag(transform.position);
        }
    }

    // Disable gameplay components: colliders, behaviors, and game objects
    private void DisableGameplay()
    {
        for (int i = 0; i < disableOnDeath.Length; i++)
        {
            disableOnDeath[i].enabled = false;
        }

        for (int i = 0; i < disableGameObjectsOnDeath.Length; i++)
        {
            disableGameObjectsOnDeath[i].SetActive(false);
        }

        if (mainCollider != null && playerRigidbody != null)
        {
            Debug.Log("Disabling Collider: " + mainCollider.name);
            mainCollider.enabled = false;
            playerRigidbody.isKinematic = true;
        }

        if (PlayerRagdoll != null)
        {
            PlayerRagdoll.transform.parent = null;
        }
    }

    // Target RPC: Called on the dead player's client to show death screen
    [TargetRpc]
    private void TargetOnDeath(NetworkConnection target)
    {
        LogPlayerState("TARGET ON DEATH");

        // Disable local controller/movement scripts
        for (int i = 0; i < disableOnDeath.Length; i++)
        {
            disableOnDeath[i].enabled = false;
        }

        for (int i = 0; i < disableGameObjectsOnDeath.Length; i++)
        {
            disableGameObjectsOnDeath[i].SetActive(false);
        }

        // Show death camera
        GameManager.instance.setSceneCameraActive(true);
        GameManager.instance.SetSceneCameraAbovePlayer(transform.position);

        var setup = GetComponent<playerSetup>();

        if (setup != null && setup.playerUIInstance != null)
            setup.playerUIInstance.SetActive(false);

        if (playerShoot != null)
        {
            playerShoot.CancelInvoke("Shoot");
        }

        StartCoroutine(LocalRespawnFinish());
    }

    // Respawn coroutine: Wait, reset state, teleport, re-enable controls
    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(GameManager.instance.matchSettings.respawnTime);

        if (!isServer)
            yield break;

        // Reset death flag
        isDead = false;

        // Reset health
        GetComponent<PlayerHealth>().currentHealth = GetComponent<PlayerHealth>().maxHealth;

        // Get spawn position
        if (spawner == null)
            spawner = FindAnyObjectByType<CharacterSpawner>();

        FlagHandler flagHandler = GetComponent<FlagHandler>();
        string playerTeam = flagHandler != null ? flagHandler.Team : "Blue";

        Transform spawnPosition = spawner != null
            ? spawner.GetAvailableSpawnPoint(playerTeam)
            : transform;

        Quaternion spawnRotation = Quaternion.identity;

        // Teleport and reset physics
        transform.SetPositionAndRotation(spawnPosition.position, spawnRotation);
        // playerRigidbody.velocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
        playerRigidbody.isKinematic = false;

        // Re-enable collider
        if (mainCollider != null)
            mainCollider.enabled = true;

        // Re-enable animations and rigging
        Animator animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
        }

        RigBuilder rigBuilder = GetComponent<RigBuilder>();

        if (rigBuilder != null)
        {
            rigBuilder.enabled = true;
            rigBuilder.Build();
        }

        // Disable ragdoll
        if (ragdollManager != null)
            ragdollManager.DisableRagdoll();

        // Re-enable name and health UI
        PlayerNameHolder.SetActive(true);
        HealthBarUIobj.SetActive(true);

        // Spawn effect
        if (spawnEffect != null)
        {
            GameObject fx = Instantiate(spawnEffect, transform.position, Quaternion.identity);
            Destroy(fx, 3f);
        }

        // Reset RPC on all clients
        RpcForceRespawnPosition(spawnPosition.position, spawnRotation);

        // Notify this player's client
        if (connectionToClient != null)
        {
            TargetFinishRespawn(connectionToClient);
        }

        GetComponent<player>().PlayerSetup();
        LogPlayerState("AFTER TELEPORT");
    }

    // Local respawn coroutine on the victim's client
    private IEnumerator LocalRespawnFinish()
    {
        yield return new WaitForSeconds(GameManager.instance.matchSettings.respawnTime);

        GameManager.instance.setSceneCameraActive(false);

        if (GameManager.instance.sceneCamera != null)
        {
            var controller = GameManager.instance.sceneCamera.GetComponent<SceneCameraController>();

            if (controller != null)
                controller.enabled = false;
        }

        var setup = GetComponent<playerSetup>();

        if (setup != null && setup.playerUIInstance != null)
            setup.playerUIInstance.SetActive(true);

        GetComponent<player>().PlayerSetup();
    }

    // Broadcast respawn position to all clients
    [ClientRpc]
    private void RpcForceRespawnPosition(Vector3 pos, Quaternion rot)
    {
        transform.SetPositionAndRotation(pos, rot);
    }

    // Target RPC: Re-enable controls on the respawned player's client
    [TargetRpc]
    private void TargetFinishRespawn(NetworkConnection target)
    {
        LogPlayerState("TARGET FINISH RESPAWN");
        Debug.Log($"TARGET local={isLocalPlayer} owned={isOwned}");

        // Re-enable local controller/movement scripts
        foreach (Behaviour behaviour in disableOnDeath)
        {
            behaviour.enabled = true;
        }

        foreach (GameObject obj in disableGameObjectsOnDeath)
        {
            obj.SetActive(true);
        }

        GameManager.instance.setSceneCameraActive(false);

        if (GameManager.instance.sceneCamera != null)
        {
            var controller = GameManager.instance.sceneCamera.GetComponent<SceneCameraController>();

            if (controller != null)
                controller.enabled = false;
        }

        var setup = GetComponent<playerSetup>();

        if (setup != null && setup.playerUIInstance != null)
        {
            setup.playerUIInstance.SetActive(true);
        }

        GetComponent<player>().PlayerSetup();
    }

    // Spawn health drop at player death position
    [Server]
    private void CmdSpawnHealthDrop()
    {
        if (healthDropPrefab == null)
            return;

        Vector3 dropPosition = transform.position + Vector3.up * 5f;
        GameObject healthDrop = Instantiate(healthDropPrefab, dropPosition, Quaternion.identity);

        // Add physics
        if (healthDrop.TryGetComponent(out Rigidbody rb))
        {
            rb.AddForce(Vector3.up * 2f, ForceMode.Impulse);
            rb.AddTorque(Vector3.up * 100f, ForceMode.Impulse);
        }

        NetworkServer.Spawn(healthDrop);
        Debug.Log("Health drop spawned at " + dropPosition);
    }

    // Broadcast kill feed message to all clients
    [ClientRpc]
    private void RpcAddKillFeed(string killer, string victim)
    {
        Debug.Log($"[ClientRpc] Feed: {killer} 🔫 {victim}");
        KillFeedManager.Instance.AddKillFeedEntry(killer, victim);
    }

    private void LogPlayerState(string stage)
    {
        var identity = GetComponent<NetworkIdentity>();

        Debug.Log(
            $@"
================ {stage} ================
name                 = {name}
netId                = {netId}

isServer             = {isServer}
isClient             = {isClient}
isLocalPlayer        = {isLocalPlayer}
isOwned              = {isOwned}

isDead               = {isDead}

position             = {transform.position}
rotation             = {transform.rotation.eulerAngles}

connectionToClient   = {connectionToClient}
==========================================
");
    }
}