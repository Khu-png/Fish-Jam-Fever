using UnityEngine;

namespace FishJam
{
    /// <summary>
    /// May trang thai cua game + level hien tai. Chi GameManager duoc doi trang thai:
    ///   GameManager.Instance.ChangeState(PlayingState.Instance);  ReplayLevel();
    /// Chua co Loading nen Start() vao thang MainMenuState.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        public const string LevelKey = "current_level";

        IGameState currentState;

        /// <summary>Level hien tai (bat dau tu 1, luu PlayerPrefs).</summary>
        public int CurrentLevel
        {
            get => PlayerPrefs.GetInt(LevelKey, 1);
            set { PlayerPrefs.SetInt(LevelKey, Mathf.Max(1, value)); PlayerPrefs.Save(); }
        }

        public IGameState GetCurrentState() => currentState;

        void Start()
        {
            Application.targetFrameRate = 60;
            ChangeState(MainMenuState.Instance);    // sau nay: LoadingState
        }

        void Update() { currentState?.OnExecute(); }

        /// <summary>OnExit cu -> gan -> OnEnter moi. Bo qua null hoac cung loai.</summary>
        public void ChangeState(IGameState newState)
        {
            if (newState == null) return;
            if (currentState != null && currentState.GetType() == newState.GetType()) return;
            var prev = currentState;
            currentState?.OnExit(newState);
            currentState = newState;
            currentState.OnEnter(prev);
            Debug.Log("[GameManager] " + (prev != null ? prev.GetType().Name : "None") + " -> " + newState.GetType().Name);
        }

        /// <summary>Choi lai level. ChangeState bo qua Playing -> Playing nen tu goi Exit roi Enter.</summary>
        public void ReplayLevel()
        {
            if (currentState is PlayingState) 
            { 
                currentState.OnExit(currentState); 
                currentState.OnEnter(currentState); 
            }
            else ChangeState(PlayingState.Instance);
            Debug.Log("[GameManager] Replay level " + CurrentLevel);
        }
    }
}
