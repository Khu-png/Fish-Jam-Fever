using UnityEngine;

namespace FishJam.UI
{
    /// <summary>San ho "gat gu" nhu nap ruong he mo (gia lap rotateX co phoi canh bang scale). Pivot o goc (0.5,0).</summary>
    public class UINod : MonoBehaviour
    {
        public float period = 3.5f, phase;
        public float squash = 0.08f;   // muc nen chieu cao o dinh (tuong ung 14 do)
        static readonly float[] T = { 0f, .2f, .35f, .5f, .65f, 1f };
        static readonly float[] V = { 0f, 1f, .14f, .64f, 0f, 0f };
        Vector3 baseScale;
        void Awake() { baseScale = transform.localScale; }
        void Update()
        {
            float t = Mathf.Repeat((Time.time + phase) / period, 1f), v = 0f;
            for (int i = 1; i < T.Length; i++)
                if (t <= T[i]) { float k = (t - T[i - 1]) / (T[i] - T[i - 1]); v = Mathf.Lerp(V[i - 1], V[i], k * k * (3f - 2f * k)); break; }
            transform.localScale = new Vector3(baseScale.x * (1f + v * squash * .3f), baseScale.y * (1f - v * squash), baseScale.z);
        }
    }
}
