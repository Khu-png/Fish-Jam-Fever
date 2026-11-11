using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishSort
{
    // Core rules: each pick goes to a matching tank or to the tray; full tray = lose, all fish placed = win.
    public sealed class GameplayController : Singleton<GameplayController>
    {
        [SerializeField] private FishSortConfig config;
        [SerializeField] private Camera gameCamera;

        // Filled by Bind() once the GameplayWorld prefab has been spawned.
        private Transform flightRoot;
        private BubbleField field;
        private TankShelf shelf;
        private WaitingTray tray;
        private ComboCounter combo;

        private int totalFish;
        private int collectedFish;
        private int placedFish;
        private int levelNumber;
        private bool isOver = true;
        private bool isTrayWarningOn;
        private bool hasPickedOnce;
        private bool isResolvingTray;

        public bool IsInputEnabled => !isOver && !isResolvingTray && GameManager.IsState(GameState.GamePlay);
        public float Progress => totalFish > 0 ? (float)collectedFish / totalFish : 0f;

        void Awake()
        {
            RegisterSingleton(this);
        }

        public void Bind(GameplayWorld world)
        {
            Unbind();
            world.Initialize(gameCamera);
            // Fish in flight are parented to the world so they hide together with it.
            flightRoot = world.transform;
            field = world.Field;
            shelf = world.Shelf;
            tray = world.Tray;
            combo = world.Combo;
            shelf.GoalCreated += HandleGoalCreated;
            shelf.FishPlaced += HandleFishPlaced;
        }

        protected override void OnDestroy()
        {
            Unbind();
            base.OnDestroy();
        }

        private void Unbind()
        {
            // shelf is null until the first Bind(); that is the unbound state, not a missing reference.
            if (shelf != null)
            {
                shelf.GoalCreated -= HandleGoalCreated;
                shelf.FishPlaced -= HandleFishPlaced;
            }
        }

        public void StartLevel(LevelData level, int number)
        {
            StopLevel();
            levelNumber = number;
            field.ShuffleSpecies();
            shelf.Setup(level.TankSequence);
            field.Build(level, number);
            totalFish = field.TotalFish;
            collectedFish = 0;
            placedFish = 0;
            hasPickedOnce = false;
            isOver = false;
            UpdateProgress();
            if (number == FishSortConst.TUTORIAL_LEVEL)
            {
                StartCoroutine(TutorialRoutine());
            }
        }

        public void StopLevel()
        {
            StopAllCoroutines();
            isOver = true;
            isResolvingTray = false;
            SetTrayWarning(false);
            HBPool.CollectAll();
            field.Clear();
            tray.Clear();
            shelf.ResetAll();
            combo.ResetCombo();
        }

        public void PickFish(Fish fish)
        {
            if (isOver || isResolvingTray || !fish.IsSelectable)
            {
                return;
            }

            hasPickedOnce = true;
            ParticlePool.Play(ParticleType.FishPick, fish.TF.position, Quaternion.identity);
            int typeId = fish.TypeId;
            field.RemoveFish(fish);
            fish.Detach(flightRoot);

            TankGoal goal = shelf.FindGoal(typeId);
            if (goal != null)
            {
                combo.RegisterCorrect();
                SoundManager.Ins.PlayFx(FxID.PickCorrect, config.PickPitch(combo.Streak));
                SendToTank(fish, goal);
            }
            else
            {
                combo.Break();
                SoundManager.Ins.PlayFx(FxID.PickTray);
                SendToTray(fish);
            }
        }

        private void SendToTank(Fish fish, TankGoal goal)
        {
            collectedFish++;
            int iconIndex = shelf.Reserve(goal);
            fish.FlyIntoTank(() => shelf.GetIconPosition(goal, iconIndex),
                () => shelf.GetWaterPosition(goal, iconIndex), config.flyToTankDuration,
                fish.ScaleToFit(config.tankIconWidth, config.tankFishHeight),
                () => shelf.Splash(goal, iconIndex), arrived => shelf.Land(goal, arrived, iconIndex));
        }

        private void SendToTray(Fish fish)
        {
            tray.Add(fish);
            fish.FlyIntoTray(() => tray.GetTargetFor(fish), config.flyToTrayDuration,
                fish.ScaleToFit(config.trayFishWidth, config.trayFishHeight),
                () => tray.OnWaterEntry(fish), tray.OnFishArrived);
            if (tray.IsFull)
            {
                StartCoroutine(LoseRoutine());
            }
            else
            {
                SetTrayWarning(tray.IsOneLeft);
            }
        }

        // Replacement tank finished entering: matching tray fish can now fly up.
        private void HandleGoalCreated(TankGoal goal)
        {
            List<Fish> spilled = new List<Fish>(config.tankCapacity);
            tray.TakeMatching(goal.TypeId, config.tankCapacity - goal.Reserved, spilled);
            for (int i = 0; i < spilled.Count; i++)
            {
                SendToTank(spilled[i], goal);
            }

            SetTrayWarning(!isOver && tray.IsOneLeft);
        }

        private void HandleFishPlaced()
        {
            placedFish++;
            UpdateProgress();
            if (placedFish >= totalFish && !isOver)
            {
                StartCoroutine(WinRoutine());
            }
        }

        private IEnumerator WinRoutine()
        {
            isOver = true;
            SetTrayWarning(false);
            while (!shelf.IsIdle)
            {
                yield return null;
            }

            yield return new WaitForSeconds(config.winDelay);
            GameManager.Ins.OnWin();
        }

        private IEnumerator LoseRoutine()
        {
            isResolvingTray = true;
            SetTrayWarning(false);
            yield return new WaitForSeconds(config.loseDelay);
            // A tank that is being replaced may still free space in the full tray.
            while (tray.IsFull && !shelf.IsIdle)
            {
                yield return null;
            }

            isResolvingTray = false;
            if (!tray.IsFull)
            {
                SetTrayWarning(!isOver && tray.IsOneLeft);
                yield break;
            }

            isOver = true;
            GameManager.Ins.OnLose(Progress);
        }

        private void SetTrayWarning(bool isOn)
        {
            if (isOn == isTrayWarningOn)
            {
                return;
            }

            isTrayWarningOn = isOn;
            if (isOn)
            {
                SoundManager.Ins.PlayFx(FxID.Warning);
            }

            if (UIManager.Ins.IsOpened<UIGameplay>())
            {
                UIManager.Ins.GetUI<UIGameplay>().SetWarning(isOn);
            }
        }

        private void UpdateProgress()
        {
            UIManager.Ins.GetUI<UIGameplay>().SetFishProgress(placedFish, totalFish);
        }

        private IEnumerator TutorialRoutine()
        {
            UIGameplay ui = UIManager.Ins.GetUI<UIGameplay>();
            Fish first = field.FirstSelectableFish();
            if (first != null)
            {
                ui.ShowTutorialHand(gameCamera.WorldToScreenPoint(first.TF.position), gameCamera);
            }

            ui.ShowTip(FishSortConst.TIP_TAP_FISH);
            while (!hasPickedOnce)
            {
                yield return null;
            }

            ui.HideTutorialHand();
            ui.ShowTip(FishSortConst.TIP_TARGET_TANK);
            yield return new WaitForSeconds(config.tipStepDelay);
            ui.ShowTip(FishSortConst.TIP_GOAL);
            yield return new WaitForSeconds(config.tipStepDelay);
            ui.ShowTip(FishSortConst.TIP_COMPLETE);
            yield return new WaitForSeconds(config.tipStepDelay);
            ui.HideTip();
        }
    }
}
