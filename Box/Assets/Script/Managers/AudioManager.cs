using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer / Groups")]
    public AudioMixer masterMixer;          // ��ѡ������ͨ������������������
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup uiGroup;

    [Header("Pool settings")]
    public int sfxPoolSize = 12;

    // Sources
    private AudioSource musicSource;
    private AudioSource uiSource;
    private AudioSource[] sfxPool;
    private int sfxPoolIndex = 0;
    private float globalPlaybackPitch = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CreateSources()
    {
        // Music source (looping)
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.outputAudioMixerGroup = musicGroup;
        musicSource.pitch = globalPlaybackPitch;

        // UI source (non-spatial)
        uiSource = gameObject.AddComponent<AudioSource>();
        uiSource.playOnAwake = false;
        uiSource.loop = false;
        uiSource.spatialBlend = 0f;
        uiSource.outputAudioMixerGroup = uiGroup;
        uiSource.pitch = globalPlaybackPitch;

        // SFX pool
        sfxPool = new AudioSource[Mathf.Max(1, sfxPoolSize)];
        for (int i = 0; i < sfxPool.Length; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f; // default 2D; change per-play if needed
            src.outputAudioMixerGroup = sfxGroup;
            src.pitch = globalPlaybackPitch;
            sfxPool[i] = src;
        }
    }

    // MUSIC
    public void PlayMusic(AudioClip clip, float volume = 1f, bool loop = true)
    {
        if (clip == null) return;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = Mathf.Clamp01(volume);
        musicSource.pitch = globalPlaybackPitch;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    // UI
    public void PlayUi(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        uiSource.pitch = globalPlaybackPitch;
        uiSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // SFX (non-positioned)
    public void PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        var src = sfxPool[sfxPoolIndex];
        sfxPoolIndex = (sfxPoolIndex + 1) % sfxPool.Length;
        src.pitch = pitch * globalPlaybackPitch;
        src.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // SFX at world position (keeps mixer routing)
    public void PlaySfxAtPosition(AudioClip clip, Vector3 position, float volume = 1f, bool spatialize = false, float minDistance = 1f, float maxDistance = 50f)
    {
        if (clip == null) return;
        GameObject go = new GameObject("SfxAt_" + clip.name);
        go.transform.position = position;
        var src = go.AddComponent<AudioSource>();
        src.outputAudioMixerGroup = sfxGroup;
        src.clip = clip;
        src.pitch = globalPlaybackPitch;
        src.spatialBlend = spatialize ? 1f : 0f;
        if (spatialize)
        {
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.rolloffMode = AudioRolloffMode.Linear;
        }
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }

    // Mixer helper (assumes you exposed a float parameter like "MasterVolume" etc.)
    // volumeLinear: 0..1
    public void SetMixerVolume(string exposedParamName, float volumeLinear)
    {
        if (masterMixer == null) return;
        float v = Mathf.Clamp01(volumeLinear);
        float dB = (v <= 0f) ? -80f : Mathf.Log10(v) * 20f;
        masterMixer.SetFloat(exposedParamName, dB);
    }

    // Optional: convenience methods
    public void SetMusicVolume(float linear) => SetMixerVolume("MusicVolume", linear);
    public void SetSfxVolume(float linear) => SetMixerVolume("SfxVolume", linear);
    public void SetUiVolume(float linear)  => SetMixerVolume("UiVolume", linear);

    public void SetGlobalPlaybackPitch(float pitch)
    {
        globalPlaybackPitch = Mathf.Max(0.01f, pitch);

        var audioSources = Object.FindObjectsByType<AudioSource>();
        foreach (var source in audioSources)
        {
            if (source != null)
            {
                source.pitch = globalPlaybackPitch;
            }
        }
    }
}