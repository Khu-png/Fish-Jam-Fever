using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FishJam.UI
{
    /// <summary>
    /// Start-up loading screen (Resources/UI/Canvas_Loading): a fish jumping out of a bowl, a gold progress bar with a crab
    /// riding on its tip, the percentage and the name of the step that is running. Driven by LoadingManager.
    /// </summary>
    public sealed class Loading : SkinCanvas
    {
        private const float SMOOTHING = 3f;
        private const float FADE_TIME = 0.6f;
        private const float FULL = 0.995f;
        private const float PERCENT = 100f;

        [Tooltip("The gold part of the bar. Anchored to the left of the track; only its width changes.")]
        [SerializeField] private RectTransform fill;
        [Tooltip("Shortest and longest width of the fill: the shortest keeps the rounded ends undistorted.")]
        [SerializeField] private float minFillWidth = 52f;
        [SerializeField] private float maxFillWidth = 808f;
        [Tooltip("Crab holder, anchored to the left of the track. It follows the tip of the fill.")]
        [SerializeField] private RectTransform crab;
        [Tooltip("How far the crab sits back from the very tip of the fill.")]
        [SerializeField] private float crabInset = 40f;
        [Tooltip("The crab stays between these x positions, so it never hangs over either end of the bar.")]
        [SerializeField] private float crabMinX = 90f;
        [SerializeField] private float crabMaxX = 750f;
        [SerializeField] private TMP_Text percentLabel;
        [SerializeField] private TMP_Text stepLabel;
        [SerializeField] private CanvasGroup group;

        private float target;
        private float shown;
        private Graphic[] graphics;
        private Color[] baseColors;

        // How much of the bar is drawn right now (it eases towards the real progress).
        public float Shown => shown;
        public bool IsFull => shown >= FULL;

        public override void Setup()
        {
            base.Setup();
            target = 0f;
            shown = 0f;
            group.alpha = 1f;
            group.blocksRaycasts = true;
            SetFade(1f);
            Apply();
        }

        // Real progress 0..1 and the name of the step being done.
        public void SetProgress(float progress, string step)
        {
            target = Mathf.Clamp01(progress);
            stepLabel.text = step;
        }

        // Fades the screen away over whatever is beneath it, then closes it.
        public IEnumerator FadeOut()
        {
            group.blocksRaycasts = false;
            float elapsed = 0f;
            while (elapsed < FADE_TIME)
            {
                elapsed += Time.unscaledDeltaTime;
                SetFade(1f - Mathf.Clamp01(elapsed / FADE_TIME));
                yield return null;
            }

            CloseDirectly();
        }

        // Fades every graphic by its own colour. A CanvasGroup would only lower the alpha of the Spine drawings (their
        // colours are premultiplied), which makes them glow white while they fade.
        private void SetFade(float factor)
        {
            if (graphics == null)
            {
                graphics = GetComponentsInChildren<Graphic>(true);
                baseColors = new Color[graphics.Length];
                for (int i = 0; i < graphics.Length; i++)
                {
                    baseColors[i] = graphics[i].color;
                }
            }

            for (int i = 0; i < graphics.Length; i++)
            {
                Color color = baseColors[i];
                color.a *= factor;
                graphics[i].color = color;
            }
        }

        private void Update()
        {
            shown = Mathf.Lerp(shown, target, 1f - Mathf.Exp(-SMOOTHING * Time.unscaledDeltaTime));
            if (target - shown < 1f - FULL && target >= 1f)
            {
                shown = target;
            }

            Apply();
        }

        private void Apply()
        {
            float width = Mathf.Lerp(minFillWidth, maxFillWidth, shown);
            fill.sizeDelta = new Vector2(width, fill.sizeDelta.y);
            float crabX = Mathf.Clamp(fill.anchoredPosition.x + width - crabInset, crabMinX, crabMaxX);
            crab.anchoredPosition = new Vector2(crabX, crab.anchoredPosition.y);
            if (percentLabel != null)
            {
                percentLabel.text = Mathf.RoundToInt(shown * PERCENT) + "%";
            }
        }
    }
}
