using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace NexusChaser.CycloneAMS.UI
{
    [AddComponentMenu("Nexus Chaser/Cyclone AMS/UI/Cyclone Button Tween")]
    // Agregamos ISubmitHandler
    public class CycloneButtonTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        [Header("Target")]
        [SerializeField] private Transform targetTransform;

        [Header("Scale Settings")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float clickScale = 0.95f;
        [SerializeField] private float animationDuration = 0.2f;
        [SerializeField] private Ease easeType = Ease.OutBack;

        private Vector3 _originalScale;
        private bool _isHovering = false;

        private void Awake()
        {
            if (targetTransform == null) targetTransform = transform;
            _originalScale = targetTransform.localScale;
        }

        private void OnDisable()
        {
            targetTransform.localScale = _originalScale;
        }

        private Tween AnimateScale(float targetMultiplier, Ease ease)
        {
            return targetTransform.DOScale(_originalScale * targetMultiplier, animationDuration)
                           .SetEase(ease)
                           .SetUpdate(true);
        }

        #region Mouse Logic
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovering = true;
            AnimateScale(hoverScale, easeType);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
            AnimateScale(1.0f, Ease.OutQuad);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            AnimateScale(clickScale, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            float target = _isHovering ? hoverScale : 1.0f;
            AnimateScale(target, easeType);
        }
        #endregion

        #region Keyboard / Gamepad Logic
        public void OnSelect(BaseEventData eventData)
        {
            if (!_isHovering) AnimateScale(hoverScale, easeType);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _isHovering = false;
            AnimateScale(1.0f, Ease.OutQuad);
        }

        // ESTO HACE QUE EL BOTÓN SE MUEVA AL DAR ENTER
        public void OnSubmit(BaseEventData eventData)
        {
            // Hacemos una secuencia rapida: Achicar -> Esperar -> Volver a Hover
            targetTransform.DOScale(_originalScale * clickScale, animationDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    // Al terminar de presionar, volvemos a escala Hover (porque sigue seleccionado)
                    AnimateScale(hoverScale, easeType);
                });
        }
        #endregion
    }
}