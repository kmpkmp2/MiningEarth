using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace DeepEarth.UI
{
    public class TooltipTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public event Action OnShowTooltip;
        public event Action OnHideTooltip;

        public void OnPointerDown(PointerEventData eventData)
        {
            OnShowTooltip?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnHideTooltip?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnHideTooltip?.Invoke();
        }
    }
}
