using UnityEngine;
using UnityEngine.UI;

namespace FishSort
{
    // VICTORY! screen: confetti, Next and Home.
    public sealed class UIWin : UICanvas
    {
        [SerializeField] private Button nextButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private ParticleSystem confetti;

        private void Awake()
        {
            nextButton.onClick.AddListener(OnNextClicked);
            homeButton.onClick.AddListener(OnHomeClicked);
        }

        public override void Open()
        {
            base.Open();
            SoundManager.Ins.PlayFx(FxID.Win);
            confetti.Play();
        }

        private void OnNextClicked()
        {
            SoundManager.Ins.PlayFx(FxID.Button);
            LevelManager.Ins.OnNextLevel();
        }

        private void OnHomeClicked()
        {
            SoundManager.Ins.PlayFx(FxID.Button);
            LevelManager.Ins.OnHome();
        }
    }
}
