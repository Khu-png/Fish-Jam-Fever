using UnityEngine;

namespace FishSort
{
    // Fish type id = index used by the level data. Which species a type id looks like is shuffled
    // every time a level starts (Shuffle), so levels keep their structure but look different each run.
    [CreateAssetMenu(fileName = "FishCatalog", menuName = "FishSort/Fish Catalog")]
    public sealed class FishCatalog : ScriptableObject
    {
        [SerializeField] private Sprite[] fishSprites;

        // Runtime only: skin[typeId] = index into fishSprites. Null until the first Shuffle.
        [System.NonSerialized] private int[] skin;

        public int Count => fishSprites.Length;

        public Sprite GetSprite(int typeId)
        {
            int index = skin != null && typeId < skin.Length ? skin[typeId] : typeId;
            return fishSprites[index];
        }

        // Random one-to-one assignment of type ids to species (Fisher-Yates), so two types never share a look.
        public void Shuffle(System.Random rng)
        {
            skin = new int[fishSprites.Length];
            for (int i = 0; i < skin.Length; i++)
            {
                skin[i] = i;
            }

            for (int i = skin.Length - 1; i > 0; i--)
            {
                int swapWith = rng.Next(i + 1);
                int held = skin[i];
                skin[i] = skin[swapWith];
                skin[swapWith] = held;
            }
        }
    }
}
