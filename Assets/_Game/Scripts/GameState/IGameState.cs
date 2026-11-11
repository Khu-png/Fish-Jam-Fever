namespace FishJam
{
    /// <summary>Mot trang thai cua game (MainMenu, Playing; sau nay: Loading, Win, Lose).</summary>
    public interface IGameState
    {
        void OnEnter(IGameState previousState);   // 1 lan khi vao
        void OnExecute();                         // moi frame
        void OnExit(IGameState nextState);        // 1 lan khi roi
    }
}
