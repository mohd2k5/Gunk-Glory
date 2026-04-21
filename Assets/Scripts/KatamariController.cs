using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class KatamariController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 500f;
    [SerializeField] private float rotationSpeed = 120f;

    [Header("Pickup Settings")]
    [SerializeField] private GameObject primObj;
    [SerializeField] private float minPlayerScoreDifferenceToAbsorb = 0.5f;
    [SerializeField] private float scorePerTrash = 0.25f;

    [Header("UI")]
    [SerializeField] private TextMeshPro scoreText;
    [SerializeField] private TextMeshPro nameText;

    public NetworkVariable<float> Score = new(
        2f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    public NetworkVariable<FixedString64Bytes> Name = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> isStick = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> isEliminated = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    
    public NetworkVariable<int> Placement = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    public int ObjectCount { get; private set; }

    // Scale stays fixed now, so score is the pickup-size metric.
    public float KatamariSize => Score.Value;

    private readonly List<GameObject> pickedObjects = new();

    private Rigidbody rb;
    private NetworkRigidbody networkRigidbody;
    private NetworkTransform networkTransform;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool hasRegisteredPlayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        networkRigidbody = GetComponent<NetworkRigidbody>();
        networkTransform = GetComponent<NetworkTransform>();
        playerInput = GetComponent<PlayerInput>();
    }

    public override void OnNetworkSpawn()
    {
        RegisterWithPlayersList();

        if (primObj != null && !pickedObjects.Contains(primObj))
        {
            pickedObjects.Add(primObj);
        }

        isEliminated.OnValueChanged += OnEliminatedChanged;
        ApplyEliminatedState(isEliminated.Value);

        if (!IsOwner)
        {
            if (playerInput != null)
            {
                playerInput.enabled = false;
            }

            return;
        }

        if (NetworkFreeLook.Instance != null)
        {
            NetworkFreeLook.Instance.SetLocalPlayer(transform);
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        MoveToAssignedSpawnPlatform();
        RefreshLocalName();
        RefreshUI();
    }

    public override void OnNetworkDespawn()
    {
        isEliminated.OnValueChanged -= OnEliminatedChanged;
        UnregisterFromPlayersList();
    }

    private void Update()
    {
        RefreshUI();

        if (!IsOwner || isStick.Value || isEliminated.Value)
        {
            return;
        }

        RefreshLocalName();
        HandleRotation();
    }

    private void FixedUpdate()
    {
        if (!IsOwner || isStick.Value || isEliminated.Value)
        {
            return;
        }

        HandleMovement();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!IsOwner || isEliminated.Value)
        {
            return;
        }

        if (context.performed || context.started)
        {
            moveInput = context.ReadValue<Vector2>();
        }
        else if (context.canceled)
        {
            moveInput = Vector2.zero;
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (!IsOwner || isEliminated.Value)
        {
            return;
        }

        if (context.performed || context.started)
        {
            lookInput = context.ReadValue<Vector2>();
        }
        else if (context.canceled)
        {
            lookInput = Vector2.zero;
        }
    }

    private void HandleMovement()
    {
        if (rb == null || moveInput == Vector2.zero)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraRight * moveInput.x + cameraForward * moveInput.y).normalized;
        rb.AddForce(moveDirection * speed, ForceMode.Force);
    }

    private void HandleRotation()
    {
        if (Mathf.Approximately(lookInput.x, 0f))
        {
            return;
        }

        transform.Rotate(Vector3.up, lookInput.x * rotationSpeed * Time.deltaTime, Space.World);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner || isStick.Value || isEliminated.Value)
        {
            return;
        }

        if (collision.gameObject.TryGetComponent(out KatamariStick stick))
        {
            TryPickUpStick(stick, collision.gameObject);
            return;
        }

        if (collision.gameObject.TryGetComponent(out KatamariController otherController))
        {
            TryAbsorbPlayer(otherController, collision.gameObject);
        }
    }

    private void TryPickUpStick(KatamariStick stick, GameObject stickObject)
    {
        if (stick == null || stickObject == null || stick.isStick.Value)
        {
            return;
        }

        if (stick.sizeValue >= KatamariSize)
        {
            return;
        }

        // Cache the exact local pose seen by the picking player.
        Vector3 localPosition = transform.InverseTransformPoint(stickObject.transform.position);
        Quaternion localRotation = Quaternion.Inverse(transform.rotation) * stickObject.transform.rotation;

        stick.AttachToPlayerServerRpc(NetworkObjectId, localPosition, localRotation);

        if (!pickedObjects.Contains(stickObject))
        {
            pickedObjects.Add(stickObject);
            ObjectCount++;
        }

        transform.localScale += Vector3.one * (scorePerTrash / 5f);
        Score.Value += scorePerTrash;
    }

    private void TryAbsorbPlayer(KatamariController otherController, GameObject otherObject)
    {
        if (otherController == null || otherObject == null)
        {
            return;
        }

        if (otherController == this || otherController.isStick.Value || otherController.isEliminated.Value)
        {
            return;
        }

        if ((Score.Value - otherController.Score.Value) < minPlayerScoreDifferenceToAbsorb)
        {
            return;
        }

        otherController.EliminateServerRpc();
        transform.localScale += Vector3.one * (scorePerTrash / 5f);
        Score.Value += scorePerTrash;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void EliminateServerRpc()
    {
        if (isEliminated.Value)
        {
            return;
        }

        int activePlayers = 0;

        if (NetworkManager.Singleton != null)
        {
            PlayersList playersList = NetworkManager.Singleton.GetComponent<PlayersList>();
            if (playersList != null)
            {
                foreach (GameObject player in playersList.players)
                {
                    if (player == null) continue;

                    if (!player.TryGetComponent(out KatamariController controller)) continue;
                    if (controller.isEliminated.Value) continue;

                    activePlayers++;
                }
            }
        }

        // If 4 players are still alive and this player gets eliminated now, they are place 4.
        Placement.Value = activePlayers;
        isEliminated.Value = true;
    }

    private void OnEliminatedChanged(bool oldValue, bool newValue)
    {
        ApplyEliminatedState(newValue);
    }

    private void ApplyEliminatedState(bool eliminated)
    {
        if (!eliminated)
        {
            return;
        }

        RemoveFromPlayersList();

        moveInput = Vector2.zero;
        lookInput = Vector2.zero;

        if (playerInput != null)
        {
            playerInput.enabled = false;
        }

        if (networkRigidbody != null)
        {
            networkRigidbody.enabled = false;
        }

        if (networkTransform != null)
        {
            networkTransform.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider colliderComponent in colliders)
        {
            colliderComponent.enabled = false;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rendererComponent in renderers)
        {
            rendererComponent.enabled = false;
        }

        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            canvas.enabled = false;
        }
    }

    private void RefreshUI()
    {
        if (scoreText != null)
        {
            scoreText.text = Math.Round(Score.Value, 2).ToString();
        }

        if (nameText != null)
        {
            nameText.text = Name.Value.ToString();
        }
    }

    private void RefreshLocalName()
    {
        if (LocalDataSingleton.Instance == null)
        {
            return;
        }

        FixedString64Bytes desiredName = new(LocalDataSingleton.Instance.PlayerName);
        if (!Name.Value.Equals(desiredName))
        {
            SubmitNameServerRpc(desiredName);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SubmitNameServerRpc(FixedString64Bytes desiredName)
    {
        if (!Name.Value.Equals(desiredName))
        {
            Name.Value = desiredName;
        }
    }

    private void MoveToAssignedSpawnPlatform()
    {
        if (LocalDataSingleton.Instance == null || LocalDataSingleton.Instance.SpawnPlatforms == null)
        {
            return;
        }

        GameObject[] spawnPlatforms = LocalDataSingleton.Instance.SpawnPlatforms;
        if (spawnPlatforms.Length == 0)
        {
            return;
        }

        int platformIndex = Mathf.Clamp((int)OwnerClientId, 0, spawnPlatforms.Length - 1);
        GameObject platform = spawnPlatforms[platformIndex];
        if (platform == null)
        {
            return;
        }

        transform.position = platform.transform.position + Vector3.up * 1.5f;
    }

    private void RegisterWithPlayersList()
    {
        if (NetworkManager.Singleton == null || hasRegisteredPlayer)
        {
            return;
        }

        PlayersList playersList = NetworkManager.Singleton.GetComponent<PlayersList>();
        if (playersList == null)
        {
            return;
        }

        if (!playersList.players.Contains(gameObject))
        {
            playersList.players.Add(gameObject);
        }

        hasRegisteredPlayer = true;
    }

    private void RemoveFromPlayersList()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        PlayersList playersList = NetworkManager.Singleton.GetComponent<PlayersList>();
        if (playersList != null)
        {
            playersList.players.Remove(gameObject);
        }
    }

    private void UnregisterFromPlayersList()
    {
        RemoveFromPlayersList();
        hasRegisteredPlayer = false;
    }
}