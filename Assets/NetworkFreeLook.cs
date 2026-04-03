
using Unity.Cinemachine;
using UnityEngine;

public class NetworkFreeLook : MonoBehaviour
{
    public static NetworkFreeLook Instance { get; private set; }

    Transform localPlayer;
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
        localPlayer = null;
    }

    public void SetLocalPlayer(Transform p)
    {
        localPlayer = p;
        GetComponent<CinemachineCamera>().Follow = p;
        GetComponent<CinemachineCamera>().LookAt = p;
    }
}