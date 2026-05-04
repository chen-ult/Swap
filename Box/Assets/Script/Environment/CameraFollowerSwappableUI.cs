using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody2D))]
public class CameraFollowerSwappableUI : MonoBehaviour, IMomentumSwappable
{
    [Header("UI跟随设置")]
    public Camera mainCamera;
    public Vector3 followOffset = new Vector3(0, 0, 0); // 与相机的偏移量
    public float followSpeed = 8f; // 跟随速度（平滑）

    [Header("当前状态")]
    public float lifeTime;
    public bool isPhysical;
    public bool isReturning = false;

    private Vector3 lockedReturnPos;
    private float returnTimer = 0f;
    public float maxReturnTime = 3f; // 最大回归时间，防止卡死

    [Header("物理与碰撞")]
    public Rigidbody2D rb;
    [Tooltip("用于变为实体后的物理碰撞体")]
    public Collider2D physicalCollider;
    [Tooltip("用于在UI模式下仍能被射线点击交换的触发器")]
    public Collider2D clickCollider;

    [Header("视觉与颜色设置")]
    public float entranceDelay = 0.5f; // 开场时的出场延迟
    private bool isFirstEntrance = true;

    public SpriteRenderer sr;
    public Color bulletTimeHintColor = Color.yellow;
    public Color selectedColor = Color.cyan;
    public Color successColor = Color.green;
    private Color originalColor = Color.white;
    private Vector3 originalScale = Vector3.one;

    private Sequence uiAnimSeq; // 用于UI模式的动画序列
    private bool isFloating = false;

    [Header("生命倒计文本显示")]
    public Vector3 textOffset = new Vector3(0, 1.2f, 0);
    public Color textColor = Color.white;
    private TextMesh lifeTimeText;
    private GameObject textObj;

    private bool isInBulletTime = false;
    private bool isCurrentlySelected = false;
    private bool isFlashing = false;

    void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        // 确保能获取到物理碰撞体（防止变量重命名导致引用丢失，从而在UI态一直开着碰撞导致穿墙抖动）
        if (physicalCollider == null) physicalCollider = GetComponent<Collider2D>();

        if (sr != null)
        {
            originalColor = sr.color;
        }
        originalScale = transform.localScale;

        // ====== 初始化倒计时文本 ======
        textObj = new GameObject("LifeTimeDisplay_" + gameObject.name);
        textObj.transform.SetParent(null); // 防止受父物体旋转/缩放影响
        
        lifeTimeText = textObj.AddComponent<TextMesh>();
        lifeTimeText.anchor = TextAnchor.MiddleCenter;
        lifeTimeText.alignment = TextAlignment.Center;
        lifeTimeText.characterSize = 0.05f;
        lifeTimeText.fontSize = 80;
        lifeTimeText.color = textColor;
        lifeTimeText.text = "";
        lifeTimeText.gameObject.SetActive(false);

        MeshRenderer meshRenderer = textObj.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = "Ground"; // 保持与其他文本同层级
            meshRenderer.sortingOrder = 80;
        }

        // 确保点击触发器存在
        if (clickCollider == null)
        {
            clickCollider = gameObject.AddComponent<CircleCollider2D>();
            clickCollider.isTrigger = true;
        }

        // 把物理关闭逻辑放到 Awake 里，但入场动画留到 Start 里和标题绝对同频触发
        isPhysical = false;
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
        if (physicalCollider != null) physicalCollider.enabled = false;

        transform.localScale = Vector3.zero;
        if (sr != null) 
        {
            Color c = sr.color;
            c.a = 0;
            sr.color = c;
        }
    }

    private void Start()
    {
        SwitchToUIMode();
    }

    private void OnDestroy()
    {
        if (uiAnimSeq != null) uiAnimSeq.Kill();
        transform.DOKill();
        if (textObj != null)
        {
            textObj.transform.DOKill(); // 清理 DOTween 动画
            Destroy(textObj);
        }
    }

    private void OnEnable()
    {
        MomentumSwapManager.OnBulletTimeToggled += HandleBulletTimeToggle;
    }

    private void OnDisable()
    {
        MomentumSwapManager.OnBulletTimeToggled -= HandleBulletTimeToggle;
    }

    void Update()
    {
        if (isPhysical)
        {
            CountDownLifeTime();
            UpdateTextDisplay();
        }
        else if (lifeTimeText != null && lifeTimeText.gameObject.activeSelf)
        {
            textObj.transform.DOKill(); // 隐藏时杀掉循环动画
            lifeTimeText.gameObject.SetActive(false);
        }
    }

    void UpdateTextDisplay()
    {
        if (lifeTimeText == null) return;

        if (!lifeTimeText.gameObject.activeSelf)
        {
            lifeTimeText.gameObject.SetActive(true);
            lifeTimeText.color = textColor; // 重置文字颜色

            // 可爱的果冻弹出效果
            textObj.transform.localScale = Vector3.zero;
            textObj.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).OnComplete(() => {
                // 弹出后无缝接上连续的呼吸缩放动画（变得肉肉的）
                textObj.transform.DOScale(new Vector3(1.15f, 0.9f, 1f), 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            });
        }

        // 保留简单的平滑上下浮动，配合缩放会显得像悬浮生物一样有生机
        float floatOffsetY = Mathf.Sin(Time.time * 4f) * 0.15f;
        textObj.transform.position = transform.position + textOffset + new Vector3(0, floatOffsetY, 0);
        textObj.transform.rotation = Quaternion.identity;

        // 快到极限时间时，变红加以警告提示，让玩家知道要赶紧跳走
        if (lifeTime <= 1f)
            lifeTimeText.color = Color.red;

        lifeTimeText.text = lifeTime.ToString("F1"); // 保留一位小数
    }

    void FixedUpdate()
    {
        if (isReturning)
        {
            returnTimer += Time.fixedDeltaTime;

            Vector2 newPos = Vector2.MoveTowards(rb.position, (Vector2)lockedReturnPos, followSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);

            if (Vector2.Distance(rb.position, (Vector2)lockedReturnPos) < 0.05f || returnTimer >= maxReturnTime)
            {
                isReturning = false;
                SwitchToUIMode();
            }
        }
    }

    void LateUpdate()
    {
        if (!isPhysical && !isReturning)
        {
            FollowCamera();
        }
    }

    void FollowCamera()
    {
        if (mainCamera == null) return;

        // 如果意外丢失了父级，再认一次父子集
        if (transform.parent != mainCamera.transform)
        {
            transform.SetParent(mainCamera.transform, true);
        }

        // 以完美的局部坐标绑定，实现0延迟、绝对不抖动的UI视觉效果！
        Vector3 targetLocalPos = followOffset;

        if (isFloating)
        {
            float floatOffset = Mathf.Sin((Time.time) * Mathf.PI / 1.5f) * 0.1f;
            targetLocalPos.y += floatOffset;
        }

        // 保留进入 UI 态时自身相对相机的原本层级深度，防止受 followOffset.z 影响导致 Z 层错乱看不见
        targetLocalPos.z = transform.localPosition.z;

        transform.localPosition = targetLocalPos;
        // 注意：这里绝不能每帧将 localRotation 写死为 Quaternion.identity，
        // 否则会百分百吃掉基于 DOTween DOPunchRotation 制作的开场摇摆“闪出”特效！
    }

    void CountDownLifeTime()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0)
        {
            StartReturn();
        }
    }

    void StartReturn()
    {
        isPhysical = false;
        isReturning = true;
        returnTimer = 0f;

        transform.SetParent(null); // 回归途中剥离父子关系，按照世界坐标系精确停靠

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (physicalCollider != null)
            physicalCollider.enabled = true;

        if (mainCamera != null)
        {
            lockedReturnPos = mainCamera.transform.position + followOffset;
            lockedReturnPos.z = transform.position.z;
        }
        else
        {
            lockedReturnPos = transform.position;
        }
    }

    public void SwitchToUIMode()
    {
        isPhysical = false;
        isReturning = false;
        isFloating = false;
        lifeTime = 0;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.interpolation = RigidbodyInterpolation2D.None; // 彻底关闭插值防止跟随相机时物理引擎拖后腿抖动
        }

        if (physicalCollider != null)
            physicalCollider.enabled = false;

        if (clickCollider != null)
            clickCollider.enabled = true;

        // 播放和 InteractiveLevelTitle 一致的可爱入场动画
        if (uiAnimSeq != null) uiAnimSeq.Kill();
        transform.DOKill();

        transform.localScale = Vector3.zero;
        transform.localRotation = Quaternion.identity; // 保证是从绝对正的角度开始播放摇摆动画
        if (sr != null) 
        {
            Color c = sr.color;
            c.a = 0;
            sr.color = c;
        }

        uiAnimSeq = DOTween.Sequence();

        // 如果是游戏刚开始第一次进场，加入和标题一样的延迟
        if (isFirstEntrance)
        {
            uiAnimSeq.AppendInterval(entranceDelay);
            isFirstEntrance = false;
        }

        // 1. Q弹变大
        uiAnimSeq.Append(transform.DOScale(originalScale, 0.8f).SetEase(Ease.OutBack, 2.5f));
        // 伴随透明度淡入
        if (sr != null) uiAnimSeq.Join(sr.DOFade(originalColor.a, 0.5f));
        // 2. 长到最大时轻微摇摆打招呼
        uiAnimSeq.Append(transform.DOPunchRotation(new Vector3(0, 0, 12f), 0.6f, 6, 0.5f));

        // 3. 动画结束开始与标题一致的浮动
        uiAnimSeq.AppendCallback(() => {
            isFloating = true;
        });
    }

    public void SwitchToPhysicalMode(Vector2 velocity)
    {
        isPhysical = true;
        isReturning = false;
        isFloating = false;

        transform.SetParent(null); // 脱离相机关系，作为一个自由的世界对象

        // 打断 UI 状态下的可爱表现，恢复原本实体形态
        if (uiAnimSeq != null) uiAnimSeq.Kill();
        transform.DOKill();
        transform.localScale = originalScale;
        transform.localRotation = Quaternion.identity;
        if (sr != null)
        {
            Color c = sr.color;
            c.a = originalColor.a;
            sr.color = c;
        }

        lifeTime = velocity.magnitude;
        if (lifeTime <= 0.1f) lifeTime = 1f;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = velocity;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 恢复抛掷时的物理平滑表现
        }

        if (physicalCollider != null)
            physicalCollider.enabled = true;
    }

    public Rigidbody2D MomentumRigidbody => rb;

    public void ApplyMomentum(Vector2 momentum)
    {
        SwitchToPhysicalMode(momentum);
    }

    public void SetSelectedVisual(bool isSelected)
    {
        isCurrentlySelected = isSelected;

        if (sr != null && !isFlashing)
        {
            if (isSelected)
                sr.color = selectedColor;
            else
                sr.color = isInBulletTime ? bulletTimeHintColor : originalColor;
        }
    }

    public void FlashSuccess()
    {
        if (sr != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(FlashRoutine());
        }
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        isFlashing = true;
        sr.color = successColor;
        yield return new WaitForSecondsRealtime(0.25f);
        isFlashing = false;

        if (!isCurrentlySelected)
        {
            sr.color = isInBulletTime ? bulletTimeHintColor : originalColor;
        }
    }

    private void HandleBulletTimeToggle(bool isBulletTimeActive)
    {
        isInBulletTime = isBulletTimeActive;

        if (sr == null || isCurrentlySelected || isFlashing) return;

        if (isBulletTimeActive)
        {
            sr.color = bulletTimeHintColor;
        }
        else
        {
            sr.color = originalColor;
        }
    }
}