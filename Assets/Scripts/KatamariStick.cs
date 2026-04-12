using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;



[RequireComponent(typeof(Rigidbody))]
public class KatamariStick : NetworkBehaviour
{

     public NetworkVariable<bool> isStick = new NetworkVariable<bool>(false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);

    public float size;
    private Collider objCollider;
    public string objName = "placeholder";

    void Start()
    {
        objCollider = GetComponent<Collider>();
        Rigidbody rb = GetComponent<Rigidbody>();
        size = (objCollider.bounds.size.x + objCollider.bounds.size.y + objCollider.bounds.size.z)/3;
    }


    // Call a server rpc that transfer the ownership of the object to the player who collided with it, and set isStick to true
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
