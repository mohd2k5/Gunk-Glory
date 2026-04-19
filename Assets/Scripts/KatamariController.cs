using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Collections;
using TMPro;
using Unity.Netcode.Components;


public class KatamariController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 500f;
    [SerializeField] private float rotationSpeed = 120f;

    private Rigidbody rb;
    private SphereCollider katamariCollider;

    private Vector2 moveInput;
    private Vector2 lookInput;

    public float katamariSize;

    [Header("Pickup Settings")]
    public int maxObjCount = 20;
    public int objCount = 0;

    private readonly List<GameObject> pickedObjects = new();

    [SerializeField] private GameObject primObj;
    private GameObject lastPickedObject;



    public NetworkVariable<int> Score = new NetworkVariable<int>(0, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner);


public NetworkVariable<FixedString64Bytes> Name = new NetworkVariable<FixedString64Bytes>(
    default,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner
);

    public TextMeshPro scoreText;
    public TextMeshPro nameText;

    
    public NetworkVariable<bool> isStick = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );


private PlayerInput playerInput;

private void Awake()
{
    rb = GetComponent<Rigidbody>();
    katamariCollider = GetComponent<SphereCollider>();
    playerInput = GetComponent<PlayerInput>();
}

public override void OnNetworkSpawn()
{
    NetworkManager.Singleton.GetComponent<PlayersList>().players.Add(gameObject);

    if (playerInput != null)
        playerInput.enabled = IsOwner;

    if (!IsOwner)
        return;

    if (NetworkFreeLook.Instance != null)
        NetworkFreeLook.Instance.SetLocalPlayer(transform);

    Cursor.visible = false;
    Cursor.lockState = CursorLockMode.Locked;

    katamariSize = katamariCollider.bounds.size.x;

    if (primObj != null)
    {
        pickedObjects.Add(primObj);
        lastPickedObject = primObj;
    }
    
    playerInput.enabled = false;
    transform.position =
        LocalDataSingleton.Instance.SpawnPlatforms[(int)NetworkManager.Singleton.LocalClientId].transform.position +
        Vector3.up * 1.5f;


}

    private void Update()
    {


        scoreText.text = Score.Value.ToString();
        nameText.text = Name.Value.ToString();


        if (!IsOwner)
            return;

        Name.Value = new FixedString64Bytes(LocalDataSingleton.Instance.playerName);
        


        Debug.Log("Yes");

        katamariSize = katamariCollider.bounds.size.x;

        HandleRotation();

        if (pickedObjects.Count > maxObjCount)
        {
            pickedObjects.RemoveAt(maxObjCount - 1);
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;

        HandleMovement();
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (!IsOwner)
            return;


        Debug.Log("Move Input: " + ctx.phase);
        if (ctx.performed || ctx.started)
        {
            moveInput = ctx.ReadValue<Vector2>();
        }
        else if (ctx.canceled)
        {
            moveInput = Vector2.zero;
        }
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        if (!IsOwner)
            return;

        if (ctx.performed || ctx.started)
        {
            lookInput = ctx.ReadValue<Vector2>();
        }
        else if (ctx.canceled)
        {
            lookInput = Vector2.zero;
        }
    }

    private void HandleMovement()
    {
        if (rb == null || moveInput == Vector2.zero)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 camForward = cam.transform.forward;
        Vector3 camRight = cam.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = (camRight * moveInput.x + camForward * moveInput.y).normalized;

        rb.AddForce(moveDir * speed, ForceMode.Force);
    }

    private void HandleRotation()
    {
        if (lookInput.x == 0f)
            return;

        transform.Rotate(Vector3.up, lookInput.x * rotationSpeed * Time.deltaTime, Space.World);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner)
            return;
        
        

        KatamariStick stick = collision.gameObject.GetComponent<KatamariStick>();
        KatamariController controller = collision.gameObject.GetComponent<KatamariController>();
        if (stick != null)
        {
            if(stick.isStick.Value == true)
                return;

            float objColSize = stick.size;

            if (objColSize < katamariSize)
            {
                stick.TransferOwnershipServerRPC(OwnerClientId, NetworkObjectId);

                Rigidbody otherRb = collision.gameObject.GetComponent<Rigidbody>();
                if (otherRb != null)
                {
                    otherRb.isKinematic = true;
                    otherRb.useGravity = false;
                    otherRb.detectCollisions = false;
                }

                pickedObjects.Add(collision.gameObject);
                lastPickedObject = collision.gameObject;

                katamariCollider.radius += objColSize / 50f;
                objCount += 1;

                Score.Value = objCount;
            }
        }

        if (controller != null)
        {
            if(controller.isStick.Value == true)
                return;
            

            if (Score.Value > controller.Score.Value)
            {
                controller.TransferOwnershipServerRPC(OwnerClientId, NetworkObjectId);

                collision.gameObject.GetComponent<KatamariController>().enabled = false;
                Destroy(collision.gameObject.GetComponent<Rigidbody>());
                collision.gameObject.GetComponent<Collider>().enabled = false;
                collision.gameObject.GetComponent<PlayerInput>().enabled = false;
                
                pickedObjects.Add(collision.gameObject);
                lastPickedObject = collision.gameObject;

                for (int i = 0; i < collision.transform.childCount; i++)
                {
                    if (collision.transform.GetChild(i).GetComponent<KatamariStick>() != null ||collision.transform.GetChild(i).GetComponent<KatamariController>())
                    {
                        collision.transform.GetChild(i).parent = transform;
                    }
                }
                katamariCollider.radius += controller.Score.Value / 50f;
                objCount += controller.Score.Value;

                Score.Value = objCount;
            }
        }
     

        
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TransferOwnershipServerRPC(ulong newOwnerId, ulong parentId)
    {
        var netObj = GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.ChangeOwnership(newOwnerId);

            var parentObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[parentId];
            netObj.TrySetParent(parentObj);

            isStick.Value = true;
        }
    }
    
    
    
}