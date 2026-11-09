using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FishSort
{
    // Core rules: each pick goes to a matching tank or to the tray; full tray = lose, all fish placed = win.
    public sealed class GameplayController : Singleton<GameplayController>
    {
        [SerializeField] private FishSortConfig config;
        [SerializeField] private Camera gameCamera;
        [SerializeField] private BubbleField field;
        [SerializeField] private TankShelf shelf;
        [SerializeField] private WaitingTray tray;
        [SerializeField] private ComboCounter combo;
        [SerializeField] private Transform flightRoot;
        [SerializeField] private TextMeshPro progressText;

        private int totalFish;
        private int collectedFish;
        private int placedFish;
        private int levelNumber;
        private bool isOver = true;
        private bool isTrayWarningOn;
        private bool hasPickedOnce;

        public bool IsInputEnabled => !isOver && GameManager.IsState(GameState.GamePlay);
        public float Progress => totalFish > 0 ? (float)collectedFish / totalFish : 0f;

        private void Awake()
        {
            shelf.GoalCreated += HandleGoalCreated;
            shelf.FishPlaced += HandleFishPlaced;
        }

        private void OnDestroy()
        {
            shelf.GoalCreated -= HandleGoalCreated;
            shelf.FishPlaced -= HandleFishPlaced;
        }

        public void StartLevel(LevelData level, int number)
        {
            StopLevel();
            levelNumber = number;
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
            SetTrayWarning(false);
            HBPool.CollectAll();
            field.Clear();
            tray.Clear();
            shelf.ResetAll();
            combo.ResetCombo();
        }

        public void PickFish(Fish fish)
        {
            if (isOver || !fish.IsSelectable)
            {
                return;
            }

            hasPickedOnce = true;
            int typeId = fish.TypeId;
            field.RemoveFish(fish);
            fish.Detach(flightRoot);
            field.CrackIce(typeId);

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
            Vector3 target = shelf.GetIconPosition(goal, iconIndex);
            fish.FlyTo(() => target, config.flyToTankDuration, config.tankIconWidth,
                arrived => shelf.Land(goal, arrived, iconIndex));
        }

        private void SendToTray(Fish fish)
        {
            tray.Add(fish);
            fish.FlyTo(() => tray.GetTargetFor(fish), config.flyToTrayDuration, config.trayFishWidth, tray.OnFishArrived);
            if (tray.IsFull)
            {
                StartCoroutine(LoseRoutine());
            }
            else
            {
                SetTrayWarning(tray.IsOneLeft);
            }
        }

        // New goal opened: matching fish in the tray fly up on their own (domino if it fills again).
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
            LevelManager.Ins.OnWin();
        }

        private IEnumerator LoseRoutine()
        {
            isOver = true;
            SetTrayWarning(false);
            yield return new WaitForSeconds(config.loseDelay);
            LevelManager.Ins.OnLose(Progress);
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
            progressText.text = string.Format(FishSortConst.FORMAT_PROGRESS, placedFish, totalFish);
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
