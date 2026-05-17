using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class UIButtonMomentumStore : MonoBehaviour
{
    [Header("视觉效果")]
    [Tooltip("子弹时间下的高亮颜色")]
    public Color bulletTimeColor = Color.yellow;
    [Tooltip("选中时的颜色")]
    public Color selectedColor = Color.cyan;
    [Tooltip("正常颜色")]
    public Color normalColor = Color.white;

    [Header("储速设置")]
    public float maxStoredSpeed = 50f;
    public float minShowSpeed = 0.5f;
    [HideInInspector] public Vector2 storedVelocity = Vector2.zero;

    [Header("可视化圆环箭头")]
    public float ringRadius = 1.5f;
    private LineRenderer ringLine;
    private LineRenderer arrowLine;

    [Header("悬浮文字")]
    public Vector3 textOffset = new Vector3(0, 1.2f, 0);
    public Color textColor = Color.white;

    private Rigidbody2D rb;
    private TextMesh speedText;
    private GameObject textObj;
    private float lastSpeed = -1f;
    private Sequence textBounceSeq;

    private UnityEngine.UI.Image btnImage;
    private TMPro.TextMeshProUGUI tmpText;

    private bool isInBulletTime = false;
    private bool isCurrentlySelected = false;

    void Awake()
    {
        // UI层级关键设置
        GetComponent<Collider2D>().isTrigger = false;
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        btnImage = GetComponent<UnityEngine.UI.Image>();
        tmpText = GetComponentInChildren<TMPro.TextMeshProUGUI>();

        // 初始化文字、圆环箭头（完全照搬传送门）
        InitSpeedText();
        InitRingAndArrow();
    }

    private void OnEnable()
    {
        MomentumSwapManager.OnBulletTimeToggled += HandleBulletTimeToggle;
    }

    private void OnDisable()
    {
        MomentumSwapManager.OnBulletTimeToggled -= HandleBulletTimeToggle;
    }

    private void HandleBulletTimeToggle(bool active)
    {
        isInBulletTime = active;
        UpdateColor();
    }

    private void UpdateColor()
    {
        Color c = normalColor;
        if (isCurrentlySelected) c = selectedColor;
        else if (isInBulletTime) c = bulletTimeColor;

        if (btnImage != null) btnImage.color = c;
        if (tmpText != null) tmpText.color = c;
    }

    void Update()
    {
        bool hitMaxLimitThisFrame = false;

        // 【废弃每帧自然物理累加，改用 IMomentumSwappable 确切获取系统传递的动量】
        if (storedVelocity.magnitude >= maxStoredSpeed)
        {
            hitMaxLimitThisFrame = true;
            storedVelocity = Vector2.ClampMagnitude(storedVelocity, maxStoredSpeed);
        }

        UpdateVisual(hitMaxLimitThisFrame);
    }

    // 初始化速度文字
    void InitSpeedText()
    {
        textObj = new GameObject("UIButtonSpeedText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = textOffset;

        speedText = textObj.AddComponent<TextMesh>();
        speedText.anchor = TextAnchor.MiddleCenter;
        speedText.alignment = TextAlignment.Center;
        speedText.characterSize = 0.05f;
        speedText.fontSize = 80;
        speedText.color = textColor;
        speedText.text = "0";
        speedText.gameObject.SetActive(false);

        MeshRenderer meshRend = textObj.GetComponent<MeshRenderer>();
        meshRend.sortingLayerName = "UI";
        meshRend.sortingOrder = 100;
    }

    // 初始化圆环+箭头
    void InitRingAndArrow()
    {
        GameObject ringObj = new GameObject("UIButtonRing");
        ringObj.transform.SetParent(transform);
        ringObj.transform.localPosition = Vector3.zero;

        ringLine = ringObj.AddComponent<LineRenderer>();
        ringLine.material = new Material(Shader.Find("Sprites/Default"));
        ringLine.startColor = Color.white;
        ringLine.endColor = Color.white;
        ringLine.startWidth = 0.1f;
        ringLine.endWidth = 0.1f;
        ringLine.useWorldSpace = false;
        ringLine.sortingLayerName = "UI";
        ringLine.sortingOrder = 90;

        int segments = 40;
        ringLine.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            ringLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * ringRadius, Mathf.Sin(angle) * ringRadius, 0));
        }
        ringLine.enabled = false;

        GameObject arrowObj = new GameObject("UIButtonArrow");
        arrowObj.transform.SetParent(ringObj.transform);
        arrowObj.transform.localPosition = Vector3.zero;

        arrowLine = arrowObj.AddComponent<LineRenderer>();
        arrowLine.material = new Material(Shader.Find("Sprites/Default"));
        arrowLine.startColor = Color.white;
        arrowLine.endColor = new Color(1, 1, 1, 0);
        arrowLine.startWidth = 0.5f;
        arrowLine.endWidth = 0f;
        arrowLine.useWorldSpace = false;
        arrowLine.positionCount = 2;
        arrowLine.sortingLayerName = "UI";
        arrowLine.sortingOrder = 91;
        arrowLine.enabled = false;
    }

    // 更新文字、圆环、箭头显示
    void UpdateVisual(bool hitMax)
    {
        float currentSpeed = storedVelocity.magnitude;

        // 圆环箭头显示逻辑
        if (currentSpeed >= minShowSpeed)
        {
            ringLine.enabled = true;
            arrowLine.enabled = true;

            Vector2 dir = storedVelocity.normalized;
            Vector3 arrowStart = (Vector3)(dir * ringRadius);
            float arrowLength = 0.5f + (currentSpeed / maxStoredSpeed) * 2f;
            Vector3 arrowEnd = arrowStart + (Vector3)(dir * arrowLength);

            arrowLine.SetPosition(0, arrowStart);
            arrowLine.SetPosition(1, arrowEnd);
        }
        else
        {
            ringLine.enabled = false;
            arrowLine.enabled = false;
        }

        // 速度文字+弹跳动画
        if (currentSpeed < minShowSpeed)
        {
            speedText.gameObject.SetActive(false);
            lastSpeed = currentSpeed;
        }
        else
        {
            speedText.gameObject.SetActive(true);
            bool speedChanged = Mathf.Abs(currentSpeed - lastSpeed) > 0.1f;

            if (speedChanged || hitMax)
            {
                speedText.text = currentSpeed >= maxStoredSpeed ? maxStoredSpeed.ToString() : currentSpeed.ToString("F1");
                speedText.color = currentSpeed >= maxStoredSpeed ? Color.red : textColor;

                // 超限弹跳动画
                if (hitMax)
                {
                    textBounceSeq?.Kill();
                    textBounceSeq = DOTween.Sequence();
                    textBounceSeq.Append(textObj.transform.DOScale(1.5f, 0.1f));
                    textBounceSeq.Append(textObj.transform.DOScale(1f, 0.2f));
                }

                lastSpeed = currentSpeed;
            }
        }
    }

    void OnDestroy()
    {
        textBounceSeq?.Kill();
        Destroy(textObj);
        Destroy(ringLine.gameObject);
    }

    // 外部调用：清空储存速度
    public void ClearStoredVelocity()
    {
        storedVelocity = Vector2.zero;
    }

    // ====== IMomentumSwappable 接口实现 ======
    public Rigidbody2D MomentumRigidbody => rb;

    public void ApplyMomentum(Vector2 momentum)
    {
        if (momentum.magnitude < 0.1f) return;

        storedVelocity = momentum;
        rb.linearVelocity = Vector2.zero; // 消耗掉附在本体刚体上的实际物理动量

        FlashSuccess();
    }

    public void SetSelectedVisual(bool isSelected)
    {
        isCurrentlySelected = isSelected;
        UpdateColor();
    }

    public void FlashSuccess()
    {
        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0), 0.3f, 5, 1f);
    }
}
