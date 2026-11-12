using System;
using System.Collections;
using System.Collections.Generic;
using FishSort;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FishJam.UI
{
    /// <summary>
    /// Shop popup (Resources/UI/Canvas_BoosterShop) opened by tapping a power-up button that has none left: shows the power-up,
    /// its price in coins and a buy button. Buying gives one use. The game is paused while it is open.
    /// </summary>
    public sealed class BoosterShop : SkinCanvas
    {
        // Coins for one purchase, and how many uses one purchase gives.
        private const int PRICE = 1;
        private const int USES_PER_PURCHASE = 1;
        // Entry of icons / titles / descriptions used for a new tank (after the power-ups).
        private const int TANK_INDEX = 2;
        private const float SHAKE_TIME = 0.3f;
        private const float SHAKE_SPEED = 60f;
        private const float SHAKE_DISTANCE = 10f;
        // The panel is swiped down from above the screen while it fades in, dips a little below its place and then
        // bounces a few times (each smaller) until it rests; the dim fades in meanwhile.
        private const float DROP_HEIGHT = 1300f;
        private const float DROP_TIME = 0.32f;
        private const float FADE_TIME = 0.2f;
        private const float DIP = 45f;
        private const float BOUNCE_TIME = 0.6f;
        private const float BOUNCE_DAMPING = 6f;
        private const float BOUNCE_CYCLES = 2.2f;
        private const float DIM_TIME = 0.25f;

        [Tooltip("Per power-up, in the order of BoosterKind: picture, title and what it does.")]
        [SerializeField] private Sprite[] icons;
        [SerializeField] private string[] titles;
        [SerializeField] private string[] descriptions;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private CoinBar coinBar;
        [SerializeField] private Image dim;

        private readonly List<RectTransform> movers = new List<RectTransform>();
        private readonly List<Vector2> restPositions = new List<Vector2>();
        private readonly List<Graphic> fadeGraphics = new List<Graphic>();
        private readonly List<Color> fadeColors = new List<Color>();
        private BoosterKind kind;
        private Action onBought;
        private bool isClicked;
        private float dimAlpha;

        protected override void Awake()
        {
            base.Awake();
            buyButton.onClick.AddListener(Buy);
            closeButton.onClick.AddListener(Close);
            dimAlpha = dim.color.a;
            CollectMovers();
        }

        // Opens the shop for the power-up. Call after OpenUI.
        public void Show(BoosterKind boosterKind)
        {
            kind = boosterKind;
            onBought = null;
            Present((int)boosterKind);
        }

        // Opens the shop for a new tank: what is bought is whatever the caller does in bought.
        public void ShowTank(Action bought)
        {
            onBought = bought;
            Present(TANK_INDEX);
        }

        private void Present(int index)
        {
            iconImage.sprite = icons[index];
            titleLabel.text = titles[index];
            descriptionLabel.text = descriptions[index];
            priceLabel.text = PRICE.ToString();
        }

        public override void Setup()
        {
            base.Setup();
            isClicked = false;
            GameManager.ChangeState(GameState.Setting);
            Time.timeScale = 0f;
            SoundManager.Ins.PlayFx(FxID.Whoosh);
        }

        public override void Open()
        {
            base.Open();
            StartCoroutine(Appear());
        }

        public override void CloseDirectly()
        {
            Time.timeScale = 1f;
            if (GameManager.IsState(GameState.Setting))
            {
                GameManager.ChangeState(GameState.GamePlay);
            }

            base.CloseDirectly();
        }

        // Everything that makes up the panel: all parts except the dim and the coin bar, which stay still like in the win
        // and revive screens. Found by their place in the popup, so the layout of the prefab does not matter.
        private void CollectMovers()
        {
            Transform stage = coinBar.transform.parent;
            AddMovers(transform, stage);
            AddMovers(stage, stage);
            for (int i = 0; i < movers.Count; i++)
            {
                restPositions.Add(movers[i].anchoredPosition);
                Graphic[] graphics = movers[i].GetComponentsInChildren<Graphic>(true);
                for (int k = 0; k < graphics.Length; k++)
                {
                    fadeGraphics.Add(graphics[k]);
                    fadeColors.Add(graphics[k].color);
                }
            }
        }

        private void AddMovers(Transform parent, Transform stage)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                bool isFixed = child == dim.transform || child == stage || child == coinBar.transform;
                if (!isFixed && child is RectTransform rect && !movers.Contains(rect))
                {
                    movers.Add(rect);
                }
            }
        }

        private IEnumerator Appear()
        {
            float elapsed = 0f;
            while (elapsed < DROP_TIME)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / DROP_TIME);
                float slow = 1f - Mathf.Pow(1f - t, 3f);
                Place(Mathf.Lerp(DROP_HEIGHT, -DIP, slow), Mathf.Clamp01(elapsed / FADE_TIME));
                SetDim(Mathf.Clamp01(elapsed / DIM_TIME));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < BOUNCE_TIME)
            {
                elapsed += Time.unscaledDeltaTime;
                float decay = Mathf.Exp(-BOUNCE_DAMPING * elapsed);
                float swing = Mathf.Cos(2f * Mathf.PI * BOUNCE_CYCLES * elapsed);
                Place(-DIP * decay * swing, 1f);
                SetDim(1f);
                yield return null;
            }

            Place(0f, 1f);
            SetDim(1f);
        }

        // Every part is raised by the offset above its resting place, with the given opacity.
        private void Place(float offset, float opacity)
        {
            for (int i = 0; i < movers.Count; i++)
            {
                movers[i].anchoredPosition = restPositions[i] + Vector2.up * offset;
            }

            for (int i = 0; i < fadeGraphics.Count; i++)
            {
                Color color = fadeColors[i];
                color.a *= opacity;
                fadeGraphics[i].color = color;
            }
        }

        private void SetDim(float amount)
        {
            Color color = dim.color;
            color.a = dimAlpha * amount;
            dim.color = color;
        }

        private void Buy()
        {
            if (isClicked)
            {
                return;
            }

            SoundManager.Ins.PlayFx(FxID.Button);
            if (!PlayerData.SpendCoins(PRICE))
            {
                coinBar.Bump();
                StartCoroutine(Shake(buyButton.transform));
                return;
            }

            isClicked = true;
            Action bought = onBought;
            if (bought == null)
            {
                PlayerData.AddBooster(kind, USES_PER_PURCHASE);
            }

            CloseDirectly();
            bought?.Invoke();
        }

        private void Close()
        {
            if (!isClicked)
            {
                isClicked = true;
                SoundManager.Ins.PlayFx(FxID.Button);
                CloseDirectly();
            }
        }

        private static IEnumerator Shake(Transform target)
        {
            Vector3 rest = target.localPosition;
            for (float elapsed = 0f; elapsed < SHAKE_TIME; elapsed += Time.unscaledDeltaTime)
            {
                float damping = 1f - elapsed / SHAKE_TIME;
                target.localPosition = rest + Vector3.right * (Mathf.Sin(elapsed * SHAKE_SPEED) * SHAKE_DISTANCE * damping);
                yield return null;
            }

            target.localPosition = rest;
        }
    }
}
