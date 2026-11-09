using System.Collections.Generic;

namespace FishSort
{
    // Data for one tank goal ("3 fish of type X"). Reserved counts fish already sent; Landed counts fish placed.
    public sealed class TankGoal
    {
        public struct PendingFish
        {
            public Fish fish;
            public int iconIndex;
        }

        public readonly int TypeId;
        public readonly int SlotIndex;
        public readonly List<PendingFish> Pending = new List<PendingFish>();

        public int Reserved;
        public int Landed;
        public bool IsShown;
        public TankGoal Next;

        public TankGoal(int typeId, int slotIndex)
        {
            TypeId = typeId;
            SlotIndex = slotIndex;
        }
    }
}
