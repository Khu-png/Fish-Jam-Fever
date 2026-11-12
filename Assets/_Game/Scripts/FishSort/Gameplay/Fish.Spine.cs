using UnityEngine;

namespace FishSort
{
    // The animated (Spine) look of a fish: used only while it rests and is not selected or frozen. While flying,
    // sliding the tray or held under a finger the plain sprite is shown (no animation, outline works on it).
    public sealed partial class Fish
    {
        private const float SPINE_PHASE_SCALE = 3.7f;

        [SerializeField] private FishSpine spine;

        private bool isHighlighted;
        private bool isSpineShown;

        private void BindSpine(FishLook look)
        {
            isSpineShown = false;
            isHighlighted = false;
            if (spine == null)
            {
                return;
            }

            spine.Bind(look.Skeleton, look.SkeletonFacesRight, look.Sprite, swimPhase * SPINE_PHASE_SCALE);
        }

        private void HideSpine()
        {
            isSpineShown = false;
            if (spine != null)
            {
                spine.Show(false);
            }
        }

        private bool IsResting => !IsFlying && !isHighlighted && !IsFrozen
            && (idleInContainer || (idleInBubble && (Owner != null || IsInJar)));

        // Every frame: swap between the sprite and the animated rig, and keep the rig in step with the sprite.
        private void UpdateSpine()
        {
            bool showSpine = spine != null && spine.IsReady && IsResting;
            if (showSpine != isSpineShown)
            {
                isSpineShown = showSpine;
                spine.Show(showSpine);
                bodyRenderer.enabled = !showSpine;
            }

            if (isSpineShown)
            {
                spine.Sync(bodyRenderer.sortingOrder, bodyRenderer.maskInteraction,
                    bodyRenderer.sharedMaterial == bubbleMaterial, bodyRenderer.flipX, moveRoutine == null);
            }
        }
    }
}
