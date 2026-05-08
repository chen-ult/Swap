using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BGMMManager : MonoBehaviour
{
    public static BGMMManager instance;

    [Header("背景音乐设置")]
    public AudioClip bgmClip;
    [Range(0f, 1f)] public float bgmVolume = 0.6f;

    private AudioSource audioSource;
    private double nextStartTime;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false; // 不循环，我们自己控制循环
        audioSource.volume = bgmVolume;
        audioSource.playOnAwake = false;
        audioSource.ignoreListenerPause = true;
    }

    void Start()
    {
        if (bgmClip != null)
        {
            nextStartTime = AudioSettings.dspTime + 0.1f;
            audioSource.clip = bgmClip;
            audioSource.PlayScheduled(nextStartTime);
        }
    }

    void Update()
    {
        // 手动循环播放，不受Time.timeScale影响
        if (!audioSource.isPlaying && bgmClip != null)
        {
            nextStartTime = AudioSettings.dspTime + 0.1f;
            audioSource.clip = bgmClip;
            audioSource.PlayScheduled(nextStartTime);
        }

        // 强制保持pitch为1
        audioSource.pitch = 1f;
    }

    public void PlayBGM()
    {
        if (bgmClip != null && !audioSource.isPlaying)
        {
            nextStartTime = AudioSettings.dspTime + 0.1f;
            audioSource.clip = bgmClip;
            audioSource.PlayScheduled(nextStartTime);
        }
    }

    public void StopBGM()
    {
        audioSource.Stop();
    }
}