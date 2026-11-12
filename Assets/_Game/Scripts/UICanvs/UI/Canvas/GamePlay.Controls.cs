using System.Collections;
using FishSort;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FishJam.UI
{
    // Gameplay controls and tutorial belong to the same Canvas_GamePlay as the HUD.
    public partial class GamePlay
    {
        [Header("Gameplay controls")]
        [SerializeField] private Button[] boosterButtons;
        [Tooltip("Per power-up button: the '+' shown when none are left, and the badge with the number owned.")]
        [SerializeField] private GameObject[] boosterPlus;
        [SerializeField] private GameObject[] boosterBadge;
        [SerializeField] private TMP_Text[] boosterCount;
        [SerializeField] private RectTransform tutorialArea;
        [SerializeField] private RectTransform tutorialHand;
        [SerializeField] private Graphic tutorialPulse;
        [SerializeField] private CanvasGroup tutorialMessage;
        [SerializeField] private GameObject tipRoot;
        [SerializeField] private TextMeshProUGUI tipText;
        [SerializeField] private GameObject toastRoot;
        [Tooltip("The dark banner explaining ice bubbles; it shows when a frozen bubble is touched and fades away by itself.")]
        [SerializeField] private CanvasGroup iceTip;
        [Tooltip("The dark screen with the tanks lit up, used to choose where the fishing rod fishes.")]
        [SerializeField] private TankChoice tankChoice;

        public TankChoice TankChoice => tankChoice;

        private const float TOAST_DURATION = 1.4f;
        private const float ICE_TIP_HOLD = 2f;
        private const float ICE_TIP_FADE = 1f;
        private const string JAR_FULL_MESSAGE = "The jar is already full of fish";
        private const string NO_TRAY_FISH_MESSAGE = "There are no fish in the tray";
        private Coroutine toastRoutine;
        private Coroutine iceTipRoutine;
        private Coroutine tipRoutine;
        private Transform tutorialTarget;
        private Camera tutorialCamera;
        private Vector3 tutorialWorldOffset;
        private float tutorialStarted;

        private void OnEnable()
        {
            PlayerData.BoostersChanged += RefreshBoosterBadges;
            RefreshBoosterBadges();
        }

        private void OnDisable()
        {
            PlayerData.BoostersChanged -= RefreshBoosterBadges;
        }

        // '+' while the player has none, the number owned otherwise.
        private void RefreshBoosterBadges()
        {
            for (int i = 0; boosterPlus != null && i < boosterPlus.Length; i++)
            {
                int owned = PlayerData.GetBooster((BoosterKind)i);
                boosterPlus[i].SetActive(owned == 0);
                boosterBadge[i].SetActive(owned > 0);
                boosterCount[i].text = owned.ToString();
            }
        }

        private void InitializeGameplayControls()
        {
            for (int i = 0; i < boosterButtons.Length; i++)
            {
                if (i == 0) boosterButtons[i].onClick.AddListener(OnRodClicked);
                else if (i == 1) boosterButtons[i].onClick.AddListener(OnCannonClicked);
                else boosterButtons[i].onClick.AddListener(OnOtherBoosterClicked);
            }
        }

        private void ResetGameplayOverlays()
        {
            toastRoutine = null;
            tipRoutine = null;
            HideTutorialHand();
            HideTip();
            toastRoot.SetActive(false);
            HideIceTip();
        }

        public void ShowTutorialHand(Transform target, Camera worldCamera, Vector3 worldOffset)
        {
            if (tutorialTarget != target) tutorialStarted = Time.time;
            tutorialTarget = target;
            tutorialCamera = worldCamera;
            tutorialWorldOffset = worldOffset;
        }

        private void LateUpdate()
        {
            if (!tutorialTarget || !tutorialCamera) return;
            Vector3 screenPoint = tutorialCamera.WorldToScreenPoint(tutorialTarget.position + tutorialWorldOffset);
            Canvas canvas = tutorialArea.GetComponentInParent<Canvas>().rootCanvas;
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(tutorialArea, screenPoint, uiCamera, out Vector2 point)) return;

            float phase = Mathf.Repeat((Time.time - tutorialStarted) / 1.25f, 1f);
            float press = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(phase * Mathf.PI * 2f)), 3f);
            tutorialHand.gameObject.SetActive(true);
            // The pivot is at the fingertip, so the press never drifts off its target.
            tutorialHand.localPosition = point + new Vector2(10f, -16f) * (1f - press);
            tutorialHand.localScale = Vector3.one * Mathf.Lerp(1f, .91f, press);
            if (tutorialPulse)
            {
                tutorialPulse.gameObject.SetActive(true);
                tutorialPulse.rectTransform.localPosition = point;
                tutorialPulse.rectTransform.localScale = Vector3.one * Mathf.Lerp(.65f, 1.25f, phase);
                Color tint = tutorialPulse.color;
                tint.a = Mathf.Sin(phase * Mathf.PI) * .85f;
                tutorialPulse.color = tint;
            }
        }

        public void HideTutorialHand()
        {
            tutorialTarget = null;
            tutorialHand.gameObject.SetActive(false);
            tutorialHand.localScale = Vector3.one;
            if (tutorialPulse) tutorialPulse.gameObject.SetActive(false);
        }

        public void ShowTip(string message)
        {
            tipText.text = message;
            tipRoot.SetActive(true);
            if (tipRoutine != null) StopCoroutine(tipRoutine);
            tipRoutine = StartCoroutine(RevealTip());
        }

        private IEnumerator RevealTip()
        {
            for (float t = 0f; t < .22f; t += Time.deltaTime)
            {
                float amount = Mathf.SmoothStep(0f, 1f, t / .22f);
                if (tutorialMessage) tutorialMessage.alpha = amount;
                tipRoot.transform.localScale = Vector3.one * Mathf.Lerp(.94f, 1f, amount);
                yield return null;
            }
            if (tutorialMessage) tutorialMessage.alpha = 1f;
            tipRoot.transform.localScale = Vector3.one;
            tipRoutine = null;
        }

        public void HideTip()
        {
            if (tipRoutine != null) StopCoroutine(tipRoutine);
            tipRoutine = null;
            tipRoot.SetActive(false);
        }

        private void OnRodClicked()
        {
            UseOrBuy(BoosterKind.Rod);
        }

        private void OnCannonClicked()
        {
            UseOrBuy(BoosterKind.Cannon);
        }

        // With stock the power-up is used (and one is taken); without, the shop opens to buy more.
        private void UseOrBuy(BoosterKind kind)
        {
            SoundManager.Ins.PlayFx(FxID.Button);
            if (PlayerData.GetBooster(kind) == 0)
            {
                UIManager.Ins.OpenUI<BoosterShop>().Show(kind);
                return;
            }

            if (kind == BoosterKind.Cannon && GameplayController.Ins.IsJarFull)
            {
                ShowToast(JAR_FULL_MESSAGE);
                return;
            }

            if (kind == BoosterKind.Cannon && GameplayController.Ins.IsTrayEmpty)
            {
                ShowToast(NO_TRAY_FISH_MESSAGE);
                return;
            }

            if (kind == BoosterKind.Rod)
            {
                // The rod asks which tank to fish for first and takes its stock itself once it is used.
                if (!GameplayController.Ins.BeginRodSelection(out string message) && message.Length > 0)
                {
                    ShowToast(message);
                }

                return;
            }

            if (GameplayController.Ins.TryUseCannon())
            {
                PlayerData.UseBooster(kind);
            }
        }

        private void OnOtherBoosterClicked()
        {
            SoundManager.Ins.PlayFx(FxID.Button);
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToastRoutine());
        }

        // Touching a frozen bubble: the banner appears at once, stays a little and then fades away slowly.
        public void ShowIceTip()
        {
            if (iceTip == null) return;
            if (iceTipRoutine != null) StopCoroutine(iceTipRoutine);
            iceTipRoutine = StartCoroutine(IceTipRoutine());
        }

        private void HideIceTip()
        {
            if (iceTipRoutine != null) StopCoroutine(iceTipRoutine);
            iceTipRoutine = null;
            if (iceTip != null)
            {
                iceTip.gameObject.SetActive(false);
            }
        }

        private IEnumerator IceTipRoutine()
        {
            iceTip.gameObject.SetActive(true);
            iceTip.alpha = 1f;
            yield return new WaitForSeconds(ICE_TIP_HOLD);
            for (float t = 0f; t < ICE_TIP_FADE; t += Time.deltaTime)
            {
                iceTip.alpha = 1f - t / ICE_TIP_FADE;
                yield return null;
            }

            HideIceTip();
        }

        // A short message in the centre of the gameplay screen.
        public void ShowToast(string message)
        {
            toastRoot.GetComponentInChildren<TMP_Text>(true).text = message;
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToastRoutine());
        }

        private IEnumerator ToastRoutine()
        {
            toastRoot.SetActive(true);
            yield return new WaitForSeconds(TOAST_DURATION);
            toastRoot.SetActive(false);
            toastRoutine = null;
        }
    }
}
