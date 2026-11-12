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
        [SerializeField] private RectTransform tutorialArea;
        [SerializeField] private RectTransform tutorialHand;
        [SerializeField] private Graphic tutorialPulse;
        [SerializeField] private CanvasGroup tutorialMessage;
        [SerializeField] private GameObject tipRoot;
        [SerializeField] private TextMeshProUGUI tipText;
        [SerializeField] private GameObject toastRoot;

        private const float TOAST_DURATION = 1.4f;
        private Coroutine toastRoutine;
        private Coroutine tipRoutine;
        private Transform tutorialTarget;
        private Camera tutorialCamera;
        private Vector3 tutorialWorldOffset;
        private float tutorialStarted;

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
            SoundManager.Ins.PlayFx(FxID.Button);
            GameplayController.Ins.TryUseRod();
        }

        private void OnCannonClicked()
        {
            SoundManager.Ins.PlayFx(FxID.Button);
            GameplayController.Ins.TryUseCannon();
        }

        private void OnOtherBoosterClicked()
        {
            SoundManager.Ins.PlayFx(FxID.Button);
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
