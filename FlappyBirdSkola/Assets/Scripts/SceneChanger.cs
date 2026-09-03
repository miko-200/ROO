using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName); //changes scene by name
    }

    public void QuitGame()
    {
        Debug.Log("Application Quit");
        Application.Quit();
    }
}
