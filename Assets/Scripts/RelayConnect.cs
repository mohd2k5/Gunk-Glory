using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

public class RelayConnect : MonoBehaviour
{
    public TextMeshProUGUI joinCodeText;
    public TMP_InputField joinCodeInputField;

    public static RelayConnect Instance { get; private set; }

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed In " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateRelay()
    {
        try
        {
            SaveLocalPlayerName();

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            if (joinCodeText != null)
            {
                joinCodeText.text = joinCode;
            }

            RelayServerData relayServerData = allocation.ToRelayServerData("dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetWindow(UIManager.Instance.waitingMenu);
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogException(e);
        }
    }

    public async void JoinRelay()
    {
        string joinCode = joinCodeInputField != null ? joinCodeInputField.text.Trim() : string.Empty;

        try
        {
            SaveLocalPlayerName();

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            RelayServerData relayServerData = joinAllocation.ToRelayServerData("dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetWindow(UIManager.Instance.waitingMenu);
            }

            if (joinCodeText != null)
            {
                joinCodeText.text = joinCode;
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogException(e);
        }
    }

    private void SaveLocalPlayerName()
    {
        if (LocalDataSingleton.Instance == null || UIManager.Instance == null)
        {
            return;
        }

        string enteredName = UIManager.Instance.playerNameInput != null
            ? UIManager.Instance.playerNameInput.text
            : string.Empty;

        LocalDataSingleton.Instance.PlayerName =
            string.IsNullOrWhiteSpace(enteredName) ? "Player" : enteredName.Trim();
    }
}