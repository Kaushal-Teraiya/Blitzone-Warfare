using Mirror;
using UnityEngine;

public class PlayerWeaponDisplay : NetworkBehaviour
{
    [SerializeField]
    public GameObject worldGun; // Gun visible to others (third-person)

    [SerializeField]
    public GameObject fpsGun; // Gun visible only to me (first-person)

    [SerializeField]
    public GameObject FPShands; // FPS hands (first-person only)

    [SerializeField]
    public GameObject WeaponHolder;

    [SerializeField]
    public GameObject NewWeaponHolder;

    private void Start()
    {
        // Initialize gun visibility based on player perspective
        if (!isLocalPlayer)
        {
            SetupRemotePlayerWeaponDisplay();
        }
        else
        {
            SetupLocalPlayerWeaponDisplay();
        }

        // Disable new weapon holder initially
        if (NewWeaponHolder != null)
            NewWeaponHolder.SetActive(false);
    }

    private void SetupRemotePlayerWeaponDisplay()
    {
        // Hide first-person gun and hands
        if (fpsGun != null)
            fpsGun.SetActive(false);

        if (FPShands != null)
            FPShands.SetActive(false);

        // Show third-person world gun
        if (worldGun != null)
            worldGun.SetActive(true);

        Debug.Log($"[PlayerWeaponDisplay] {gameObject.name} is remote player - showing world gun");
    }

    private void SetupLocalPlayerWeaponDisplay()
    {
        // Show first-person gun and hands
        if (fpsGun != null)
            fpsGun.SetActive(true);

        if (FPShands != null)
            FPShands.SetActive(true);

        // Hide third-person world gun
        if (worldGun != null)
            worldGun.SetActive(false);

        Debug.Log($"[PlayerWeaponDisplay] {gameObject.name} is local player - showing FPS gun");
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        // Ensure correct setup when becoming local player
        if (fpsGun != null)
            fpsGun.SetActive(true);

        if (worldGun != null)
            worldGun.SetActive(false);

        if (FPShands != null)
            FPShands.SetActive(true);
    }

    // Switch to world gun display (for cutscenes or third-person mode).
    public void ShowWorldGun()
    {
        if (worldGun != null)
            worldGun.SetActive(true);

        if (fpsGun != null)
            fpsGun.SetActive(false);
    }

    // Switch to FPS gun display (normal first-person mode).
    public void ShowFpsGun()
    {
        if (fpsGun != null)
            fpsGun.SetActive(true);

        if (worldGun != null)
            worldGun.SetActive(false);
    }

    // Hide all gun displays.
    public void HideAllGuns()
    {
        if (worldGun != null)
            worldGun.SetActive(false);

        if (fpsGun != null)
            fpsGun.SetActive(false);

        if (FPShands != null)
            FPShands.SetActive(false);
    }

    // Get the currently active gun game object.
    public GameObject GetActiveGun()
    {
        if (isLocalPlayer && fpsGun != null && fpsGun.activeSelf)
            return fpsGun;

        if (worldGun != null && worldGun.activeSelf)
            return worldGun;

        return null;
    }
}