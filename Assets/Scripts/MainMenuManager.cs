using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject tutorialPanel;
    public GameObject aboutPanel;

    public void PlayGame()
    {
        SceneManager.LoadScene("MainFacility");
    }

    public void OpenTutorial()
    {
        CloseAllPanels();

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);
    }

    public void OpenAbout()
    {
        CloseAllPanels();

        if (aboutPanel != null)
            aboutPanel.SetActive(true);
    }

    public void CloseAllPanels()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (aboutPanel != null)
            aboutPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}