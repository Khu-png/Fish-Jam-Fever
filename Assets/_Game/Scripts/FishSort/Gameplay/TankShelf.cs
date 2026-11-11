using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishSort
{
    // Owns the open tank goals. Replacement goals open after their tanks finish entering.
    public sealed class TankShelf : MonoBehaviour
    {
        private const int BURST_ICON_INDEX = 1;

        [SerializeField] private TankSlotView[] slots;
        [SerializeField] private FishSortConfig config;
        [SerializeField] private FishCatalog catalog;

        private TankGoal[] currentGoals;
        private Queue<TankGoal>[] completed;
        private bool[] isSlotBusy;
        private int[] sequence;
        private int nextSequenceIndex;

        // Raised when a replacement tank is ready to receive fish from the tray.
        public event Action<TankGoal> GoalCreated;
        // Raised every time a fish is placed in a tank.
        public event Action FishPlaced;

        public bool IsIdle
        {
            get
            {
                bool idle = true;
                for (int i = 0; i < isSlotBusy.Length && idle; i++)
                {
                    idle = !isSlotBusy[i];
                }

                return idle;
            }
        }

        private void Awake()
        {
            currentGoals = new TankGoal[slots.Length];
            completed = new Queue<TankGoal>[slots.Length];
            isSlotBusy = new bool[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                completed[i] = new Queue<TankGoal>();
            }
        }

        public void Setup(int[] tankSequence)
        {
            ResetAll();
            sequence = tankSequence;
            nextSequenceIndex = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                TankGoal goal = TakeNextGoal(i);
                if (goal != null)
                {
                    goal.IsShown = true;
                }

                currentGoals[i] = goal;
                RefreshBadge(i, false);
            }
        }

        public void ResetAll()
        {
            StopAllCoroutines();
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].ResetView();
                completed[i].Clear();
                isSlotBusy[i] = false;
                currentGoals[i] = null;
            }
        }

        public int SlotCount => currentGoals.Length;

        // The goal currently open in a slot (null when empty); it may be full or still animating.
        public TankGoal GoalAt(int slot)
        {
            return currentGoals[slot];
        }

        public TankGoal FindGoal(int typeId)
        {
            TankGoal result = null;
            for (int i = 0; i < currentGoals.Length && result == null; i++)
            {
                TankGoal goal = currentGoals[i];
                if (goal != null && goal.IsShown && goal.TypeId == typeId && goal.Reserved < config.tankCapacity)
                {
                    result = goal;
                }
            }

            return result;
        }

        // Claims the next icon spot. A full goal stays visible until its completion animation ends.
        public int Reserve(TankGoal goal)
        {
            int iconIndex = goal.Reserved;
            goal.Reserved++;
            if (goal.Reserved >= config.tankCapacity)
            {
                Complete(goal);
            }

            return iconIndex;
        }

        public Vector3 GetIconPosition(TankGoal goal, int iconIndex)
        {
            return slots[goal.SlotIndex].GetIconPosition(iconIndex);
        }

        public Vector3 GetWaterPosition(TankGoal goal, int iconIndex)
        {
            return slots[goal.SlotIndex].GetWaterPosition(iconIndex);
        }

        public void Splash(TankGoal goal, int iconIndex)
        {
            if (goal.IsShown) slots[goal.SlotIndex].Splash(iconIndex);
        }

        // Fish arrived at its spot. If the tank is still animating in, it waits there.
        public void Land(TankGoal goal, Fish fish, int iconIndex)
        {
            if (goal.IsShown)
            {
                Place(goal, fish, iconIndex);
            }
            else
            {
                goal.Pending.Add(new TankGoal.PendingFish { fish = fish, iconIndex = iconIndex });
            }
        }

        private void Place(TankGoal goal, Fish fish, int iconIndex)
        {
            slots[goal.SlotIndex].PlaceFish(fish, iconIndex);
            goal.Landed++;
            if (currentGoals[goal.SlotIndex] == goal)
            {
                RefreshBadge(goal.SlotIndex, true);
            }

            ParticlePool.Play(ParticleType.FishLand, fish.TF.position, Quaternion.identity);
            FishPlaced?.Invoke();
        }

        private void Complete(TankGoal goal)
        {
            int slot = goal.SlotIndex;
            TankGoal next = TakeNextGoal(slot);
            goal.Next = next;
            completed[slot].Enqueue(goal);
            if (!isSlotBusy[slot])
            {
                StartCoroutine(RunSlot(slot));
            }
        }

        private IEnumerator RunSlot(int slot)
        {
            isSlotBusy[slot] = true;
            while (completed[slot].Count > 0)
            {
                TankGoal goal = completed[slot].Peek();
                while (goal.Landed < config.tankCapacity)
                {
                    yield return null;
                }

                SoundManager.Ins.PlayFx(FxID.TankComplete);
                ParticlePool.Play(ParticleType.TankBurst, slots[slot].GetIconPosition(BURST_ICON_INDEX), Quaternion.identity);
                yield return slots[slot].PlayComplete();
                goal.IsShown = false;
                currentGoals[slot] = goal.Next;
                RefreshBadge(slot, false);
                if (goal.Next != null)
                {
                    yield return slots[slot].PlayEnter();
                    ShowGoal(goal.Next);
                }

                completed[slot].Dequeue();
            }

            isSlotBusy[slot] = false;
        }

        private void ShowGoal(TankGoal goal)
        {
            goal.IsShown = true;
            for (int i = 0; i < goal.Pending.Count; i++)
            {
                Place(goal, goal.Pending[i].fish, goal.Pending[i].iconIndex);
            }

            goal.Pending.Clear();
            GoalCreated?.Invoke(goal);
        }

        private TankGoal TakeNextGoal(int slot)
        {
            TankGoal goal = null;
            if (nextSequenceIndex < sequence.Length)
            {
                goal = new TankGoal(sequence[nextSequenceIndex], slot);
                nextSequenceIndex++;
            }

            return goal;
        }

        private void RefreshBadge(int slot, bool punch)
        {
            TankGoal goal = currentGoals[slot];
            if (goal == null)
            {
                slots[slot].HideBadge();
            }
            else
            {
                slots[slot].ShowBadge(catalog.GetSprite(goal.TypeId), goal.Landed, config.tankCapacity, punch);
            }
        }
    }
}
