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
        // Slots open at the start of a level; the rest stay locked until a revive opens them, one at a time.
        [SerializeField] private int startSlots = 2;
        // The grey tank with a plus drawn over each locked slot: the first entry covers slot startSlots.
        [SerializeField] private GameObject[] lockedViews;

        private TankGoal[] currentGoals;
        private Queue<TankGoal>[] completed;
        private bool[] isSlotBusy;
        private int[] sequence;
        private int nextSequenceIndex;
        private int openSlots;

        // Raised when a replacement tank is ready to receive fish from the tray.
        public event Action<TankGoal> GoalCreated;
        // Raised every time a fish is placed in a tank.
        public event Action FishPlaced;
        public event Action TankCompleted;

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
            // A copy: a revive may pull a tank forward, and the level asset must not change.
            sequence = (int[])tankSequence.Clone();
            nextSequenceIndex = 0;
            for (int i = 0; i < openSlots; i++)
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
            openSlots = Mathf.Min(startSlots, slots.Length);
            for (int i = 0; i < slots.Length; i++)
            {
                // A slot hidden by the last level must be awake to reset.
                slots[i].gameObject.SetActive(true);
                slots[i].ResetView();
                completed[i].Clear();
                isSlotBusy[i] = false;
                currentGoals[i] = null;
                ApplyLock(i);
            }
        }

        // True while a locked slot is left and the sequence still has a tank for it.
        public bool CanUnlock => openSlots < slots.Length && sequence != null && nextSequenceIndex < sequence.Length;

        // Revive: opens the next locked slot with the next tank of the sequence. preferredType (-1 for none) pulls
        // the first remaining tank of that type forward, so the new tank can take fish that are stuck in the tray.
        public bool UnlockSlot(int preferredType)
        {
            if (!CanUnlock)
            {
                return false;
            }

            PullForward(preferredType);
            int slot = openSlots;
            openSlots++;
            ApplyLock(slot);
            slots[slot].ResetView();
            TankGoal goal = TakeNextGoal(slot);
            currentGoals[slot] = goal;
            RefreshBadge(slot, false);
            StartCoroutine(OpenSlot(slot, goal));
            return true;
        }

        private IEnumerator OpenSlot(int slot, TankGoal goal)
        {
            isSlotBusy[slot] = true;
            yield return slots[slot].PlayEnter();
            ShowGoal(goal);
            isSlotBusy[slot] = false;
        }

        // Moves the first remaining tank of the type to the front of what is left. The tank counts do not change.
        private void PullForward(int typeId)
        {
            if (typeId < 0)
            {
                return;
            }

            for (int i = nextSequenceIndex; i < sequence.Length; i++)
            {
                if (sequence[i] == typeId)
                {
                    sequence[i] = sequence[nextSequenceIndex];
                    sequence[nextSequenceIndex] = typeId;
                    return;
                }
            }
        }

        // Open slots show their tank; locked ones show the grey cover instead.
        private void ApplyLock(int slot)
        {
            bool isOpen = slot < openSlots;
            slots[slot].gameObject.SetActive(isOpen);
            int cover = slot - startSlots;
            if (cover >= 0 && cover < lockedViews.Length && lockedViews[cover] != null)
            {
                lockedViews[cover].SetActive(!isOpen);
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
                TankCompleted?.Invoke();
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
