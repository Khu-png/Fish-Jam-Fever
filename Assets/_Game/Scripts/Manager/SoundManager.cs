using System.Collections;
using UnityEngine;


public enum SoundID
{
    BG_1 = 0,
}

// Index = position in SoundManager.fxAus. Keep values contiguous.
public enum FxID
{
    Button = 0,
    SummonItem = 1,
    PickCorrect = 2,
    PickTray = 3,
    TankComplete = 4,
    BubblePop = 5,
    IceCrack = 6,
    Warning = 7,
    Win = 8,
    Lose = 9,
    // Win screen. Append only: the value is the index into SoundManager.fxAus.
    ChestOpen = 10,
    RewardPop = 11,
    Claim = 12,
    CoinCollect = 13,
    // Popups. Append only.
    Whoosh = 14,
    ButtonSwitch = 15,
}


public class SoundManager : Singleton<SoundManager>
{
    private const float DEFAULT_PITCH = 1f;

    [SerializeField] private AudioClip[] soundAus;
    [SerializeField] private AudioClip[] fxAus;
    // Praise voices in the order of GamePlay.praises: nice, cool, good, great, excellent, amazing, fantastic, perfect.
    [SerializeField] private AudioClip[] praiseAus;

    private AudioSource soundSource;
    private AudioSource praiseSource;
    private AudioSource[] fxSource = new AudioSource[Utilities.GetEnumCount<FxID>()];

    void Awake()
    {
        RegisterSingleton(this);
        soundSource = gameObject.AddComponent<AudioSource>();
        soundSource.loop = true;
        praiseSource = new GameObject("Praise").AddComponent<AudioSource>();
        praiseSource.playOnAwake = false;
        praiseSource.transform.SetParent(transform);
        CreateFxSources();
    }

    void OnEnable()
    {
        FishJam.UI.GameSettings.Changed += ApplyMusicSetting;
    }

    void OnDisable()
    {
        FishJam.UI.GameSettings.Changed -= ApplyMusicSetting;
    }

    private void ApplyMusicSetting()
    {
        soundSource.mute = !FishJam.UI.GameSettings.Music;
    }

    void Start()
    {
        ApplyMusicSetting();
    }

    // Background music is optional: no clips assigned means no music. It starts once the loading screen is done.
    public void StartMusic()
    {
        if (soundAus.Length > 0 && !soundSource.isPlaying)
        {
            PlaySound((SoundID)Random.Range(0, soundAus.Length));
        }
    }

    public void PlaySound(SoundID ID)
    {
        soundSource.clip = soundAus[(int)ID];
        soundSource.Play();
    }

    public void PlayFx(FxID ID)
    {
        PlayFx(ID, DEFAULT_PITCH);
    }

    // Plays a one-shot effect. Clips are optional while art/audio is placeholder: an unassigned slot stays silent.
    public void PlayFx(FxID ID, float pitch)
    {
        int index = (int)ID;
        if (!FishSort.PlayerData.SoundOn || index >= fxAus.Length || fxAus[index] == null)
        {
            return;
        }

        fxSource[index].pitch = pitch;
        fxSource[index].PlayOneShot(fxAus[index]);
    }

    // Praise voice by index (same order as praiseAus). One voice at a time: a new one replaces the old.
    public void PlayPraise(int index)
    {
        if (!FishSort.PlayerData.SoundOn || praiseAus == null || praiseAus.Length == 0)
        {
            return;
        }

        AudioClip clip = praiseAus[Mathf.Clamp(index, 0, praiseAus.Length - 1)];
        if (clip == null)
        {
            return;
        }

        praiseSource.Stop();
        praiseSource.PlayOneShot(clip);
    }

    private void CreateFxSources()
    {
        for (int i = 0; i < fxSource.Length; i++)
        {
            AudioSource source = new GameObject(((FxID)i).ToString()).AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.transform.SetParent(transform);
            fxSource[i] = source;
        }
    }
}
