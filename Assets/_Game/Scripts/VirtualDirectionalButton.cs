using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoreGuard
{
    [RequireComponent(typeof(Image))]
    public sealed class VirtualDirectionalButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Vector2 Direction;
        public MobileControlsPresenter Presenter;
        public Image BackgroundImage;
        public Text LabelText;
        public float NormalAlpha = .4f;
        public float PressedAlpha = .85f;

        public bool IsPressed { get; private set; }

        private void Awake()
        {
            if (!BackgroundImage) BackgroundImage = GetComponent<Image>();
            if (!LabelText) LabelText = GetComponentInChildren<Text>();
            UpdateVisual();
        }

        private void OnEnable()
        {
            UpdateVisual();
        }

        private void OnDisable()
        {
            if (IsPressed)
            {
                IsPressed = false;
                UpdateVisual();
                if (Presenter) Presenter.NotifyDirectionChanged();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsPressed = true;
            UpdateVisual();
            if (Presenter) Presenter.NotifyDirectionChanged();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsPressed = false;
            UpdateVisual();
            if (Presenter) Presenter.NotifyDirectionChanged();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsPressed = false;
            UpdateVisual();
            if (Presenter) Presenter.NotifyDirectionChanged();
        }

        public void SetPressedState(bool pressed)
        {
            IsPressed = pressed;
            UpdateVisual();
            if (Presenter) Presenter.NotifyDirectionChanged();
        }

        private void UpdateVisual()
        {
            var alpha = IsPressed ? PressedAlpha : NormalAlpha;
            if (BackgroundImage)
            {
                var color = BackgroundImage.color;
                color.a = alpha;
                BackgroundImage.color = color;
            }
            if (LabelText)
            {
                var textColor = LabelText.color;
                textColor.a = Mathf.Clamp01(alpha + .25f);
                LabelText.color = textColor;
            }
        }
    }
}
