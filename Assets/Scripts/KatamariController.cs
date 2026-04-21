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
    [SerializeField] private float pickupScaleIncrease = 0.05f;
    [SerializeField] private float minPlayerScoreDifferenceToAbsorb = 0.5f;

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

    public int ObjectCount { get; private set; }
    public float KatamariSize => transform.localScale.x;

    private readonly List<GameObject> pickedObjects = new();

    private Rigidbody rb;
    private NetworkRigidbody networkRigidbody;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool hasRegisteredPlayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        networkRigidbody = GetComponent<NetworkRigidbody>();
        playerInput = GetComponent<PlayerInput>();
    }

    public override void OnNetworkSpawn()
    {
        RegisterWithPlayersList();

        if (primObj != null && !pickedObjects.Contains(primObj))
        {
            pickedObjects.Add(primObj);
        }

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
        UnregisterFromPlayersList();
    }

    private void Update()
    {
        RefreshUI();

        if (!IsOwner || isStick.Value)
        {
            return;
        }

        RefreshLocalName();
        HandleRotation();
    }

    private void FixedUpdate()
    {
        if (!IsOwner || isStick.Value)
        {
            return;
        }

        HandleMovement();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!IsOwner)
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
        if (!IsOwner)
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
        if (!IsOwner || isStick.Value)
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

        stick.TransferOwnershipServerRPC(OwnerClientId, NetworkObjectId);

        if (!pickedObjects.Contains(stickObject))
        {
            pickedObjects.Add(stickObject);
            ObjectCount++;
        }

        transform.localScale += Vector3.one * pickupScaleIncrease;
        Score.Value = KatamariSize;
    }

    private void TryAbsorbPlayer(KatamariController otherController, GameObject otherObject)
    {
        if (otherController == null || otherObject == null)
        {
            return;
        }

        if (otherController == this || otherController.isStick.Value)
        {
            return;
        }

        if ((Score.Value - otherController.Score.Value) < minPlayerScoreDifferenceToAbsorb)
        {
            return;
        }

        otherController.TransferOwnershipServerRPC(OwnerClientId, NetworkObjectId);

        if (!pickedObjects.Contains(otherObject))
        {
            pickedObjects.Add(otherObject);
            ObjectCount++;
        }

        transform.localScale += Vector3.one * otherController.Score.Value;
        Score.Value = KatamariSize;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TransferOwnershipServerRPC(ulong newOwnerId, ulong parentId)
    {
        NetworkObject networkObjectComponent = GetComponent<NetworkObject>();
        if (networkObjectComponent == null || NetworkManager.Singleton == null)
        {
            return;
        }

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(parentId, out NetworkObject parentObject))
        {
            Debug.LogWarning($"KatamariController parent NetworkObject {parentId} was not found.");
            return;
        }

        if (playerInput != null)
        {
            playerInput.enabled = false;
        }

        bool parented = networkObjectComponent.TrySetParent(parentObject, true);
        if (!parented)
        {
            Debug.LogWarning($"{name} failed to parent to {parentObject.name}");
            return;
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

        if (networkRigidbody != null)
        {
            networkRigidbody.enabled = false;
        }

        if (TryGetComponent(out Collider colliderComponent))
        {
            colliderComponent.enabled = false;
        }

        isStick.Value = true;
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

    private void UnregisterFromPlayersList()
    {
        if (NetworkManager.Singleton == null)
        {
            hasRegisteredPlayer = false;
            return;
        }

        PlayersList playersList = NetworkManager.Singleton.GetComponent<PlayersList>();
        if (playersList != null)
        {
            playersList.players.Remove(gameObject);
        }

        hasRegisteredPlayer = false;
    }
}