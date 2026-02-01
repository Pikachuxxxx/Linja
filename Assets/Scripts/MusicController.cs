using UnityEngine;
using System.Collections;

public class MusicController : MonoBehaviour
{
    public static MusicController Instance;

    [Header("Audio Sources")]
    public AudioSource sourceA;
    public AudioSource sourceB;

    [Header("Music Clips")]
    public AudioClip normalMusic;
    public AudioClip stealthMusic;
    public AudioClip actionMusic;

    [Header("Settings")]
    public float fadeDuration = 1.5f;
    public float maxVolume = 0.8f;

    private AudioSource currentSource;
    private AudioSource nextSource;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentSource = sourceA;
        nextSource = sourceB;

        PlayNormal();
    }

    // -------------------------
    // PUBLIC API
    // -------------------------

    public void PlayNormal()
    {
        SwitchMusic(normalMusic);
    }

    public void PlayStealth()
    {
        SwitchMusic(stealthMusic);
    }

    public void PlayAction()
    {
        SwitchMusic(actionMusic);
    }

    // -------------------------
    // CORE LOGIC
    // -------------------------

    private void SwitchMusic(AudioClip newClip)
    {
        if (currentSource.clip == newClip)
            return;

        StopAllCoroutines();
        StartCoroutine(Crossfade(newClip));
    }

    private IEnumerator Crossfade(AudioClip newClip)
    {
        nextSource.clip = newClip;
        nextSource.volume = 0f;
        nextSource.Play();

        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float blend = t / fadeDuration;

            currentSource.volume = Mathf.Lerp(maxVolume, 0f, blend);
            nextSource.volume = Mathf.Lerp(0f, maxVolume, blend);

            yield return null;
        }

        currentSource.Stop();

        // Swap sources
        AudioSource temp = currentSource;
        currentSource = nextSource;
        nextSource = temp;
    }
}
