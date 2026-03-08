using TMPro;
using UnityEngine;

namespace ShooterB
{
    public class ComboPopupController : MonoBehaviour
    {
        [Header("Text Target")]
        public TMP_Text popupText;
        [Header("Text Style")]
        public bool applyOutline = true;
        public Color outlineColor = new Color32(27, 37, 51, 255);
        [Range(0f, 1f)] public float outlineWidth = 0.15f;

        [Header("Colors")]
        public Color doubleKillColor = new Color(0.3820755f, 1f, 0.51313657f, 1f);
        public Color tripleKillColor = new Color(1f, 0.302f, 0.302f, 1f);
        public Color quadraKillColor = new Color(0.705f, 0.424f, 1f, 1f);

        private void Awake()
        {
            if (popupText == null)
                popupText = GetComponent<TMP_Text>();

            ApplyOutlineToText();
        }

        public void Configure(Constants.MultiKillType type, string label)
        {
            if (popupText == null)
                return;

            popupText.text = label;
            popupText.color = GetColor(type);
            ApplyOutlineToText();
        }

        private void ApplyOutlineToText()
        {
            if (!applyOutline || popupText == null)
                return;

            Material instanceMaterial = popupText.fontMaterial;
            if (instanceMaterial == null)
                return;

            if (instanceMaterial.HasProperty("_OutlineColor"))
                instanceMaterial.SetColor("_OutlineColor", outlineColor);

            if (instanceMaterial.HasProperty("_OutlineWidth"))
                instanceMaterial.SetFloat("_OutlineWidth", Mathf.Clamp01(outlineWidth));

            popupText.fontMaterial = instanceMaterial;
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
