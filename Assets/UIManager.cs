using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;
using System.Collections.Generic;
using System.Collections;

using UnityEngine.InputSystem;

public class UIManager : NetworkBehaviour
{
    public GameObject startMenu;
    public GameObject waitingMenu;
    public GameObject inGameUI;
    public GameObject CountdownUI;
    public GameObject loseUI;
    public GameObject spectateUI;



    public TMP_InputField playerNameInput;


    public TextMeshProUGUI PlayerCountWaiting;
    
    public NetworkVariable<int> Countdown = new NetworkVariable<int>(3, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);

    public Transform[] TrahsSpawnPoints;
    public GameObject TrashPrefab;
    
    
    public List<GameObject> windows = new List<GameObject>();
    
    
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        SetWindow(startMenu);
    }

    void Update()
    {
        if (NetworkManager.Singleton != null)
        {
            PlayerCountWaiting.text = NetworkManager.Singleton.ConnectedClientsList.Count.ToString() + "/2";
            
            if (NetworkManager.Singleton.ConnectedClientsList.Count >= 2 && waitingMenu.activeSelf && IsServer)
            {
                SetCountdownUIServerRPC();
                StartCoroutine(StartCountdown());
            }
        }

        if (PlayerSingleton.Instance != null)
        {
            if (PlayerSingleton.Instance.GetComponent<KatamariController>().isStick.Value && spectateUI.activeSelf == false)
            {
                SetWindow(loseUI);
                foreach (GameObject p in NetworkManager.Singleton.GetComponent<PlayersList>().players)
                {
                    if (p != PlayerSingleton.Instance.gameObject)
                    {
                        Cursor.visible = true;
                        Cursor.lockState = CursorLockMode.None;
                        NetworkFreeLook.Instance.SetLocalPlayer(p.transform);
                        break;
                    }
                }
                
            }
        }
       
        
        CountdownUI.GetComponent<TextMeshProUGUI>().text = Countdown.Value.ToString();
        
    }

    public void StartHost()
    {
        RelayConnect.Instance.CreateRelay();
        SetWindow(waitingMenu);
        LocalDataSingleton.Instance.playerName = playerNameInput.text;
        
    }

    public void StartClient()
    {
        RelayConnect.Instance.JoinRelay();
        SetWindow(waitingMenu);
        LocalDataSingleton.Instance.playerName = playerNameInput.text;
    }

    public void SetWindow(GameObject window)
    {
        foreach (GameObject win in windows)
        {
            if (win == window)
            {
                win.SetActive(true);
            }
            else
            {
                win.SetActive(false);
            }
        }
    }



    IEnumerator StartCountdown()
    {
        Countdown.Value = 3;
        yield return new WaitForSeconds(1);
        Countdown.Value = 2;
        yield return new WaitForSeconds(1);
        Countdown.Value = 1;
        yield return new WaitForSeconds(1);
        StartGameAfterCountdownServerRPC();
    }
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void SetCountdownUIServerRPC()
    {
        SetWindow(CountdownUI);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void StartGameAfterCountdownServerRPC()
    {
        if (IsServer)
        {
            foreach (GameObject platform in LocalDataSingleton.Instance.SpawnPlatforms)
            {
                platform.GetComponent<NetworkObject>().Despawn(true);
            }
            
            SpawnRandomInQuad(TrashPrefab,100,TrahsSpawnPoints[0].position,TrahsSpawnPoints[1].position,TrahsSpawnPoints[2].position,TrahsSpawnPoints[3].position);
            
        }
        PlayerSingleton.Instance.GetComponent<PlayerInput>().enabled = true;
        SetWindow(inGameUI);
        NetworkMatchTimer.Instance.timerRunning = true;
    }
    
    
    
    public void SpawnRandomInQuad(GameObject prefab, int count, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        for (int i = 0; i < count; i++)
        {
            float u = Random.value;
            float v = Random.value;

            // Bilinear interpolation inside the quad
            Vector3 spawnPos =
                (1 - u) * (1 - v) * p1 +
                u * (1 - v) * p2 +
                u * v * p3 +
                (1 - u) * v * p4;

            GameObject trash = Instantiate(prefab, spawnPos, Quaternion.identity);
            trash.GetComponent<NetworkObject>().Spawn(true);
        }
    }


    public void SpeculateButton()
    {
        SetWindow(spectateUI);
    }

    public void SpectateNext()
    {
        foreach (GameObject p in NetworkManager.Singleton.GetComponent<PlayersList>().players)
        {
            if (p != PlayerSingleton.Instance.gameObject && p != NetworkFreeLook.Instance.wathcingPlayer.gameObject)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                NetworkFreeLook.Instance.SetLocalPlayer(p.transform);
                break;
            }
        }
    }


}
