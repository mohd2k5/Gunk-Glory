using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkObject))]
public class UIManager : NetworkBehaviour
{
    [Header("Windows")]
    [SerializeField] public GameObject startMenu;
    [SerializeField] public GameObject waitingMenu;
    [SerializeField] public GameObject inGameUI;
    [SerializeField] public GameObject countdownUI;
    [SerializeField] public GameObject loseUI;
    [SerializeField] public GameObject spectateUI;
    [SerializeField] public GameObject endUi;
    [SerializeField] public List<GameObject> windows = new();

    [Header("Player Setup")]
    [SerializeField] public TMP_InputField playerNameInput;
    [SerializeField] public TextMeshProUGUI playerCountWaiting;

    [Header("Countdown")]
    [SerializeField] public TextMeshProUGUI countdownText;
    [SerializeField] public int playersRequiredToStart = 2;
    [SerializeField] private float countdownStepSeconds = 1f;

    [Header("Spawning")]
    [SerializeField] private Transform[] trashSpawnPoints;
    [SerializeField] private GameObject trashPrefab;
    [SerializeField] private int trashSpawnCount = 100;

    public NetworkVariable<int> Countdown = new(
        3,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> CountdownActiveNet = new(
        false,
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
        Countdown.OnValueChanged += OnCountdownChanged;
        CountdownActiveNet.OnValueChanged += OnCountdownActiveChanged;
        GameStartedNet.OnValueChanged += OnGameStartedChanged;

        ApplyWindowState();
        UpdateCountdownText();
    }

    public override void OnNetworkDespawn()
    {
        Countdown.OnValueChanged -= OnCountdownChanged;
        CountdownActiveNet.OnValueChanged -= OnCountdownActiveChanged;
        GameStartedNet.OnValueChanged -= OnGameStartedChanged;
    }

    private void Start()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            SetWindow(startMenu);
        }
    }

    private void Update()
    {
        UpdateWaitingPlayerCount();
        UpdateCountdownText();

        if (IsServer)
        {
            TryStartCountdown();
        }

        KeepLocalInputDisabledBeforeStart();
        TryInitializeLocalGameplay();
        TryHandleLocalPlayerLoss();
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        SaveLocalPlayerName();
        NetworkManager.Singleton.StartHost();
        ApplyWindowState();
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        SaveLocalPlayerName();
        NetworkManager.Singleton.StartClient();
        ApplyWindowState();
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

    private void TryStartCountdown()
    {
        if (!IsServer)
        {
            return;
        }

        if (GameStartedNet.Value || CountdownActiveNet.Value || countdownCoroutine != null)
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

        countdownCoroutine = StartCoroutine(StartCountdown());
    }

    private IEnumerator StartCountdown()
    {
        CountdownActiveNet.Value = true;

        Countdown.Value = 3;
        yield return new WaitForSeconds(countdownStepSeconds);

        Countdown.Value = 2;
        yield return new WaitForSeconds(countdownStepSeconds);

        Countdown.Value = 1;
        yield return new WaitForSeconds(countdownStepSeconds);

        Countdown.Value = 0;
        yield return new WaitForSeconds(0.2f);

        DespawnSpawnPlatforms();
        SpawnTrashInPlayArea();

        CountdownActiveNet.Value = false;
        GameStartedNet.Value = true;
        countdownCoroutine = null;
    }

    private void KeepLocalInputDisabledBeforeStart()
    {
        if (GameStartedNet.Value)
        {
            return;
        }

        if (PlayerSingleton.Instance == null)
        {
            return;
        }

        if (PlayerSingleton.Instance.TryGetComponent(out PlayerInput playerInput))
        {
            playerInput.enabled = false;
        }
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

    private void TryHandleLocalPlayerLoss()
    {
        if (!GameStartedNet.Value || loseWindowShown || PlayerSingleton.Instance == null || NetworkManager.Singleton == null)
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

    private void UpdateWaitingPlayerCount()
    {
        if (playerCountWaiting == null || NetworkManager.Singleton == null)
        {
            return;
        }

        playerCountWaiting.text = $"{NetworkManager.Singleton.ConnectedClientsList.Count}/{playersRequiredToStart}";
    }

    private void UpdateCountdownText()
    {
        if (countdownText == null)
        {
            return;
        }

        countdownText.text = CountdownActiveNet.Value ? Countdown.Value.ToString() : string.Empty;
    }

    private void OnCountdownChanged(int oldValue, int newValue)
    {
        UpdateCountdownText();
    }

    private void OnCountdownActiveChanged(bool oldValue, bool newValue)
    {
        ApplyWindowState();
        UpdateCountdownText();
    }

    private void OnGameStartedChanged(bool oldValue, bool newValue)
    {
        ApplyWindowState();
    }

    private void ApplyWindowState()
    {
        if (loseWindowShown)
        {
            SetWindow(loseUI);
            return;
        }

        if (GameStartedNet.Value)
        {
            SetWindow(inGameUI);
            return;
        }

        if (CountdownActiveNet.Value)
        {
            SetWindow(countdownUI);
            return;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            SetWindow(waitingMenu);
            return;
        }

        SetWindow(startMenu);
    }

    private void DespawnSpawnPlatforms()
    {
        if (!IsServer)
        {
            return;
        }

        if (LocalDataSingleton.Instance == null || LocalDataSingleton.Instance.SpawnPlatforms == null)
        {
            return;
        }

        foreach (GameObject platform in LocalDataSingleton.Instance.SpawnPlatforms)
        {
            if (platform == null)
            {
                continue;
            }

            if (!platform.TryGetComponent(out NetworkObject networkObjectComponent))
            {
                continue;
            }

            if (!networkObjectComponent.IsSpawned)
            {
                continue;
            }

            networkObjectComponent.Despawn(true);
        }
    }

    private void SpawnTrashInPlayArea()
    {
        if (!IsServer)
        {
            return;
        }

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