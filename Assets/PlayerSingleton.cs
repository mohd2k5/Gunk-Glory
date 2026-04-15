using Unity.Netcode;
using UnityEngine;

public class PlayerSingleton : NetworkBehaviour
{
    public static PlayerSingleton Instance { get; private set; }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        Instance = this;
    }

}