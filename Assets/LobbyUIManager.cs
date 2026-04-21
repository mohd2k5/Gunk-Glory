using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyUIManager : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string nextSceneName;

    [Header("Main Panels")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject passPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject charactersPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject morePanel;
    [SerializeField] private GameObject popPanel;

    private GameObject currentPanel;
    private Stack<GameObject> panelHistory = new Stack<GameObject>();

    private void Start()
    {
        DeactivateAllPanels();
    }

    public void PlayGame()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("Next scene name is not set in LobbyUIManager.");
        }
    }

    public void OpenShop()
    {
        OpenPanel(shopPanel);
    }

    public void OpenPass()
    {
        OpenPanel(passPanel);
    }

    public void OpenSettings()
    {
        OpenPanel(settingsPanel);
    }

    public void OpenCharacters()
    {
        OpenPanel(charactersPanel);
    }

    public void OpenControls()
    {
        OpenPanel(controlsPanel);
    }

    public void OpenMore()
    {
        OpenPanel(morePanel);
    }

    public void ActivatePop()
    {
        OpenPanel(popPanel);
    }

    public void Back()
    {
        if (panelHistory.Count > 0)
        {
            if (currentPanel != null)
            {
                currentPanel.SetActive(false);
            }

            currentPanel = panelHistory.Pop();

            if (currentPanel != null)
            {
                currentPanel.SetActive(true);
            }
        }
        else
        {
            if (currentPanel != null)
            {
                currentPanel.SetActive(false);
                currentPanel = null;
            }
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }

    private void OpenPanel(GameObject panelToOpen)
    {
        if (panelToOpen == null)
        {
            Debug.LogWarning("Tried to open a panel, but it is not assigned.");
            return;
        }

        if (currentPanel != null && currentPanel != panelToOpen)
        {
            panelHistory.Push(currentPanel);
        }

        DeactivateAllPanels();

        panelToOpen.SetActive(true);
        currentPanel = panelToOpen;
    }

    private void DeactivateAllPanels()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        if (passPanel != null) passPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (charactersPanel != null) charactersPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (morePanel != null) morePanel.SetActive(false);
        if (popPanel != null) popPanel.SetActive(false);
    }
}