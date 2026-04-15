using UnityEngine;

public class LocalDataSingleton : MonoBehaviour
{
    public static LocalDataSingleton Instance { get; private set; }

    public string playerName;

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
