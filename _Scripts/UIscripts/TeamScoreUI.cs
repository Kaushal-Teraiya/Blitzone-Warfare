using TMPro;
using UnityEngine;

public class TeamScoreUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text blueTeamScoreText;
    [SerializeField] private TMP_Text redTeamScoreText;

    private WinningConditions winningConditions;

    private void Start()
    {
        winningConditions = WinningConditions.Instance;
    }

    private void Update()
    {
        if (winningConditions == null)
        {
            winningConditions = WinningConditions.Instance;
            return;
        }

        blueTeamScoreText.text = winningConditions.BlueTeamScore.ToString();
        redTeamScoreText.text = winningConditions.RedTeamScore.ToString();
    }
}