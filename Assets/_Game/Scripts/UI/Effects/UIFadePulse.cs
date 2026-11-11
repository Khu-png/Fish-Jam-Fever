using UnityEngine;
using UnityEngine.UI;

namespace FishJam.UI
{
    /// <summary>Sang toi dan (tia nang, quang sang), tuy chon xoay / troi ngang.</summary>
    [RequireComponent(typeof(Graphic))]
    public class UIFadePulse : MonoBehaviour
    {
        public float minAlpha = 0.2f, maxAlpha = 0.65f;
        public float period = 6f, phase;
        public float rotateAmount = 0f;
        public float driftX = 0f;
        Graphic g; RectTransform rt; float baseZ; Vector2 basePos;
        void Awake() 
        { 
            g = GetComponent<Graphic>(); 
            rt = (RectTransform)transform; 
            baseZ = rt.localEulerAngles.z; 
            basePos = rt.anchoredPosition; 
        }
        void Update()
        {
            float s = Mathf.Sin((Time.time + phase) * Mathf.PI * 2f / period) * 0.5f + 0.5f;
            var c = g.color; 
            c.a = Mathf.Lerp(minAlpha, maxAlpha, s); 
            g.color = c;
            if (rotateAmount != 0f) rt.localRotation = Quaternion.Euler(0, 0, baseZ - s * rotateAmount);
            if (driftX != 0f) rt.anchoredPosition = basePos + new Vector2(Mathf.Lerp(-driftX, driftX, s), 0);
        }
    }
}
