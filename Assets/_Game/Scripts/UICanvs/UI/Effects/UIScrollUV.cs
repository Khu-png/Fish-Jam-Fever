using UnityEngine;
using UnityEngine.UI;

namespace FishJam.UI
{
    /// <summary>Truot texture lap lien tren RawImage (vet sang tren cat, anh nuoc).</summary>
    [RequireComponent(typeof(RawImage))]
    public class UIScrollUV : MonoBehaviour
    {
        public Vector2 speed = new Vector2(0.08f, 0f);
        public float waveY = 0.02f, wavePeriod = 6f;
        RawImage img; Rect baseRect;
        void Awake()
        {
            img = GetComponent<RawImage>();
            baseRect = img.uvRect;
        }
        void Update()
        {
            var r = baseRect;
            r.x = baseRect.x + Time.time * speed.x;
            r.y = baseRect.y + Time.time * speed.y + Mathf.Sin(Time.time * Mathf.PI * 2f / wavePeriod) * waveY;
            img.uvRect = r;
        }
    }
}
