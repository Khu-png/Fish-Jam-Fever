using UnityEngine;

namespace FishJam.UI
{
    /// <summary>Vet sang luot qua nut (dat duoi 1 RectMask2D).</summary>
    public class UIShine : MonoBehaviour
    {
        public float width = 480f, period = 2.8f, idleRatio = 0.55f;
        RectTransform rt;
        void Awake() 
        { 
            rt = (RectTransform)transform; 
        }
        void Update()
        {
            float t = Mathf.Repeat(Time.time / period, 1f);
            float k = t < idleRatio ? 0f : (t - idleRatio) / (1f - idleRatio);
            rt.anchoredPosition = new Vector2(Mathf.Lerp(-width * 0.5f - 120f, width * 0.5f + 120f, k), 0);
        }
    }
}
