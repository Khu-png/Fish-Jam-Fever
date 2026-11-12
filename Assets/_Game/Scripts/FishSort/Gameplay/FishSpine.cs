using Spine.Unity;
using UnityEngine;

namespace FishSort
{
    // The animated look of a fish (Spine rig, "standby_1": tail, eyes). It is a child of the fish body, shown only while
    // the fish is resting; it copies sorting order, mask and flip from the still sprite, which stays the look for
    // flying, selecting and ice. Size and position are fitted to that sprite so swapping the two is not noticeable.
    public sealed class FishSpine : MonoBehaviour
    {
        private const string ANIMATION = "standby_1";
        private const int TRACK = 0;
        private const float MIN_SIZE = 0.0001f;
        private static readonly int FadeOnId = Shader.PropertyToID("_FadeOn");

        [SerializeField] private SkeletonAnimation skeleton;
        [SerializeField] private MeshRenderer meshRenderer;

        private MaterialPropertyBlock block;
        private Vector2 spriteCenter;
        private Vector2 rigCenter;
        private float fitScale;
        private bool rigFacesRight;
        private bool isFlipped;
        private bool hasFlip;
        private bool isFading;
        private bool hasFade;

        public bool IsReady { get; private set; }

        // Loads the rig for the fish and fits it to the sprite. Returns false when the fish has no rig.
        public bool Bind(SkeletonDataAsset asset, bool facesRight, Sprite reference, float phaseSeed)
        {
            IsReady = false;
            Show(false);
            if (asset == null || reference == null)
            {
                return false;
            }

            skeleton.skeletonDataAsset = asset;
            skeleton.Initialize(true);
            Spine.SkeletonData data = asset.GetSkeletonData(false);
            Spine.TrackEntry entry = skeleton.AnimationState.SetAnimation(TRACK, ANIMATION, true);
            entry.TrackTime = Mathf.Repeat(phaseSeed, Mathf.Max(MIN_SIZE, entry.Animation.Duration));
            skeleton.Update(0f);
            float width = Mathf.Max(MIN_SIZE, data.Width * asset.scale);
            float height = Mathf.Max(MIN_SIZE, data.Height * asset.scale);
            fitScale = (reference.bounds.size.x / width + reference.bounds.size.y / height) * 0.5f;
            spriteCenter = reference.bounds.center;
            rigCenter = new Vector2(data.X + data.Width * 0.5f, data.Y + data.Height * 0.5f) * asset.scale;
            rigFacesRight = facesRight;
            hasFlip = false;
            hasFade = false;
            IsReady = true;
            return true;
        }

        public void Show(bool isVisible)
        {
            skeleton.enabled = isVisible;
            meshRenderer.enabled = isVisible;
        }

        // Called every frame while shown. spriteFlipped = the still sprite is mirrored (the fish faces right).
        public void Sync(int sortingOrder, SpriteMaskInteraction mask, bool fadeAtTop, bool spriteFlipped, bool isPlaying)
        {
            meshRenderer.sortingOrder = sortingOrder;
            skeleton.maskInteraction = mask;
            skeleton.timeScale = isPlaying ? 1f : 0f;
            ApplyFade(fadeAtTop);
            ApplyFlip(spriteFlipped);
        }

        private void ApplyFlip(bool spriteFlipped)
        {
            if (hasFlip && spriteFlipped == isFlipped)
            {
                return;
            }

            hasFlip = true;
            isFlipped = spriteFlipped;
            // The sprite faces left; a rig drawn facing right has to be mirrored when the sprite is not.
            bool mirrorRig = spriteFlipped != rigFacesRight;
            float rigSign = mirrorRig ? -1f : 1f;
            float spriteSign = spriteFlipped ? -1f : 1f;
            skeleton.Skeleton.ScaleX = rigSign;
            Transform own = transform;
            own.localScale = Vector3.one * fitScale;
            own.localPosition = new Vector3(spriteSign * spriteCenter.x - rigSign * rigCenter.x * fitScale,
                spriteCenter.y - rigCenter.y * fitScale, 0f);
        }

        private void ApplyFade(bool fadeAtTop)
        {
            if (hasFade && fadeAtTop == isFading)
            {
                return;
            }

            hasFade = true;
            isFading = fadeAtTop;
            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }

            meshRenderer.GetPropertyBlock(block);
            block.SetFloat(FadeOnId, fadeAtTop ? 1f : 0f);
            meshRenderer.SetPropertyBlock(block);
        }
    }
}
