using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FishJam.UI
{
    /// <summary>
    /// Giao dien man choi (chi UI). Gameplay goi cac ham public:
    ///   SetLevel(5) · SetFishCount(45,120) · SetWarning(true/false)
    ///   OnTankCompleted(): goi moi khi 1 be du ca. Hoan thanh be tiep theo trong 'comboWindow' giay -> combo +1;
    ///     combo 2..9 -> Nice, Cool, Good, Great, Excellent, Amazing, Fantastic, Perfect (tren 9 giu Perfect). Qua thoi gian -> ve 1.
    ///   ShowPraise(0..7): hien thang 1 chu khen (khong qua combo).
    /// Lay HUD: UIManager.Instance.GetUI&lt;GamePlay&gt;(). PlayingState mo/dong HUD.
    /// Dat doi tuong gameplay (ke be, khay cho, ca) vao 'gameplayLayer' – nam giua nen va HUD.
    /// </summary>
    public partial class GamePlay : SkinCanvas
    {
        [Header("HUD")]
        public TextMeshProUGUI levelLabel;
        public TextMeshProUGUI countLabel;
        public RectTransform countBox;
        public Button settingsButton;
        [Header("Chu khen")]
        [Tooltip("Giay toi da giua 2 lan hoan thanh be lien tiep de tinh combo")]
        public float comboWindow = 5f;
        public Image[] praises;           // Nice, Cool, Good, Great, Excellent, Amazing, Fantastic, Perfect
        public Image spark;
        [Header("Canh bao con 1 o")]
        public CanvasGroup warningFrame;
        public RectTransform warningText;
        [SerializeField] private CanvasGroup warningMessage;
        [Header("Lop gameplay")]
        public RectTransform gameplayLayer;

        bool warning; Coroutine praiseCo, bumpCo, warnCo;
        int combo; float lastTankTime = -999f;
        public int Combo => combo;

        /// <summary>Goi khi 1 be vua du ca. Tu tinh combo theo thoi gian va hien chu khen tuong ung.</summary>
        public void OnTankCompleted()
        {
            combo = Time.time - lastTankTime <= comboWindow ? combo + 1 : 1;
            lastTankTime = Time.time;
            if (combo >= 2) ShowPraise(Mathf.Min(combo - 2, praises.Length - 1));
        }

        /// <summary>Dat lai combo (vd khi bat dau / choi lai level).</summary>
        public void ResetCombo() { combo = 0; lastTankTime = -999f; }

        protected override void Awake()
        {
            base.Awake();
            InitializeGameplayControls();
            ResetVisuals();
            if (settingsButton) settingsButton.onClick.AddListener(SettingButton);
        }

        /// <summary>Moi lan vao man / choi lai: dat level, reset combo, an chu khen + canh bao.</summary>
        public override void Setup()
        {
            base.Setup();
            SetLevel(FishSort.PlayerData.Level);
            ResetCombo();
            ResetVisuals();
        }

        /// <summary>Nut banh rang: mo popup Cai dat (che do InGame, game dung).</summary>
        public void SettingButton() { SoundManager.Ins.PlayFx(FxID.Button); UIManager.Ins.OpenUI<Setting>(); }

        void ResetVisuals()
        {
            StopAllCoroutines(); praiseCo = bumpCo = warnCo = null; warning = false;
            ResetGameplayOverlays();
            foreach (var p in praises) if (p) p.gameObject.SetActive(false);
            if (spark) spark.gameObject.SetActive(false);
            if (warningFrame) warningFrame.alpha = 0;
            if (warningMessage) warningMessage.alpha = 0f;
            if (warningText)
            {
                warningText.localScale = Vector3.one;
                warningText.gameObject.SetActive(false);
            }
            if (countBox)
            {
                countBox.gameObject.SetActive(true);
                countBox.localScale = Vector3.one;
            }
        }

        void Update()
        {
            if (warning && warningFrame)
                warningFrame.alpha = Mathf.Lerp(.35f, 1f, Mathf.Sin(Time.time * Mathf.PI * 2f / .9f) * .5f + .5f);
        }

        public void SetLevel(int level) { if (levelLabel) levelLabel.text = level.ToString(); }

        public void SetFishCount(int collected, int total)
        {
            if (countLabel) countLabel.text = collected + "/" + total;
            if (countBox) { if (bumpCo != null) StopCoroutine(bumpCo); bumpCo = StartCoroutine(Bump()); }
        }

        public void SetFishCounterVisible(bool isVisible)
        {
            if (countBox) countBox.gameObject.SetActive(isVisible);
        }

        public void ShowPraise(int tier)
        {
            if (praises == null || praises.Length == 0) return;
            tier = Mathf.Clamp(tier, 0, praises.Length - 1);
            SoundManager.Ins.PlayPraise(tier);
            if (praiseCo != null) StopCoroutine(praiseCo);
            praiseCo = StartCoroutine(Praise(praises[tier]));
        }

        public void HidePraise()
        {
            if (praiseCo != null) StopCoroutine(praiseCo);
            praiseCo = null;
            foreach (var praise in praises) if (praise) praise.gameObject.SetActive(false);
            if (spark) spark.gameObject.SetActive(false);
        }

        public void SetWarning(bool on)
        {
            if (warning == on) return;
            warning = on;
            if (warnCo != null) StopCoroutine(warnCo);
            warnCo = StartCoroutine(Warn(on));
        }

        IEnumerator Bump()
        {
            for (float t = 0; t < .25f; t += Time.unscaledDeltaTime)
            { 
                countBox.localScale = Vector3.one * (1f + .12f * Mathf.Sin(t / .25f * Mathf.PI)); 
                yield return null; 
            }
            countBox.localScale = Vector3.one;
        }

        IEnumerator Praise(Image img)
        {
            foreach (var p in praises) if (p) p.gameObject.SetActive(false);
            var rt = img.rectTransform; var basePos = rt.anchoredPosition; float h = rt.rect.height;
            img.gameObject.SetActive(true);
            if (spark) spark.gameObject.SetActive(true);
            const float D = 1.1f;
            for (float t = 0; t < D; t += Time.unscaledDeltaTime)
            {
                float k = t / D, s, a, dy;
                if (k < .18f) { float q = k / .18f; s = Mathf.Lerp(0, 1.25f, q); a = q; dy = 0; }
                else if (k < .3f) { s = Mathf.Lerp(1.25f, 1f, (k - .18f) / .12f); a = 1; dy = 0; }
                else if (k < .75f) { s = 1; a = 1; dy = Mathf.Lerp(0, .1f, (k - .3f) / .45f); }
                else { float q = (k - .75f) / .25f; s = Mathf.Lerp(1, .95f, q); a = 1 - q; dy = Mathf.Lerp(.1f, .3f, q); }
                rt.localScale = Vector3.one * s; rt.anchoredPosition = basePos + new Vector2(0, dy * h); SetA(img, a);
                if (spark)
                {
                    float ks = Mathf.Clamp01(t / .8f);
                    spark.rectTransform.localScale = Vector3.one * Mathf.Lerp(.2f, 1.3f, ks);
                    SetA(spark, ks < .3f ? ks / .3f : 1 - (ks - .3f) / .7f);
                }
                yield return null;
            }
            rt.anchoredPosition = basePos; img.gameObject.SetActive(false);
            if (spark) spark.gameObject.SetActive(false);
        }

        IEnumerator Warn(bool on)
        {
            if (!on)
            {
                if (warningMessage) warningMessage.alpha = 0f;
                if (warningText) warningText.gameObject.SetActive(false);
                float startAlpha = warningFrame ? warningFrame.alpha : 0f;
                for (float t = 0; t < .2f; t += Time.unscaledDeltaTime)
                {
                    if (warningFrame) warningFrame.alpha = Mathf.Lerp(startAlpha, 0f, t / .2f);
                    yield return null;
                }
                if (warningFrame) warningFrame.alpha = 0f;
                warnCo = null;
                yield break;
            }

            if (warningText)
            {
                warningText.localScale = Vector3.one;
                warningText.gameObject.SetActive(true);
            }
            const float messageDuration = 2f;
            for (float t = 0; t < messageDuration; t += Time.unscaledDeltaTime)
            {
                float blink = Mathf.Lerp(.45f, 1f, Mathf.Sin(t * Mathf.PI * 2f / .48f) * .5f + .5f);
                float fadeIn = Mathf.Clamp01(t / .12f);
                float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.2f, messageDuration, t));
                if (warningMessage) warningMessage.alpha = blink * fadeIn * fadeOut;
                yield return null;
            }
            if (warningMessage) warningMessage.alpha = 0f;
            if (warningText) warningText.gameObject.SetActive(false);
            // Update keeps the red border visible until the tray is no longer one slot from full.
            warnCo = null;
        }

        static void SetA(Graphic g, float a) 
        { 
            var c = g.color; 
            c.a = a; 
            g.color = c; 
        }
    }
}
