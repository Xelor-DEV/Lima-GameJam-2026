using UnityEngine;
using UnityEngine.UI;

namespace NexusChaser.CycloneAMS.UI
{
    [RequireComponent(typeof(Slider))]
    public class CycloneVolumeSender : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Slider slider;
        [SerializeField] private CycloneMemory memory; 

        [Header("Settings")]
        [Tooltip("El tipo de canal que este slider controlará.")]
        [SerializeField] private ChannelType channelType;
        [SerializeField] private bool initializeOnStart = true;

        private void Awake()
        {
            if (slider == null)
            {
                slider = GetComponent<Slider>();
            }
        }

        private void Start()
        {
            if (initializeOnStart == true)
            {
                if (memory != null)
                {
                    Initialize();
                }
            }
        }

        private void OnDestroy()
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(UpdateCycloneVolume);
            }
        }

        public void Initialize()
        {
            if (memory == null)
            {
                Debug.LogError($"[CycloneUI] Memory received in sender '{name}' is null.");
                return;
            }

            // 1. OBTENER VALOR ACTUAL: Sincroniza el slider con lo que haya en la memoria al iniciar
            // Esto evita que el slider empiece en 0 si el volumen real es 0.5
            try
            {
                float currentVol = memory.GetVolume(channelType);
                slider.SetValueWithoutNotify(currentVol); // Usamos WithoutNotify para no disparar el evento de audio al iniciar
            }
            catch
            {
                // Si el canal no existe en la config, deshabilitamos el slider para evitar errores
                Debug.LogWarning($"[CycloneUI] Channel '{channelType}' not configured in Memory. Disabling slider.");
                slider.interactable = false;
                return;
            }

            slider.onValueChanged.AddListener(UpdateCycloneVolume);
        }

        private void UpdateCycloneVolume(float value)
        {
            if (memory != null)
            {
                // Enviamos el nuevo valor a CycloneMemory
                // La memoria ya se encarga de convertir a decibelios y clampear
                memory.SetVolume(channelType, value);
            }
        }
    }
}