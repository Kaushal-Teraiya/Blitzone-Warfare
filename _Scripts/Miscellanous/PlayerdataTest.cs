using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;
using UnityEngine;

public class PlayerDataTest : MonoBehaviour
{
    private FirebaseFirestore db;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        // Create a test player data instance
        PlayerData testPlayer = new PlayerData
        {
            userID = "AXbcU0Ia55TDlUAZ7m48RfrJODp2",
            username = "Sybau",
            email = "Sybau@example.com",
            XP = 500,
            level = 2,
            kills = 10,
            deaths = 3,
            //lastLoginTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };

        // Save to Firestore
        SavePlayerData(testPlayer);

        // Load from Firestore after a short delay
        StartCoroutine(LoadAfterDelay("AXbcU0Ia55TDlUAZ7m48RfrJODp2", 2f));
    }

    private IEnumerator LoadAfterDelay(string userID, float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadPlayerData(userID);
    }

    void SavePlayerData(PlayerData data)
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection("users").Document(data.userID);

        // Convert to dictionary
        Dictionary<string, object> playerDict = new Dictionary<string, object>
        {
            { "userID", data.userID },
            { "username", data.username },
            { "email", data.email },
            { "XP", data.XP },
            { "level", data.level },
            { "kills", data.kills },
            { "deaths", data.deaths },
           // { "lastLoginTimestamp", data.lastLoginTimestamp },
        };

        docRef
            .SetAsync(playerDict)
            .ContinueWith(task =>
            {
                if (task.IsCompleted)
                    Debug.Log("Player data saved successfully!");
                else
                    Debug.LogError("Failed to save player data: " + task.Exception);
            });
    }

    void LoadPlayerData(string userID)
    {
        DocumentReference docRef = db.Collection("users").Document(userID);
        docRef
            .GetSnapshotAsync()
            .ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    DocumentSnapshot snapshot = task.Result;
                    if (snapshot.Exists)
                    {
                        Dictionary<string, object> data = snapshot.ToDictionary();

                        string username = data.ContainsKey("username")
                            ? data["username"].ToString()
                            : "N/A";
                        int xp = data.ContainsKey("XP") ? Convert.ToInt32(data["XP"]) : 0;
                        int level = data.ContainsKey("level") ? Convert.ToInt32(data["level"]) : 0;

                        Debug.Log($"Loaded Player: {username}, Coins (XP): {xp}, Level: {level}");
                    }
                    else
                    {
                        Debug.LogWarning("Player data not found!");
                    }
                }
                else
                {
                    Debug.LogError("Failed to load PlayerData: " + task.Exception);
                }
            });
    }
}
