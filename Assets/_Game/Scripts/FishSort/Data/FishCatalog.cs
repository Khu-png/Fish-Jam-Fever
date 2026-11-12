using Spine.Unity;
using UnityEngine;

namespace FishSort
{
    // What a fish looks like: the still sprite (flying, selecting, ice) and its animated Spine rig (resting).
    public readonly struct FishLook
    {
        public readonly Sprite Sprite;
        public readonly SkeletonDataAsset Skeleton;
        public readonly bool SkeletonFacesRight;

        public FishLook(Sprite sprite, SkeletonDataAsset skeleton, bool skeletonFacesRight)
        {
            Sprite = sprite;
            Skeleton = skeleton;
            SkeletonFacesRight = skeletonFacesRight;
        }
    }

    // Fish type id = index used by the level data. Which species a type id looks like is shuffled
    // every time a level starts (Shuffle), so levels keep their structure but look different each run.
    [CreateAssetMenu(fileName = "FishCatalog", menuName = "FishSort/Fish Catalog")]
    public sealed class FishCatalog : ScriptableObject
    {
        [SerializeField] private Sprite[] fishSprites;
        [Tooltip("Spine rig of each sprite (same order). Filled by the setup tool; empty = the sprite is used alone.")]
        [SerializeField] private SkeletonDataAsset[] fishSkeletons;
        [Tooltip("True for a rig that is drawn facing right (the sprites face left).")]
        [SerializeField] private bool[] skeletonFacesRight;

        // Runtime only: skin[typeId] = index into fishSprites. Null until the first Shuffle.
        [System.NonSerialized] private int[] skin;

        public int Count => fishSprites.Length;
        public int RigCount => fishSkeletons != null ? fishSkeletons.Length : 0;

        // Parses the rig now (instead of the first time a fish uses it), used while the loading screen is up.
        public void PrewarmRig(int index)
        {
            if (fishSkeletons[index] != null)
            {
                fishSkeletons[index].GetSkeletonData(false);
            }
        }

        public Sprite GetSprite(int typeId)
        {
            int index = skin != null && typeId < skin.Length ? skin[typeId] : typeId;
            return fishSprites[index];
        }

        public FishLook GetLook(int typeId)
        {
            int index = skin != null && typeId < skin.Length ? skin[typeId] : typeId;
            bool hasRig = fishSkeletons != null && index < fishSkeletons.Length;
            bool facesRight = skeletonFacesRight != null && index < skeletonFacesRight.Length && skeletonFacesRight[index];
            return new FishLook(fishSprites[index], hasRig ? fishSkeletons[index] : null, facesRight);
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
