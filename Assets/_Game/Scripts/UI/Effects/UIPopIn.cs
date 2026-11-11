using System.Collections;
using UnityEngine;

namespace FishJam.UI
{
    /// <summary>Hieu ung mo (moi lan OnEnable): nen toi 0 -> 1, khung nay 0.6 -> 1.08 -> 1. Chay ca khi Time.timeScale = 0.</summary>
    public class UIPopIn : MonoBehaviour
    {
        public CanvasGroup dim;
        public RectTransform panel;
        public float duration = .28f;

        void OnEnable() { StartCoroutine(Play()); }

        IEnumerator Play()
        {
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                if (dim) dim.alpha = k;
                if (panel) panel.localScale = Vector3.one * Back(k);
                yield return null;
            }
            if (dim) dim.alpha = 1;
            if (panel) panel.localScale = Vector3.one;
        }

        static float Back(float k) { const float s = 1.9f; k -= 1; return Mathf.Lerp(.6f, 1f, 1 + k * k * ((s + 1) * k + s)); }
    }
}
