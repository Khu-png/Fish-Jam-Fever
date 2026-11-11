using System.Collections;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FishJam.UI
{
    /// <summary>
    /// Man Thang (Resources/UI/Canvas_Win, sortingOrder 20): chu WELL DONE + ruong (Spine), +30 xu, nut Claim.
    /// Ruong mo (event Spine "Open" trong anim Opening) -> phan thuong nhay ra tu mieng ruong roi roi xuong cho cua no -> hien Claim.
    /// Claim -> xu (prefab FlyCoin) bay ve o xu -> hien "Tap to continue" -> sang level ke tiep.
    /// </summary>
    public class Win : SkinCanvas
    {
        public int reward = 30;
        public RectTransform ray;

        [Header("Ruong + phan thuong")]
        public SkeletonGraphic chest;
        [Tooltip("Diem mieng ruong: phan thuong bat dau nhay ra tu day")]
        public RectTransform chestMouth;
        public RectTransform rewardGroup;
        [Tooltip("Chu \"+30\" – giam dan khi tung dong xu bay di")]
        public TMP_Text rewardLabel;
        public string openEvent = "Open";
        [Tooltip("Neu khong nhan duoc event Open sau bay nhieu giay thi van cho phan thuong nhay ra")]
        public float fallbackDelay = 3.5f;
        public float jumpTime = .6f;
        public float jumpHeight = 350f;

        [Header("Nut")]
        public Button claimButton;
        public Button tapToContinue;
        public CanvasGroup tapText;

        [Header("Xu bay")]
        public CoinBar coinBar;
        [Tooltip("Prefab dong xu bay (Prefabs/UI/FlyCoin)")]
        public RectTransform flyCoinPrefab;
        public RectTransform flyFrom;
        public int flyCount = 12;

        bool isClicked, rewardShown;
        Vector2 rewardRestPos;
        Spine.AnimationState hooked;
        Coroutine fallbackCo;

        protected override void Awake()
        {
            base.Awake();
            claimButton.onClick.AddListener(ClaimButton);
            tapToContinue.onClick.AddListener(ContinueButton);
            rewardRestPos = rewardGroup.anchoredPosition;
        }

        public override void Setup()
        {
            base.Setup();
            isClicked = false; rewardShown = false;
            Time.timeScale = 0f;
            rewardGroup.gameObject.SetActive(false);
            rewardGroup.anchoredPosition = rewardRestPos; rewardGroup.localScale = Vector3.one;
            claimButton.gameObject.SetActive(false);
            tapToContinue.gameObject.SetActive(false);
            SetRewardText(reward);
            if (coinBar) coinBar.Set(Wallet.Coins);
        }

        public override void Open()
        {
            base.Open();   // bat canvas -> SpineSequence chay Appear -> Opening -> Open_idle
            Unhook();
            if (chest && chest.AnimationState != null) { hooked = chest.AnimationState; hooked.Event += OnSpineEvent; }
            fallbackCo = StartCoroutine(Fallback());
        }

        void OnSpineEvent(TrackEntry entry, Spine.Event e)
        {
            if (e.Data.Name == openEvent) ShowReward();
        }

        IEnumerator Fallback()
        {
            yield return new WaitForSecondsRealtime(fallbackDelay);
            ShowReward();
        }

        void ShowReward()
        {
            if (rewardShown) return; rewardShown = true;
            SoundManager.Ins.PlayFx(FxID.ChestOpen);
            Unhook();
            StartCoroutine(RewardJump());
        }

        /// <summary>Phan thuong nhay ra tu mieng ruong: bay vong len roi roi xuong vi tri cua no, phong to dan.</summary>
        IEnumerator RewardJump()
        {
            SoundManager.Ins.PlayFx(FxID.RewardPop);
            // tinh bang toa do the gioi (vi tri that tren man) -> khong phu thuoc anchor/pivot
            rewardGroup.anchoredPosition = rewardRestPos;
            Vector3 to = rewardGroup.position;
            Vector3 from = chestMouth ? chestMouth.position : to;
            float s = rewardGroup.lossyScale.y;
            Vector3 ctrl = (from + to) * .5f; ctrl.y = Mathf.Max(from.y, to.y) + jumpHeight * s;
            rewardGroup.gameObject.SetActive(true);
            for (float t = 0; t < jumpTime; t += Time.unscaledDeltaTime)
            {
                float k = t / jumpTime;
                rewardGroup.position = Vector3.Lerp(Vector3.Lerp(from, ctrl, k), Vector3.Lerp(ctrl, to, k), k);
                rewardGroup.localScale = Vector3.one * (k < .7f ? Mathf.Lerp(.2f, 1.15f, k / .7f) : Mathf.Lerp(1.15f, 1f, (k - .7f) / .3f));
                yield return null;
            }
            rewardGroup.anchoredPosition = rewardRestPos; rewardGroup.localScale = Vector3.one;
            yield return Punch(rewardGroup, .12f);
            claimButton.gameObject.SetActive(true);
            yield return Pop(claimButton.transform, .25f);
        }

        void Update()
        {
            if (ray) ray.Rotate(0, 0, -25f * Time.unscaledDeltaTime);
            if (tapText && tapToContinue.gameObject.activeSelf) tapText.alpha = .35f + .65f * (Mathf.Sin(Time.unscaledTime * 5f) * .5f + .5f);
        }

        public void ClaimButton()
        {
            if (isClicked) return; isClicked = true;
            SoundManager.Ins.PlayFx(FxID.Claim);
            claimButton.gameObject.SetActive(false);
            StartCoroutine(Claim());
        }

        IEnumerator Claim()
        {
            int start = Wallet.Coins;
            Vector3 to = coinBar && coinBar.icon ? coinBar.icon.position : transform.position;
            for (int i = 0; i < flyCount; i++)
            {
                int given = Mathf.RoundToInt(reward * (i + 1f) / flyCount);
                StartCoroutine(Fly(to, i * .055f, start + given, reward - given));
            }
            yield return new WaitForSecondsRealtime(.8f + flyCount * .055f);
            Wallet.Add(reward);
            rewardGroup.gameObject.SetActive(false);
            tapToContinue.gameObject.SetActive(true);
            isClicked = false;
        }

        IEnumerator Fly(Vector3 to, float delay, int showValue, int remain)
        {
            yield return new WaitForSecondsRealtime(delay);
            SetRewardText(remain);   // xu roi khoi phan thuong -> chu giam dan
            if (!flyCoinPrefab) yield break;
            var parent = (RectTransform)transform;
            var rt = Instantiate(flyCoinPrefab, parent);
            float s = parent.lossyScale.x;
            Vector3 from = (flyFrom ? flyFrom.position : rewardGroup.position) + (Vector3)(Random.insideUnitCircle * 60f * s);
            Vector3 mid = Vector3.Lerp(from, to, .5f) + Vector3.right * Random.Range(-150f, 150f) * s;
            for (float t = 0; t < .7f; t += Time.unscaledDeltaTime)
            {
                float k = t / .7f, e = k * k;
                rt.position = Vector3.Lerp(Vector3.Lerp(from, mid, e), Vector3.Lerp(mid, to, e), e);
                rt.localScale = Vector3.one * Mathf.Lerp(1f, .7f, k);
                yield return null;
            }
            Destroy(rt.gameObject);
            SoundManager.Ins.PlayFx(FxID.CoinCollect);
            if (coinBar) coinBar.Show(showValue);
        }

        /// <summary>Tap to continue: sang level ke tiep.</summary>
        public void ContinueButton()
        {
            if (isClicked) return; isClicked = true;
            SoundManager.Ins.PlayFx(FxID.Button);
            CloseDirectly();
            // the level was already advanced when the win was reported
            LevelManager.Ins.OnNextLevel();
        }

        public override void CloseDirectly()
        {
            Unhook();
            if (fallbackCo != null) { StopCoroutine(fallbackCo); fallbackCo = null; }
            Time.timeScale = 1f;
            base.CloseDirectly();
        }

        void SetRewardText(int v)
        {
            if (!rewardLabel) return;
            rewardLabel.text = "+" + v;
            if (v < reward) StartCoroutine(Punch(rewardLabel.transform, .08f));
        }

        void Unhook() { if (hooked != null) { hooked.Event -= OnSpineEvent; hooked = null; } }

        static IEnumerator Punch(Transform t, float d)
        {
            for (float x = 0; x < d; x += Time.unscaledDeltaTime) { t.localScale = Vector3.one * (1 - .1f * Mathf.Sin(x / d * Mathf.PI)); yield return null; }
            t.localScale = Vector3.one;
        }
        static IEnumerator Pop(Transform t, float d)
        {
            for (float x = 0; x < d; x += Time.unscaledDeltaTime) { float k = x / d; t.localScale = Vector3.one * (k < .7f ? Mathf.Lerp(0, 1.1f, k / .7f) : Mathf.Lerp(1.1f, 1, (k - .7f) / .3f)); yield return null; }
            t.localScale = Vector3.one;
        }
    }
}
