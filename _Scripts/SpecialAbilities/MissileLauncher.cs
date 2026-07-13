using Mirror;
using UnityEngine;

public class RocketLauncher : NetworkBehaviour
{
    public GameObject rocketPrefab; // Assign in Inspector
    public Transform firePoint; // Assign in Inspector
    public float fireRate = 1f;
    private float nextFireTime = 0f;

    void Update()
    {
        if (!isOwned)
        {
            Debug.Log("RocketLauncher is not owned by this player, skipping Update.");
            return;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0) && Time.time >= nextFireTime)
        {
            Debug.Log(
                $"Shooting rocket! Next fire time: {nextFireTime}, Current time: {Time.time}"
            );
            nextFireTime = Time.time + fireRate;
            CmdFireRocket();
        }
    }

    [Command]
    void CmdFireRocket()
    {
        Debug.Log("CmdFireRocket called on the server!");

        if (rocketPrefab == null || firePoint == null)
        {
            Debug.LogError("Rocket Prefab or FirePoint is missing!");
            return;
        }

        GameObject rocketInstance = Instantiate(
            rocketPrefab,
            firePoint.position,
            firePoint.rotation
        );
        NetworkServer.Spawn(rocketInstance);

        Debug.Log("Rocket spawned on the server!");
    }
}
