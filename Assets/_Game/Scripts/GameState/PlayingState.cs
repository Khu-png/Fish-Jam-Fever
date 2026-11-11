using FishJam.UI;

namespace FishJam
{
    /// <summary>Dang choi: mo HUD GamePlay. Gameplay (sinh man, kiem tra thang/thua) them vao cac cho danh dau.</summary>
    public class PlayingState : IGameState
    {
        public static PlayingState Instance { get; } = new PlayingState();

        public void OnEnter(IGameState previousState)
        {
            UIManager.Instance.CloseAll();
            UIManager.Instance.Open<GamePlay>();      // SetUp(): dat level, reset combo, tat canh bao
            // (gameplay) sinh man: LevelManager.Instance.LoadLevel(GameManager.Instance.CurrentLevel);
        }

        public void OnExecute()
        {
            // (gameplay) kiem tra thang/thua -> GameManager.Instance.ChangeState(WinState/LoseState) – dot sau
        }

        public void OnExit(IGameState nextState)
        {
            UIManager.Instance.CloseDirectly<GamePlay>();
        }
    }
}
