using TMPro;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ScoreLeaderboard : NetworkBehaviour
{
    public TextMeshProUGUI first;
    public TextMeshProUGUI second;
    public TextMeshProUGUI third;

    private List<GameObject> playersList;
    void Update()
    {
        
        if(NetworkManager.Singleton != null)
            playersList = NetworkManager.Singleton.GetComponent<PlayersList>().players;

        if (playersList == null || playersList.Count == 0)
            return;

        // Get all valid controllers
        List<KatamariController> controllers = new List<KatamariController>();

        foreach (GameObject player in playersList)
        {
            if (player == null) continue;

            var controller = player.GetComponent<KatamariController>();
            if (controller != null)
            {
                controllers.Add(controller);
            }
        }

        // Sort by score DESCENDING
        var sorted = controllers
            .OrderByDescending(c => c.Score.Value)
            .ToList();

        // Update leaderboard safely
        first.text = sorted.Count > 0 ? $"{sorted[0].Name.Value} : {sorted[0].Score.Value}" : "---";
        second.text = sorted.Count > 1 ? $"{sorted[1].Name.Value} : {sorted[1].Score.Value}" : "---";
        third.text = sorted.Count > 2 ? $"{sorted[2].Name.Value} : {sorted[2].Score.Value}" : "---";
    }
}