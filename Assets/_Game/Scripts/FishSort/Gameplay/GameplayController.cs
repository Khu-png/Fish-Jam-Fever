using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishSort
{
    // Core rules: each pick goes to a matching tank or to the tray; full tray = lose, all fish placed = win.
    public sealed partial class GameplayController : Singleton<GameplayController>
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

        public bool IsInputEnabled => !isOver && !isResolvingTray && !isBoosterBusy && GameManager.IsState(GameState.GamePlay);
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
            rod = world.Rod;
            cannon = world.Cannon;
            fishJar = world.Jar;
            shelf.GoalCreated += HandleGoalCreated;
            shelf.FishPlaced += HandleFishPlaced;
            shelf.TankCompleted += HandleTankCompleted;
            FishJam.UI.Revive.Revived += HandleRevived;
        }

        protected override void OnDestroy()
        {
            Unbind();
            base.OnDestroy();
        }

        private void Unbind()
        {
            FishJam.UI.Revive.Revived -= HandleRevived;
            // shelf is null until the first Bind(); that is the unbound state, not a missing reference.
            if (shelf != null)
            {
                shelf.GoalCreated -= HandleGoalCreated;
                shelf.FishPlaced -= HandleFishPlaced;
                shelf.TankCompleted -= HandleTankCompleted;
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
            GameManager.Ins.ReviveCount = 0;
            UpdateProgress();
            if (number == FishSortConst.TUTORIAL_LEVEL)
            {
                StartCoroutine(TutorialRoutine());
            }
        }

        public void StopLevel()
        {
            StopAllCoroutines();
            if (UIManager.Ins.IsOpened<FishJam.UI.GamePlay>())
            {
                FishJam.UI.GamePlay ui = UIManager.Ins.GetUI<FishJam.UI.GamePlay>();
                ui.HideTutorialHand();
                ui.HideTip();
            }
            isOver = true;
            isResolvingTray = false;
            StopRod();
            StopCannon();
            SetTrayWarning(false);
            HBPool.CollectAll();
            field.Clear();
            tray.Clear();
            shelf.ResetAll();
            combo.ResetCombo();
        }

        public void PickFish(Fish fish)
        {
            if (isOver || isResolvingTray || isBoosterBusy || !fish.IsSelectable)
            {
                return;
            }

            hasPickedOnce = true;
            ParticlePool.Play(ParticleType.FishPick, fish.TF.position, Quaternion.identity);
            int typeId = fish.TypeId;
            TakeFishOut(fish);
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
                // A wrong pick (the fish goes to the tray) starts the tank combo again.
                UIManager.Ins.GetUI<FishJam.UI.GamePlay>().ResetCombo();
                SoundManager.Ins.PlayFx(FxID.PickTray);
                SendToTray(fish);
            }
        }

        private void SendToTank(Fish fish, TankGoal goal)
        {
            FlyToTank(fish, goal, shelf.Reserve(goal));
        }

        // The icon spot must already be reserved (a goal can fill up while a fish is still on its way).
        private void FlyToTank(Fish fish, TankGoal goal, int iconIndex)
        {
            collectedFish++;
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
            // The jar has priority over the tray: its fish fill the new tank first, the tray gets what is left.
            PullJarFish(goal);
            List<Fish> spilled = new List<Fish>(config.tankCapacity);
            tray.TakeMatching(goal.TypeId, config.tankCapacity - goal.Reserved, spilled);
            for (int i = 0; i < spilled.Count; i++)
            {
                SendToTank(spilled[i], goal);
            }

            SetTrayWarning(!isOver && tray.IsOneLeft);
        }

        // A tank is full: the HUD counts tanks completed in a row and shows the praise (text and voice).
        private void HandleTankCompleted()
        {
            UIManager.Ins.GetUI<FishJam.UI.GamePlay>().OnTankCompleted();
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
            // A revive needs both a locked tank left to open (at most 2 revives) and a free cannon (it cannot fire
            // while the jar of an earlier shot is still open). Otherwise the player could not recover: lose.
            if (shelf.CanUnlock && !fishJar.IsOpen)
            {
                GameManager.Ins.OnAlmostLose(Progress);
            }
            else
            {
                GameManager.Ins.OnLose(Progress);
            }
        }

        // Revive accepted: a locked tank opens (of the type most common in the tray) and the cannon clears the full
        // tray for free.
        private void HandleRevived()
        {
            GameManager.Ins.ReviveCount++;
            UIManager.Ins.GetUI<FishJam.UI.GamePlay>().ResetCombo();
            GameManager.ChangeState(GameState.GamePlay);
            isOver = false;
            bool opened = shelf.UnlockSlot(tray.MostCommonType());
            bool fired = TryUseCannon();
            if (!opened || !fired)
            {
                isOver = true;
                GameManager.Ins.OnLose(Progress);
            }
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

            if (UIManager.Ins.IsOpened<FishJam.UI.GamePlay>())
            {
                UIManager.Ins.GetUI<FishJam.UI.GamePlay>().SetWarning(isOn);
            }
        }

        private void UpdateProgress()
        {
            UIManager.Ins.GetUI<FishJam.UI.GamePlay>().SetFishCount(placedFish, totalFish);
        }

        private IEnumerator TutorialRoutine()
        {
            FishJam.UI.GamePlay ui = UIManager.Ins.GetUI<FishJam.UI.GamePlay>();
            Fish first = null;
            bool shown = false;
            while (!hasPickedOnce)
            {
                // Opening bubbles fall from above the field; wait for a visible, tappable fish.
                if (first == null || !first.IsSelectable || first.TF.position.y > field.FieldTopY - .35f)
                    first = field.FirstSelectableFish();
                if (first != null)
                {
                    Vector3 tapOffset = PlayerData.ProbeOffset ? Vector3.down * config.probeOffset : Vector3.zero;
                    ui.ShowTutorialHand(first.TF, gameCamera, tapOffset);
                    if (!shown) { ui.ShowTip(FishSortConst.TIP_TAP_FISH); shown = true; }
                }
                else ui.HideTutorialHand();
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
