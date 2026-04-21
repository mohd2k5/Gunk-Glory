using Unity.Netcode;
using UnityEngine;

public class PlayerSingleton : NetworkBehaviour
{
    public static PlayerSingleton Instance { get; private set; }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Instance = this;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}