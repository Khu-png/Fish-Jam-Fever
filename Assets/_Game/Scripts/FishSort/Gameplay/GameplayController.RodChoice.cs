using System.Collections.Generic;
using UnityEngine;

namespace FishSort
{
    // Fishing-rod booster, step one: the screen goes dark except for the open tanks the rod can fish for, and the player
    // taps the one to fish for. With a single possible tank there is nothing to choose and the rod goes at once.
    public sealed partial class GameplayController
    {
        private const string NO_ROD_FISH_MESSAGE = "No fish to catch for the open tanks";

        private readonly List<TankGoal> rodGoals = new List<TankGoal>();
        private readonly List<Fish> noFish = new List<Fish>();
        private bool isChoosingRod;

        // Starts the rod. Returns false (with a message when there is something to tell the player) if it cannot start now.
        public bool BeginRodSelection(out string message)
        {
            message = string.Empty;
            bool canUse = !isOver && !isResolvingTray && !isBoosterBusy && GameManager.IsState(GameState.GamePlay);
            if (!canUse)
            {
                return false;
            }

            CollectRodGoals();
            if (rodGoals.Count == 0)
            {
                message = NO_ROD_FISH_MESSAGE;
                return false;
            }

            if (rodGoals.Count == 1)
            {
                StartRod(rodGoals[0]);
            }
            else
            {
                ShowTankChoice();
            }

            return true;
        }

        // Leaves the choice without using the rod (nothing is taken from the player's stock).
        private void CancelRodSelection()
        {
            if (isChoosingRod)
            {
                isChoosingRod = false;
                isBoosterBusy = false;
                if (UIManager.Ins.IsLoaded<FishJam.UI.GamePlay>())
                {
                    UIManager.Ins.GetUI<FishJam.UI.GamePlay>().TankChoice.Hide();
                }
            }
        }

        // Every open tank (in slot order) that still has a free, unfrozen matching fish somewhere in the field.
        private void CollectRodGoals()
        {
            rodGoals.Clear();
            for (int slot = 0; slot < shelf.SlotCount; slot++)
            {
                TankGoal goal = shelf.GoalAt(slot);
                if (IsRodTarget(goal))
                {
                    rodGoals.Add(goal);
                }
            }
        }

        private bool IsRodTarget(TankGoal goal)
        {
            noFish.Clear();
            return goal != null && goal.IsShown && goal.Reserved < config.tankCapacity
                && field.FindLowestFish(goal.TypeId, noFish) != null;
        }

        private void ShowTankChoice()
        {
            Rect[] rects = new Rect[rodGoals.Count];
            int[] slots = new int[rodGoals.Count];
            for (int i = 0; i < rodGoals.Count; i++)
            {
                slots[i] = rodGoals[i].SlotIndex;
                Bounds bounds = shelf.GetTankBounds(slots[i]);
                Vector3 low = gameCamera.WorldToScreenPoint(bounds.min);
                Vector3 high = gameCamera.WorldToScreenPoint(bounds.max);
                rects[i] = Rect.MinMaxRect(low.x, low.y, high.x, high.y);
            }

            // The choice blocks fish picking like any other power-up in action.
            isChoosingRod = true;
            isBoosterBusy = true;
            UIManager.Ins.GetUI<FishJam.UI.GamePlay>().TankChoice.Show(rects, slots, ChooseRodTank, CancelRodSelection);
        }

        private void ChooseRodTank(int slot)
        {
            TankGoal goal = shelf.GoalAt(slot);
            CancelRodSelection();
            if (IsRodTarget(goal))
            {
                StartRod(goal);
            }
        }

        // The rod is used for real now: one is taken from the stock and the hook goes down.
        private void StartRod(TankGoal goal)
        {
            PlayerData.UseBooster(BoosterKind.Rod);
            StartCoroutine(RodRoutine(goal));
        }
    }
}
