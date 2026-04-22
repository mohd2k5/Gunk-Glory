using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NetworkMatchTimer : NetworkBehaviour
{
    [Header("Default Timer Value")]
    [SerializeField] private float defaultStartTimeSeconds = 120f;

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_InputField timerInputField;

    private readonly NetworkVariable<float> timeLeft = new(
        120f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public bool timerRunning { get; set; }
    public float TimeLeft => timeLeft.Value;

    public static NetworkMatchTimer Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        timerRunning = false;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ResetTimer(defaultStartTimeSeconds);
        }

        timeLeft.OnValueChanged += OnTimeChanged;
        UpdateTimerText(timeLeft.Value);

        if (timerInputField != null)
        {
            timerInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            timerInputField.text = Mathf.RoundToInt(defaultStartTimeSeconds).ToString();
        }
    }

    public override void OnNetworkDespawn()
    {
        timeLeft.OnValueChanged -= OnTimeChanged;
    }

    private void Update()
    {
        if (!IsServer || !timerRunning)
        {
            return;
        }

        if (timeLeft.Value <= 0f)
        {
            timeLeft.Value = 0f;
            timerRunning = false;
            TimerEndedRPC();
            return;
        }

        timeLeft.Value -= Time.deltaTime;
    }

    public void StartTimerFromInput()
    {
        if (timerInputField == null)
        {
            Debug.LogWarning("Timer input field is not assigned.");
            return;
        }

        if (!float.TryParse(timerInputField.text, out float inputSeconds))
        {
            Debug.LogWarning("Invalid timer input.");
            return;
        }

        inputSeconds = Mathf.Max(1f, inputSeconds);

        if (IsServer)
        {
            ResetTimer(inputSeconds);
        }
        else
        {
            SetTimerFromInputServerRpc(inputSeconds);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetTimerFromInputServerRpc(float newTime)
    {
        ResetTimer(newTime);
    }

    public void ResetTimer(float newTime)
    {
        timeLeft.Value = newTime;
        timerRunning = false;
        UpdateTimerText(timeLeft.Value);
    }

    public void StartTimer()
    {
        if (IsServer)
        {
            timerRunning = true;
        }
    }

    public void StopTimer()
    {
        if (IsServer)
        {
            timerRunning = false;
        }
    }

    private void OnTimeChanged(float oldValue, float newValue)
    {
        UpdateTimerText(Mathf.Max(0f, newValue));
    }

    private void UpdateTimerText(float time)
    {
        if (timerText == null)
        {
            return;
        }

        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void TimerEndedRPC()
    {
        Debug.Log("Match timer ended.");
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        UIManager.Instance.SetWindow(UIManager.Instance.endUi);
    }
}