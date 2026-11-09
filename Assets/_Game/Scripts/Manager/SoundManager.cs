using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


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
    // TODO: Add UserData when persistent audio settings are implemented.
    // [SerializeField] UserData userData;

    private const float DEFAULT_PITCH = 1f;

    private AudioSource soundSource;
    private AudioSource[] fxSource = new AudioSource[Utilities.GetEnumCount<FxID>()];

    [SerializeField] private AudioClip[] soundAus;
    [SerializeField] private AudioClip[] fxAus;

    private bool isLoaded = false;
    private int indexSound;

    public void Awake()
    {
        DontDestroyOnLoad(gameObject);

        soundSource = gameObject.AddComponent<AudioSource>();
        soundSource.loop = true;
    }

    private void Start()
    {
        Invoke(nameof(OnLoad), 1);
    }

    private void OnLoad()
    {
        if (soundAus.Length > 0)
        {
            isLoaded = true;

            indexSound = Random.Range(0, soundAus.Length);
            PlaySound((SoundID)indexSound);
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

    // Plays a one-shot effect. Missing clips are skipped so placeholder builds stay silent instead of erroring.
    public void PlayFx(FxID ID, float pitch)
    {
        int index = (int)ID;
        if (!FishSort.PlayerData.SoundOn || index >= fxAus.Length || fxAus[index] == null)
        {
            return;
        }

        if (fxSource[index] == null)
        {
            fxSource[index] = new GameObject(ID.ToString()).AddComponent<AudioSource>();
            fxSource[index].playOnAwake = false;
            fxSource[index].loop = false;
            fxSource[index].transform.SetParent(transform);
        }

        fxSource[index].pitch = pitch;
        fxSource[index].PlayOneShot(fxAus[index]);
    }

    public void ChangeSound(SoundID ID, float time)
    {
        
    }

}
