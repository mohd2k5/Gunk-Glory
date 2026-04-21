using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineCamera))]
public class NetworkFreeLook : MonoBehaviour
{
    public static NetworkFreeLook Instance { get; private set; }

    public Transform WatchingPlayer { get; private set; }

    [Header("Orbital Radius")]
    [SerializeField] private float baseRadius = 8f;
    [SerializeField] private float radiusPerScale = 2f;
    [SerializeField] private float minRadius = 6f;
    [SerializeField] private float maxRadius = 40f;
    [SerializeField] private float smoothSpeed = 5f;

    private CinemachineCamera cinemachineCamera;
    private CinemachineOrbitalFollow orbitalFollow;
    private float currentRadius;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        cinemachineCamera = GetComponent<CinemachineCamera>();
        orbitalFollow = GetComponent<CinemachineOrbitalFollow>();

        currentRadius = baseRadius;
    }

    private void LateUpdate()
    {
        if (WatchingPlayer == null || orbitalFollow == null)
        {
            return;
        }

        float playerScale = WatchingPlayer.localScale.x;
        float targetRadius = Mathf.Clamp(
            baseRadius + playerScale * radiusPerScale,
            minRadius,
            maxRadius
        );

        currentRadius = Mathf.Lerp(currentRadius, targetRadius, Time.deltaTime * smoothSpeed);
        orbitalFollow.Radius = currentRadius;
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