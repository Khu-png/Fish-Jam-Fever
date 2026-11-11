using FishJam.UI;

namespace FishJam
{
    /// <summary>Main Menu: dong moi UI, mo MainMenu.</summary>
    public class MainMenuState : IGameState
    {
        public static MainMenuState Instance { get; } = new MainMenuState();

        public void OnEnter(IGameState previousState)
        {
            // (gameplay) don man choi neu co
            UIManager.Instance.CloseAll();
            UIManager.Instance.Open<MainMenu>();
        }

        public void OnExecute() { }

        public void OnExit(IGameState nextState)
        {
            UIManager.Instance.CloseDirectly<MainMenu>();
        }
    }
}
