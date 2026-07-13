using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManagerLobby : NetworkManager
{
    [SerializeField]
    private string menuScene = "Lobby";

    //[SerializeField][SyncVar] private string mapName = null;

    [Header("Room")]
    [SerializeField]
    private NetworkRoomPlayerLobby roomPlayerPrefab = null;

    [SerializeField]
    private int minPlayers = 2;

    [Header("Game")]
    [SerializeField]
    private NetworkMatchPlayer gamePlayerPrefab = null;

    public static event Action onClientConnected;
    public static event Action OnClientDisconnected;

    public List<NetworkRoomPlayerLobby> RoomPlayers { get; } = new List<NetworkRoomPlayerLobby>();
    public List<NetworkMatchPlayer> GamePlayers { get; } = new List<NetworkMatchPlayer>();
    public static NetworkManagerLobby instance;
    public MapSelectionMessage MapSelection;
    public string selectedMapName;
    public MapData[] availableMaps;
    private int connectedPlayers = 0;

    [SerializeField] private float serverResetDelay = 3f;
    [SerializeField] private bool autoResetserver = true;
    private bool isServerResetting = false;
    [SerializeField] private string characterSelectionScene;
    //private object availableMaps;

    public struct MapSelectionMessage : NetworkMessage
    {
        public string mapName;
    }

    private void OnReceiveMapSelection(NetworkConnectionToClient conn, MapSelectionMessage msg)
    {
        Debug.Log("Received Map Selection Message!");

        if (conn != NetworkServer.localConnection) // Only process from the host
        {
            return;
        }

        selectedMapName = msg.mapName;
        Debug.Log($"Map selection updated: {selectedMapName}");

        // Get map scene name
        MapData selectedMapData = availableMaps.FirstOrDefault(map =>
            map.mapName == selectedMapName
        );
        if (selectedMapData != null)
        {
            selectedMapName = selectedMapData.mapName;
            Debug.Log($"Selected scene: {selectedMapName}");
        }

        // Broadcast to all clients
        NetworkServer.SendToAll(msg);
    }

    public override void Awake()
    {
        instance = this; // Singleton setup
        //MapSelection = GetComponent<MapSelectionMessage>();

        NetworkClient.OnConnectedEvent += OnClientConnect;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        spawnPrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs").ToList();
        NetworkServer.RegisterHandler<MapSelectionMessage>(OnReceiveMapSelection);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        NetworkClient.RegisterHandler<MapSelectionMessage>(
            (conn, msg) =>
            {
                Debug.Log($"Client received map name: {selectedMapName}");
            }
        );

        var spawnablePrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs");
        foreach (var prefab in spawnablePrefabs)
        {
            NetworkClient.RegisterPrefab(prefab);
        }

        Debug.Log("🔹 Registering scene objects...");
        NetworkServer.SpawnObjects(); // Ensure scene objects sync correctly
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        onClientConnected?.Invoke();

        // Grab Firebase UID from client’s auth
        string uid = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        Debug.LogWarning("[Client] Firebase UID: " + uid);
        // request spawn
        if (NetworkClient.localPlayer == null)
        {
            Debug.Log("Requesting AddPlayer...");
            NetworkClient.Send(new AddPlayerMessage());
        }
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        OnClientDisconnected?.Invoke();
        Debug.Log("Client disconnected (custom manager).");

        // If we're not the host (server + client), load character selection
        if (!NetworkServer.active)
        {
            Debug.Log("Client-only disconnected. Returning to Character Selection...");
            SceneManager.LoadScene("Character Selection");
        }
        else
        {
            Debug.Log("Host/server disconnect detected. Not loading scene.");
        }
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);

        connectedPlayers++;
        isServerResetting = false;
        StopAllCoroutines();

        Debug.Log($"[Server] Players Connected. Total Players : {connectedPlayers}");

        if (numPlayers >= maxConnections)
        {
            conn.Disconnect();
            return;
        }

        if (SceneManager.GetActiveScene().path != menuScene)
        {
            conn.Disconnect();
            return;
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // base.OnServerAddPlayer(conn);
        if (
            SceneManager.GetActiveScene().name
            == System.IO.Path.GetFileNameWithoutExtension(menuScene)
        )
        {
            bool isLeader = RoomPlayers.Count == 0;
            NetworkRoomPlayerLobby roomPlayerInstance = Instantiate(roomPlayerPrefab);
            roomPlayerInstance.IsLeader = isLeader;
            int blueCount = RoomPlayers.Count(p => p.Team == "Blue");
            int redCount = RoomPlayers.Count(p => p.Team == "Red");
            roomPlayerInstance.SetTeam(blueCount <= redCount ? "Blue" : "Red");
            RoomPlayers.Add(roomPlayerInstance);
            NetworkServer.AddPlayerForConnection(conn, roomPlayerInstance.gameObject);
        }

        Debug.Log($"OnServerAddPlayer called for {conn.connectionId}");

    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log("############################");
        Debug.Log($"OnServerDisconnect {conn.connectionId}");
        Debug.Log($"Identity = {conn.identity}");
        Debug.Log("############################");

        if (conn.identity != null)
        {
            var player = conn.identity.GetComponent<NetworkRoomPlayerLobby>();
            if (player != null)
            {
                RoomPlayers.Remove(player);
            }

            NotifyPlayersOfReadyState();
            NetworkServer.Destroy(conn.identity.gameObject);
        }

        connectedPlayers--;
     
        Debug.Log($"[Server] Player Disconnected. Left Players: {connectedPlayers}");

        if (connectedPlayers <= 0 && autoResetserver && !isServerResetting)
        {
            Debug.Log($"[Server] All Players Disocnnected. Resetting in : {serverResetDelay}s..");
            StartCoroutine(ResetServer());
        }

        base.OnServerDisconnect(conn);
    }
    public override void OnStopServer()
    {
        RoomPlayers.Clear();
        GamePlayers.Clear();
        Debug.Log("[Server] All players list cleared");
    }

    private IEnumerator ResetServer()
    {
        yield return new WaitForSeconds(serverResetDelay);

        if (connectedPlayers > 0)
        {
            isServerResetting = false;
            yield break;
        }

        Debug.Log("Server resetting.");
        RoomPlayers.Clear();
        GamePlayers.Clear();
        yield return StartCoroutine(CleanupServerObjects());
        yield return null;

        if (SceneManager.GetActiveScene().name != "Lobby")
        {
            Debug.Log("Returning to character selection screen");
            ServerChangeScene(menuScene);
        }
        else
        {
            Debug.Log("Already in the scene");
        }

    }

    private IEnumerator CleanupServerObjects()
    {
        foreach (var gamePlayer in GamePlayers.ToList())
        {
            if (gamePlayer != null && gamePlayer.gameObject != null)
            {
                SceneManager.MoveGameObjectToScene(gamePlayer.gameObject, SceneManager.GetActiveScene());
            }
        }

        yield return null;

        NetworkIdentity[] allNetworkObjects = FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None);

        foreach (var netId in allNetworkObjects)
        {
            if (netId.GetComponent<NetworkManager>() != null)
            {
                continue;
            }

            if (netId.isServer)
            {
                NetworkServer.Destroy(netId.gameObject);
            }
        }

        yield return null;

    }
    public void NotifyPlayersOfReadyState()
    {
        Debug.Log("========== NotifyPlayersOfReadyState ==========");

        bool ready = IsReadyToStart();

        Debug.Log($"Ready Result: {ready}");

        foreach (var player in RoomPlayers)
        {
            Debug.Log($"{player.DisplayName} Ready = {player.IsReady}");
            player.HandleReadyToStart(ready);
        }

        if (ready)
        {
            Debug.Log("Calling StartGame()...");
            StartGame();
        }

        Debug.Log("========== End NotifyPlayersOfReadyState ==========");
    }
    private bool IsReadyToStart()
    {
        if (numPlayers < minPlayers)
        {
            Debug.Log($"Not enough players! Current: {numPlayers}, Required: {minPlayers}");
            return false;
        }

        foreach (var player in RoomPlayers)
        {
            if (!player.IsReady)
            {
                Debug.Log($"Player {player.DisplayName} is NOT ready!");
                return false;
            }
        }

        Debug.Log("All players are ready!");
        return true;
    }

    public void StartGame()
    {
        Debug.Log("StartGame() function was called!");
        Debug.Log($"menuScene is assigned as: {menuScene}");

        Debug.Log($"Active Scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"menuScene variable: {menuScene}");

        string currentScene = "Lobby";
        Debug.Log($"Current Scene: {currentScene}, Expected: {menuScene}");
        Debug.Log($"menuScene is assigned as: {menuScene}");

        if (currentScene == "Lobby")
        {
            if (!IsReadyToStart())
            {
                Debug.Log("Not all players are ready, game cannot start.");
                return;
            }

            Debug.Log("All players are ready! Changing scene...");
            ServerChangeScene(selectedMapName); // load Map scene
            Debug.Log("Start button clicked, changing scene.");
        }
        else
        {
            Debug.LogError("Scene does not match menuScene! Cannot start game.");
        }
    }

    public override void Start()
    {
        base.Start();
        Debug.Log("NetworkManagerLobby.Start()");
        Debug.Log("Batch Mode = " + Application.isBatchMode);
        if (string.IsNullOrEmpty(menuScene))
        {
            Debug.LogError("menuScene is not set in the Inspector! Assigning default...");
            menuScene = "Lobby"; // Change this to your actual menu scene name.
        }

        if (Application.isBatchMode)
        {
            Debug.Log("Dedicated Server detected. Starting server...");
            StartServer();
        }
    }

    public override void ServerChangeScene(string newSceneName)
    {
        Debug.Log($"ServerChangeScene() called! Changing to: {newSceneName}");
        Debug.Log($"ServerChangeScene to {newSceneName}");

        Debug.Log($"RoomPlayers count: {RoomPlayers.Count}");
        for (int i = 0; i < RoomPlayers.Count; i++)
        {
            Debug.Log(
                $"RoomPlayer[{i}]: {RoomPlayers[i].DisplayName} - Team: {RoomPlayers[i].Team}"
            );
        }

        if (SceneManager.GetActiveScene().name == "Lobby")
        {
            for (int i = RoomPlayers.Count - 1; i >= 0; i--)
            {
                var conn = RoomPlayers[i].connectionToClient;
                Debug.Log($"Replacing player for connection: {conn}");

                var gameplayerInstance = Instantiate(gamePlayerPrefab);
                gameplayerInstance.SetDisplayName(RoomPlayers[i].DisplayName);
                gameplayerInstance.SetTeam(RoomPlayers[i].Team);
                gameplayerInstance.SetSelectedCharacterIndex(RoomPlayers[i].SelectedCharacterIndex);

                // if (conn.identity != null)
                // {
                //     Debug.Log($"Destroying: {conn.identity.gameObject.name}");
                //     NetworkServer.Destroy(conn.identity.gameObject);
                // }
                // else
                // {
                //     Debug.LogError($"ERROR: conn.identity is NULL for player {i}!");
                // }

                NetworkServer.ReplacePlayerForConnection(
                    conn,
                    gameplayerInstance.gameObject,
                    ReplacePlayerOptions.KeepAuthority
                );
                Debug.Log("===== AFTER REPLACE =====");
                Debug.Log($"conn.identity = {conn.identity}");
                Debug.Log($"conn.identity.name = {conn.identity?.gameObject.name}");
                Debug.Log($"conn.identity.netId = {conn.identity?.netId}");
                Debug.Log(
                    $"ReplacePlayerForConnection called for conn {conn.connectionId} with {gameplayerInstance.name}"
                );
                Debug.Log(
    $"After Replace: conn.identity = {conn.identity.name}, scene = {conn.identity.gameObject.scene.name}"
);
            }
        }

        //tartCoroutine(DelayedSpawnObjects());

        base.ServerChangeScene(newSceneName);
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        Debug.Log("========== OnServerSceneChanged ==========");

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn.identity == null)
            {
                Debug.Log($"Conn {conn.connectionId} -> identity = NULL");
                continue;
            }

            Debug.Log(
                $"Conn {conn.connectionId} -> identity = {conn.identity.name}, " +
                $"netId = {conn.identity.netId}, " +
                $"scene = {conn.identity.gameObject.scene.name}"
            );
        }

        if (sceneName != selectedMapName)
        {
            Debug.Log("Wrong scene, returning.");
            return;
        }

        CharacterSpawner spawner = FindFirstObjectByType<CharacterSpawner>();

        if (spawner == null)
        {
            Debug.LogError("CharacterSpawner NOT FOUND!");
            return;
        }

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn.identity == null)
                continue;

            NetworkMatchPlayer matchPlayer = conn.identity.GetComponent<NetworkMatchPlayer>();

            if (matchPlayer == null)
            {
                Debug.LogError(
                    $"Conn {conn.connectionId}: identity '{conn.identity.name}' has NO NetworkMatchPlayer!"
                );
                continue;
            }

            Debug.Log(
                $"Spawning character for {matchPlayer.PlayerName} ({matchPlayer.Team}) " +
                $"netId={matchPlayer.netId}"
            );

            spawner.SpawnCharacter(matchPlayer);
        }

        Debug.Log("========== End OnServerSceneChanged ==========");
    }
    IEnumerator DelayedSpawnObjects()
    {
        yield return new WaitForSeconds(1f);
        NetworkServer.SpawnObjects();
        Debug.Log("SpawnObjects called post-scene change.");
    }


}
