using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FishJam.UI
{
    /// <summary>Man Main Menu (Resources/UI/Canvas_MainMenu): nut Level (Play) va nut Cai dat.</summary>
    public class MainMenu : SkinCanvas
    {
        public Button playButton;
        public TMP_Text levelLabel;
        public Button settingsButton;

        protected override void Awake()
        {
            base.Awake();
            if (playButton) playButton.onClick.AddListener(PlayButton);
            if (settingsButton) settingsButton.onClick.AddListener(SettingButton);
        }

        public override void Setup()
        {
            base.Setup();
            if (levelLabel) levelLabel.text = "Level " + FishSort.PlayerData.Level;
        }

        /// <summary>Nut Level: vao choi.</summary>
        public void PlayButton()
        {
            SoundManager.Ins.PlayFx(FxID.Button);
            LevelManager.Ins.OnPlay();
        }

        /// <summary>Nut banh rang: mo popup Cai dat (che do Menu).</summary>
        public void SettingButton() { UIManager.Ins.OpenUI<Setting>(); }
    }
}
