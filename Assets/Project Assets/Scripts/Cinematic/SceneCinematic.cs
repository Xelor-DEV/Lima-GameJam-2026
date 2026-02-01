using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneCinematic : MonoBehaviour
{
    public void IrACinematica(string nombreEscena)
    {
        SceneManager.LoadScene(nombreEscena);
    }
}