using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(AudioSource))]
public class BoxSound2D : MonoBehaviour
{
    [Header("音效设置")]
    public AudioClip jumpSound;
    public AudioClip hitSound;

    [Header("音量设置")]
    [Range(0f, 1f)] public float jumpVolume = 0.7f;
    [Range(0f, 1f)] public float hitVolume = 0.5f;

    [Header("碰撞设置")]
    public float minHitVelocity = 0.3f;
    public float jumpTriggerSpeed = 2f;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private float lastYVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        // 初始化检查
        if (jumpSound == null) Debug.LogWarning("Jump Sound 未赋值！", this);
        if (hitSound == null) Debug.LogWarning("Hit Sound 未赋值！", this);
    }

    void Update()
    {
        // 检测弹起：Y轴速度从负变正，且速度超过阈值
        if (lastYVelocity < 0 && rb.linearVelocity.y > jumpTriggerSpeed)
        {
            Debug.Log("✅ 弹起音效触发，速度：" + rb.linearVelocity.y, this);
            PlayJumpSound();
        }

        lastYVelocity = rb.linearVelocity.y;
    }

    // 2D 碰撞检测
    void OnCollisionEnter2D(Collision2D collision)
    {
        float speed = collision.relativeVelocity.magnitude;
        Debug.Log("碰撞发生，速度：" + speed + "，物体：" + collision.gameObject.name, this);

        if (speed < minHitVelocity)
        {
            Debug.Log("❌ 速度低于阈值，不播放音效", this);
            return;
        }

        Debug.Log("✅ 碰撞音效触发", this);
        PlayHitSound();
    }

    void PlayJumpSound()
    {
        if (jumpSound != null)
            audioSource.PlayOneShot(jumpSound, jumpVolume*2);
    }

    void PlayHitSound()
    {
        if (hitSound != null)
            audioSource.PlayOneShot(hitSound, hitVolume*2);
    }
}