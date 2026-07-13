using System;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Firestore;
using Mirror;
using UnityEngine;

public class MatchResultHandler : NetworkBehaviour
{
    [Server]
    public void SaveMatchResults(string matchId)
    {
        FirebaseFirestore db = FirebaseInit.Instance.db;

        NetworkManagerLobby manager = NetworkManager.singleton as NetworkManagerLobby;

        foreach (NetworkMatchPlayer player in manager.GamePlayers)
        {
            if (player == null)
                continue;

            if (string.IsNullOrEmpty(player.FirebaseUID))
            {
                Debug.LogWarning($"No Firebase UID for {player.PlayerName}");
                continue;
            }

            PlayerData update = new PlayerData
            {
                userID = player.FirebaseUID,
                kills = player.Kills,
                deaths = player.Deaths,
                XP = CalculateXP(player.Kills, player.Deaths),
                coins = CalculateCoins(player.Kills, player.Deaths),
                aetherShards = UnityEngine.Random.Range(0, 2),
                lastLogin = Timestamp.FromDateTime(DateTime.UtcNow),
                currentSessionId = matchId,
            };

            DocumentReference docRef =
                db.Collection("players").Document(update.userID);

            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "kills", FieldValue.Increment(update.kills) },
                { "deaths", FieldValue.Increment(update.deaths) },
                { "XP", FieldValue.Increment(update.XP) },
                { "coins", FieldValue.Increment(update.coins) },
                { "aetherShards", FieldValue.Increment(update.aetherShards) },
                { "lastLogin", update.lastLogin },
                { "currentSessionId", update.currentSessionId }
            };

            docRef.SetAsync(updates, SetOptions.MergeAll)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        Debug.LogError($"Failed to save {update.userID}: {t.Exception}");
                    else
                        Debug.Log($"Updated stats for {update.userID}");
                });
        }
    }

    private int CalculateXP(int kills, int deaths)
    {
        return (kills * 100) - (deaths * 20);
    }

    private int CalculateCoins(int kills, int deaths)
    {
        return kills * 10;
    }
}