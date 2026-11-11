using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FishJam.UI
{
    /// <summary>
    /// Popup Cai dat (Resources/UI/Canvas_Setting – Canvas rieng, sortingOrder 30). Mo bang UIManager.Instance.Open&lt;Setting&gt;().
    /// SetUp() tu chon che do:
    ///  - dang PlayingState -> InGame: Music/Sound/Vibration + Home, Retry; Time.timeScale = 0 (game dung)
    ///  - con lai           -> Menu  : Music/Sound/Vibration + Language, Support, Version
    /// Moi duong dong deu qua CloseDirectly() -> tra Time.timeScale = 1.
    /// </summary>
    public class Setting : SkinCanvas
    {
        public enum Mode { Menu, InGame }
        public Mode mode = Mode.Menu;

        /// <summary>Popup dang hien (khong can UIManager – dung cho bang TEST).</summary>
        public static bool IsShowing { get; private set; }

        [Header("Tham chieu")]
        public Button closeButton;
        public Toggle3 music, sound, vibration;
        public GameObject menuGroup, ingameGroup;
        public Button languageButton, supportButton, homeButton, retryButton;
        public TMP_Text versionLabel;

        [Header("Su kien (chua co chuc nang)")]
        public UnityEvent onLanguage, onSupport;

        [System.Serializable]
        public class Toggle3
        {
            public Button button; public Image bg;
            public GameObject offMark;
            public Sprite onSprite, offSprite;
            public void Show(bool on) 
            { 
                if (bg) bg.sprite = on ? onSprite : offSprite;
                if (offMark) offMark.SetActive(!on);
            }
        }

        bool isClicked;

        [Header("Hieu ung mo/dong")]
        public CanvasGroup dim;          // nen toi: 0 -> 1
        public RectTransform panel;      // khung: 0.6 -> 1.08 -> 1 khi mo, 1 -> 0.7 khi dong
        Coroutine anim;

        protected override void Awake()
        {
            base.Awake();
            if (closeButton) closeButton.onClick.AddListener(CloseButton);
            if (music?.button) music.button.onClick.AddListener(() => { GameSettings.Music = !GameSettings.Music; SoundManager.Ins.PlayFx(FxID.ButtonSwitch); Tap(music.button); });
            if (sound?.button) sound.button.onClick.AddListener(() => { GameSettings.Sound = !GameSettings.Sound; SoundManager.Ins.PlayFx(FxID.ButtonSwitch); Tap(sound.button); });
            if (vibration?.button) vibration.button.onClick.AddListener(() => { GameSettings.Vibration = !GameSettings.Vibration; GameSettings.Vibrate(); SoundManager.Ins.PlayFx(FxID.ButtonSwitch); Tap(vibration.button); });
            if (languageButton) languageButton.onClick.AddListener(() => { SoundManager.Ins.PlayFx(FxID.Button); onLanguage.Invoke(); });
            if (supportButton) supportButton.onClick.AddListener(() => { SoundManager.Ins.PlayFx(FxID.Button); onSupport.Invoke(); });
            if (homeButton) homeButton.onClick.AddListener(HomeButton);
            if (retryButton) retryButton.onClick.AddListener(RetryButton);
            if (versionLabel) versionLabel.text = "Version " + Application.version;
        }

        void OnEnable() { GameSettings.Changed += Refresh; Refresh(); }
        void OnDisable() { GameSettings.Changed -= Refresh; IsShowing = false; }
        void OnValidate() { ApplyMode(); }

        public override void Setup()
        {
            base.Setup();
            isClicked = false;
            mode = GameManager.IsState(GameState.GamePlay) ? Mode.InGame : Mode.Menu;
            ApplyMode();
            Refresh();
            if (mode == Mode.InGame)
            {
                Time.timeScale = 0f;
                GameManager.ChangeState(GameState.Setting);
            }
        }

        public override void Open()
        {
            IsShowing = true;
            transform.SetAsLastSibling();
            base.Open();
            if (dim || panel) Play(true);
        }

        /// <summary>Nut X: dong co hieu ung, choi tiep.</summary>
        public void CloseButton() { if (isClicked) return; SoundManager.Ins.PlayFx(FxID.Button); CloseAnimated(); }

        /// <summary>Nut Home: ve Main Menu.</summary>
        public void HomeButton()
        {
            if (isClicked) return; isClicked = true;
            SoundManager.Ins.PlayFx(FxID.Button);
            CloseDirectly();
            LevelManager.Ins.OnHome();
        }

        /// <summary>Nut Retry: choi lai level hien tai.</summary>
        public void RetryButton()
        {
            if (isClicked) return; isClicked = true;
            SoundManager.Ins.PlayFx(FxID.Button);
            CloseDirectly();
            LevelManager.Ins.OnRetry();
        }

        /// <summary>Moi duong dong deu qua day: tra thoi gian ve binh thuong (quen la game dung hinh mai mai).</summary>
        public override void CloseDirectly()
        {
            Time.timeScale = 1f;
            IsShowing = false;
            if (GameManager.IsState(GameState.Setting))
            {
                GameManager.ChangeState(GameState.GamePlay);
            }

            if (anim != null) { StopCoroutine(anim); anim = null; }
            if (dim) dim.alpha = 1;
            if (panel) panel.localScale = Vector3.one;
            base.CloseDirectly();
        }

        /// <summary>Dong co hieu ung (neu co dim/panel), xong thi goi CloseDirectly().</summary>
        public void CloseAnimated()
        {
            if ((dim || panel) && gameObject.activeInHierarchy) Play(false); else CloseDirectly();
        }

        void Play(bool open)
        {
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(Anim(open));
        }

        System.Collections.IEnumerator Anim(bool open)
        {
            float d = open ? .28f : .16f;
            for (float t = 0; t < d; t += Time.unscaledDeltaTime)
            {
                float k = t / d;
                if (dim) dim.alpha = open ? k : 1 - k;
                if (panel) panel.localScale = Vector3.one * (open ? Back(k) : Mathf.Lerp(1f, .7f, k));
                yield return null;
            }
            if (dim) dim.alpha = open ? 1 : 0;
            if (panel) panel.localScale = Vector3.one;
            anim = null;
            if (!open) CloseDirectly();
        }

        // 0.6 -> 1.08 -> 1
        static float Back(float k) { const float s = 1.9f; k -= 1; return Mathf.Lerp(.6f, 1f, 1 + k * k * ((s + 1) * k + s)); }

        public void ApplyMode()
        {
            if (menuGroup) menuGroup.SetActive(mode == Mode.Menu);
            if (ingameGroup) ingameGroup.SetActive(mode == Mode.InGame);
        }

        void Refresh() 
        { 
            music?.Show(GameSettings.Music); 
            sound?.Show(GameSettings.Sound); 
            vibration?.Show(GameSettings.Vibration); 
        }

        static void Tap(Button b) { if (b) b.StartCoroutine(Punch(b.transform)); }
        static System.Collections.IEnumerator Punch(Transform t)
        {
            for (float x = 0; x < .15f; x += Time.unscaledDeltaTime) 
            { 
                t.localScale = Vector3.one * (1 - .12f * Mathf.Sin(x / .15f * Mathf.PI)); 
                yield return null; 
            }
            t.localScale = Vector3.one;
        }
    }
}
