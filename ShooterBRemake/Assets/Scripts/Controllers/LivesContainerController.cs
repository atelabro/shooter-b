using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class LivesContainerController : MonoBehaviour
    {
        [Header("References")]
        public TextMeshProUGUI overflowText;
        public List<Image> lifeIcons = new List<Image>();

        [Header("Sprites")]
        public Sprite lifeOnSprite;
        public Sprite lifeOffSprite;

        [Header("Config")]
        public int maxDisplayedLives = 5;

        private readonly List<Sprite> initialLifeSprites = new List<Sprite>();
        private bool initialized;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        public void SetLives(int lives)
        {
            InitializeIfNeeded();

            if (lifeIcons.Count == 0)
                return;

            int totalSlots = Mathf.Min(Mathf.Max(1, maxDisplayedLives), lifeIcons.Count);
            int activeSlots = Mathf.Clamp(Constants.INITIAL_LIVES, 1, totalSlots);
            if (lives > activeSlots)
                activeSlots = Mathf.Min(totalSlots, lives);

            int shownLives = Mathf.Clamp(lives, 0, activeSlots);
            int lostCount = activeSlots - shownLives;
            int hiddenLeadingSlots = totalSlots - activeSlots;

            for (int i = 0; i < totalSlots; i++)
            {
                Image icon = lifeIcons[i];
                if (icon == null)
                    continue;

                if (i < hiddenLeadingSlots)
                {
                    icon.enabled = false;
                    continue;
                }

                int activeIndex = i - hiddenLeadingSlots;
                Sprite availableSprite = ResolveOnSprite(i, icon);
                Sprite lostSprite = lifeOffSprite != null ? lifeOffSprite : availableSprite;
                bool isLost = activeIndex < lostCount;

                icon.sprite = isLost ? lostSprite : availableSprite;
                icon.enabled = icon.sprite != null;
            }

            if (overflowText != null)
            {
                bool hasOverflow = lives > totalSlots;
                overflowText.gameObject.SetActive(hasOverflow);
                if (hasOverflow)
                    overflowText.text = lives.ToString();
            }
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
                return;

            if (overflowText == null)
                overflowText = GetComponentInChildren<TextMeshProUGUI>(true);

            if (lifeIcons.Count == 0)
                DiscoverLifeIconsFromChildren();

            CacheInitialSprites();
            initialized = true;
        }

        private void DiscoverLifeIconsFromChildren()
        {
            lifeIcons.Clear();

            foreach (Transform child in transform)
            {
                if (child == null)
                    continue;

                if (child.GetComponent<TextMeshProUGUI>() != null)
                    continue;

                Image icon = child.GetComponent<Image>();
                if (icon != null)
                    lifeIcons.Add(icon);
            }
        }

        private void CacheInitialSprites()
        {
            initialLifeSprites.Clear();

            foreach (Image icon in lifeIcons)
            {
                if (icon == null)
                {
                    initialLifeSprites.Add(null);
                    continue;
                }

                initialLifeSprites.Add(icon.sprite);
            }
        }

        private Sprite ResolveOnSprite(int index, Image icon)
        {
            if (lifeOnSprite != null)
                return lifeOnSprite;

            if (index >= 0 && index < initialLifeSprites.Count && initialLifeSprites[index] != null)
                return initialLifeSprites[index];

            return icon != null ? icon.sprite : null;
        }
    }
}
