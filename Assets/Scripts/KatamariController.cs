using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class KatamariController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 500f;
    [SerializeField] private float rotationSpeed = 120f;

    private Rigidbody rb;
    private SphereCollider katamariCollider;

    private Vector2 moveInput;
    private Vector2 lookInput;

    private float katamariSize;

    [Header("Pickup Settings")]
    public int maxObjCount = 20;
    public int objCount = 0;

    private readonly List<GameObject> pickedObjects = new();

    [SerializeField] private GameObject primObj;
    private GameObject lastPickedObject;

private PlayerInput playerInput;

private void Awake()
{
    rb = GetComponent<Rigidbody>();
    katamariCollider = GetComponent<SphereCollider>();
    playerInput = GetComponent<PlayerInput>();
}

public override void OnNetworkSpawn()
{
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
}

    private void Update()
    {
        if (!IsOwner)
            return;


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
        if (stick == null)
            return;

        float objColSize = stick.size;

        if (objColSize < katamariSize)
        {
            collision.transform.SetParent(transform);

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
        }
    }
}