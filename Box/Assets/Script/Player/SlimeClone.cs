using UnityEngine;
using DG.Tweening;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class SlimeClone : MonoBehaviour
{
    [Header("分身设置")]
    public float bounceForceMultiplier = 1.5f; // 弹力倍数

    private float lifetime;
    private float currentTimer;
    private bool isCountingDown = false;
    private bool isDestroying = false; // 增加防连击摧毁标记
    private Player playerSource;

    private GameObject textObj;
    private TextMesh timeText;
    private Vector3 originalScale; // 预制体自身的原本大小

    public void Init(Player player, float initialSpeed)
    {
        playerSource = player;
        // 存在时间就是玩家刚才的速度绝对值
        lifetime = initialSpeed;
        currentTimer = lifetime;

        // 记录预制体自带的缩放大小
        originalScale = transform.localScale;

        // 呈现弹出动画
        transform.localScale = Vector3.zero;
        transform.DOScale(originalScale, 0.4f).SetEase(Ease.OutBack);

        // 创建头上的倒计时文字 (为了不受分身被拉伸影响，把文字跟它脱离父子关系，或者修正缩放)
        textObj = new GameObject("CloneTimerText");
        textObj.transform.SetParent(null); // 让文字脱离本体，防止本体缩小导致文字也看不见
        // 根据预制体的体积往上挂一点位置
        textObj.transform.position = transform.position + new Vector3(0, originalScale.y * 1.5f + 0.5f, 0);

        timeText = textObj.AddComponent<TextMesh>();
        timeText.anchor = TextAnchor.MiddleCenter;
        timeText.alignment = TextAlignment.Center;
        timeText.characterSize = 0.05f;
        timeText.fontSize = 60; // 缩小字号，使其不至于太突兀
        timeText.color = Color.white;
        timeText.text = currentTimer.ToString("0.0");

        MeshRenderer meshRenderer = textObj.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            // 确保渲染在最前面，防止被背景或者门遮挡
            meshRenderer.sortingLayerName = "Ground";
            meshRenderer.sortingOrder = 999;
        }
    }

    private void Update()
    {
        if (isDestroying) return; // 已经进入销毁流程，停止计算

        // 维持文字的位置跟踪
        if (textObj != null)
        {
            textObj.transform.position = transform.position + new Vector3(0, originalScale.y * 1.5f + 0.5f, 0);
        }
        if (!isCountingDown && playerSource != null)
        {
            // 当玩家开始移动时（通过检测moveInput判断），分身开始倒计时
            if (playerSource.moveInput.magnitude > 0.1f)
            {
                isCountingDown = true;
            }
        }

        if (isCountingDown)
        {
            currentTimer -= Time.deltaTime;

            if (timeText != null)
            {
                timeText.text = Mathf.Max(currentTimer, 0).ToString("0.0");
            }

            // 倒计时结束
            if (currentTimer <= 0)
            {
                isCountingDown = false;
                DestroyClone();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroying) return; // 销毁期间屏蔽碰撞

        // 当玩家碰到分身时，将玩家弹开
        if (collision.gameObject.CompareTag("Player"))
        {
            Entity entity = collision.gameObject.GetComponent<Entity>();
            Rigidbody2D pRb = collision.gameObject.GetComponent<Rigidbody2D>();

            if (entity != null && pRb != null)
            {
                // 获取碰击的反方向
                Vector2 normal = collision.contacts[0].normal;
                Vector2 bounceDir = -normal.normalized;

                // 【手感优化1：修正弹射角度，防侧滑呲飞】
                // 因为分身的碰撞体是圆形的，如果稍微偏斜一点踩上去，法向量就会把玩家沿对角线狠狠斜着射出去，完全无法预判。
                // 修复：只要玩家大概是从上方踩下来的，就强制抹平X轴，改为“绝对垂直向上”弹跳。
                if (bounceDir.y > 0.1f)
                {
                    bounceDir = Vector2.up; // 笔直往上弹，把轨迹变得完全可控
                }

                // 【手感优化2：交还空中控制权 + 稍微降低弹力底数】
                // 把硬直失控时间从 0.2秒 缩短为 0.02秒，
                // 玩家被弹起来的瞬间只要拨动左右摇杆，立刻就能在空中微调轨迹去钻传送门！
                entity.ApplyBounceKnockback(bounceDir, 13f * bounceForceMultiplier, 0.02f);

                // 弹一下缩放特效，确保动画最终一定回到 originalScale，避免出生时因为玩家踩着它瞬间打断动画变回0的bug
                transform.DOKill();
                Sequence bounceSeq = DOTween.Sequence();
                bounceSeq.Append(transform.DOScale(originalScale * 1.2f, 0.1f).SetEase(Ease.OutQuad));
                bounceSeq.Append(transform.DOScale(originalScale, 0.1f).SetEase(Ease.InQuad));
            }
        }
    }

    private void DestroyClone()
    {
        if (isDestroying) return; // 绝对防多次死亡触发
        isDestroying = true;

        // 取消物理和碰撞
        GetComponent<Collider2D>().enabled = false;

        if (textObj != null) Destroy(textObj);

        // 动画缩回后销毁
        transform.DOKill();

        // 【关键修复点】：将动画绑定在 gameObject 上并设置 Link。
        // 如果此游戏物体在 0.3 秒内被意外销毁（如由于玩家死亡或场景重启），
        // DOTween 就会因为绑定了 Link 而自动取消后续播完的回调，不会再去找茬空指针报错！
        Sequence seq = DOTween.Sequence()
            .SetLink(gameObject)
            .Append(transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                if (playerSource != null && playerSource.gameObject != null)
                {
                    // 玩家恢复正常大小
                    playerSource.RestoreFromSplit();
                }
                if (gameObject != null) Destroy(gameObject);
            });
    }
    private void OnDestroy()
    {
        transform.DOKill();
        if (textObj != null)
        {
            textObj.transform.DOKill();
            Destroy(textObj);
        }
    }
}