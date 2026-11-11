using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FishJam.UI
{
    /// <summary>
    /// Popup Hoi sinh "Almost there!" (Resources/UI/Canvas_Revive, sortingOrder 25). Mo khi sap thua:
    ///   UIManager.Instance.Open&lt;Revive&gt;().SetProgress(0.37f);
    /// Nut xu (600) / Revive (quang cao – chua co SDK, coi nhu xem xong) -> dong popup, phat Revive.Revived (gameplay mo them be).
    /// Nut X -> mo popup Lose. Mo popup -> Time.timeScale = 0.
    /// </summary>
    public class Revive : SkinCanvas
    {
        [Tooltip("Base price. Each revive in the same level multiplies it: x1, x2, x3...")]
        public int cost = 600;
        public TMP_Text progressLabel;
        public TMP_Text costLabel;
        public Button closeButton, coinButton, adButton;
        public CoinBar coinBar;

        /// <summary>Gameplay nghe: hoi sinh thanh cong (vd mo khoa them 1 be).</summary>
        public static event Action Revived;

        bool isClicked;
        int price;
        float progress;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(CloseButton);
            coinButton.onClick.AddListener(CoinButton);
            adButton.onClick.AddListener(AdButton);
        }

        public override void Setup()
        {
            base.Setup();
            isClicked = false;
            Time.timeScale = 0f;
            SoundManager.Ins.PlayFx(FxID.Whoosh);
            price = cost * (GameManager.Ins.ReviveCount + 1);
            if (costLabel) costLabel.text = price.ToString();
        }

        /// <summary>Tien do man choi 0..1 (hien "Progress: 37%").</summary>
        public void SetProgress(float p)
        {
            progress = p;
            if (progressLabel) progressLabel.text = "Progress: <color=#E8262B>" + Mathf.RoundToInt(Mathf.Clamp01(p) * 100) + "%</color>";
        }

        public void CoinButton()
        {
            if (isClicked) return;
            SoundManager.Ins.PlayFx(FxID.Button);
            if (!Wallet.Spend(price)) 
            {
                //if (coinBar) coinBar.Bump();
                if (coinBar) coinBar.Bump(); StartCoroutine(Shake(coinButton.transform)); 
                return; 
            }
            DoRevive();
        }

        public void AdButton() { if (isClicked) return; SoundManager.Ins.PlayFx(FxID.Button); DoRevive(); } 

        public void CloseButton()
        {
            if (isClicked) return; isClicked = true;
            SoundManager.Ins.PlayFx(FxID.Button);
            CloseDirectly();
            GameManager.Ins.OnLose(progress);
        }

        void DoRevive()
        {
            isClicked = true;
            CloseDirectly();
            Revived?.Invoke();
        }

        public override void CloseDirectly() { Time.timeScale = 1f; base.CloseDirectly(); }

        static IEnumerator Shake(Transform t)
        {
            var p = t.localPosition;
            for (float x = 0; x < .3f; x += Time.unscaledDeltaTime) 
            { 
                t.localPosition = p + Vector3.right * Mathf.Sin(x * 60) * 10 * (1 - x / .3f); 
                yield return null; 
            }
            t.localPosition = p;
        }
    }
}
