using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CameraFollowerSwappableUI : MonoBehaviour, IMomentumSwappable
{
    [Header("核心设置")]
    public Camera mainCamera;
    public Vector3 followOffset = new Vector3(0, 0, 1);
    public float followSpeed = 8f;

    [Header("物理状态")]
    public float lifeTime;
    public bool isPhysical;

    [Header("引用")]
    public Rigidbody2D rb;
    public Collider2D col;          // 物理碰撞（开关）
    public Collider2D clickCollider;// 专用可点击碰撞（永远开启）
    public SpriteRenderer sr;

    private Vector2 receivedVelocity;

    void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        // 专门给鼠标点击用的碰撞体（永远开启）
        if (clickCollider == null)
        {
            clickCollider = gameObject.AddComponent<CircleCollider2D>();
            clickCollider.isTrigger = true;
            clickCollider.enabled = true; // 关键！一直开着才能被点
        }

        SwitchToUIMode();
    }

    void Update()
    {
        if (!isPhysical)
            FollowCamera();
        else
            CountDownLifeTime();
    }

    void FollowCamera()
    {
        if (mainCamera == null) return;
        Vector3 targetPos = mainCamera.transform.position + followOffset;
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
    }

    void CountDownLifeTime()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0)
            SwitchToUIMode();
    }

    // ====================== UI 模式 ======================
    public void SwitchToUIMode()
    {
        isPhysical = false;
        lifeTime = 0;

        // 物理刚体关闭
        if (rb != null)
        {
            rb.simulated = false;
            rb.linearVelocity = Vector2.zero;
        }

        // 物理碰撞关闭，但点击碰撞保持开启
        if (col != null)
            col.enabled = false;
    }

    // ====================== 物理模式 ======================
    public void SwitchToPhysicalMode(Vector2 velocity)
    {
        isPhysical = true;
        lifeTime = velocity.magnitude;

        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = velocity;
        }

        if (col != null)
            col.enabled = true;
    }

    // ====================== 交换系统接口 ======================
    public Rigidbody2D MomentumRigidbody => rb;

    public void ApplyMomentum(Vector2 momentum)
    {
        receivedVelocity = momentum;
        SwitchToPhysicalMode(momentum);
    }

    public void SetSelectedVisual(bool isSelected)
    {
        if (sr == null) return;
        sr.color = isSelected ? Color.cyan : Color.white;
    }

    public void FlashSuccess()
    {
        if (sr == null) return;
        sr.color = Color.green;
        Invoke(nameof(ResetColor), 0.2f);
    }

    void ResetColor()
    {
        if (sr != null)
            sr.color = Color.white;
    }
}