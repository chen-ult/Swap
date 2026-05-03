using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class SwappableObject : MonoBehaviour, IMomentumSwappable
{
    #region 组件引用
    [HideInInspector] public Rigidbody2D rb;
    private SpriteRenderer sr;
    private Color originalColor; // 原始颜色
    #endregion

    #region 状态变量
    private bool isCurrentlySelected; // 是否被选中
    private bool isInBulletTime;      // 是否在子弹时间
    private bool isFlashing;          // 是否在闪烁（成功反馈）
    #endregion

    #region 可配置参数
    [Header("视觉效果")]
    [Tooltip("子弹时间下的高亮颜色")]
    public Color bulletTimeHintColor = Color.yellow;
    [Tooltip("选中时的颜色")]
    public Color selectedColor = Color.cyan;
    [Tooltip("操作成功的闪烁颜色")]
    public Color successColor = Color.green;

    [Header("速度文本显示")]
    [Tooltip("文本在物体上方的偏移量")]
    public Vector3 textOffset = new Vector3(0, 1.2f, 0);
    [Tooltip("文本颜色")]
    public Color textColor = Color.white;
    [Tooltip("最小显示速度（低于此值不显示）")]
    public float minShowSpeed = 0.5f;
    #endregion

    #region 速度文本相关
    private TextMesh speedText;       // 速度文本组件
    private GameObject textObj;       // 文本物体
    private float lastSpeed = -1f;    // 上一帧速度（避免频繁更新）
    private Sequence textBounceSeq;   // 文本弹跳动画
    #endregion

    #region 生命周期
    private void Awake()
    {
        // 初始化组件
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

        if (sr == null)
        {
            Debug.LogWarning("未找到SpriteRenderer组件，视觉反馈失效", this);
        }
        else
        {
            originalColor = sr.color;
        }

        // 初始化速度文本
        InitSpeedText();
    }

    private void OnEnable()
    {
        // 注册子弹时间事件
        MomentumSwapManager.OnBulletTimeToggled += HandleBulletTimeToggle;
    }

    private void OnDisable()
    {
        // 注销事件（防止内存泄漏）
        MomentumSwapManager.OnBulletTimeToggled -= HandleBulletTimeToggle;
    }

    private void OnDestroy()
    {
        // 销毁动画和文本物体
        textBounceSeq?.Kill();
        if (textObj != null)
        {
            textObj.transform.DOKill();
            Destroy(textObj);
        }
        transform.DOKill();
    }

    private void Update()
    {
        // 更新速度文本
        UpdateSpeedText();

        // 重置颜色（非选中/非子弹时间/非闪烁）
        ResetColorIfIdle();
    }
    #endregion

    #region 初始化
    /// <summary>
    /// 初始化速度显示文本
    /// </summary>
    private void InitSpeedText()
    {
        // 创建文本物体（不设为子物体，避免旋转干扰）
        textObj = new GameObject("SpeedDisplay_" + gameObject.name);
        textObj.transform.SetParent(null);
        textObj.transform.position = transform.position + textOffset;

        // 初始化TextMesh
        speedText = textObj.AddComponent<TextMesh>();
        speedText.anchor = TextAnchor.MiddleCenter;
        speedText.alignment = TextAlignment.Center;
        speedText.characterSize = 0.05f;
        speedText.fontSize = 80;
        speedText.color = textColor;
        speedText.text = "0";
        speedText.gameObject.SetActive(false);

        // 设置渲染层级
        MeshRenderer meshRenderer = textObj.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = "Ground";
            meshRenderer.sortingOrder = 80; // 确保文本在物体上方
        }
    }
    #endregion

    #region 速度文本更新
    /// <summary>
    /// 更新速度文本的显示和动画
    /// </summary>
    private void UpdateSpeedText()
    {
        if (rb == null || speedText == null) return;

        // 文本跟随物体（带轻微上下浮动）
        if (speedText.gameObject.activeSelf)
        {
            float floatOffsetY = Mathf.Sin(Time.time * 3f) * 0.15f;
            textObj.transform.position = transform.position + textOffset + new Vector3(0, floatOffsetY, 0);
            textObj.transform.rotation = Quaternion.identity; // 强制水平
        }

        // 获取当前速度
        float currentSpeed = rb.linearVelocity.magnitude;

        // 速度过低：隐藏文本
        if (currentSpeed < minShowSpeed)
        {
            if (speedText.gameObject.activeSelf)
                speedText.gameObject.SetActive(false);
            lastSpeed = currentSpeed;
            return;
        }

        // 速度足够：显示文本
        if (!speedText.gameObject.activeSelf)
            speedText.gameObject.SetActive(true);

        // 速度变化超过阈值才更新（避免频繁刷新）
        if (Mathf.Abs(currentSpeed - lastSpeed) > 0.1f)
        {
            speedText.text = currentSpeed.ToString("F1"); // 保留1位小数

            // 速度突变时播放弹跳动画
            if (currentSpeed - lastSpeed > 5f)
            {
                PlayTextBounceAnimation();
            }

            lastSpeed = currentSpeed;
        }
    }

    /// <summary>
    /// 播放文本弹跳动画
    /// </summary>
    private void PlayTextBounceAnimation()
    {
        textBounceSeq?.Kill(); // 停止旧动画
        textBounceSeq = DOTween.Sequence();
        textBounceSeq.Append(textObj.transform.DOScale(new Vector3(1.3f, 0.7f, 1f), 0.1f));
        textBounceSeq.Append(textObj.transform.DOScale(new Vector3(0.8f, 1.2f, 1f), 0.1f));
        textBounceSeq.Append(textObj.transform.DOScale(Vector3.one, 0.1f));
    }
    #endregion

    #region 颜色和视觉反馈
    /// <summary>
    /// 处理子弹时间切换事件
    /// </summary>
    private void HandleBulletTimeToggle(bool isBulletTimeActive)
    {
        isInBulletTime = isBulletTimeActive;

        if (sr == null || isCurrentlySelected) return;

        // 切换子弹时间颜色
        sr.color = isBulletTimeActive ? bulletTimeHintColor : originalColor;
    }

    /// <summary>
    /// 重置空闲状态的颜色
    /// </summary>
    private void ResetColorIfIdle()
    {
        if (sr == null || isInBulletTime || isCurrentlySelected || isFlashing) return;

        sr.color = originalColor;
    }

    /// <summary>
    /// 设置选中状态的视觉反馈
    /// </summary>
    public void SetSelectedVisual(bool isSelected)
    {
        isCurrentlySelected = isSelected;

        if (sr == null) return;

        sr.color = isSelected ? selectedColor : (isInBulletTime ? bulletTimeHintColor : originalColor);
    }

    /// <summary>
    /// 播放成功闪烁动画
    /// </summary>
    public void FlashSuccess()
    {
        if (sr == null || !gameObject.activeInHierarchy) return;

        StartCoroutine(FlashSuccessCoroutine());
    }

    private System.Collections.IEnumerator FlashSuccessCoroutine()
    {
        isFlashing = true;
        sr.color = successColor;
        yield return new WaitForSecondsRealtime(0.25f); // 不受时间缩放影响
        isFlashing = false;

        // 恢复颜色（优先选中状态，其次子弹时间）
        sr.color = isCurrentlySelected ? selectedColor : (isInBulletTime ? bulletTimeHintColor : originalColor);
    }
    #endregion

    #region IMomentumSwappable 接口实现
    public Rigidbody2D MomentumRigidbody => rb;

    public void ApplyMomentum(Vector2 momentum)
    {
        if (rb != null)
        {
            rb.linearVelocity = momentum;
        }
    }
    #endregion
}