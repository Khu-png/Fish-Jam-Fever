using System.Collections;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace FishJam.UI
{
    /// <summary>
    /// The screen transition (Resources/UI/Canvas_Transition): a solid cover closes in on the middle of the screen like an
    /// iris, a fish jumps out of its bowl, then the cover opens up on the new screen. Driven by TransitionManager.
    /// </summary>
    public sealed class Transition : SkinCanvas
    {
        private const string BOWL_ANIMATION = "action";
        private const float HALF = 0.5f;
        private const float BOWL_END_SCALE = 0.6f;
        // Width of the soft edge of the iris, as a fraction of the screen height.
        private const float FEATHER = 0.18f;
        private const float CLOSE_TIME = 0.28f;
        private const float OPEN_TIME = 0.35f;
        // The bowl shrinks and fades during the first part of the opening.
        private const float BOWL_FADE_TIME = 0.2f;
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int FeatherId = Shader.PropertyToID("_Feather");

        [Tooltip("Full-screen image that uses the FishSort/UIIris material.")]
        [SerializeField] private Image cover;
        [SerializeField] private RectTransform bowlHolder;
        [SerializeField] private SkeletonGraphic bowl;

        private Material coverMaterial;

        // Radius at which nothing is covered any more (the hole reaches past the corners of the screen).
        private float OpenRadius => HALF * Mathf.Sqrt(Mathf.Pow((float)Screen.width / Screen.height, 2f) + 1f) + FEATHER;

        protected override void Awake()
        {
            base.Awake();
            coverMaterial = new Material(cover.material);
            cover.material = coverMaterial;
            coverMaterial.SetFloat(FeatherId, FEATHER);
        }

        public override void Setup()
        {
            base.Setup();
            SetRadius(OpenRadius);
            bowl.gameObject.SetActive(false);
            bowlHolder.localScale = Vector3.one;
            bowl.color = Color.white;
            transform.SetAsLastSibling();
        }

        // Other screens opening or closing must not hide the transition: put it back, on top, as it was.
        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public IEnumerator Close()
        {
            yield return Animate(OpenRadius, 0f, CLOSE_TIME);
        }

        // The bowl jumps with its fish; it starts from the first frame each time.
        public void PlayBowl()
        {
            bowl.gameObject.SetActive(true);
            bowl.color = Color.white;
            bowlHolder.localScale = Vector3.one;
            bowl.AnimationState.SetAnimation(0, BOWL_ANIMATION, false);
        }

        public IEnumerator Open()
        {
            float elapsed = 0f;
            while (elapsed < OPEN_TIME)
            {
                elapsed += Time.unscaledDeltaTime;
                float bowlProgress = Mathf.Clamp01(elapsed / BOWL_FADE_TIME);
                bowl.color = new Color(1f, 1f, 1f, 1f - bowlProgress);
                bowlHolder.localScale = Vector3.one * Mathf.Lerp(1f, BOWL_END_SCALE, bowlProgress);
                SetRadius(Mathf.Lerp(0f, OpenRadius, Smooth(elapsed / OPEN_TIME)));
                yield return null;
            }

            SetRadius(OpenRadius);
            CloseDirectly();
        }

        private IEnumerator Animate(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetRadius(Mathf.Lerp(from, to, Smooth(elapsed / duration)));
                yield return null;
            }

            SetRadius(to);
        }

        private void SetRadius(float radius)
        {
            coverMaterial.SetFloat(RadiusId, radius);
        }

        private static float Smooth(float t)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
        }
    }
}
