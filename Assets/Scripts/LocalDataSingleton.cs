using UnityEngine;

public class LocalDataSingleton : MonoBehaviour
{
    public static LocalDataSingleton Instance { get; private set; }

    [field: SerializeField] public GameObject[] SpawnPlatforms { get; private set; }
    public string PlayerName { get; set; } = "Player";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}