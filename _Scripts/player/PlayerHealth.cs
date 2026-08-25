using Mirror;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField]
    public int maxHealth = 100;

    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth;

    private int RealHealth;

    [SerializeField]
    private HealthUI healthUI;

    [SerializeField]
    public Slider HealthBarUI;

    [SerializeField]
    private TextMeshProUGUI HealthText;

    private PlayerHealthBar playerHealthBar;

    private void Start()
    {
        playerHealthBar = GetComponent<PlayerHealthBar>();
        healthUI = GetComponentInChildren<HealthUI>();

        if (isServer)
        {
            currentHealth = maxHealth;
            RealHealth = currentHealth;
        }

        if (isLocalPlayer && healthUI != null)
            healthUI.SetHealth(currentHealth);
    }

    [Server]
    public void TakeDamage(int amount, NetworkIdentity killerId, Vector3 hitPoint, Vector3 hitDirection, string hitBodyPartName)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        RpcUpdateHealth(currentHealth);

        if (currentHealth <= 0)
        {
            PlayerDeath death = GetComponent<PlayerDeath>();

            if (death != null)
            {
                death.ServerDie(killerId, hitPoint, hitDirection, hitBodyPartName);
            }
            else
            {
                Debug.LogError($"[PlayerHealth] PlayerDeath missing on {gameObject.name}");
            }
        }
    }

    [ClientRpc]
    public void RpctakeDamageP(int _amount, NetworkIdentity killerName, Vector3 hitpoint, Vector3 hitDirection, string hitBodyPartName)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Clamp(currentHealth - _amount, 0, maxHealth);
        Debug.Log(transform.name + " now has " + currentHealth + " Health");
    }

    [ClientRpc]
    public void RpctakeDamageU(int _amount, NetworkIdentity killerName)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Clamp(currentHealth - _amount, 0, maxHealth);
        Debug.Log(transform.name + " now has " + currentHealth + " Health");
    }

    [Server]
    public void Heal(int healAmount)
    {
        Debug.Log($"[SERVER] {gameObject.name} Healing by {healAmount}. Old Health: {currentHealth}");

        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);

        Debug.Log($"[SERVER] {gameObject.name} New Health: {currentHealth}/{maxHealth}");

        UpdateHealth(currentHealth);

        if (playerHealthBar != null)
            playerHealthBar.RpcUpdateHealthBar(currentHealth);
    }

    [Server]
    public void UpdateHealth(int newHealth)
    {
        currentHealth = newHealth;
        RpcUpdateHealthUI(newHealth);
    }

    [ClientRpc]
    public void RpcUpdateHealthUI(int newHealth)
    {
        Debug.Log($"[RPC] Update Health Bar called on {gameObject.name}, isServer: {isServer}, isClient: {isClient}, isLocalPlayer: {isLocalPlayer}");

        if (!isClient)
            return;

        if (!isLocalPlayer)
            return;

        HealthUI _healthUI = GetComponentInChildren<HealthUI>();

        if (_healthUI != null)
        {
            _healthUI.SetHealth(newHealth);
            Debug.Log($"[CLIENT] Health text updated to {newHealth}");
        }
        else
        {
            Debug.LogError("[CLIENT] HealthUI reference missing on respawn!");
        }
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        Debug.Log($"[CLIENT] {gameObject.name} HP updated: {newHealth}");

        if (healthUI == null)
            healthUI = GetComponentInChildren<HealthUI>();

        if (healthUI != null)
            healthUI.SetHealth(newHealth);
    }

    [ClientRpc]
    private void RpcUpdateHealth(int newHealth)
    {
        currentHealth = newHealth;

        if (healthUI == null)
            healthUI = GetComponentInChildren<HealthUI>();

        if (healthUI != null && isLocalPlayer)
            healthUI.SetHealth(newHealth);
    }

    public bool isDead => GetComponent<PlayerDeath>()?.isDead ?? false;
}