using UnityEngine;

public class LoaderTool : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        SceneLoader.Instance.LoadLevel(sceneName);
    }

    public void QuitGame() 
    {
        Application.Quit();
    }
}
