using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FishJam.UI
{
    /// <summary>
    /// The screen goes dark except for the tanks the fishing rod can fish for (they stay bright). Tapping
    /// one of them picks it; tapping anywhere else cancels. Lives in the gameplay HUD (Canvas_GamePlay).
    /// </summary>
    public sealed class TankChoice : MonoBehaviour, IPointerClickHandler
    {
        private const int MAX_TANKS = 4;
        private const float FADE_TIME = 0.2f;
        private const float HALF = 0.5f;
        // Extra room around a tank (screen pixels), so the glow does not cut into it and a tap beside it still counts.
        private const float PADDING = 12f;
        private static readonly int[] HoleIds =
        {
            Shader.PropertyToID("_Hole0"), Shader.PropertyToID("_Hole1"), Shader.PropertyToID("_Hole2"), Shader.PropertyToID("_Hole3")
        };
        private static readonly int HoleCountId = Shader.PropertyToID("_HoleCount");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");

        [Tooltip("Full-screen image that uses the FishSort/UISpotlight material.")]
        [SerializeField] private RawImage dim;
        [SerializeField] private TMP_Text hint;
        [SerializeField] private CanvasGroup group;

        private Material material;
        private Rect[] tankRects = new Rect[0];
        private int[] tankSlots = new int[0];
        private Action<int> onChosen;
        private Action onCancelled;

        // The object starts switched off, so Awake only runs once it is first shown: the material is made here instead.
        private void EnsureMaterial()
        {
            if (material == null)
            {
                material = new Material(dim.material);
                dim.material = material;
            }
        }

        // screenRects: where each tank is on the screen (pixels); slots: which tank each one is.
        public void Show(Rect[] screenRects, int[] slots, Action<int> chosen, Action cancelled)
        {
            EnsureMaterial();
            tankRects = screenRects;
            tankSlots = slots;
            onChosen = chosen;
            onCancelled = cancelled;
            // The dark layer covers the area the game is drawn in (the whole screen, or less with side bars).
            Camera uiCamera = dim.canvas.worldCamera;
            Rect area = uiCamera != null ? uiCamera.pixelRect : new Rect(0f, 0f, Screen.width, Screen.height);
            material.SetFloat(AspectId, area.width / area.height);
            int count = Mathf.Min(screenRects.Length, MAX_TANKS);
            material.SetFloat(HoleCountId, count);
            for (int i = 0; i < count; i++)
            {
                Rect rect = screenRects[i];
                material.SetVector(HoleIds[i], new Vector4((rect.center.x - area.x) / area.width, (rect.center.y - area.y) / area.height,
                    (rect.width * HALF + PADDING) / area.width, (rect.height * HALF + PADDING) / area.height));
            }

            gameObject.SetActive(true);
            StartCoroutine(FadeIn());
        }

        public void Hide()
        {
            StopAllCoroutines();
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            int hit = -1;
            for (int i = 0; i < tankRects.Length && i < MAX_TANKS; i++)
            {
                Rect padded = new Rect(tankRects[i].x - PADDING, tankRects[i].y - PADDING,
                    tankRects[i].width + PADDING * 2f, tankRects[i].height + PADDING * 2f);
                if (padded.Contains(eventData.position))
                {
                    hit = i;
                }
            }

            if (hit >= 0)
            {
                onChosen?.Invoke(tankSlots[hit]);
            }
            else
            {
                onCancelled?.Invoke();
            }
        }

        private IEnumerator FadeIn()
        {
            group.alpha = 0f;
            for (float elapsed = 0f; elapsed < FADE_TIME; elapsed += Time.unscaledDeltaTime)
            {
                group.alpha = elapsed / FADE_TIME;
                yield return null;
            }

            group.alpha = 1f;
        }
    }
}
