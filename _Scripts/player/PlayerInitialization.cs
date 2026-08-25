using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.UI;

public class PlayerInitialization : NetworkBehaviour
{
    [SyncVar]
    public uint matchPlayerNetId;

    private playerShoot playerShoot;
    private RagdollManager ragdollManager;
    private FlagHandler flagHandler;
    private weaponManager weaponManager;
    private playerWeapon currentWeapon;
    private CharacterSpawner spawner;
    private Rigidbody playerRigidbody;
    private CapsuleCollider mainCollider;

    [SerializeField]
    private GameObject spawnEffect;

    private bool firstSetup = true;
    private bool[] wasEnabled;

    [SerializeField]
    private Behaviour[] disableOnDeath;

    public NetworkMatchPlayer MatchPlayer
    {
        get
        {
            NetworkIdentity identity = null;

            if (NetworkServer.active)
                NetworkServer.spawned.TryGetValue(matchPlayerNetId, out identity);
            else
                NetworkClient.spawned.TryGetValue(matchPlayerNetId, out identity);

            return identity != null
                ? identity.GetComponent<NetworkMatchPlayer>()
                : null;
        }
    }

    private void Start()
    {
        InitializeComponentReferences();
        SetupAnimationRigging();
    }

    private void InitializeComponentReferences()
    {
        playerShoot = GetComponent<playerShoot>();
        ragdollManager = GetComponent<RagdollManager>();
        flagHandler = GetComponent<FlagHandler>();
        mainCollider = GetComponent<CapsuleCollider>();
        playerRigidbody = GetComponent<Rigidbody>();
        spawner = FindAnyObjectByType<CharacterSpawner>();
        weaponManager = GetComponent<weaponManager>();

        if (weaponManager != null)
            currentWeapon = weaponManager.GetcurrentWeapon();

        Debug.Log($"[PlayerInitialization] Components initialized for {gameObject.name}");
    }

    private void SetupAnimationRigging()
    {
        StartCoroutine(DelayedRigInit());
    }

    private IEnumerator DelayedRigInit()
    {
        yield return null; // wait one frame
        yield return null; // and one more, to be safe

        RigBuilder rigBuilder = GetComponent<RigBuilder>();

        if (rigBuilder != null)
        {
            rigBuilder.enabled = true;
            rigBuilder.Build();
        }

        Animator animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
        }
    }

    // Public setup method called during respawn.
    public void PlayerSetup()
    {
        Debug.Log($"PLAYERSETUP name={name} local={isLocalPlayer} owned={isOwned}");

        if (isLocalPlayer)
        {
            GameObject playerNameHolder = GetComponentInChildren<PlayerNameUI>()?.gameObject.transform.parent.gameObject;
            GameObject healthBarUIobj = GetComponentInChildren<Slider>()?.gameObject.transform.parent.gameObject;

            if (playerNameHolder != null)
                playerNameHolder.SetActive(false);

            if (healthBarUIobj != null)
                healthBarUIobj.SetActive(false);
        }

        BroadCastNewPlayerSetup();
    }

    // Server broadcast: Setup player on all clients.
    [Server]
    private void BroadCastNewPlayerSetup()
    {
        RpcSetupPlayerOnAllClients();
    }

    // RPC: Setup player on all clients.
    [ClientRpc]
    private void RpcSetupPlayerOnAllClients()
    {
        if (firstSetup)
        {
            wasEnabled = new bool[disableOnDeath.Length];

            for (int i = 0; i < wasEnabled.Length; i++)
            {
                wasEnabled[i] = disableOnDeath[i].enabled;
            }

            firstSetup = false;
        }

        SetDefaults();
    }

    // Reset player to default state after respawn.
    public void SetDefaults()
    {
        weaponManager = GetComponent<weaponManager>();
        playerShoot = GetComponent<playerShoot>();

        if (weaponManager == null)
        {
            Debug.LogError("[SetDefaults] WeaponManager is NULL");
            return;
        }

        if (playerShoot == null)
        {
            Debug.LogError("[SetDefaults] PlayerShoot is NULL");
            return;
        }

        currentWeapon = weaponManager.GetcurrentWeapon();

        if (currentWeapon == null)
        {
            Debug.LogError("[SetDefaults] CurrentWeapon is NULL");
            return;
        }

        playerShoot.InitializeWeapon();

        if (playerShoot.currentWeapon == null)
        {
            Debug.LogError("[SetDefaults] InitializeWeapon failed.");
            return;
        }

        // Sync weapon references
        playerShoot.currentWeapon = currentWeapon;

        // Restore ammo UI
        playerShoot.currentWeapon.currentAmmo = currentWeapon.currentAmmo;

        if (playerShoot.ammoText != null)
        {
            playerShoot.ammoText.text = playerShoot.currentWeapon.currentAmmo.ToString();
        }

        // Stop any lingering invokes
        playerShoot.CancelInvoke("Shoot");
        playerShoot.CancelInvoke("Recoil");

        // Hide death camera
        if (GameManager.instance != null &&
            GameManager.instance.sceneCamera != null)
        {
            GameManager.instance.sceneCamera
                .GetComponent<SceneCameraController>()
                .enabled = false;
        }

        // Spawn VFX
        if (spawnEffect != null)
        {
            GameObject fx = Instantiate(
                spawnEffect,
                transform.position,
                Quaternion.identity);

            Destroy(fx, 3f);
        }
    }

    // Server: Initialize health on server start.
    public override void OnStartServer()
    {
        base.OnStartServer();

        GetComponent<PlayerHealth>().currentHealth = GetComponent<PlayerHealth>().maxHealth;
    }

    // Get current weapon reference.
    public playerWeapon GetCurrentWeapon()
    {
        if (currentWeapon == null && weaponManager != null)
        {
            currentWeapon = weaponManager.GetcurrentWeapon();
        }

        return currentWeapon;
    }

    // Get weapon manager.
    public weaponManager GetWeaponManager()
    {
        if (weaponManager == null)
            weaponManager = GetComponent<weaponManager>();

        return weaponManager;
    }

    // Get player shoot component.
    public playerShoot GetPlayerShoot()
    {
        if (playerShoot == null)
            playerShoot = GetComponent<playerShoot>();

        return playerShoot;
    }

    // Get ragdoll manager.
    public RagdollManager GetRagdollManager()
    {
        if (ragdollManager == null)
            ragdollManager = GetComponent<RagdollManager>();

        return ragdollManager;
    }
}