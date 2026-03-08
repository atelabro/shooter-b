using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class GameStartingModalController : MonoBehaviour
    {
        [Header("Modal Root")]
        public GameObject modalRoot;

        [Header("Stage Info")]
        public TMP_Text titleText;
        public TMP_Text briefingText;

        [Header("Countdown")]
        public TMP_Text countdownText;
        public Button startButton;
        public Button plusLivesButton;
        public Button extraActionButton;
        public TMP_Text plusLivesText;
        public TMP_Text extraActionText;

        private bool startRequested;
        private bool plusLivesUsedThisStage;
        private bool extraActionUsedThisStage;
        private bool mercyBonusProcessedThisStage;
        private bool adRequestInFlight;
        private Coroutine statusCoroutine;

        public void Configure(string stageName, string stageBriefing)
        {
            EnsureReferences();
            plusLivesUsedThisStage = false;
            extraActionUsedThisStage = false;
            mercyBonusProcessedThisStage = false;
            adRequestInFlight = false;

            if (titleText != null)
                titleText.text = string.IsNullOrWhiteSpace(stageName)
                    ? LocalizationManager.Instance.Get("campaign.starting.default_stage", "Stage")
                    : stageName;

            if (briefingText != null)
                briefingText.text = stageBriefing ?? string.Empty;

            if (plusLivesText != null)
                plusLivesText.text = LocalizationManager.Instance.Get("campaign.starting.plus_lives", "+2 Lives");

            if (extraActionText != null)
                extraActionText.text = LocalizationManager.Instance.Get("campaign.starting.plus_bullets", "+ Bullets");

            if (plusLivesButton != null)
                plusLivesButton.interactable = !adRequestInFlight;

            if (extraActionButton != null)
                extraActionButton.interactable = !adRequestInFlight;
        }

        public IEnumerator PlayCountdown()
        {
            EnsureReferences();
            startRequested = false;

            if (modalRoot != null)
            {
                modalRoot.transform.SetAsLastSibling();
                modalRoot.SetActive(true);
            }

            SetCountdownText(LocalizationManager.Instance.Get("campaign.starting.start", "Start"));

            if (startButton != null)
                startButton.onClick.AddListener(HandleStartClicked);

            if (plusLivesButton != null)
                plusLivesButton.onClick.AddListener(HandlePlusLivesClicked);

            if (extraActionButton != null)
                extraActionButton.onClick.AddListener(HandleExtraActionClicked);

            TryOfferMercyBoost();

            while (!startRequested)
                yield return null;

            if (startButton != null)
                startButton.onClick.RemoveListener(HandleStartClicked);

            if (plusLivesButton != null)
                plusLivesButton.onClick.RemoveListener(HandlePlusLivesClicked);

            if (extraActionButton != null)
                extraActionButton.onClick.RemoveListener(HandleExtraActionClicked);

            if (modalRoot != null)
                modalRoot.SetActive(false);
        }

        private void SetCountdownText(string value)
        {
            if (countdownText != null)
                countdownText.text = value;
        }

        private void EnsureReferences()
        {
            if (modalRoot == null)
                modalRoot = gameObject;

            Image rootImage = modalRoot.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.enabled = true;
                rootImage.raycastTarget = true;
            }

            if (titleText == null)
            {
                Transform titleTransform = transform.Find("Card/TitleText");
                if (titleTransform != null)
                    titleText = titleTransform.GetComponent<TMP_Text>();
            }

            if (briefingText == null)
            {
                Transform briefingTransform = transform.Find("Card/BriefingText");
                if (briefingTransform != null)
                    briefingText = briefingTransform.GetComponent<TMP_Text>();
            }

            if (countdownText == null)
            {
                Transform countdownTransform = transform.Find("Card/CountdownText");
                if (countdownTransform != null)
                    countdownText = countdownTransform.GetComponent<TMP_Text>();
            }

            if (plusLivesText == null)
            {
                Transform plusLivesTransform = transform.Find("Card/ButtonsRow/Plus2LivesButton/PlusLivesButton");
                if (plusLivesTransform != null)
                    plusLivesText = plusLivesTransform.GetComponent<TMP_Text>();
            }

            if (plusLivesButton == null)
            {
                Transform plusLivesButtonTransform = transform.Find("Card/ButtonsRow/Plus2LivesButton");
                if (plusLivesButtonTransform != null)
                    plusLivesButton = plusLivesButtonTransform.GetComponent<Button>();
            }

            if (extraActionText == null)
            {
                Transform extraActionTransform = transform.Find("Card/ButtonsRow/ExtraActionButton/ExtraActionText");
                if (extraActionTransform != null)
                    extraActionText = extraActionTransform.GetComponent<TMP_Text>();
            }

            if (extraActionButton == null)
            {
                Transform extraActionButtonTransform = transform.Find("Card/ButtonsRow/ExtraActionButton");
                if (extraActionButtonTransform != null)
                    extraActionButton = extraActionButtonTransform.GetComponent<Button>();
            }

            if (startButton == null)
            {
                Transform startButtonTransform = transform.Find("Card/StartButton");
                if (startButtonTransform != null)
                    startButton = startButtonTransform.GetComponent<Button>();
            }

            if (startButton == null)
            {
                Transform fallbackButtonTransform = transform.Find("Card/CountdownText");
                if (fallbackButtonTransform != null)
                    startButton = fallbackButtonTransform.GetComponent<Button>();
            }
        }

        private void HandleStartClicked()
        {
            startRequested = true;
        }

        private void HandlePlusLivesClicked()
        {
            if (plusLivesUsedThisStage || adRequestInFlight)
                return;

            adRequestInFlight = true;
            RefreshAdButtonsState();
            SetCountdownText(LocalizationManager.Instance.Get("ads.status.loading", "Loading ad..."));
            RewardedAdService.Instance.ShowRewardedAd(
                RewardedAdPlacement.CampaignStartPlusLives,
                "campaign_start_plus_lives",
                adResult =>
                {
                    adRequestInFlight = false;

                    if (adResult != RewardedAdResult.Completed)
                    {
                        ShowStatusMessage(RewardedAdService.Instance.GetResultMessage(adResult));
                        RefreshAdButtonsState();
                        return;
                    }

                    if (GameManager.Instance != null)
                        GameManager.Instance.AddBonusLives(2);

                    plusLivesUsedThisStage = true;
                    RefreshAdButtonsState();
                    ShowStatusMessage(LocalizationManager.Instance.Get("campaign.starting.plus_lives_granted", "Reward granted: +2 Lives"));
                });
        }

        private void HandleExtraActionClicked()
        {
            if (extraActionUsedThisStage || adRequestInFlight)
                return;

            adRequestInFlight = true;
            RefreshAdButtonsState();
            SetCountdownText(LocalizationManager.Instance.Get("ads.status.loading", "Loading ad..."));
            RewardedAdService.Instance.ShowRewardedAd(
                RewardedAdPlacement.CampaignStartPlusBullets,
                "campaign_start_plus_bullets",
                adResult =>
                {
                    adRequestInFlight = false;

                    if (adResult != RewardedAdResult.Completed)
                    {
                        ShowStatusMessage(RewardedAdService.Instance.GetResultMessage(adResult));
                        RefreshAdButtonsState();
                        return;
                    }

                    ShooterController shooterController = FindObjectOfType<ShooterController>();
                    if (shooterController != null)
                        shooterController.ApplyConfiguredStageAmmoBonusToAllWeapons();

                    extraActionUsedThisStage = true;
                    RefreshAdButtonsState();
                    ShowStatusMessage(LocalizationManager.Instance.Get("campaign.starting.plus_bullets_granted", "Reward granted: + Bullets"));
                });
        }

        private void ShowStatusMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (statusCoroutine != null)
                StopCoroutine(statusCoroutine);

            statusCoroutine = StartCoroutine(ShowStatusMessageCoroutine(message));
        }

        private IEnumerator ShowStatusMessageCoroutine(string message)
        {
            SetCountdownText(message);
            yield return new WaitForSecondsRealtime(1.75f);
            SetCountdownText(LocalizationManager.Instance.Get("campaign.starting.start", "Start"));
            statusCoroutine = null;
        }

        private void TryOfferMercyBoost()
        {
            if (mercyBonusProcessedThisStage)
                return;

            GameManager manager = GameManager.Instance;
            if (manager == null || manager.ConsecutiveFailedRuns < 2)
                return;

            mercyBonusProcessedThisStage = true;
            SetCountdownText(LocalizationManager.Instance.Get("campaign.starting.mercy_prompt", "Mercy boost available"));
            adRequestInFlight = true;
            RefreshAdButtonsState();
            SetCountdownText(LocalizationManager.Instance.Get("ads.status.loading", "Loading ad..."));
            RewardedAdService.Instance.ShowRewardedAd(
                RewardedAdPlacement.CampaignStartPlusLives,
                "campaign_start_mercy_boost",
                adResult =>
                {
                    adRequestInFlight = false;

                    if (adResult == RewardedAdResult.Completed)
                    {
                        manager.AddBonusLives(2);
                        manager.ResetConsecutiveFailedRuns();
                        plusLivesUsedThisStage = true;
                        RefreshAdButtonsState();
                        ShowStatusMessage(LocalizationManager.Instance.Get("campaign.starting.mercy_granted", "Rescue bonus granted: +2 Lives"));
                        return;
                    }

                    RefreshAdButtonsState();
                    ShowStatusMessage(RewardedAdService.Instance.GetResultMessage(adResult));
                });
        }

        private void RefreshAdButtonsState()
        {
            if (plusLivesButton != null)
                plusLivesButton.interactable = !plusLivesUsedThisStage && !adRequestInFlight;

            if (extraActionButton != null)
                extraActionButton.interactable = !extraActionUsedThisStage && !adRequestInFlight;
        }
    }
}
