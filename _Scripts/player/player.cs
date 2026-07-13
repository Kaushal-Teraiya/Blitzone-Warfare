//using Mirror;
//using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.UI;

[RequireComponent(typeof(playerSetup))]
public class player : NetworkBehaviour
{

    [SyncVar]
    public uint matchPlayerNetId;

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
    [SyncVar]
    private bool _isDead = false;
    public bool isDead
    {
        get { return _isDead; }
        protected set { _isDead = value; }
    }

    [SerializeField]
    private HealthUI healthUI;
    public Rigidbody _rb;
    private weaponGraphics WG;

    [SerializeField]
    private GameObject PlayerRagdoll;

    [SerializeField]
    public int maxHealth = 100;
    private int RealHealth;

    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth;

    [SerializeField]
    private Behaviour[] disableOnDeath;

    [SerializeField]
    private GameObject[] disableGameObjectsOnDeath;
    private bool[] wasEnabled;

    [SerializeField]
    private GameObject deathEffect;

    [SerializeField]
    private GameObject spawnEffect;
    private bool firstSetup = true;
    private FlagHandler fh;
    private playerShoot PS;

    [SerializeField]
    private float gunForce;

    [SerializeField]
    private float gunTorque;
    RagdollManager RM;
    public GameObject WeaponHolder;
    public GameObject gun;

    public GameObject healthUICanvas;

    //Animator anim = GetComponent<Animator>();
    [SerializeField]
    private CapsuleCollider mainCollider;

    public GameObject worldGun; // Gun A (Visible to others, not to me)
    public GameObject fpsGun; // Gun B (Visible only to me)
    public GameObject healthDropPrefab;

    private FlagHandler FHD;
    private string teamNamefromFH;
    private PlayerNameUI nameUI;

    // True for blue, false for red

    private CharacterSpawner spawner;

    public GameObject NewWeaponHolder;
    public GameObject FPShands;
    public TextMeshProUGUI HealthText;
    Camera cam;
    string killerName;

    [SerializeField]
    private GameObject PlayerNameHolder;

    [SerializeField]
    private GameObject HealthBarUIobj;

    public Slider HealthBarUI;
    private PlayerHealthBar pplayerHealthBar;

    [SyncVar]
    public string nameOfPlayer;
    private playerWeapon currentWeapon;
    public weaponManager WeaponManager;

    public Vector3 storedHitPoint;
    public Vector3 storedHitDirection;
    public string storedHitBodyPartName;

    void Start()
    {
        fh = GetComponent<FlagHandler>();
        PS = GetComponent<playerShoot>(); // Assign playerShoot reference
        RM = GetComponent<RagdollManager>();
        mainCollider = GetComponent<CapsuleCollider>();
        _rb = GetComponent<Rigidbody>();
        FHD = GetComponent<FlagHandler>();

        //  NetworkMatchPlayer MatchPlayer = connectionToClient.identity.GetComponent<NetworkMatchPlayer>();

        if (fh == null)
            fh = GetComponent<FlagHandler>();

        teamNamefromFH = fh.Team;
        // Animator animator = GetComponent<Animator>();
        spawner = FindAnyObjectByType<CharacterSpawner>();
        pplayerHealthBar = GetComponent<PlayerHealthBar>();
        HealthBarUI = HealthBarUIobj.GetComponent<Slider>();
        HealthText = FindAnyObjectByType<TextMeshProUGUI>();
        healthUI = GetComponentInChildren<HealthUI>();
        WeaponManager = GetComponent<weaponManager>();
        currentWeapon = WeaponManager.GetcurrentWeapon();
        // currentWeapon = GetComponentInChildren<playerWeapon>();

        NewWeaponHolder.SetActive(false);
        cam = Camera.main;
        if (spawnEffect == null)
        {
            Debug.Log("spawner is null");
        }
        else
        {
            Debug.Log("found spawner");
        }

        if (HealthBarUI == null)
        {
            Debug.Log("UI for HealthBar is NULL");
        }

        StartCoroutine(DelayedRigInit());
        // Ensure correct gun visibility at Start (Backup fix)
        if (!isLocalPlayer)
        {
            if (FPShands != null)
            {
                FPShands.SetActive(false);
            }
            if (fpsGun != null)
                fpsGun.SetActive(false); // Hide FPS gun for non-local
            if (worldGun != null)
                worldGun.SetActive(true); // Show gun holder for non-local
            gameObject.GetComponentInChildren<Canvas>().gameObject.SetActive(false);

            if (healthUICanvas != null)
            {
                healthUICanvas.SetActive(false);
            }
        }

        if (isServer)
        {
            currentHealth = maxHealth; // Initialize health only on server
            RealHealth = currentHealth;
        }
        if (isLocalPlayer)
        {
            healthUI = GetComponentInChildren<HealthUI>();
            if (healthUI == null)
            {
                Debug.LogError("HealthUI not found in player prefab!");
            }
            else
            {
                healthUI.SetHealth(currentHealth);
            }
            healthUI.SetHealth(currentHealth);
        }

        nameUI = GetComponentInChildren<PlayerNameUI>();

        // Fetch name from PlayerPrefs
        string playerName = PlayerPrefs.GetString("PlayerName", "Player"); // Default to "Player" if not set
        nameOfPlayer = playerName;
        if (nameUI != null)
        {
            nameUI.SetPlayerName(playerName, teamNamefromFH);
        }

        // if (!NetworkClient.ready)
        // {
        //     NetworkClient.Ready();
        // }
    }

    [Command]
    void CmdSetPlayerName(string _name)
    {
        nameOfPlayer = _name; // SyncVar will now update across all clients
    }

    void Awake()
    {
        // StartCoroutine(Respawn());
    }

    public void PlayerSetup()
    {
        Debug.Log($"PLAYERSETUP name={name} local={isLocalPlayer} owned={isOwned}");

        if (isLocalPlayer)
        {
            PlayerNameHolder.gameObject.SetActive(false);
            HealthBarUIobj.gameObject.SetActive(false);
        }

        BroadCastNewPlayerSetup();
    }
    [Command]
    public void CmdSuicide()
    {
        if (isDead)
            return;

        currentHealth = 0;

        ServerDie(
            null,
            transform.position,
            Vector3.zero,
            "Suicide"
        );
    }
    [Server]
    private void BroadCastNewPlayerSetup()
    {
        RpcSetupPlayerOnAllClients();
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
        //StartCoroutine(TrySetDefaultsDelayed());
    }

    [Command]
    public void CmdKillSelf(int DMGamount)
    {
        RpcKillSelf(DMGamount);
    }
    [TargetRpc]
    private void TargetOnDeath(NetworkConnection target)
    {
        LogPlayerState("TARGET ON DEATH");

        // NEW: disable local controller/movement scripts on the victim's own client
        for (int i = 0; i < disableOnDeath.Length; i++)
        {
            disableOnDeath[i].enabled = false;
        }
        for (int i = 0; i < disableGameObjectsOnDeath.Length; i++)
        {
            disableGameObjectsOnDeath[i].SetActive(false);
        }

        GameManager.instance.setSceneCameraActive(true);
        GameManager.instance.SetSceneCameraAbovePlayer(transform.position);

        var setup = GetComponent<playerSetup>();
        if (setup != null && setup.playerUIInstance != null)
            setup.playerUIInstance.SetActive(false);

        if (PS != null)
        {
            PS.CancelInvoke("Shoot");
        }
        StartCoroutine(LocalRespawnFinish());
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
currentHealth        = {currentHealth}

position             = {transform.position}
rotation             = {transform.rotation.eulerAngles}

connectionToClient   = {connectionToClient}
owner identity       = {connectionToClient?.identity}
owner netId          = {connectionToClient?.identity?.netId}

NetworkClient.local  = {Mirror.NetworkClient.localPlayer?.name}
Local netId          = {Mirror.NetworkClient.localPlayer?.netId}

identity.isOwned     = {identity.isOwned}
identity.isClient    = {identity.isClient}
identity.isServer    = {identity.isServer}
identity.isLocal     = {identity.isLocalPlayer}
==========================================
");
    }

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

        PlayerSetup();
    }
    [ClientRpc]
    public void RpctakeDamageP(
        int _amount,
        NetworkIdentity killerName,
        Vector3 hitpoint,
        Vector3 hitDirection,
        string hitBodyPartName
    )
    {
        if (isDead)
            return;


        currentHealth = Mathf.Clamp(currentHealth - _amount, 0, maxHealth);
        Debug.Log(transform.name + " now has " + currentHealth + " Health");

        if (currentHealth <= 0 && !isDead)
        {
            CancelInvoke("Shoot");

            // Send attacker name to Suicide (aka Die)
            ServerDie(killerName, hitpoint, hitDirection, hitBodyPartName);
        }
    }

    [Server]
    public void TakeDamage(
     int amount,
     NetworkIdentity killerId,
     Vector3 hitPoint,
     Vector3 hitDirection,
     string hitBodyPartName)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);

        RpcUpdateHealth(currentHealth);

        if (currentHealth <= 0)
        {
            ServerDie(killerId, hitPoint, hitDirection, hitBodyPartName);
        }
    }
    [ClientRpc]
    private void RpcUpdateHealth(int newHealth)
    {
        currentHealth = newHealth;
        UpdateHealth(newHealth);
    }

    [ClientRpc]
    public void RpctakeDamageU(int _amount, NetworkIdentity killerName)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Clamp(currentHealth - _amount, 0, maxHealth);
        Debug.Log(transform.name + " now has " + currentHealth + " Health");

        if (currentHealth <= 0 && !isDead)
        {
            CancelInvoke("Shoot");

            // Send attacker name to Suicide (aka Die)
            DieU(killerName);
        }
    }

    [ClientRpc]
    private void RpcKillSelf(int _amount)
    {
        if (isDead)
        {
            return;
        }
        currentHealth = Mathf.Clamp(currentHealth - _amount, 0, maxHealth);
        //PlayerHealthBar.RpcUpdateHealthBar(currentHealth);
        Debug.Log(transform.name + "now has " + currentHealth + "Health");
        // Force SyncVar to trigger its hook by setting it to a different value
        int temp = currentHealth;
        currentHealth = -1; // Temporary invalid value
        currentHealth = temp; // Set back to correct value
    }

    private void Suicide()
    {
        isDead = true;
        // PS.enabled = false;
        _rb.isKinematic = true;

        Animator animator = GetComponent<Animator>();
        animator.enabled = false;
        GetComponent<UnityEngine.Animations.Rigging.RigBuilder>().enabled = false;

        if (fh.heldFlag != null)
        {
            Debug.Log($"Player died while holding flag: {fh.heldFlag.name}");
            fh.heldFlag.transform.SetParent(null);
            fh.ServerDropFlag(transform.position);
        }

        if (PlayerRagdoll != null)
        {
            PlayerRagdoll.transform.parent = null;
        }

        for (int i = 0; i < disableOnDeath.Length; i++)
        {
            disableOnDeath[i].enabled = false;
        }
        for (int i = 0; i < disableGameObjectsOnDeath.Length; i++)
        {
            disableGameObjectsOnDeath[i].SetActive(false);
        }

        if (mainCollider != null && _rb != null)
        {
            Debug.Log("Disabling Collider: " + mainCollider.name);
            mainCollider.enabled = false;
            _rb.isKinematic = true;
        }
        Debug.Log(transform.name + " is dead");

        GameObject _gfxInstance = Instantiate(deathEffect, transform.position, Quaternion.identity);
        Destroy(_gfxInstance, 3f);

        if (isLocalPlayer)
        {
            GameManager.instance.setSceneCameraActive(true);
            GameManager.instance.SetSceneCameraAbovePlayer(transform.position);
            GetComponent<playerSetup>().playerUIInstance.SetActive(false);
        }

        RM.EnableRagdollLocal();
        // RM.CmdEnableRagdollNoForce();
        //  RM.CmdSetRagdoll();
        PlayerNameHolder.SetActive(false);
        HealthBarUIobj.SetActive(false);

        //  Ensure UI updates health text when dying
        UpdateHealth(0);
        //currentWeapon.currentAmmo = 0;
        if (PS != null)
        {
            PS.CancelInvoke("Shoot");
        }

        if (isServer)
            CmdSpawnHealthDrop();

        transform.rotation = Quaternion.Euler(
            90f,
            transform.rotation.eulerAngles.y,
            transform.rotation.eulerAngles.z
        );

        StartCoroutine(Respawn());
    }

    [ClientRpc]
    void RpcAddKillFeed(string killer, string victim)
    {
        Debug.Log($"[ClientRpc] Feed: {killer} 🔫 {victim}");
        KillFeedManager.Instance.AddKillFeedEntry(killer, victim);
    }

    [Server]
    private void ServerDie(
            NetworkIdentity killerId,
            Vector3 hitPoint,
            Vector3 hitDirection,
            string hitBodyPartName
        )
    {

        isDead = true;
        LogPlayerState("DIE START");
        // PS.enabled = false;
        _rb.isKinematic = true;

        Debug.Log(
            $"<color=yellow>[Die()]</color> <color=green>killerId:</color> <color=cyan>{killerId?.netId} ({killerId?.name})</color> | <color=red>This Player:</color> <color=cyan>{GetComponent<NetworkIdentity>()?.netId} ({nameOfPlayer})</color>"
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

        Debug.Log("===== K/D UPDATE =====");

        Debug.Log($"isServer = {isServer}");

        Debug.Log($"killerPlayer = {killerPlayer}");

        Debug.Log($"killerMatchPlayer = {killerMatchPlayer}");

        Debug.Log($"victim MatchPlayer = {MatchPlayer}");

        if (isServer)
        {
            Debug.Log("Inside server block");

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
        // Victim color
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
            : nameOfPlayer;

        // Build colored strings
        string coloredKiller = $"<color={killerColor}>{killerName}</color>";
        string coloredVictim = $"<color={victimColor}>{victimName}</color>";

        // Send kill feed
        if (isServer)
        {
            RpcAddKillFeed(coloredKiller, coloredVictim);
        }
        // KillFeedManager.Instance?.AddKillFeedEntry(killerName, victimName);

        Animator animator = GetComponent<Animator>();
        animator.enabled = false;
        GetComponent<UnityEngine.Animations.Rigging.RigBuilder>().enabled = false;

        if (fh.heldFlag != null)
        {
            Debug.Log($"Player died while holding flag: {fh.heldFlag.name}");
            //fh.heldFlag.transform.SetParent(null);
            fh.ServerDropFlag(transform.position);
        }

        if (PlayerRagdoll != null)
        {
            PlayerRagdoll.transform.parent = null;
        }

        for (int i = 0; i < disableOnDeath.Length; i++)
        {
            disableOnDeath[i].enabled = false;
        }
        for (int i = 0; i < disableGameObjectsOnDeath.Length; i++)
        {
            disableGameObjectsOnDeath[i].SetActive(false);
        }

        if (mainCollider != null && _rb != null)
        {
            Debug.Log("Disabling Collider: " + mainCollider.name);
            mainCollider.enabled = false;
            _rb.isKinematic = true;
        }
        Debug.Log(transform.name + " is dead");

        GameObject _gfxInstance = Instantiate(deathEffect, transform.position, Quaternion.identity);
        Destroy(_gfxInstance, 3f);

        Debug.Log($"connectionToClient = {connectionToClient}");
        Debug.Log($"owner identity = {connectionToClient?.identity}");
        Debug.Log($"connectionId = {connectionToClient?.connectionId}");
        // With:
        if (isServer && connectionToClient != null)
        {
            TargetOnDeath(connectionToClient);
        }
        RM.EnableRagdoll();
        // RM.CmdEnableRagdoll(hitPoint, hitDirection, 500f);
        // RM.CmdSetRagdoll(hitPoint, hitDirection, 500f);
        GetComponent<Animator>().enabled = false;
        storedHitPoint = hitPoint;
        storedHitDirection = hitDirection;
        storedHitBodyPartName = hitBodyPartName;
        // StartCoroutine(DelayedForceToHips());
        //  StartCoroutine(ApplyRagdollForce());
        PlayerNameHolder.SetActive(false);
        HealthBarUIobj.SetActive(false);

        // Ensure UI updates health text when dying
        UpdateHealth(0);
        if (currentWeapon != null)
        {
            currentWeapon.currentAmmo = 0;
        }
        if (PS != null)
        {
            PS.CancelInvoke("Shoot");
        }

        if (isServer)
            CmdSpawnHealthDrop();

        transform.rotation = Quaternion.Euler(
            90f,
            transform.rotation.eulerAngles.y,
            transform.rotation.eulerAngles.z
        );
        Debug.Log($"Victim MatchPlayer = {MatchPlayer}");
        Debug.Log($"Killer MatchPlayer = {killerMatchPlayer}");
        Debug.Log($"SERVER Die: netId={netId}");
        Debug.Log($"connectionToClient={connectionToClient}");
        Debug.Log($"identity owner={connectionToClient?.identity}");
        StartCoroutine(Respawn());
        //Debug.Log($"NetworkClient.localPlayer = {NetworkClient.localPlayer.name} netId={NetworkClient.localPlayer.netId}");

    }

    [ClientRpc]
    private void RpcHandleDeathEffects(NetworkIdentity killerId)
    {
        string victimColor = "white";
        string killerColor = "white";

        NetworkMatchPlayer victimMatchPlayer =
            connectionToClient.identity.GetComponent<NetworkMatchPlayer>();

        if (victimMatchPlayer != null)
        {
            victimColor = victimMatchPlayer.Team == "Blue"
                ? "#00BFFF"
                : "#FF3E3E";
        }

        if (killerId != null && killerId != GetComponent<NetworkIdentity>())
        {
            NetworkMatchPlayer killerMatchPlayer =
                killerId.connectionToClient.identity.GetComponent<NetworkMatchPlayer>();

            if (killerMatchPlayer != null)
            {
                killerColor = killerMatchPlayer.Team == "Blue"
                    ? "#00BFFF"
                    : "#FF3E3E";
            }
        }

        string coloredKiller = $"<color={killerColor}>{killerName}</color>";
        string coloredVictim = $"<color={victimColor}>{nameOfPlayer}</color>";

        Debug.Log(
            $"<color=yellow>KillFeed</color> - Killer: {coloredKiller} | Victim: {coloredVictim}"
        );
        RpcAddKillFeed(coloredKiller, coloredVictim);

        // Handle ragdoll visuals
        if (PlayerRagdoll != null)
            PlayerRagdoll.transform.parent = null;

        Animator animator = GetComponent<Animator>();
        animator.enabled = false;
        GetComponent<UnityEngine.Animations.Rigging.RigBuilder>().enabled = false;
    }

    private void DieU(NetworkIdentity killer)
    {
        isDead = true;
        _rb.isKinematic = true;

        Animator animator = GetComponent<Animator>();
        animator.enabled = false;

        GetComponent<UnityEngine.Animations.Rigging.RigBuilder>().enabled = false;

        // ---------------------------
        // Update Kills / Deaths
        // ---------------------------
        if (isServer)
        {
            NetworkMatchPlayer killerMatchPlayer = null;

            if (killer != null)
            {
                player killerPlayer = killer.GetComponent<player>();

                if (killerPlayer != null)
                    killerMatchPlayer = killerPlayer.MatchPlayer;
            }

            if (killerMatchPlayer != null)
            {
                Debug.Log($"Awarding kill to {killerMatchPlayer.PlayerName}");
                killerMatchPlayer.AddKill();
            }
            else
            {
                Debug.LogWarning("Killer MatchPlayer was NULL.");
            }

            if (MatchPlayer != null)
            {
                Debug.Log($"Awarding death to {MatchPlayer.PlayerName}");
                MatchPlayer.AddDeath();
            }
            else
            {
                Debug.LogWarning("Victim MatchPlayer was NULL.");
            }
        }

        // ---------------------------
        // Drop Flag
        // ---------------------------
        if (fh.heldFlag != null)
        {
            Debug.Log($"Player died while holding flag: {fh.heldFlag.name}");
            fh.heldFlag.transform.SetParent(null);
            fh.ServerDropFlag(transform.position);
        }

        // ---------------------------
        // Ignore ragdoll collisions
        // ---------------------------
        CapsuleCollider playerCollider = GetComponent<CapsuleCollider>();

        foreach (Collider ragdollCollider in GetComponentsInChildren<Collider>())
        {
            Physics.IgnoreCollision(playerCollider, ragdollCollider, true);
        }

        if (PlayerRagdoll != null)
        {
            PlayerRagdoll.transform.parent = null;
        }

        // ---------------------------
        // Disable gameplay
        // ---------------------------
        foreach (Behaviour behaviour in disableOnDeath)
            behaviour.enabled = false;

        foreach (GameObject obj in disableGameObjectsOnDeath)
            obj.SetActive(false);

        if (mainCollider == null)
            mainCollider = GetComponent<CapsuleCollider>();

        if (mainCollider != null)
        {
            Debug.Log("Disabling Collider: " + mainCollider.name);
            mainCollider.enabled = false;
        }
        else
        {
            Debug.LogError("mainCollider is NULL!");
        }

        Debug.Log($"{transform.name} is dead");

        GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
        Destroy(effect, 3f);

        if (isLocalPlayer)
        {
            GameManager.instance.setSceneCameraActive(true);
            GetComponent<playerSetup>().playerUIInstance.SetActive(false);
        }

        // ---------------------------
        // Enable ragdoll
        // ---------------------------
        if (RM != null)
        {
            RM.EnableRagdoll();
        }
        else
        {
            Debug.LogError("RagdollManager is NULL!");
        }

        PlayerNameHolder.SetActive(false);
        HealthBarUIobj.SetActive(false);

        StartCoroutine(Respawn());
    }
    IEnumerator Respawn()
    {
        LogPlayerState("RESPAWN START");
        yield return new WaitForSeconds(4f);

        if (PlayerRagdoll != null)
        {
            PlayerRagdoll.transform.parent = transform;
        }

        PlayerNameHolder.SetActive(true);
        HealthBarUIobj.SetActive(true);

        yield return new WaitForSeconds(GameManager.instance.matchSettings.respawnTime - 4f);

        Transform _spawnPoint = null;

        if (fh == null)
            fh = GetComponent<FlagHandler>();

        string playerTeam = fh != null ? fh.Team : null;

        if (string.IsNullOrEmpty(playerTeam))
        {
            Debug.LogError($"[Respawn] Could not resolve team for {name}.");
            yield break;
        }
        //string playerTeam = MatchPlayer.Team;

        if (playerTeam == "Blue")
        {
            if (spawner.availableTeamASpawns.Count == 0)
            {
                Debug.Log("🔄 Resetting available spawn points for Team Blue.");
                spawner.availableTeamASpawns = new List<Transform>(spawner.teamASpawnPoints);
            }

            int index = Random.Range(0, spawner.availableTeamASpawns.Count);
            _spawnPoint = spawner.availableTeamASpawns[index];
            spawner.availableTeamASpawns.RemoveAt(index);
        }
        else if (playerTeam == "Red")
        {
            if (spawner.availableTeamBSpawns.Count == 0)
            {
                Debug.Log("🔄 Resetting available spawn points for Team Red.");
                spawner.availableTeamBSpawns = new List<Transform>(spawner.teamBSpawnPoints);
            }

            int index = Random.Range(0, spawner.availableTeamBSpawns.Count);
            _spawnPoint = spawner.availableTeamBSpawns[index];
            spawner.availableTeamBSpawns.RemoveAt(index);
        }

        if (_spawnPoint == null)
        {
            Debug.LogError($"[ERROR] No available spawn point for team {playerTeam}!");
            yield break;
        }

        // Disable ragdoll BEFORE restoring player
        RM.DisableRagdoll();
        RM.ResetRagdollPose();

        // Move player to spawn
        transform.SetPositionAndRotation(_spawnPoint.position, _spawnPoint.rotation);
        RpcForceRespawnPosition(_spawnPoint.position, _spawnPoint.rotation);
        Debug.Log($"Spawn Point = {_spawnPoint.name}");
        Debug.Log($"Spawn Position = {_spawnPoint.position}");
        Debug.Log($"Actual Position = {transform.position}");
        yield return null;

        // Restore gameplay state
        isDead = false;
        currentHealth = maxHealth;

        _rb.isKinematic = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        if (mainCollider != null)
            mainCollider.enabled = true;

        foreach (Behaviour behaviour in disableOnDeath)
        {
            behaviour.enabled = true;
        }

        foreach (GameObject obj in disableGameObjectsOnDeath)
        {
            obj.SetActive(true);
        }

        Animator animator = GetComponent<Animator>();
        if (animator != null)
            animator.enabled = true;

        UnityEngine.Animations.Rigging.RigBuilder rig =
            GetComponent<UnityEngine.Animations.Rigging.RigBuilder>();

        if (rig != null)
            rig.enabled = true;

        PS = GetComponent<playerShoot>();

        if (PS != null)
        {
            PS.ResetWeaponState();
        }

        UpdateHealth(maxHealth);

        if (isServer)
        {
            PlayerHealthBar playerHealthBar = GetComponentInChildren<PlayerHealthBar>();

            if (playerHealthBar != null)
                playerHealthBar.RpcUpdateHealthBar(currentHealth);
        }

        yield return null;
        Debug.Log(
            $"BEFORE TARGET RESPAWN name={name} server={isServer} client={isClient} local={isLocalPlayer} owned={isOwned}"
        );
        Debug.Log("===== BEFORE TargetFinishRespawn =====");
        Debug.Log($"isServer = {isServer}");
        Debug.Log($"connection = {connectionToClient}");
        Debug.Log($"identity = {connectionToClient?.identity}");
        Debug.Log($"identity.isSpawned = {connectionToClient?.identity?.netId}");

        Debug.Log("BEFORE TargetFinishRespawn");
        Debug.Log($"SERVER this.netId={netId}");
        Debug.Log($"SERVER identity={GetComponent<NetworkIdentity>().netId}");
        //Debug.Log($"SERVER conn.identity={connectionToClient.identity.netId}");
        if (connectionToClient != null)
        {
            TargetFinishRespawn(connectionToClient);
        }
        PlayerSetup();
        LogPlayerState("AFTER TELEPORT");
    }

    [ClientRpc]
    private void RpcForceRespawnPosition(Vector3 pos, Quaternion rot)
    {
        transform.SetPositionAndRotation(pos, rot);
    }
    [TargetRpc]
    private void TargetFinishRespawn(NetworkConnection target)
    {
        LogPlayerState("TARGET FINISH RESPAWN");
        Debug.Log($"TARGET local={isLocalPlayer} owned={isOwned}");
        Debug.Log($"NetworkClient.localPlayer={NetworkClient.localPlayer?.name}");
        Debug.Log($"This identity={GetComponent<NetworkIdentity>()}");

        // NEW: re-enable local controller/movement scripts on the victim's own client
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

        PlayerSetup();
    }

    [Server]
    public void UpdateHealth(int newHealth)
    {
        currentHealth = newHealth;
        RpcUpdateHealthUI(currentHealth); // Make sure only the server calls this!
    }
    public void SetDefaults()
    {
        WeaponManager = GetComponent<weaponManager>();
        PS = GetComponent<playerShoot>();

        if (WeaponManager == null)
        {
            Debug.LogError("[SetDefaults] WeaponManager is NULL");
            return;
        }

        if (PS == null)
        {
            Debug.LogError("[SetDefaults] PlayerShoot is NULL");
            return;
        }

        currentWeapon = WeaponManager.GetcurrentWeapon();

        if (currentWeapon == null)
        {
            Debug.LogError("[SetDefaults] CurrentWeapon is NULL");
            return;
        }

        PS.InitializeWeapon();

        if (PS.currentWeapon == null)
        {
            Debug.LogError("[SetDefaults] InitializeWeapon failed.");
            return;
        }

        // Sync weapon references
        PS.currentWeapon = currentWeapon;

        // Restore ammo UI
        PS.currentWeapon.currentAmmo = currentWeapon.currentAmmo;

        if (PS.ammoText != null)
        {
            PS.ammoText.text = PS.currentWeapon.currentAmmo.ToString();
        }

        // Stop any lingering invokes
        PS.CancelInvoke("Shoot");
        PS.CancelInvoke("Recoil");

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
    private bool IsPlayerSetupComplete()
    {
        return PS != null && PS.ammoText != null && currentWeapon != null;
    }


    void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            //CmdKillSelf(99999);
            CmdSuicide();
        }
        if (currentWeapon == null)
        {
            currentWeapon = WeaponManager.GetcurrentWeapon();
        }
    }

    public override void OnStartLocalPlayer()
    {
        if (fpsGun != null)
        {
            fpsGun.SetActive(true); // Enable FPS gun only for local player
        }

        // Transform gunHolder = transform.Find("RightHand/GunHolder");
        if (worldGun != null)
        {
            worldGun.gameObject.SetActive(false); // Disable world gun for local player
        }

        if (FPShands != null)
        {
            FPShands.SetActive(true);
        }

        if (healthUICanvas != null)
        {
            healthUICanvas.SetActive(true); // Enable only for the local player
        }

        string playerName = PlayerPrefs.GetString("PlayerName", "Player");
        CmdSetPlayerName(playerName); // Sends it to the server
    }


    public override void OnStartServer()
    {
        base.OnStartServer();

        currentHealth = maxHealth; // Ensure server initializes health
    }

    void OnHealthChanged(int oldHealth, int newHealth)
    {
        Debug.Log($"[CLIENT] {gameObject.name} HP updated: {newHealth}");

        if (healthUI == null)
        {
            healthUI = GetComponentInChildren<HealthUI>(); // Ensure UI is assigned
            if (healthUI == null)
                return;
        }

        healthUI.SetHealth(newHealth); //  Update the UI
    }

    [ClientRpc]
    public void RpcUpdateHealthUI(int newHealth)
    {
        Debug.Log(
            $"[RPC] Update Health Bar called on {gameObject.name}, isServer: {isServer}, isClient: {isClient}, isLocalPlayer: {isLocalPlayer}"
        );
        if (!isClient)
            return; // Prevents execution if not a client
        if (!isLocalPlayer)
            return; // Ensure only local player updates UI

        Debug.Log(
            $"[CLIENT] {gameObject.name} updating health text to {newHealth} HP after respawn"
        );

        HealthUI _healthUI = GetComponentInChildren<HealthUI>();
        if (_healthUI != null)
        {
            _healthUI.SetHealth(newHealth); // Update health text
            Debug.Log($"[CLIENT] Health text updated to {newHealth}");
        }
        else
        {
            Debug.LogError("[CLIENT] HealthUI reference missing on respawn!");
        }
    }

    [Server]
    void CmdSpawnHealthDrop()
    {
        if (healthDropPrefab == null)
            return;

        Vector3 dropPosition = transform.position + Vector3.up * 5f;
        GameObject healthDrop = Instantiate(healthDropPrefab, dropPosition, Quaternion.identity);

        // Ensure Rigidbody starts without random movement
        if (healthDrop.TryGetComponent(out Rigidbody rb))
        {
            rb.AddForce(Vector3.up * 2f, ForceMode.Impulse); // Add upward force
            rb.AddTorque(Vector3.up * 100f, ForceMode.Impulse); // Add random spin
        }

        // Corrected: Now spawning it on the server properly!
        NetworkServer.Spawn(healthDrop);
        Debug.Log("Health drop spawned at " + dropPosition);
    }

    void OnDestroy()
    {
        Debug.Log(gameObject.name + " was destroyed!");
    }

    public void Heal(int healAmount)
    {
        if (!isServer)
            return; // Ensure this runs only on the server

        Debug.Log(
            $"[SERVER] {gameObject.name} Healing by {healAmount}. Old Health: {currentHealth}"
        );

        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth); // Ensure health doesn't exceed max

        Debug.Log($"[SERVER] {gameObject.name} New Health: {currentHealth}/{maxHealth}");

        //RpcUpdateHealthUI(currentHealth);
        UpdateHealth(currentHealth);
        PlayerHealthBar playerHealthBar = GetComponentInChildren<PlayerHealthBar>();
        playerHealthBar.RpcUpdateHealthBar(currentHealth);
    }

    IEnumerator DelayedHealthUpdate(int newHealth)
    {
        yield return new WaitUntil(() => NetworkClient.active);
        // RpcUpdateHealthUI(newHealth);
    }
}