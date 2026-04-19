
using Unity.Cinemachine;
using UnityEngine;

public class NetworkFreeLook : MonoBehaviour
{
    public static NetworkFreeLook Instance { get; private set; }

    public Transform wathcingPlayer;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        wathcingPlayer = null;
    }

    public void SetLocalPlayer(Transform p)
    {
        wathcingPlayer = p;
        GetComponent<CinemachineCamera>().Follow = p;
        GetComponent<CinemachineCamera>().LookAt = p;
    }
    
}