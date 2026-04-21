using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineCamera))]
public class NetworkFreeLook : MonoBehaviour
{
    public static NetworkFreeLook Instance { get; private set; }

    public Transform WatchingPlayer { get; private set; }

    private CinemachineCamera cinemachineCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        cinemachineCamera = GetComponent<CinemachineCamera>();
    }

    public void SetLocalPlayer(Transform target)
    {
        WatchingPlayer = target;

        if (cinemachineCamera == null)
        {
            return;
        }

        cinemachineCamera.Follow = target;
        cinemachineCamera.LookAt = target;
    }
}