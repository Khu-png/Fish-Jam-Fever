using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FishSort
{
    // Main menu: big "Level N" button + settings.
    public sealed class UIMainMenu : UICanvas
    {
        [SerializeField] private Button playButton;
        [SerializeField] private TextMeshProUGUI playLabel;
        [SerializeField] private Button settingsButton;

        private void Awake()
        {
            playButton.onClick.AddListener(OnPlayClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        public override void Open()
        {
            base.Open();
            playLabel.text = string.Format(FishSortConst.FORMAT_LEVEL, PlayerData.Level);
        }

        private void OnPlayClicked()
        {
            SoundManager.Ins.PlayFx(FxID.Button);
            LevelManager.Ins.OnPlay();
        }

        private void OnSettingsClicked()
        {
            SoundManager.Ins.PlayFx(FxID.Button);
            UIManager.Ins.OpenUI<UISettings>().SetInGame(false);
        }
    }
}
