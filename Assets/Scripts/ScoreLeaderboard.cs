using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ScoreLeaderboard : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI first;
    [SerializeField] private TextMeshProUGUI second;
    [SerializeField] private TextMeshProUGUI third;
    [SerializeField] private TextMeshProUGUI localPlayerRankText;

    private void Update()
    {
        if (NetworkManager.Singleton == null)
        {
            SetText(first, "---");
            SetText(second, "---");
            SetText(third, "---");
            SetText(localPlayerRankText, "---");
            return;
        }

        PlayersList playersList = NetworkManager.Singleton.GetComponent<PlayersList>();
        if (playersList == null)
        {
            SetText(first, "---");
            SetText(second, "---");
            SetText(third, "---");
            SetText(localPlayerRankText, "---");
            return;
        }

        List<KatamariController> sortedControllers = playersList.players
            .Where(player => player != null)
            .Select(player => player.GetComponent<KatamariController>())
            .Where(controller => controller != null)
            .OrderByDescending(controller => controller.Score.Value)
            .ToList();

        SetPlacementText(first, sortedControllers, 0);
        SetPlacementText(second, sortedControllers, 1);
        SetPlacementText(third, sortedControllers, 2);
        SetLocalPlayerRankText(sortedControllers);
    }

    private void SetPlacementText(TextMeshProUGUI label, IReadOnlyList<KatamariController> sortedControllers, int index)
    {
        if (label == null)
        {
            return;
        }

        if (sortedControllers.Count <= index)
        {
            label.text = "---";
            return;
        }

        KatamariController controller = sortedControllers[index];
        label.text = $"{controller.Name.Value} : {controller.Score.Value:0.00}";
    }

    private void SetLocalPlayerRankText(IReadOnlyList<KatamariController> sortedControllers)
    {
        if (localPlayerRankText == null)
        {
            return;
        }

        if (PlayerSingleton.Instance == null)
        {
            localPlayerRankText.text = "---";
            return;
        }

        KatamariController localController = PlayerSingleton.Instance.GetComponent<KatamariController>();
        if (localController == null)
        {
            localPlayerRankText.text = "---";
            return;
        }

        int rank = sortedControllers
            .Select((controller, index) => new { controller, index })
            .Where(x => x.controller == localController)
            .Select(x => x.index + 1)
            .FirstOrDefault();

        localPlayerRankText.text = "#" + rank.ToString() +" PLACE";
    }

    private static void SetText(TMP_Text label, string value)
    {
        if (label != null)
        {
            label.text = value;
        }
    }
}