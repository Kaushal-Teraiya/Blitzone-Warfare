using System.Collections.Generic;
using Mirror;
using UnityEngine;
public class CharacterSpawner : NetworkBehaviour
{
    public Character[] allCharacters; // Assign prefabs in Inspector

    // public Transform[] spawnPoints;  // Assign spawn points in Inspector
    public Transform[] teamASpawnPoints; // Assign in Inspector
    public Transform[] teamBSpawnPoints; // Assign in Inspector

    [SerializeField]
    public List<Transform> availableTeamASpawns;

    [SerializeField]
    public List<Transform> availableTeamBSpawns;

    //[SerializeField]
    //private string mapName = null;
    void Start()
    {
        if (!isServer)
            return;

        availableTeamASpawns = new List<Transform>(teamASpawnPoints);
        availableTeamBSpawns = new List<Transform>(teamBSpawnPoints);
    }

    public static CharacterSpawner instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public Transform GetAvailableSpawnPoint(string team)
    {
        List<Transform> spawnList = (team == "Blue") ? availableTeamASpawns : availableTeamBSpawns;

        if (spawnList.Count == 0)
        {
            Debug.Log($"Resetting spawn points for Team {team}");
            spawnList = (team == "Blue")
                    ? new List<Transform>(teamASpawnPoints)
                    : new List<Transform>(teamBSpawnPoints);
        }

        if (spawnList.Count == 0)
        {
            Debug.LogError($"No available spawn points for team {team}!");
            return null;
        }

        // Select a random spawn point
        int index = Random.Range(0, spawnList.Count);
        Transform spawnPoint = spawnList[index];
        spawnList.RemoveAt(index); // Remove used spawn point

        return spawnPoint;
    }

    public void SpawnCharacter(NetworkMatchPlayer matchPlayer)
    {
        if (!isServer)
        {
            Debug.LogError(
                "CmdSpawnCharacter() was called on the client! It must be called on the server."
            );
            return;
        }

        if (matchPlayer.connectionToClient == null)
        {
            Debug.LogError("MatchPlayer has no connection.");
            return;
        }

        if (matchPlayer.SelectedCharacterIndex < 0 || matchPlayer.SelectedCharacterIndex >= allCharacters.Length)
        {
            Debug.LogError(
                $"[ERROR] Player {(matchPlayer != null ? matchPlayer.connectionToClient.connectionId.ToString() : "NULL CONNECTION")} tried to spawn an invalid character index: {matchPlayer.SelectedCharacterIndex}"
            );
            return;
        }

        GameObject selectedCharacterPrefab = allCharacters[matchPlayer.SelectedCharacterIndex].GameplayCharacterPrefab;



        string playerTeam = matchPlayer.Team;
        Debug.Log(
            $"[DEBUG] Assigning team '{playerTeam}' to new character for player {matchPlayer.connectionToClient.connectionId}"
        );

        Transform spawnPoint = null;

        if (playerTeam == "Blue" && availableTeamASpawns.Count >= 0)
        {
            if (availableTeamASpawns.Count == 0)
            {
                Debug.Log("Resetting available spawn points for Team Blue.");
                availableTeamASpawns = new List<Transform>(teamASpawnPoints);
            }

            int index = Random.Range(0, availableTeamASpawns.Count);
            spawnPoint = availableTeamASpawns[index];
            availableTeamASpawns.RemoveAt(index); // Remove used spawn point
        }
        else if (playerTeam == "Red" && availableTeamBSpawns.Count >= 0)
        {
            if (availableTeamBSpawns.Count == 0)
            {
                Debug.Log("Resetting available spawn points for Team Red.");
                availableTeamBSpawns = new List<Transform>(teamBSpawnPoints);
            }
            int index = Random.Range(0, availableTeamBSpawns.Count);
            spawnPoint = availableTeamBSpawns[index];
            availableTeamBSpawns.RemoveAt(index); // Remove used spawn point
        }

        if (spawnPoint == null)
        {
            Debug.LogError($"[ERROR] No available spawn point for team {playerTeam}!");
            return;
        }


        GameObject playerInstance = Instantiate(
        selectedCharacterPrefab,
        spawnPoint.position,
        spawnPoint.rotation
    );

        if (playerInstance == null)
        {
            Debug.LogError("Failed to instantiate player.");
            return;
        }

        matchPlayer.SetCurrentCharacter(playerInstance.GetComponent<NetworkIdentity>());

        player playerScript = playerInstance.GetComponent<player>();

        if (playerScript != null)
        {
            playerScript.matchPlayerNetId = matchPlayer.netId;
        }
        Debug.Log($"Before Replace identity = {matchPlayer.connectionToClient.identity.netId}");


        NetworkServer.ReplacePlayerForConnection(matchPlayer.connectionToClient, playerInstance, ReplacePlayerOptions.KeepAuthority);
        Debug.Log($"After Replace identity = {matchPlayer.connectionToClient.identity.netId}");
    }
}
