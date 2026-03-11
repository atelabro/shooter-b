using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class DevPanelController : MonoBehaviour
    {
        private GameObject panelRoot;
        private TextMeshProUGUI statusText;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        public void Show()
        {
            EnsureUi();
            if (panelRoot == null)
                return;

            RefreshStatus();
            panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        public void Toggle()
        {
            if (IsVisible)
                Hide();
            else
                Show();
        }

        private void EnsureUi()
        {
            if (panelRoot != null)
                return;

            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
                rootCanvas = FindObjectOfType<Canvas>(true);

            if (rootCanvas == null)
            {
                GameLog.Warning("[DevPanel] Canvas missing, cannot create debug panel.");
                return;
            }

            panelRoot = new GameObject("DevPanel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(rootCanvas.transform, false);

            RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image overlay = panelRoot.GetComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.7f);

            GameObject panel = new GameObject("Window", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(panelRoot.transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.17f, 0.18f);
            panelRect.anchorMax = new Vector2(0.83f, 0.82f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.12f, 0.14f, 0.18f, 1f);

            CreateLabel(panel.transform, "Title", "DEV PANEL", 34f, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.94f), TextAlignmentOptions.Center);
            statusText = CreateLabel(panel.transform, "Status", string.Empty, 20f, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.82f), TextAlignmentOptions.Center);

            CreateActionButton(panel.transform, "Coins100", "+100 Coins", new Vector2(0.1f, 0.56f), new Vector2(0.44f, 0.67f), () =>
            {
                GameManager.Instance.AddCoins(100);
                RefreshStatus();
            });
            CreateActionButton(panel.transform, "Coins1000", "+1000 Coins", new Vector2(0.56f, 0.56f), new Vector2(0.9f, 0.67f), () =>
            {
                GameManager.Instance.AddCoins(1000);
                RefreshStatus();
            });
            CreateActionButton(panel.transform, "UnlockAll", "Unlock All", new Vector2(0.1f, 0.41f), new Vector2(0.44f, 0.52f), () =>
            {
                GameManager.Instance.UnlockAllWeapons();
                RefreshStatus();
            });
            CreateActionButton(panel.transform, "ResetArmory", "Reset Armory", new Vector2(0.56f, 0.41f), new Vector2(0.9f, 0.52f), () =>
            {
                GameManager.Instance.ResetUnlockedWeaponsToDefault();
                RefreshStatus();
            });
            CreateActionButton(panel.transform, "ResetEconomy", "Reset Economy", new Vector2(0.1f, 0.26f), new Vector2(0.44f, 0.37f), () =>
            {
                GameManager.Instance.ResetCoins();
                RefreshStatus();
            });
            CreateActionButton(panel.transform, "ResetSave", "Reset All Save", new Vector2(0.56f, 0.26f), new Vector2(0.9f, 0.37f), () =>
            {
                GameManager.Instance.ResetAllProgressForTesting();
                RefreshStatus();
            });
            CreateActionButton(panel.transform, "Close", "Close", new Vector2(0.32f, 0.08f), new Vector2(0.68f, 0.18f), Hide);

            panelRoot.SetActive(false);
        }

        private void RefreshStatus()
        {
            if (statusText == null)
                return;

            statusText.text = $"Coins: {GameManager.Instance.Coins} | Selected: {GameManager.Instance.SelectedWeaponType}";
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string textValue, float fontSize, Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = textValue;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            return text;
        }

        private static void CreateActionButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Action callback)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.2f, 0.45f, 0.78f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() => callback?.Invoke());

            CreateLabel(buttonObject.transform, "Text", label, 22f, Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
        }
    }
}
