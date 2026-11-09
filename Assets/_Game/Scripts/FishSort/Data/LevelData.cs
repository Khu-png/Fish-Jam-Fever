using System.Collections.Generic;
using UnityEngine;

namespace FishSort
{
    [System.Serializable]
    public struct BubbleData
    {
        // Column used only for the initial stacking; physics takes over after spawn.
        public int column;
        public int[] fishTypes;
        // 0 = normal bubble, >0 = frozen, taps needed to melt.
        public int iceTaps;
    }

    [CreateAssetMenu(fileName = "Level_00", menuName = "FishSort/Level Data")]
    public sealed class LevelData : ScriptableObject
    {
        [SerializeField] private int columns = 4;
        [SerializeField] private BubbleData[] bubbles;
        [SerializeField] private int[] tankSequence;

        public int Columns => columns;
        public BubbleData[] Bubbles => bubbles;
        public int[] TankSequence => tankSequence;

        public int TotalFish
        {
            get
            {
                int total = 0;
                for (int i = 0; i < bubbles.Length; i++)
                {
                    total += bubbles[i].fishTypes.Length;
                }

                return total;
            }
        }

        public void Initialize(int columnCount, BubbleData[] bubbleList, int[] sequence)
        {
            columns = columnCount;
            bubbles = bubbleList;
            tankSequence = sequence;
        }

        // Rule from design doc 4.5: every type count is a multiple of 3 and tanks = fish / 3.
        public bool Validate(int tankCapacity, out string error)
        {
            error = string.Empty;
            Dictionary<int, int> fishPerType = new Dictionary<int, int>();
            List<int> typeIds = new List<int>();
            for (int i = 0; i < bubbles.Length; i++)
            {
                int count = bubbles[i].fishTypes.Length;
                if (count < 1 || count > FishSortConst.MAX_FISH_PER_BUBBLE)
                {
                    error = $"Bubble {i} has {count} fish (allowed 1-{FishSortConst.MAX_FISH_PER_BUBBLE}).";
                    return false;
                }

                for (int k = 0; k < count; k++)
                {
                    int type = bubbles[i].fishTypes[k];
                    if (!fishPerType.TryGetValue(type, out int current))
                    {
                        typeIds.Add(type);
                    }

                    fishPerType[type] = current + 1;
                }
            }

            Dictionary<int, int> tanksPerType = new Dictionary<int, int>();
            for (int i = 0; i < tankSequence.Length; i++)
            {
                tanksPerType.TryGetValue(tankSequence[i], out int current);
                tanksPerType[tankSequence[i]] = current + 1;
            }

            for (int i = 0; i < typeIds.Count; i++)
            {
                int type = typeIds[i];
                int fishCount = fishPerType[type];
                tanksPerType.TryGetValue(type, out int tanks);
                if (fishCount % tankCapacity != 0 || tanks * tankCapacity != fishCount)
                {
                    error = $"Fish type {type}: {fishCount} fish but {tanks} tanks in the sequence.";
                    return false;
                }
            }

            if (tankSequence.Length * tankCapacity != TotalFish)
            {
                error = $"Tank sequence covers {tankSequence.Length * tankCapacity} fish, level has {TotalFish}.";
                return false;
            }

            return true;
        }

        [ContextMenu("Validate")]
        private void ValidateFromMenu()
        {
            const int DEFAULT_TANK_CAPACITY = 3;
            if (Validate(DEFAULT_TANK_CAPACITY, out string error))
            {
                Debug.Log($"{name}: OK ({TotalFish} fish, {tankSequence.Length} tanks).", this);
            }
            else
            {
                Debug.LogError($"{name}: {error}", this);
            }
        }
    }
}
