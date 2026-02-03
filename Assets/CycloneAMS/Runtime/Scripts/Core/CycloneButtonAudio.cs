using UnityEngine;
using UnityEngine.EventSystems;

namespace NexusChaser.CycloneAMS.UI
{
    [AddComponentMenu("Nexus Chaser/Cyclone AMS/UI/Cyclone Button Audio")]
    // Agregamos ISubmitHandler para detectar el teclado/gamepad
    public class CycloneButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, ISelectHandler, ISubmitHandler
    {
        [Header("Audio Clips")]
        [SerializeField] private CycloneClip hoverSound;
        [SerializeField] private CycloneClip clickSound;

        [Header("Settings")]
        [Tooltip("Evita que el sonido de Hover se reproduzca al hacer click con el mouse.")]
        [SerializeField] private bool preventHoverSoundOnClick = false;

        private bool _justClicked = false;

        private void PlaySound(CycloneClip clip)
        {
            if (clip != null && CycloneAudioDriver.Instance != null)
            {
                CycloneAudioDriver.Instance.PlayOneShot(clip);
            }
        }

        #region Mouse / Pointer Logic
        public void OnPointerEnter(PointerEventData eventData)
        {
            PlaySound(hoverSound);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _justClicked = true;
            PlaySound(clickSound);
            Invoke(nameof(ResetClickFlag), 0.1f);
        }
        #endregion

        #region Keyboard / Gamepad Logic
        public void OnSelect(BaseEventData eventData)
        {
            // Si acabamos de hacer click con el mouse, Unity llama a OnSelect automáticamente.
            // Esto evita que suene doble (Click + Hover) al usar el mouse.
            if (preventHoverSoundOnClick && _justClicked) return;

            PlaySound(hoverSound);
        }

        // ESTA ES LA PARTE NUEVA: Detecta Enter o Espacio
        public void OnSubmit(BaseEventData eventData)
        {
            PlaySound(clickSound);
        }
        #endregion

        private void ResetClickFlag()
        {
            _justClicked = false;
        }
    }
}