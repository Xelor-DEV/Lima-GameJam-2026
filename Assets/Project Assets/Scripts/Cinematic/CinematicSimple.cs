using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Necesario para la variable Button

public class CinematicSimple : MonoBehaviour
{
    public GameObject[] vinetas;
    public CanvasGroup panelNegro;
    public Button botonSkip; // Arrastra tu botón aquí en el Inspector

    [Header("Configuración de Tiempos")]
    public float tiempoEspera = 3.0f;
    public float tiempoFinal = 6.0f;
    public float velocidadAlpha = 1.5f;

    [Header("Escena a Cargar")]
    public string nombreEscenaMenu = "MainMenu";
    void Update()
    {
        // Si el jugador toca una tecla o mueve el joystick y nada está seleccionado...
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0 || Input.anyKeyDown)
            {
                // Le damos el foco al botón Skip
                botonSkip.Select();
            }
        }
    }
    void Start()
    {
        foreach (GameObject v in vinetas)
        {
            CanvasGroup cg = v.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0;
            v.SetActive(false);
        }

        if (panelNegro != null) panelNegro.alpha = 0;

        StartCoroutine(ReproducirHistoria());
    }

    IEnumerator ReproducirHistoria()
    {
        for (int i = 0; i < vinetas.Length; i++)
        {
            vinetas[i].SetActive(true);
            CanvasGroup cg = vinetas[i].GetComponent<CanvasGroup>();
            yield return StartCoroutine(AparecerAlpha(cg));

            if (i == vinetas.Length - 1)
                yield return new WaitForSeconds(tiempoFinal);
            else
                yield return new WaitForSeconds(tiempoEspera);
        }

        // --- FINAL DE LA CINEMÁTICA ---
        // Desactivamos el botón justo antes del fade para que no sea un "mal tercio"
        if (botonSkip != null) botonSkip.gameObject.SetActive(false);

        yield return StartCoroutine(AparecerAlpha(panelNegro));
        SceneManager.LoadScene(nombreEscenaMenu);
    }

    IEnumerator AparecerAlpha(CanvasGroup cg)
    {
        float t = 0;
        while (t < 1.0f)
        {
            t += Time.deltaTime / velocidadAlpha;
            if (cg != null) cg.alpha = t;
            yield return null;
        }
        if (cg != null) cg.alpha = 1;
    }

    public void SaltarCinematica()
    {
        // Al hacer skip, desactivamos el botón inmediatamente
        if (botonSkip != null)
        {
            botonSkip.interactable = false; // Ya no se puede cliquear
            botonSkip.gameObject.SetActive(false); // Desaparece visualmente
        }
        StartCoroutine(SecuenciaSalida());
    }

    IEnumerator SecuenciaSalida()
    {
        // Detenemos la historia para que no sigan saliendo viñetas
        StopCoroutine("ReproducirHistoria");

        if (panelNegro != null)
        {
            // Hacemos el fundido un poco más rápido para el skip
            float t = panelNegro.alpha;
            while (t < 1.0f)
            {
                t += Time.deltaTime / (velocidadAlpha / 2);
                panelNegro.alpha = t;
                yield return null;
            }
            panelNegro.alpha = 1;
        }

        SceneManager.LoadScene(nombreEscenaMenu);
    }
}