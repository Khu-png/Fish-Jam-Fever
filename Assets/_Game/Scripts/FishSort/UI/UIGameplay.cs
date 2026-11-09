using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FishSort
{
    // In-game HUD: level badge, settings button, "one slot left" warning and tutorial hints.
    public sealed class UIGameplay : UICanvas
    {
        private const float WARNING_MIN_ALPHA = 0.35f;
        private const float WARNING_PULSE_SPEED = 1.4f;

        [SerializeField] private TextMeshProUGUI levelLabel;
        [SerializeField] private Button settingsButton;
        [SerializeField] private GameObject warningRoot;
        [SerializeField] private CanvasGroup warningGroup;
        [SerializeField] private RectTransform tutorialArea;
        [SerializeField] private RectTransform tutorialHand;
        [SerializeField] private GameObject tipRoot;
        [SerializeField] private TextMeshProUGUI tipText;
        // Booster buttons are placeholders until boosters are designed (design doc: "bổ sung sau").
        [SerializeField] private Button[] boosterButtons;
        [SerializeField] private GameObject toastRoot;

        private const float TOAST_DURATION = 1.4f;

        private bool isWarningOn;
        private Coroutine toastRoutine;

        private void Awake()
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
            for (int i = 0; i < boosterButtons.Length; i++)
            {
                boosterButtons[i].onClick.AddListener(OnBoosterClicked);
            }
        }

        private void Update()
        {
            if (isWarningOn)
            {
                warningGroup.alpha = Mathf.Lerp(WARNING_MIN_ALPHA, 1f, Mathf.PingPong(Time.time * WARNING_PULSE_SPEED, 1f));
            }
        }

        public override void Open()
        {
            base.Open();
            SetWarning(false);
            HideTutorialHand();
            HideTip();
            toastRoot.SetActive(false);
        }

        public void SetLevel(int levelNumber)
        {
            levelLabel.text = string.Format(FishSortConst.FORMAT_LEVEL, levelNumber);
        }

        public void SetWarning(bool isOn)
        {
            isWarningOn = isOn;
            warningRoot.SetActive(isOn);
        }

        // uiCamera = the camera of the Screen Space - Camera canvas (prefabs cannot reference scene cameras).
        public void ShowTutorialHand(Vector3 screenPoint, Camera uiCamera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(tutorialArea, screenPoint, uiCamera, out Vector2 localPoint);
            tutorialHand.localPosition = localPoint;
            tutorialHand.gameObject.SetActive(true);
        }

        public void HideTutorialHand()
        {
            tutorialHand.gameObject.SetActive(false);
        }

        public void ShowTip(string message)
        {
            tipText.text = message;
            tipRoot.SetActive(true);
        }

        public void HideTip()
        {
            tipRoot.SetActive(false);
        }

        private void OnSettingsClicked()
        {
            SoundManager.Ins.PlayFx(FxID.Button);
            UIManager.Ins.OpenUI<UISettings>().SetInGame(true);
        }

        //TODO: replace with real booster logic once boosters are designed.
        private void OnBoosterClicked()
        {
            SoundManager.Ins.PlayFx(FxID.Button);
            if (toastRoutine != null)
            {
                StopCoroutine(toastRoutine);
            }

            toastRoutine = StartCoroutine(ToastRoutine());
        }

        private System.Collections.IEnumerator ToastRoutine()
        {
            toastRoot.SetActive(true);
            yield return new WaitForSeconds(TOAST_DURATION);
            toastRoot.SetActive(false);
            toastRoutine = null;
        }
    }
}
