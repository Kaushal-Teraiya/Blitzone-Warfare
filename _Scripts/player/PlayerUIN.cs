using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIN : NetworkBehaviour
{
    [SerializeField]
    private GameObject PlayerNameHolder;

    [SerializeField]
    private GameObject HealthBarUIobj;

    [SerializeField]
    public Slider HealthBarUI;

    [SerializeField]
    private TextMeshProUGUI HealthText;

    [SerializeField]
    private GameObject healthUICanvas;

    private PlayerNameUI nameUI;
    private HealthUI healthUI;
    private PlayerHealthBar playerHealthBar;

    [SyncVar]
    public string nameOfPlayer;

    private void Start()
    {
        playerHealthBar = GetComponent<PlayerHealthBar>();
        healthUI = GetComponentInChildren<HealthUI>();
        nameUI = GetComponentInChildren<PlayerNameUI>();

        if (HealthBarUIobj != null)
            HealthBarUI = HealthBarUIobj.GetComponent<Slider>();

        // Non-local players: hide first-person UI
        if (!isLocalPlayer)
        {
            HideLocalPlayerUI();
        }

        // Local players: show and initialize UI
        if (isLocalPlayer)
        {
            ShowLocalPlayerUI();
        }

        // Fetch and set player name
        string playerName = PlayerPrefs.GetString("PlayerName", "Player");
        nameOfPlayer = playerName;

        FlagHandler flagHandler = GetComponent<FlagHandler>();

        if (nameUI != null && flagHandler != null)
        {
            nameUI.SetPlayerName(playerName, flagHandler.Team);
        }
    }

    // Hide all local player UI (used for remote players and spectators).
    private void HideLocalPlayerUI()
    {
        if (healthUICanvas != null)
            healthUICanvas.SetActive(false);

        if (gameObject.GetComponentInChildren<Canvas>() != null)
            gameObject.GetComponentInChildren<Canvas>().gameObject.SetActive(false);
    }

    // Show all local player UI (used for local player only).
    private void ShowLocalPlayerUI()
    {
        if (healthUICanvas != null)
            healthUICanvas.SetActive(true);

        if (gameObject.GetComponentInChildren<Canvas>() != null)
            gameObject.GetComponentInChildren<Canvas>().gameObject.SetActive(true);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        ShowLocalPlayerUI();

        // Sync name to server
        string playerName = PlayerPrefs.GetString("PlayerName", "Player");
        CmdSetPlayerName(playerName);
    }

    // Command: Set player name on server (syncs via SyncVar).
    [Command]
    public void CmdSetPlayerName(string _name)
    {
        nameOfPlayer = _name;
    }

    // Hide name and health bar (used on death).
    public void HidePlayerUIElements()
    {
        if (PlayerNameHolder != null)
            PlayerNameHolder.SetActive(false);

        if (HealthBarUIobj != null)
            HealthBarUIobj.SetActive(false);
    }

    // Show name and health bar (used on respawn).
    public void ShowPlayerUIElements()
    {
        if (PlayerNameHolder != null)
            PlayerNameHolder.SetActive(true);

        if (HealthBarUIobj != null)
            HealthBarUIobj.SetActive(true);
    }

    // Update health bar display.
    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (HealthBarUI != null)
        {
            HealthBarUI.value = (float)currentHealth / maxHealth;
        }

        if (HealthText != null)
        {
            HealthText.text = $"{currentHealth}/{maxHealth}";
        }
    }

    // Get the health UI component.
    public HealthUI GetHealthUI()
    {
        if (healthUI == null)
            healthUI = GetComponentInChildren<HealthUI>();

        return healthUI;
    }

    // Get the name UI component.
    public PlayerNameUI GetNameUI()
    {
        if (nameUI == null)
            nameUI = GetComponentInChildren<PlayerNameUI>();

        return nameUI;
    }
}