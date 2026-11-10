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
}


public class SoundManager : Singleton<SoundManager>
{
    private const float DEFAULT_PITCH = 1f;
    private const float MUSIC_START_DELAY = 1f;

    [SerializeField] private AudioClip[] soundAus;
    [SerializeField] private AudioClip[] fxAus;

    private AudioSource soundSource;
    private AudioSource[] fxSource = new AudioSource[Utilities.GetEnumCount<FxID>()];

    void Awake()
    {
        RegisterSingleton(this);
        soundSource = gameObject.AddComponent<AudioSource>();
        soundSource.loop = true;
        CreateFxSources();
    }

    void Start()
    {
        StartCoroutine(PlayMusicAfterDelay());
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

    private IEnumerator PlayMusicAfterDelay()
    {
        yield return new WaitForSeconds(MUSIC_START_DELAY);
        // Background music is optional: no clips assigned means no music.
        if (soundAus.Length > 0)
        {
            PlaySound((SoundID)Random.Range(0, soundAus.Length));
        }
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
