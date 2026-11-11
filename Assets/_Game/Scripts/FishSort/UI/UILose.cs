using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FishSort
{
    // YOU LOSE screen with the level progress, Retry and Home.
    public sealed class UILose : UICanvas
    {
        [SerializeField] private TextMeshProUGUI progressLabel;
        [SerializeField] private Image progressFill;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button homeButton;

        private void Awake()
        {
            retryButton.onClick.AddListener(OnRetryClicked);
            homeButton.onClick.AddListener(OnHomeClicked);
        }

        public override void Open()
        {
            base.Open();
            SoundManager.Ins.PlayFx(FxID.Lose);
        }

        public void SetProgress(float progress)
        {
            int percent = Mathf.RoundToInt(progress * FishSortConst.PERCENT);
            progressLabel.text = string.Format(FishSortConst.FORMAT_LOSE_PROGRESS, percent);
            progressFill.fillAmount = progress;
        }

        private void OnRetryClicked()
        {
            SoundManager.Ins.PlayFx(FxID.Button);
            LevelManager.Ins.OnRetry();
        }

        private void OnHomeClicked()
        {
            SoundManager.Ins.PlayFx(FxID.Button);
            LevelManager.Ins.OnHome();
        }
    }
}
