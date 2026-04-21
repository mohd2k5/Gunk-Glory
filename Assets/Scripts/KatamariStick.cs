using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class KatamariStick : NetworkBehaviour
{
    public NetworkVariable<bool> isStick = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> randomVisualIndex = new(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [Header("Data")]
    [SerializeField] private float size;
    [SerializeField] private string objectName = "placeholder";

    [Header("Visual Variants")]
    [SerializeField] private MeshFilter[] objectMeshes;
    [SerializeField] private Material[] objectMaterials;

    public float sizeValue => size;
    public string ObjectName => objectName;

    private MeshCollider meshCollider;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Rigidbody rb;
    private NetworkRigidbody networkRigidbody;
    private Collider cachedCollider;

    private void Awake()
    {
        meshCollider = GetComponent<MeshCollider>();
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        rb = GetComponent<Rigidbody>();
        networkRigidbody = GetComponent<NetworkRigidbody>();
        cachedCollider = GetComponent<Collider>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ChooseRandomVisual();
        }

        ApplyVisual(randomVisualIndex.Value);
        RefreshColliderAndSize();
        ApplyStickState(isStick.Value);

        randomVisualIndex.OnValueChanged += OnVisualIndexChanged;
        isStick.OnValueChanged += OnStickChanged;
    }

    public override void OnNetworkDespawn()
    {
        randomVisualIndex.OnValueChanged -= OnVisualIndexChanged;
        isStick.OnValueChanged -= OnStickChanged;
    }

    private void ChooseRandomVisual()
    {
        if (objectMeshes == null || objectMaterials == null || objectMeshes.Length == 0 || objectMaterials.Length == 0)
        {
            Debug.LogWarning($"{name}: visual variants are missing.");
            return;
        }

        int maxCount = Mathf.Min(objectMeshes.Length, objectMaterials.Length);
        randomVisualIndex.Value = Random.Range(0, maxCount);
    }

    private void OnVisualIndexChanged(int oldValue, int newValue)
    {
        ApplyVisual(newValue);
        RefreshColliderAndSize();
    }

    private void OnStickChanged(bool oldValue, bool newValue)
    {
        ApplyStickState(newValue);
    }

    private void ApplyVisual(int index)
    {
        if (index < 0 || objectMeshes == null || objectMaterials == null)
        {
            return;
        }

        if (index >= objectMeshes.Length || index >= objectMaterials.Length)
        {
            Debug.LogWarning($"{name}: visual index {index} is out of range.");
            return;
        }

        if (meshFilter != null && objectMeshes[index] != null)
        {
            meshFilter.sharedMesh = objectMeshes[index].sharedMesh;
        }

        if (meshRenderer != null && objectMaterials[index] != null)
        {
            meshRenderer.material = objectMaterials[index];
        }
    }

    private void RefreshColliderAndSize()
    {
        if (meshCollider == null || meshFilter == null || meshFilter.sharedMesh == null)
        {
            return;
        }

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = meshFilter.sharedMesh;

        Bounds bounds = meshCollider.bounds;
        size = (bounds.size.x + bounds.size.y + bounds.size.z) / 3f;
    }

    private void ApplyStickState(bool stuck)
    {
        if (networkRigidbody != null)
        {
            networkRigidbody.enabled = !stuck;
        }

        if (cachedCollider != null)
        {
            cachedCollider.enabled = !stuck;
        }

        if (rb == null)
        {
            return;
        }

        if (stuck)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        rb.isKinematic = stuck;
        rb.useGravity = !stuck;
        rb.detectCollisions = !stuck;
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
            Debug.LogWarning($"KatamariStick parent NetworkObject {parentId} was not found.");
            return;
        }

        if (networkRigidbody != null)
        {
            networkRigidbody.enabled = false;
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

        networkObjectComponent.ChangeOwnership(newOwnerId);
        networkObjectComponent.TrySetParent(parentObject, true);
        isStick.Value = true;
    }
}