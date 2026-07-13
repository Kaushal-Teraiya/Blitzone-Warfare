using System.Collections.Generic;
using Firebase.Firestore;

[FirestoreData] // tell Firestore this class can be serialized
[System.Serializable]
public class PlayerData
{
    [FirestoreProperty]
    public string userID { get; set; } = "";

    [FirestoreProperty]
    public string username { get; set; } = "";

    [FirestoreProperty]
    public string email { get; set; } = "";

    [FirestoreProperty]
    public int XP { get; set; } = 0;

    [FirestoreProperty]
    public int level { get; set; } = 1;

    [FirestoreProperty]
    public int kills { get; set; } = 0;

    [FirestoreProperty]
    public int deaths { get; set; } = 0;

    [FirestoreProperty]
    public int coins { get; set; } = 0;

    [FirestoreProperty]
    public int aetherShards { get; set; } = 0;

    [FirestoreProperty]
    public Timestamp createdAt { get; set; }

    [FirestoreProperty]
    public Timestamp lastLogin { get; set; }

    [FirestoreProperty]
    public string currentSessionId { get; set; } = null;

    [FirestoreProperty]
    public string currentMatchId { get; set; } = null;

    [FirestoreProperty]
    public Dictionary<string, int> consumables { get; set; } = new Dictionary<string, int>();

    [FirestoreProperty]
    public List<string> ownedItems { get; set; } = new List<string>();

    // Required empty constructor
    public PlayerData() { }
}
