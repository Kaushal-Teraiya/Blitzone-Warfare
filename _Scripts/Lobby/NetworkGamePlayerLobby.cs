using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkMatchPlayer : NetworkBehaviour
{

    [SerializeField]
    [SyncVar]
    private string team = "None"; // "Blue" or "Red"
    public string Team => team;
    [SyncVar]
    private string playerName;

    public string PlayerName => playerName;


    [SyncVar]
    private int kills;

    public int Kills => kills;

    [SyncVar]
    private int deaths;

    public int Deaths => deaths;

    [SyncVar]
    private string firebaseUID;

    public string FirebaseUID => firebaseUID;
    [SyncVar]
    private uint currentCharacterNetId;



    public NetworkIdentity CurrentCharacter
    {
        get
        {
            if (NetworkServer.spawned.TryGetValue(currentCharacterNetId, out NetworkIdentity identity))
                return identity;

            return null;
        }
    }
    [Server]
    public void SetCurrentCharacter(NetworkIdentity character)
    {
        currentCharacterNetId = character.netId;
    }
    [Server]
    public void AddKill()
    {
        kills++;
        Debug.Log($"AddKill {PlayerName} {kills}");
    }

    [Server]
    public void AddDeath()
    {
        deaths++;
        Debug.Log($"AddDeath {PlayerName} {deaths}");
    }

    [Server]
    public void SetFirebaseUID(string uid)
    {
        firebaseUID = uid;
    }

    public void SetDisplayName(string displayName)
    {
        this.playerName = displayName;
    }

    public void SetTeam(string team)
    {
        Debug.Log($"Setting Team for {PlayerName}: {team}");
        this.team = team;
    }

    private NetworkManagerLobby room;
    private NetworkManagerLobby Room
    {
        get
        {
            if (room == null)
            {
                room = NetworkManager.singleton as NetworkManagerLobby;
            }
            return room;
        }
    }

    [SyncVar]
    private int selectedCharacterIndex;

    public int SelectedCharacterIndex => selectedCharacterIndex;

    [Server]
    public void SetSelectedCharacterIndex(int index)
    {
        selectedCharacterIndex = index;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        DontDestroyOnLoad(gameObject);

        if (!Room.GamePlayers.Contains(this))
            Room.GamePlayers.Add(this);

        Debug.Log($"Added MatchPlayer. Count={Room.GamePlayers.Count}");
    }

    public override void OnStopServer()
    {
        Room.GamePlayers.Remove(this);

        Debug.Log($"Removed MatchPlayer. Count={Room.GamePlayers.Count}");

        base.OnStopServer();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        Room.GamePlayers.Remove(this);
    }

    private void OnDestroy()
    {
        Debug.Log($"[MATCHPLAYER] DESTROY {netId}");
    }
}
