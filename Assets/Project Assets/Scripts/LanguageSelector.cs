using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;
using UnityEngine.Events;
using System.Threading.Tasks; // Necesario para TaskCompletionSource

public class LanguageSelector : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI languageNameText;

    [Header("Unity Events")]
    public UnityEvent OnLanguageChangeStarted;
    public UnityEvent OnLanguageLoadFinished;

    private int currentIndex = 0;
    private bool isChangingLanguage = false;

    async void Start()
    {
        // CORRECCIÓN 1: Agregamos .Task aquí
        await LocalizationSettings.InitializationOperation.Task;

        var availableLocales = LocalizationSettings.AvailableLocales.Locales;
        Locale currentLocale = LocalizationSettings.SelectedLocale;
        currentIndex = availableLocales.IndexOf(currentLocale);

        UpdateLanguageText(currentLocale);
    }

    public void NextLanguage() => NavigateLanguage(1);
    public void PreviousLanguage() => NavigateLanguage(-1);

    private void NavigateLanguage(int direction)
    {
        if (isChangingLanguage) return;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        currentIndex += direction;
        if (currentIndex >= locales.Count) currentIndex = 0;
        if (currentIndex < 0) currentIndex = locales.Count - 1;

        ChangeLanguageAsync(locales[currentIndex]);
    }

    private async void ChangeLanguageAsync(Locale newLocale)
    {
        // Evitar cambiar al mismo idioma (el evento nunca se dispararía)
        if (LocalizationSettings.SelectedLocale == newLocale) return;

        isChangingLanguage = true;
        OnLanguageChangeStarted?.Invoke();

        // CORRECCIÓN 2: Lógica para esperar el cambio real
        // Creamos una "promesa" (TaskCompletionSource)
        var tcs = new TaskCompletionSource<bool>();

        // Definimos qué pasa cuando el idioma termina de cambiar
        System.Action<Locale> onLocaleChanged = null;
        onLocaleChanged = (locale) =>
        {
            // Nos desuscribimos para no dejar basura
            LocalizationSettings.SelectedLocaleChanged -= onLocaleChanged;
            // Completamos la promesa
            tcs.SetResult(true);
        };

        // Nos suscribimos al evento del sistema
        LocalizationSettings.SelectedLocaleChanged += onLocaleChanged;

        // Iniciamos el cambio
        LocalizationSettings.SelectedLocale = newLocale;
        UpdateLanguageText(newLocale);

        // AWAIT REAL: Esperamos a que la promesa se cumpla (el evento se dispare)
        await tcs.Task;

        OnLanguageLoadFinished?.Invoke();
        isChangingLanguage = false;
    }

    private void UpdateLanguageText(Locale locale)
    {
        if (languageNameText != null)
        {
            languageNameText.text = locale.Identifier.CultureInfo.NativeName;
        }
    }
}