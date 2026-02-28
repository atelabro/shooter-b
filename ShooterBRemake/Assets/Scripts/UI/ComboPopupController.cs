using TMPro;
using UnityEngine;

namespace ShooterB
{
    public class ComboPopupController : MonoBehaviour
    {
        [Header("Text Target")]
        public TMP_Text popupText;

        [Header("Colors")]
        public Color doubleKillColor = new Color(0.3820755f, 1f, 0.51313657f, 1f);
        public Color tripleKillColor = new Color(1f, 0.302f, 0.302f, 1f);
        public Color quadraKillColor = new Color(0.705f, 0.424f, 1f, 1f);

        private void Awake()
        {
            if (popupText == null)
                popupText = GetComponent<TMP_Text>();
        }

        public void Configure(Constants.MultiKillType type, string label)
        {
            if (popupText == null)
                return;

            popupText.text = label;
            popupText.color = GetColor(type);
        }

        private Color GetColor(Constants.MultiKillType type)
        {
            switch (type)
            {
                case Constants.MultiKillType.DoubleKill:
                    return doubleKillColor;
                case Constants.MultiKillType.TripleKill:
                    return tripleKillColor;
                case Constants.MultiKillType.QuadraKill:
                    return quadraKillColor;
                default:
                    return doubleKillColor;
            }
        }
    }
}
