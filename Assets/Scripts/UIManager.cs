using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : NetworkBehaviour
{
    [Header("Windows")]
    [SerializeField] public GameObject startMenu;
    [SerializeField] public GameObject waitingMenu;
    [SerializeField] public GameObject inGameUI;
    [SerializeField] public GameObject countdownUI;
    [SerializeField] public GameObject loseUI;
    [SerializeField] public GameObject spectateUI;
    [SerializeField] public List<GameObject> windows = new();

    [Header("Player Setup")]
    [SerializeField] public TMP_InputField playerNameInput;
    [SerializeField] public TextMeshProUGUI playerCountWaiting;

    [Header("Countdown")]
    [SerializeField] public TextMeshProUGUI countdownText;
    [SerializeField] public int playersRequiredToStart = 2;

    [Header("Spawning")]
    [SerializeField] private Transform[] trashSpawnPoints;
    [SerializeField] private GameObject trashPrefab;
    [SerializeField] private int trashSpawnCount = 100;

    public NetworkVariable<int> Countdown = new(
        3,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> GameStartedNet = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public static UIManager Instance { get; private set; }

    private Coroutine countdownCoroutine;
    private bool loseWindowShown;
    private bool localGameplayInitialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        GameStartedNet.OnValueChanged += OnGameStartedChanged;
    }

    public override void OnNetworkDespawn()
    {
        GameStartedNet.OnValueChanged -= OnGameStartedChanged;
    }

    private void Start()
    {
        SetWindow(startMenu);
    }

    private void Update()
    {
        UpdateWaitingPlayerCount();
        TryStartCountdown();
        TryHandleLocalPlayerLoss();
        UpdateCountdownText();
        TryInitializeLocalGameplay();
    }

    public void StartHost()
    {
        SaveLocalPlayerName();
        NetworkManager.Singleton.StartHost();
        SetWindow(waitingMenu);
    }

    public void StartClient()
    {
        SaveLocalPlayerName();
        NetworkManager.Singleton.StartClient();
        SetWindow(waitingMenu);
    }

    public void SetWindow(GameObject window)
    {
        foreach (GameObject candidate in windows)
        {
            if (candidate != null)
            {
                candidate.SetActive(candidate == window);
            }
        }
    }

    public void SpeculateButton()
    {
        SetWindow(spectateUI);
    }

    public void SpectateNext()
    {
        if (NetworkManager.Singleton == null || NetworkFreeLook.Instance == null || PlayerSingleton.Instance == null)
        {
            return;
        }

        PlayersList playersList = NetworkManager.Singleton.GetComponent<PlayersList>();
        if (playersList == null || playersList.players.Count == 0)
        {
            return;
        }

        GameObject currentWatchedObject = NetworkFreeLook.Instance.WatchingPlayer != null
            ? NetworkFreeLook.Instance.WatchingPlayer.gameObject
            : null;

        foreach (GameObject player in playersList.players)
        {
            if (player == null || player == PlayerSingleton.Instance.gameObject || player == currentWatchedObject)
            {
                continue;
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            NetworkFreeLook.Instance.SetLocalPlayer(player.transform);
            break;
        }
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void SetCountdownUIServerRPC()
    {
        SetWindow(countdownUI);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void StartGameAfterCountdownServerRPC()
    {
        if (IsServer)
        {
            DespawnSpawnPlatforms();
            SpawnTrashInPlayArea();
            GameStartedNet.Value = true;
        }

        SetWindow(inGameUI);
    }

    private void OnGameStartedChanged(bool oldValue, bool newValue)
    {
        if (!newValue)
        {
            return;
        }

        SetWindow(inGameUI);
    }

    private void TryInitializeLocalGameplay()
    {
        if (!GameStartedNet.Value || localGameplayInitialized)
        {
            return;
        }

        if (PlayerSingleton.Instance == null)
        {
            return;
        }

        if (PlayerSingleton.Instance.TryGetComponent(out PlayerInput playerInput))
        {
            playerInput.enabled = true;
        }

        if (NetworkFreeLook.Instance != null)
        {
            NetworkFreeLook.Instance.SetLocalPlayer(PlayerSingleton.Instance.transform);
        }

        if (NetworkMatchTimer.Instance != null)
        {
            NetworkMatchTimer.Instance.timerRunning = true;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        SetWindow(inGameUI);
        localGameplayInitialized = true;
    }

    private void UpdateWaitingPlayerCount()
    {
        if (playerCountWaiting == null || NetworkManager.Singleton == null)
        {
            return;
        }

        playerCountWaiting.text = $"{NetworkManager.Singleton.ConnectedClientsList.Count}/{playersRequiredToStart}";
    }

    private void TryStartCountdown()
    {
        if (!IsServer || GameStartedNet.Value || countdownCoroutine != null)
        {
            return;
        }

        if (waitingMenu == null || !waitingMenu.activeSelf)
        {
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (NetworkManager.Singleton.ConnectedClientsList.Count < playersRequiredToStart)
        {
            return;
        }

        SetCountdownUIServerRPC();
        countdownCoroutine = StartCoroutine(StartCountdown());
    }

    private void TryHandleLocalPlayerLoss()
    {
        if (loseWindowShown || PlayerSingleton.Instance == null || NetworkManager.Singleton == null)
        {
            return;
        }

        KatamariController localController = PlayerSingleton.Instance.GetComponent<KatamariController>();
        if (localController == null || !localController.isStick.Value)
        {
            return;
        }

        loseWindowShown = true;
        SetWindow(loseUI);

        PlayersList playersList = NetworkManager.Singleton.GetComponent<PlayersList>();
        if (playersList == null || NetworkFreeLook.Instance == null)
        {
            return;
        }

        foreach (GameObject player in playersList.players)
        {
            if (player == null || player == PlayerSingleton.Instance.gameObject)
            {
                continue;
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            NetworkFreeLook.Instance.SetLocalPlayer(player.transform);
            break;
        }
    }

    private void UpdateCountdownText()
    {
        if (countdownText != null)
        {
            countdownText.text = Countdown.Value.ToString();
        }
    }

    private IEnumerator StartCountdown()
    {
        Countdown.Value = 3;
        yield return new WaitForSeconds(1f);

        Countdown.Value = 2;
        yield return new WaitForSeconds(1f);

        Countdown.Value = 1;
        yield return new WaitForSeconds(1f);

        Countdown.Value = 0;
        yield return new WaitForSeconds(0.2f);

        countdownCoroutine = null;
        StartGameAfterCountdownServerRPC();
    }

    private void SpawnTrashInPlayArea()
    {
        if (trashPrefab == null || trashSpawnPoints == null || trashSpawnPoints.Length < 4)
        {
            Debug.LogWarning("Trash spawning was skipped because prefab or spawn points are missing.");
            return;
        }

        SpawnRandomInQuad(
            trashPrefab,
            trashSpawnCount,
            trashSpawnPoints[0].position,
            trashSpawnPoints[1].position,
            trashSpawnPoints[2].position,
            trashSpawnPoints[3].position);
    }

    private void DespawnSpawnPlatforms()
    {
        if (LocalDataSingleton.Instance == null || LocalDataSingleton.Instance.SpawnPlatforms == null)
        {
            return;
        }

        foreach (GameObject platform in LocalDataSingleton.Instance.SpawnPlatforms)
        {
            if (platform == null || !platform.TryGetComponent(out NetworkObject networkObjectComponent) || !networkObjectComponent.IsSpawned)
            {
                continue;
            }

            networkObjectComponent.Despawn(true);
        }
    }

    private void SaveLocalPlayerName()
    {
        if (LocalDataSingleton.Instance == null)
        {
            return;
        }

        string enteredName = playerNameInput != null ? playerNameInput.text : string.Empty;
        LocalDataSingleton.Instance.PlayerName = string.IsNullOrWhiteSpace(enteredName) ? "Player" : enteredName.Trim();
    }

    private void SpawnRandomInQuad(GameObject prefab, int count, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        for (int i = 0; i < count; i++)
        {
            float u = Random.value;
            float v = Random.value;

            Vector3 spawnPosition =
                (1f - u) * (1f - v) * p1 +
                u * (1f - v) * p2 +
                u * v * p3 +
                (1f - u) * v * p4;

            GameObject spawnedObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
            if (spawnedObject.TryGetComponent(out NetworkObject networkObjectComponent))
            {
                networkObjectComponent.Spawn(true);
            }
        }
    }
}