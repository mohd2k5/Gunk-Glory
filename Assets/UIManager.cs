using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;
public class UIManager : NetworkBehaviour
{
    public GameObject startMenu;
    public GameObject inGameUI;
    public TMP_InputField playerNameInput;

    void Start()
    {
        
        startMenu.SetActive(true);
        inGameUI.SetActive(false);
        
    }


    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        startMenu.SetActive(false);
        inGameUI.SetActive(true);
        LocalDataSingleton.Instance.playerName = playerNameInput.text;
        
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
       
        startMenu.SetActive(false);
        inGameUI.SetActive(true);
        LocalDataSingleton.Instance.playerName = playerNameInput.text;
        
    }





}
