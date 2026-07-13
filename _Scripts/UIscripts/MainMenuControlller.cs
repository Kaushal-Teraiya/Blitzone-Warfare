using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public GameObject tutorialPanel;

    public void Play()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex + 1
        );
    }

    public void Tutorial()
    {
        tutorialPanel.SetActive(true);
    }

    public void Back()
    {
        tutorialPanel.SetActive(false);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}