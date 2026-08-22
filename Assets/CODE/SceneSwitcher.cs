using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public void LoadByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
