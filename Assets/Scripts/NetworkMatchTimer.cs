using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NetworkMatchTimer : NetworkBehaviour
{
    [Header("Timer")]
    [SerializeField] private float startTimeSeconds = 120f;

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;

    private readonly NetworkVariable<float> timeLeft = new(
        120f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

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
            ResetTimer();
        }

        timeLeft.OnValueChanged += OnTimeChanged;
        UpdateTimerText(timeLeft.Value);
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
            TimerEnded();
            return;
        }

        timeLeft.Value -= Time.deltaTime;
    }

    public void ResetTimer()
    {
        timeLeft.Value = startTimeSeconds;
        UpdateTimerText(timeLeft.Value);
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

    private void TimerEnded()
    {
        Debug.Log("Match timer ended.");
    }
}
