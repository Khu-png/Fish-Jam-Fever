using UnityEngine;
using UnityEngine.UI;

namespace FishJam.UI
{
    /// <summary>Man Thua (Resources/UI/Canvas_Lose, sortingOrder 20): YOU LOSE + ca khoc (Spine), nut Retry va Home.</summary>
    public class Lose : SkinCanvas
    {
        public Button retryButton, homeButton;
        bool isClicked;

        protected override void Awake()
        {
            base.Awake();
            retryButton.onClick.AddListener(RetryButton);
            homeButton.onClick.AddListener(HomeButton);
        }

        public override void Setup() { base.Setup(); isClicked = false; Time.timeScale = 0f; SoundManager.Ins.PlayFx(FxID.Whoosh); }

        public void RetryButton()
        {
            if (isClicked) return; isClicked = true;
            SoundManager.Ins.PlayFx(FxID.Button);
            CloseDirectly();
            LevelManager.Ins.OnRetry();
        }

        public void HomeButton()
        {
            if (isClicked) return; isClicked = true;
            SoundManager.Ins.PlayFx(FxID.Button);
            CloseDirectly();
            LevelManager.Ins.OnHome();
        }

        public override void CloseDirectly() { Time.timeScale = 1f; base.CloseDirectly(); }
    }
}
