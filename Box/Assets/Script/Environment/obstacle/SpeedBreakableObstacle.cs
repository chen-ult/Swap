using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class SpeedBreakableObstacle : MonoBehaviour
{
    [Header("撞击要求")]
    [Tooltip("必须达到这个速度(Magnitude)以上的物体才能撞碎它")]
    public float requiredSpeed = 15f;

    [Header("悬浮文字要求提示")]
    [Tooltip("如果你想在场景里直观自由地移动文字位置，请在这里绑定一个空物体。如果不绑定，则默认使用下方的坐标偏移量生成。")]
    public Transform customTextPosition;

    [Tooltip("文字在障碍物上的坐标偏移量（如果上方没绑定就用这个）")]
    public Vector3 textOffset = new Vector3(0, 1.2f, 0);

    [Tooltip("是否开启自动上下漂浮的呼吸动画？如果你想把文字老老实实印在正中间/墙面上，取消勾选即可！")]
    public bool enableFloatingAnim = true;

    [Tooltip("要求文本的常态颜色")]
    public Color textColor = Color.white;
    [Tooltip("撞击速度不够时，文本警告闪烁的颜色")]
    public Color rejectColor = Color.red;

    [Header("破碎效果(DOTween版)")]
    [Tooltip("向指定方向弹出的相对位置及高度（例如 X:1, Y:2，表示向右上弹起）")]
    public Vector2 popOffset = new Vector2(0f, 2f);

    [Tooltip("第一段：向上弹飞花费的时间")]
    public float popDuration = 0.3f;

    [Tooltip("第二段：向下坠落到消失花费的时间")]
    public float fallDuration = 0.8f;

    [Tooltip("向下坠落脱离画面的掉落距离")]
    public float fallDistance = 15f;

    private Collider2D col;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool isBroken = false;

    private GameObject textObj;
    private TextMesh speedText;
    private Sequence textBounceSeq;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        // --- 动态创建要求文字 ---
        textObj = new GameObject("RequiredSpeedDisplay");
        textObj.transform.SetParent(transform); // 障碍物平时不动，作为子物体跟随即可

        // 如果用户在监视器里指定了一个摆放好的空物体节点，直接吸附到该节点。否则使用数字偏移量
        if (customTextPosition != null)
        {
            textObj.transform.position = customTextPosition.position;
        }
        else
        {
            textObj.transform.localPosition = textOffset;
        }

        speedText = textObj.AddComponent<TextMesh>();
        speedText.anchor = TextAnchor.MiddleCenter;
        speedText.alignment = TextAlignment.Center;

        speedText.characterSize = 0.05f; 
        speedText.fontSize = 80;
        speedText.color = textColor;
        speedText.text = "≥ " + requiredSpeed.ToString("F0");

        // 解决文字被遮挡
        MeshRenderer meshRenderer = textObj.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = "Ground";
            meshRenderer.sortingOrder = 100;
        }

        // 呼吸漂浮动画判断
        if (enableFloatingAnim)
        {
            // 在它当前初始被决定的高度基础上，向上呼吸浮动 0.15f
            float startLocalY = textObj.transform.localPosition.y;
            textObj.transform.DOLocalMoveY(startLocalY + 0.15f, 1f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        // 尝试获取刚体，如果没有就动态加一个。因为破碎效果需要刚体模拟抛物线下落
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // 初始状态下把刚体设为静态（Kinematic 或 Static），这样它就是一堵死墙
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
    }

    private void OnDestroy()
    {
        // 强力扼杀所有关联于这个物体和其文字子物体的 Tween 动画！防止空引用。
        if (textBounceSeq != null && textBounceSeq.IsActive())
        {
            textBounceSeq.Kill();
        }

        if (textObj != null)
        {
            textObj.transform.DOKill();
        }

        transform.DOKill();
    }

    private void Update()
    {
        // ------------- 防翻转机制 -------------
        // 如果你把墙壁在场景里水平翻转了，保证头上文字不出反字
        if (textObj != null)
        {
            float parentSignX = Mathf.Sign(transform.lossyScale.x);
            Vector3 currentScale = textObj.transform.localScale;
            textObj.transform.localScale = new Vector3(Mathf.Abs(currentScale.x) * parentSignX, currentScale.y, currentScale.z);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isBroken) return;

        Rigidbody2D hitterRb = collision.gameObject.GetComponent<Rigidbody2D>();

        if (hitterRb != null)
        {
            // Unity 物理引擎的小坑：在 OnCollisionEnter 里直接取物体的速度时，它其实已经撞墙停下来（或者由于反作用力减速）了！
            // 所以我们必须用 collision.relativeVelocity.magnitude （相对碰撞速度）才能真实读出它撞上来那一瞬间到底有多快。
            float hitSpeed = collision.relativeVelocity.magnitude;

            if (hitSpeed >= requiredSpeed)
            {
                BreakObstacle(hitterRb);
            }
            else
            {
                // 【生动反馈】：如果不小心碰到了但速度不够，文字会摇头闪红灯拒绝！
                ShowRejectFeedback();
            }
        }
    }

    private void ShowRejectFeedback()
    {
        if (textBounceSeq != null && textBounceSeq.IsActive()) return;

        textBounceSeq = DOTween.Sequence();
        // 因为 DOTween 官方默认未给 TextMesh 写 DOColor 的扩展，我们需要用 DOColor 的通用变体 DOTween.To
        textBounceSeq.Append(DOTween.To(() => speedText.color, x => speedText.color = x, rejectColor, 0.05f));

        // 相比之前降低了频率（Vibrato 降到 12）拉长了时间，变成一种更具人情味的“缓慢摇头拒绝”
        textBounceSeq.Join(textObj.transform.DOShakePosition(0.5f, new Vector3(0.2f, 0, 0), 12, 90));

        // 慢慢恢复白色
        textBounceSeq.Append(DOTween.To(() => speedText.color, x => speedText.color = x, textColor, 0.3f));
    }

    private void BreakObstacle(Rigidbody2D hitter)
    {
        isBroken = true;

        // 1. 关闭碰撞体拦截功能
        col.isTrigger = true;

        // 2. 停用刚体运算，因为我们要用 DOTween 直接管辖它的位移，不需要物理引擎干扰了
        if (rb != null) rb.simulated = false;

        // 3. 构建一段酷炫平滑的动画序列
        Sequence popSeq = DOTween.Sequence();

        Vector3 startPos = transform.position;
        Vector3 popPeakPos = startPos + (Vector3)popOffset;
        Vector3 fallEndPos = popPeakPos + new Vector3(0, -fallDistance, 0);

        // 第一个动画：非常弹性的地向你设定的偏移方向（比如向右上）“蹦”上去，并在最高点自然变慢（Ease.OutQuad）
        popSeq.Append(transform.DOMove(popPeakPos, popDuration).SetEase(Ease.OutQuad));
        // 同步给头上文字一个被击飞撕裂的扁平化效果
        if (textObj != null) popSeq.Join(textObj.transform.DOScale(new Vector3(1.5f, 0.2f, 1f), popDuration));

        // 第二个动画：在越过顶点后，像受到重力下坠一样越来越快地掉下屏幕深渊（Ease.InQuad）
        popSeq.Append(transform.DOMove(fallEndPos, fallDuration).SetEase(Ease.InQuad));

        // 连带掉落时文字渐渐透明消散 (同样为了支持 TextMesh 使用 DOTween.To 渐变 Alpha)
        if (speedText != null)
        {
            popSeq.Join(DOTween.ToAlpha(() => speedText.color, x => speedText.color = x, 0f, fallDuration));
        }

        // 动画全都播完以后，从场景里把它销毁
        popSeq.OnComplete(() =>
        {
            if (gameObject != null) Destroy(gameObject);
        });
    }
}