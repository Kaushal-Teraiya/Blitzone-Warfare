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
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerDeath))]
[RequireComponent(typeof(PlayerUIN))]
[RequireComponent(typeof(PlayerWeaponDisplay))]
[RequireComponent(typeof(PlayerInitialization))]
public class player : NetworkBehaviour
{
    // Component references (cached for quick access)
    private PlayerHealth playerHealth;
    private PlayerDeath playerDeath;
    private PlayerUIN playerUIN;
    private PlayerWeaponDisplay playerWeaponDisplay;
    private PlayerInitialization playerInitialization;

    // Original public fields (for backward compatibility with existing code)
    [SyncVar]
    public uint matchPlayerNetId;

    [SerializeField]
    public Rigidbody _rb;

    [SerializeField]
    private float gunForce;

    [SerializeField]
    private float gunTorque;

    public GameObject WeaponHolder;
    public GameObject gun;
    public GameObject healthUICanvas;
    public GameObject worldGun;
    public GameObject fpsGun;
    public GameObject healthDropPrefab;
    public GameObject NewWeaponHolder;
    public GameObject FPShands;
    public TextMeshProUGUI HealthText;
    public GameObject PlayerNameHolder;
    public GameObject HealthBarUIobj;
    public Slider HealthBarUI;

    [SyncVar]
    public string nameOfPlayer;

    public playerWeapon currentWeapon;
    public weaponManager WeaponManager;

    private bool _isDead = false;

    public bool isDead
    {
        get { return playerDeath != null ? playerDeath.isDead : _isDead; }
    }

    public int currentHealth
    {
        get { return playerHealth != null ? playerHealth.currentHealth : 0; }
    }

    public int maxHealth
    {
        get { return playerHealth != null ? playerHealth.maxHealth : 100; }
    }

    public Vector3 storedHitPoint
    {
        get { return playerDeath != null ? playerDeath.storedHitPoint : Vector3.zero; }
        set { if (playerDeath != null) playerDeath.storedHitPoint = value; }
    }

    public Vector3 storedHitDirection
    {
        get { return playerDeath != null ? playerDeath.storedHitDirection : Vector3.zero; }
        set { if (playerDeath != null) playerDeath.storedHitDirection = value; }
    }

    public string storedHitBodyPartName
    {
        get { return playerDeath != null ? playerDeath.storedHitBodyPartName : ""; }
        set { if (playerDeath != null) playerDeath.storedHitBodyPartName = value; }
    }

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

    Camera cam;

    void Start()
    {
        // Cache component references
        playerHealth = GetComponent<PlayerHealth>();
        playerDeath = GetComponent<PlayerDeath>();
        playerUIN = GetComponent<PlayerUIN>();
        playerWeaponDisplay = GetComponent<PlayerWeaponDisplay>();
        playerInitialization = GetComponent<PlayerInitialization>();
        _rb = GetComponent<Rigidbody>();
        cam = Camera.main;

        // Initialize weapon system
        WeaponManager = GetComponent<weaponManager>();
        if (WeaponManager != null)
        {
            currentWeapon = WeaponManager.GetcurrentWeapon();
        }

        // Log initialization
        Debug.Log($"[player] Initialized for {gameObject.name}");
    }

    // Public setup method (called during respawn). Delegates to PlayerInitialization.
    public void PlayerSetup()
    {
        if (playerInitialization != null)
        {
            playerInitialization.PlayerSetup();
        }
    }

    // Command: Set player name on server (delegates to PlayerUI).
    [Command]
    void CmdSetPlayerName(string _name)
    {
        if (playerUIN != null)
        {
            playerUIN.CmdSetPlayerName(_name);
        }
    }

    // Command: Suicide. Delegates to PlayerDeath.
    [Command]
    public void CmdSuicide()
    {
        if (playerDeath != null)
        {
            playerDeath.CmdSuicide();
        }
    }

    void Update()
    {
        if (!isLocalPlayer)
            return;

        // K key to suicide (debug)
        if (Input.GetKeyDown(KeyCode.K))
        {
            CmdSuicide();
        }

        // Ensure weapon is cached
        if (currentWeapon == null && WeaponManager != null)
        {
            currentWeapon = WeaponManager.GetcurrentWeapon();
        }
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (playerWeaponDisplay != null)
        {
            playerWeaponDisplay.OnStartLocalPlayer();
        }

        if (playerUIN != null)
        {
            playerUIN.OnStartLocalPlayer();
        }

        string playerName = PlayerPrefs.GetString("PlayerName", "Player");
        CmdSetPlayerName(playerName);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (playerInitialization != null)
        {
            playerInitialization.OnStartServer();
        }
    }

    // ===== HEALTH-RELATED DELEGATIONS =====

    // Server: Apply damage to this player.
    [Server]
    public void TakeDamage(
        int amount,
        NetworkIdentity killerId,
        Vector3 hitPoint,
        Vector3 hitDirection,
        string hitBodyPartName)
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(amount, killerId, hitPoint, hitDirection, hitBodyPartName);
        }

        // Trigger death if health is 0 or below
        if (playerHealth != null && playerHealth.currentHealth <= 0 && playerDeath != null)
        {
            playerDeath.ServerDie(killerId, hitPoint, hitDirection, hitBodyPartName);
        }
    }

    // Server: Heal this player.
    [Server]
    public void Heal(int healAmount)
    {
        if (playerHealth != null)
        {
            playerHealth.Heal(healAmount);
        }
    }

    // Server: Update health directly.
    [Server]
    public void UpdateHealth(int newHealth)
    {
        if (playerHealth != null)
        {
            playerHealth.UpdateHealth(newHealth);
        }
    }

    // ===== DEATH-RELATED DELEGATIONS =====

    // Server: Handle player death.
    [Server]
    public void ServerDie(
        NetworkIdentity killerId,
        Vector3 hitPoint,
        Vector3 hitDirection,
        string hitBodyPartName)
    {
        if (playerDeath != null)
        {
            playerDeath.ServerDie(killerId, hitPoint, hitDirection, hitBodyPartName);
        }
    }

    // ===== WEAPON-RELATED DELEGATIONS =====

    // Get the currently active weapon for this player.
    public GameObject GetActiveWeapon()
    {
        if (playerWeaponDisplay != null)
        {
            return playerWeaponDisplay.GetActiveGun();
        }

        return null;
    }

    // ===== UI-RELATED DELEGATIONS =====

    // Show player UI elements (name, health bar).
    public void ShowPlayerUIElements()
    {
        if (playerUIN != null)
        {
            playerUIN.ShowPlayerUIElements();
        }
    }

    // Hide player UI elements (name, health bar).
    public void HidePlayerUIElements()
    {
        if (playerUIN != null)
        {
            playerUIN.HidePlayerUIElements();
        }
    }

    void OnDestroy()
    {
        Debug.Log(gameObject.name + " was destroyed!");
    }
}