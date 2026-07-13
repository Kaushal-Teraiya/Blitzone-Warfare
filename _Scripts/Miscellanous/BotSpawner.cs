using UnityEngine;
using Mirror;

public class BotSpawner : NetworkBehaviour
{
    public GameObject botPrefab;
    private CharacterSpawner characterSpawner;
    public int botNum;

    private void Start()
    {
        if (!isServer)
            return;

        characterSpawner = FindAnyObjectByType<CharacterSpawner>();

        if (characterSpawner == null)
        {
            Debug.LogError("BotSpawner: CharacterSpawner not found!");
            return;
        }

        int savedBotCount = PlayerPrefs.GetInt("BotCount", botNum);
        // SpawnBots(savedBotCount);
    }

    [Server]
    public void SpawnBots(int botCount)
    {
        for (int i = 0; i < botCount; i++)
        {
            string team = (i % 2 == 0) ? "Red" : "Blue";

            Transform spawnPoint = characterSpawner.GetAvailableSpawnPoint(team);

            if (spawnPoint == null)
            {
                Debug.LogError($"No spawn point for team {team}");
                continue;
            }

            GameObject botInstance = Instantiate(
                botPrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            // Assign team BEFORE spawning
            FlagHandler flagHandler = botInstance.GetComponent<FlagHandler>();

            if (flagHandler != null)
            {
                flagHandler.SetBotTeam(team);
                Debug.Log($"Assigned {team} to {botInstance.name}");
            }
            else
            {
                Debug.LogError("Bot has no FlagHandler!");
            }

            NetworkServer.Spawn(botInstance);
        }
    }

    [Server]
    public void SpawnSingleBot(string team)
    {
        if (characterSpawner == null)
            characterSpawner = FindAnyObjectByType<CharacterSpawner>();

        Transform spawnPoint = characterSpawner.GetAvailableSpawnPoint(team);

        if (spawnPoint == null)
        {
            Debug.LogError($"No spawn point for {team}");
            return;
        }

        GameObject botInstance = Instantiate(
            botPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // Assign team BEFORE spawning
        FlagHandler flagHandler = botInstance.GetComponent<FlagHandler>();

        if (flagHandler != null)
        {
            flagHandler.SetBotTeam(team);
        }
        else
        {
            Debug.LogError("Bot has no FlagHandler!");
        }

        NetworkServer.Spawn(botInstance);
    }

    [Command(requiresAuthority = false)]
    public void CmdSpawnSingleBot(string team)
    {
        SpawnSingleBot(team);
    }
}