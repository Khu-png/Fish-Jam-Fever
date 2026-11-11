using System.Collections;
using TMPro;
using UnityEngine;

namespace FishJam.UI
{
    /// <summary>O hien so xu (doc Wallet), nay nhe khi so thay doi. 'icon' la dich cho xu bay toi.</summary>
    public class CoinBar : MonoBehaviour
    {
        public TMP_Text label;
        public RectTransform icon;
        int shown; Coroutine co;

        void OnEnable() { Wallet.Changed += OnChanged; Set(Wallet.Coins); }
        void OnDisable() { Wallet.Changed -= OnChanged; }

        public void Set(int v) { shown = v; if (label) label.text = v.ToString(); }

        /// <summary>Hien so tang dan (dung khi xu bay ve).</summary>
        public void Show(int v) { Set(v); Bump(); }

        void OnChanged(int v) { Show(v); }

        public void Bump() 
        { 
            if (co != null) StopCoroutine(co); 
            co = StartCoroutine(BumpCo()); 
        }
        IEnumerator BumpCo()
        {
            for (float t = 0; t < .15f; t += Time.unscaledDeltaTime) { transform.localScale = Vector3.one * (1 + .15f * Mathf.Sin(t / .15f * Mathf.PI)); yield return null; }
            transform.localScale = Vector3.one;
        }
    }
}
