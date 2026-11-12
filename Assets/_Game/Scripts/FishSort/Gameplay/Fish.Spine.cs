using UnityEngine;

namespace FishSort
{
    // Keep one rig through selection, flight and arrival; the sprite remains the fallback and size reference.
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

        // Frozen fish retain the existing ice sprite. All other states share the same animated appearance.
        private void UpdateSpine()
        {
            bool showSpine = spine != null && spine.IsReady && !IsFrozen;
            if (showSpine != isSpineShown)
            {
                isSpineShown = showSpine;
                spine.Show(showSpine);
                bodyRenderer.enabled = !showSpine;
            }
            glowRenderer.forceRenderingOff = showSpine;

            if (isSpineShown)
            {
                spine.Sync(bodyRenderer.sortingOrder, bodyRenderer.maskInteraction,
                    bodyRenderer.sharedMaterial == bubbleMaterial, bodyRenderer.flipX,
                    idleInContainer ? .45f : IsFlying ? .8f : 1f, bodyRenderer.color, glowRenderer);
            }
        }
    }
}
