using UnityEngine;
using UnityEngine.UI;

namespace FishJam.UI
{
    /// <summary>Man Thua (Resources/UI/Canvas_Lose, sortingOrder 20): YOU LOSE + ca khoc (Spine), nut Retry va Home.</summary>
    public class Lose : UICanvas
    {
        public Button retryButton, homeButton;
        bool isClicked;

        protected override void Awake()
        {
            base.Awake();
            retryButton.onClick.AddListener(RetryButton);
            homeButton.onClick.AddListener(HomeButton);
        }

        public override void SetUp() { base.SetUp(); isClicked = false; Time.timeScale = 0f; }

        public void RetryButton()
        {
            if (isClicked) return; isClicked = true;
            CloseDirectly();
            GameManager.Instance.ReplayLevel();
        }

        public void HomeButton()
        {
            if (isClicked) return; isClicked = true;
            CloseDirectly();
            GameManager.Instance.ChangeState(MainMenuState.Instance);
        }

        public override void CloseDirectly() { Time.timeScale = 1f; base.CloseDirectly(); }
    }
}
