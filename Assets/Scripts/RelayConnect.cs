using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Multiplayer; 
public class RelayConnect : MonoBehaviour
{
    public TextMeshProUGUI joinCodeText;
    public TMP_InputField joinCodeInputField;
    public static RelayConnect Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private async void Start()
    {
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
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            joinCodeText.text = joinCode;
            RelayServerData relayServerData = allocation.ToRelayServerData("dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartHost();
            UIManager.Instance.SetWindow(UIManager.Instance.waitingMenu);
            LocalDataSingleton.Instance.PlayerName = UIManager.Instance.playerNameInput.text;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
        
    }

    public async void JoinRelay()
    {
        string joinCode = joinCodeInputField.text;
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
            RelayServerData relayServerData = joinAllocation.ToRelayServerData("dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();
            UIManager.Instance.SetWindow(UIManager.Instance.waitingMenu);
            LocalDataSingleton.Instance.PlayerName = UIManager.Instance.playerNameInput.text;
            joinCodeText.text = joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
        
    }
}
