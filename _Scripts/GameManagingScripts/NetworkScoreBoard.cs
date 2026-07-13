using System.Collections;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkScoreBoard : NetworkBehaviour
{
    // private NetworkManagerLobby players;

    [SerializeField] private Button hideButton;
    [SerializeField] private Button unhideButton;
    [SerializeField] private GameObject scoreBoardUI;

    [Header("Blue Team")]
    [SerializeField] private TMP_Text[] blueTeamSlots = new TMP_Text[4];
    [SerializeField] private TMP_Text[] blueTeamKills = new TMP_Text[4];
    [SerializeField] private TMP_Text[] blueTeamDeaths = new TMP_Text[4];

    [Header("Red Team")]
    [SerializeField] private TMP_Text[] redTeamSlots = new TMP_Text[4];
    [SerializeField] private TMP_Text[] redTeamKills = new TMP_Text[4];
    [SerializeField] private TMP_Text[] redTeamDeaths = new TMP_Text[4];

    private void Start()
    {
        // players = NetworkManager.singleton as NetworkManagerLobby;

        hideButton.onClick.AddListener(HideBoard);
        unhideButton.onClick.AddListener(UnhideScoreBoard);

        InvokeRepeating(nameof(UpdateScoreBoard), 0f, 1f);
    }

    private void UpdateScoreBoard()
    {
        // if (players == null)
        //     return;
        // Debug.Log($"GamePlayers Count = {players.GamePlayers.Count}");

        // // Clear UI
        // for (int i = 0; i < 4; i++)
        // {
        //     blueTeamSlots[i].text = "";
        //     blueTeamKills[i].text = "";
        //     blueTeamDeaths[i].text = "";

        //     redTeamSlots[i].text = "";
        //     redTeamKills[i].text = "";
        //     redTeamDeaths[i].text = "";
        // }


        int blueIndex = 0;
        int redIndex = 0;

        NetworkMatchPlayer[] players = FindObjectsByType<NetworkMatchPlayer>(
     FindObjectsSortMode.None
 );

        foreach (NetworkMatchPlayer player in players)
        {
            if (player == null)
                continue;

            // Ignore destroyed/unspawned players if necessary
            if (!player.isClient)
                continue;

            string color = player.Team == "Blue" ? "blue" : "red";
            string playerName = $"<color={color}>{player.PlayerName}</color>";

            if (player.Team == "Blue")
            {
                if (blueIndex >= blueTeamSlots.Length)
                    continue;

                blueTeamSlots[blueIndex].text = playerName;
                blueTeamKills[blueIndex].text = player.Kills.ToString();
                blueTeamDeaths[blueIndex].text = player.Deaths.ToString();

                blueIndex++;
            }
            else
            {
                if (redIndex >= redTeamSlots.Length)
                    continue;

                redTeamSlots[redIndex].text = playerName;
                redTeamKills[redIndex].text = player.Kills.ToString();
                redTeamDeaths[redIndex].text = player.Deaths.ToString();

                redIndex++;
            }
        }

        foreach (NetworkMatchPlayer player in players)
        {
            Debug.Log(
                $"{player.PlayerName} | Team={player.Team} | K={player.Kills} | D={player.Deaths}"
            );
        }

    }

    private void HideBoard()
    {
        scoreBoardUI.SetActive(false);
        hideButton.gameObject.SetActive(false);
    }

    private void UnhideScoreBoard()
    {
        scoreBoardUI.SetActive(true);
        hideButton.gameObject.SetActive(true);
    }
}