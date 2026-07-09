using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    public int levelIndex;

    public void LoadLevel()
    {
        SceneManager.LoadScene("Level" + levelIndex);
    }
}