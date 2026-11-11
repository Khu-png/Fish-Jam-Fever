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
        // Booster buttons are placeholders until boosters are designed (design doc: "bá»• sung sau").
        [SerializeField] private Button[] boosterButtons;
        [SerializeField] private GameObject toastRoot;
        [SerializeField] private TextMeshProUGUI fishProgressText;
        [Tooltip("The fish-count board left of the tray; hidden while the cannon takes its place.")]
        [SerializeField] private GameObject fishCounterRoot;

        private const float TOAST_DURATION = 1.4f;
        // The first booster button (rod icon) is the fishing rod.
        private const int ROD_BUTTON_INDEX = 0;
        // The second booster button (cannon icon) is the cannon.
        private const int CANNON_BUTTON_INDEX = 1;

        private bool isWarningOn;
        private Coroutine toastRoutine;

        private void Awake()
        {
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            for (int i = 0; i < boosterButtons.Length; i++)
            {
                boosterButtons[i].onClick.AddListener(BoosterListener(i));
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
            // The level badge, fish count, settings button and warning frame come from Quan's HUD (Canvas_GamePlay).
            UIManager.Ins.OpenUI<FishJam.UI.GamePlay>();
            SetWarning(false);
            HideTutorialHand();
            HideTip();
            toastRoot.SetActive(false);
        }

        public override void CloseDirectly()
        {
            UIManager.Ins.CloseUI<FishJam.UI.GamePlay>();
            base.CloseDirectly();
        }

        public void SetLevel(int levelNumber)
        {
            UIManager.Ins.GetUI<FishJam.UI.GamePlay>().SetLevel(levelNumber);
        }

        // Fish counter in the HUD ("placed/total"), updated by GameplayController.
        public void SetFishProgress(int placed, int total)
        {
            fishProgressText.text = string.Format(FishSortConst.FORMAT_PROGRESS, placed, total);
            UIManager.Ins.GetUI<FishJam.UI.GamePlay>().SetFishCount(placed, total);
        }

        public void SetFishCounterVisible(bool isVisible)
        {
            fishCounterRoot.SetActive(isVisible);
        }

        public void SetWarning(bool isOn)
        {
            isWarningOn = false;
            warningRoot.SetActive(false);
            UIManager.Ins.GetUI<FishJam.UI.GamePlay>().SetWarning(isOn);
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
            UIManager.Ins.OpenUI<FishJam.UI.Setting>();
        }

        private UnityEngine.Events.UnityAction BoosterListener(int index)
        {
            UnityEngine.Events.UnityAction listener = OnBoosterClicked;
            if (index == ROD_BUTTON_INDEX)
            {
                listener = OnRodClicked;
            }
            else if (index == CANNON_BUTTON_INDEX)
            {
                listener = OnCannonClicked;
            }

            return listener;
        }

        //TODO: the cannon is free for now; add stock / the 600-coin purchase popup once a coin system exists.
        private void OnCannonClicked()
        {
            SoundManager.Ins.PlayFx(FxID.Button);
            GameplayController.Ins.TryUseCannon();
        }

        //TODO: the rod is free for now; add stock / the 500-coin purchase popup once a coin system exists.
        private void OnRodClicked()
        {
            SoundManager.Ins.PlayFx(FxID.Button);
            GameplayController.Ins.TryUseRod();
        }

        //TODO: replace with real booster logic for the other boosters once they are designed.
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
