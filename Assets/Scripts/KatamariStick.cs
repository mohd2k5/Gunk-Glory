using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class KatamariStick : NetworkBehaviour
{
    public NetworkVariable<bool> isStick = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> randomVisualIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public float size;
    private MeshCollider objCollider;

    public string objName = "placeholder";

    public MeshFilter[] objMeshs;
    public Material[] objMaterials;

    private MeshFilter myMeshFilter;
    private MeshRenderer myMeshRenderer;

    public override void OnNetworkSpawn()
    {
        objCollider = GetComponent<MeshCollider>();
        myMeshFilter = GetComponent<MeshFilter>();
        myMeshRenderer = GetComponent<MeshRenderer>();

        if (IsServer)
        {
            ChooseRandomVisual();
        }

        ApplyVisual(randomVisualIndex.Value);

        randomVisualIndex.OnValueChanged += OnVisualIndexChanged;

        Rigidbody rb = GetComponent<Rigidbody>();
        size = (objCollider.bounds.size.x + objCollider.bounds.size.y + objCollider.bounds.size.z) / 3f;
        // Force collider refresh
        objCollider.sharedMesh = null;
        objCollider.sharedMesh = myMeshFilter.mesh;
    }

    public override void OnNetworkDespawn()
    {
        randomVisualIndex.OnValueChanged -= OnVisualIndexChanged;
    }

    private void ChooseRandomVisual()
    {
        if (objMeshs == null || objMaterials == null || objMeshs.Length == 0 || objMaterials.Length == 0)
        {
            Debug.LogWarning($"{name}: Meshes or Materials are missing.");
            return;
        }

        int maxCount = Mathf.Min(objMeshs.Length, objMaterials.Length);
        randomVisualIndex.Value = Random.Range(0, maxCount);
    }

    private void OnVisualIndexChanged(int oldValue, int newValue)
    {
        ApplyVisual(newValue);
    }

    private void ApplyVisual(int index)
    {
        if (index < 0)
            return;

        if (objMeshs == null || objMaterials == null)
            return;

        if (index >= objMeshs.Length || index >= objMaterials.Length)
        {
            Debug.LogWarning($"{name}: Visual index {index} is out of range.");
            return;
        }

        if (myMeshFilter != null && objMeshs[index] != null)
        {
            myMeshFilter.mesh = objMeshs[index].sharedMesh;
        }

        if (myMeshRenderer != null && objMaterials[index] != null)
        {
            myMeshRenderer.material = objMaterials[index];
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