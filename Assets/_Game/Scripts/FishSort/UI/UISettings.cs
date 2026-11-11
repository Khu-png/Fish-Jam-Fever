using UnityEngine;
using UnityEngine.UI;

namespace FishSort
{
    // Settings popup: sound + two playtest toggles (probe offset, green/orange hint). Pauses input while open.
    public sealed class UISettings : UICanvas
    {
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle probeOffsetToggle;
        [SerializeField] private Toggle hintColorsToggle;
        [SerializeField] private GameObject inGameButtonsRoot;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button closeButton;

        private bool isInGame;

        private void Awake()
        {
            soundToggle.onValueChanged.AddListener(isOn => PlayerData.SoundOn = isOn);
            probeOffsetToggle.onValueChanged.AddListener(isOn => PlayerData.ProbeOffset = isOn);
            hintColorsToggle.onValueChanged.AddListener(isOn => PlayerData.HintColors = isOn);
            homeButton.onClick.AddListener(OnHomeClicked);
            restartButton.onClick.AddListener(OnRestartClicked);
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        public override void Open()
        {
            base.Open();
            soundToggle.SetIsOnWithoutNotify(PlayerData.SoundOn);
            probeOffsetToggle.SetIsOnWithoutNotify(PlayerData.ProbeOffset);
            hintColorsToggle.SetIsOnWithoutNotify(PlayerData.HintColors);
        }

        public void SetInGame(bool inGame)
        {
            isInGame = inGame;
            inGameButtonsRoot.SetActive(inGame);
            if (inGame)
            {
                GameManager.ChangeState(GameState.Setting);
            }
        }

        private void OnCloseClicked()
        {
            SoundManager.Ins.PlayFx(FxID.Button);
            if (isInGame)
            {
                GameManager.ChangeState(GameState.GamePlay);
            }

            CloseDirectly();
        }

        private void OnHomeClicked()
        {
            SoundManager.Ins.PlayFx(FxID.Button);
            CloseDirectly();
            LevelManager.Ins.OnHome();
        }

        private void OnRestartClicked()
        {
            SoundManager.Ins.PlayFx(FxID.Button);
            CloseDirectly();
            LevelManager.Ins.OnRetry();
        }
    }
}
