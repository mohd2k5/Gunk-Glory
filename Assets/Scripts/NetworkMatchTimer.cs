using Unity.Netcode;
using UnityEngine;
using TMPro;

public class NetworkMatchTimer : NetworkBehaviour
{
    [Header("Timer")]
    [SerializeField] private float startTimeSeconds = 120f; // 2:00
    private NetworkVariable<float> timeLeft = new NetworkVariable<float>(
        120f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;

    public bool timerRunning = false;
    
    public static NetworkMatchTimer Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        timerRunning = false;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            timeLeft.Value = startTimeSeconds;
            
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
        if (!IsServer || !timerRunning) return;

        if (timeLeft.Value > 0f)
        {
            timeLeft.Value -= Time.deltaTime;

            if (timeLeft.Value <= 0f)
            {
                timeLeft.Value = 0f;
                timerRunning = false;
                TimerEnded();
            }
        }
    }

    private void OnTimeChanged(float oldValue, float newValue)
    {
        UpdateTimerText(newValue);
    }

    private void UpdateTimerText(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);

        if (timerText != null)
        {
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    private void TimerEnded()
    {
        Debug.Log("Timer ended!");
        // Put your game over logic here
    }
}