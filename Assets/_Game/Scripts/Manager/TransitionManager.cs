using System;
using System.Collections;
using FishJam.UI;
using UnityEngine;

// The screen transition between menu and game (play, restart, home, next level, retry): the screen closes like an iris,
// the change is made while it is covered, a fish jumps out of its bowl, and the new screen is revealed.
public class TransitionManager : Singleton<TransitionManager>
{
    // How long the cover stays closed with the bowl jumping.
    private const float HOLD_TIME = 0.8f;

    public bool IsPlaying { get; private set; }

    void Awake()
    {
        RegisterSingleton(this);
    }

    // onCovered: the change of screens (runs while nothing can be seen). onReveal: runs just before the cover opens.
    public void Play(Action onCovered, Action onReveal)
    {
        if (!IsPlaying)
        {
            StartCoroutine(Run(onCovered, onReveal));
        }
    }

    private IEnumerator Run(Action onCovered, Action onReveal)
    {
        IsPlaying = true;
        Transition transition = UIManager.Ins.OpenUI<Transition>();
        yield return transition.Close();
        onCovered?.Invoke();
        // Changing screens may have closed every canvas, the transition included.
        transition.Show();
        transition.PlayBowl();
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < HOLD_TIME)
        {
            yield return null;
        }

        onReveal?.Invoke();
        yield return null;
        yield return transition.Open();
        IsPlaying = false;
    }
}
