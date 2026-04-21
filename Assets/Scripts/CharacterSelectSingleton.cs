using UnityEngine;

public class CharacterSelectSingleton : MonoBehaviour
{
    public static CharacterSelectSingleton Instance { get; private set; }

    public int skin;
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
