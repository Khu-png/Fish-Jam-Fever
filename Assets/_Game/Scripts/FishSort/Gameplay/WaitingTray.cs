using System.Collections.Generic;
using UnityEngine;

namespace FishSort
{
    // The 5-slot waiting tray. Items stay packed to the left.
    public sealed class WaitingTray : MonoBehaviour
    {
        [SerializeField] private Transform[] slotPoints;
        [SerializeField] private WaterSurface[] waterSurfaces;
        [SerializeField] private FishSortConfig config;

        private readonly List<Fish> items = new List<Fish>();

        public int Count => items.Count;
        public bool IsFull => items.Count >= config.trayCapacity;
        public bool IsOneLeft => items.Count == config.trayCapacity - 1;

        public void Add(Fish fish)
        {
            items.Add(fish);
        }

        public void Clear()
        {
            items.Clear();
        }

        // Live target for a fish flying in: follows the fish's current index if earlier items leave.
        public Vector3 GetTargetFor(Fish fish)
        {
            int index = items.IndexOf(fish);
            if (index < 0)
            {
                index = items.Count;
            }

            return slotPoints[Mathf.Min(index, slotPoints.Length - 1)].position;
        }

        public void OnFishArrived(Fish fish)
        {
            fish.RestInTray();
            Relayout();
        }

        public void OnWaterEntry(Fish fish)
        {
            int index = items.IndexOf(fish);
            if (index >= 0) waterSurfaces[index].React(fish.TF.position);
            ParticlePool.Play(ParticleType.TrayWaterBubbles, fish.TF.position, Quaternion.identity);
        }

        // Removes up to maxCount fish of the type (in tray order) into output.
        public void TakeMatching(int typeId, int maxCount, List<Fish> output)
        {
            int i = 0;
            while (i < items.Count && output.Count < maxCount)
            {
                if (items[i].TypeId == typeId)
                {
                    output.Add(items[i]);
                    items.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            Relayout();
        }

        // The fish type with most fish in the tray (-1 when it is empty): the best type for a revive tank to take.
        public int MostCommonType()
        {
            int best = -1;
            int bestCount = 0;
            for (int i = 0; i < items.Count; i++)
            {
                int count = 0;
                for (int j = 0; j < items.Count; j++)
                {
                    if (items[j].TypeId == items[i].TypeId)
                    {
                        count++;
                    }
                }

                if (count > bestCount)
                {
                    best = items[i].TypeId;
                    bestCount = count;
                }
            }

            return best;
        }

        public int SlotCount => slotPoints.Length;

        public Vector3 GetSlotPosition(int slot)
        {
            return slotPoints[slot].position;
        }

        // Empties the tray: every fish goes to output (they stay where they are for the caller to animate).
        public void TakeAll(List<Fish> output)
        {
            output.AddRange(items);
            items.Clear();
        }

        private void Relayout()
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].SlideTo(slotPoints[i].position, config.traySlideDuration);
            }
        }
    }
}
