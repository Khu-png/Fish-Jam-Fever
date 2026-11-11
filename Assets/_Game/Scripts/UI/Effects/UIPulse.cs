using UnityEngine;

namespace FishJam.UI
{
    /// <summary>Phap phong (nut Play).</summary>
    public class UIPulse : MonoBehaviour
    {
        public float amount = 0.05f;
        public float period = 1.6f;
        Vector3 baseScale;
        void Awake() 
        { 
            baseScale = transform.localScale; 
        }
        void Update()
        {
            float s = 1f + (Mathf.Sin(Time.time * Mathf.PI * 2f / period) * 0.5f + 0.5f) * amount;
            transform.localScale = baseScale * s;
        }
    }
}
