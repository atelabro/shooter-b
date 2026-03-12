using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ShooterB
{
    public class LogoTapTrigger : MonoBehaviour, IPointerClickHandler
    {
        private Action onTap;

        public void Initialize(Action callback)
        {
            onTap = callback;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
                return;

            onTap?.Invoke();
        }
    }
}
