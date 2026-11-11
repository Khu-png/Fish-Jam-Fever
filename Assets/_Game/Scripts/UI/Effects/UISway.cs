using UnityEngine;

namespace FishJam.UI
{
    /// <summary>Lac qua lai quanh pivot (dat pivot o goc cay = (0.5,0)). Dung cho rong, san ho.</summary>
    public class UISway : MonoBehaviour
    {
        public float angle = 3f;        // bien do (do)
        public float period = 3.5f;     // giay / chu ky
        public float phase;             // lech pha (giay)
        public float skew = 0f;         // gia lap uon nhe
        RectTransform rt; float baseZ; Vector3 baseScale;
        void Awake()
        {
            rt = (RectTransform)transform;
            baseZ = rt.localEulerAngles.z;
            baseScale = rt.localScale;
        }
        void Update()
        {
            float s = Mathf.Sin((Time.time + phase) * Mathf.PI * 2f / period);
            rt.localRotation = Quaternion.Euler(0, 0, baseZ + s * angle);
            if (skew != 0f) rt.localScale = new Vector3(baseScale.x * (1f + s * skew), baseScale.y * (1f - Mathf.Abs(s) * skew * 0.5f), baseScale.z);
        }
    }
}
