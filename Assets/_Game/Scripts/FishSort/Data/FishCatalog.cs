using UnityEngine;

namespace FishSort
{
    // Fish type id = index in this array. Swap placeholder sprites for real art here.
    [CreateAssetMenu(fileName = "FishCatalog", menuName = "FishSort/Fish Catalog")]
    public sealed class FishCatalog : ScriptableObject
    {
        [SerializeField] private Sprite[] fishSprites;

        public int Count => fishSprites.Length;

        public Sprite GetSprite(int typeId)
        {
            return fishSprites[typeId];
        }
    }
}
