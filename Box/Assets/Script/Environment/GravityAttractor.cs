using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(CircleCollider2D))]
public class GravityAttractor : MonoBehaviour
{
    [Header("太空引力设置")]
    [Tooltip("引力生效的圆形半径范围。进入此范围的物体将体验失重。")]
    public float radius = 5f;
    
    [Tooltip("向中心牵引的微重力强度。模拟太空中的微弱向心力。")]
    public float pullForce = 2f;

    [Tooltip("环绕旋转的切向推力。数值越大，物体绕圈转得越快。")]
    public float orbitForce = 8f;

    [Tooltip("是否顺时针环绕？不勾选则为逆时针转动。")]
    public bool isClockwise = true;

    [Tooltip("太空中的空气阻力。数值越大，物体进入后滑行减速越快。它能稳定环绕轨道防飞出。")]
    public float spaceDrag = 1f;

    [Header("动态虚线外观")]
    [Tooltip("虚线圈的基础颜色。推荐使用带有发光感的颜色（如青色或紫色）。")]
    public Color baseColor = new Color(0f, 1f, 1f, 1f); 

    [Tooltip("呼吸特效的最低透明度 (0~1)。")]
    [Range(0f, 1f)] public float minAlpha = 0.1f;

    [Tooltip("呼吸特效的最高透明度 (0~1)。")]
    [Range(0f, 1f)] public float maxAlpha = 0.8f;

    [Tooltip("圈圈呼吸闪烁的变化速度。")]
    public float pulseSpeed = 3f;

    // --- 组件引用 ---
    private LineRenderer lineRenderer;
    private CircleCollider2D col;

    /// <summary>
    /// 内部数据结构：用于记住物体在进入太空圈之前的物理状态。
    /// 以防止物体离开圈子时无法恢复原本的重力和摩擦力。
    /// </summary>
    private class OriginalState
    {
        public float gravityScale;
        public float drag;
    }
    
    // 追踪当前正处于太空圈内的所有受到影响的刚体
    private Dictionary<Rigidbody2D, OriginalState> spaceObjects = new Dictionary<Rigidbody2D, OriginalState>();

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        col = GetComponent<CircleCollider2D>();

        // 强制初始化碰撞体为触发器，并对齐其半径与视觉半径
        col.isTrigger = true;
        col.radius = radius;

        DrawCircle();
    }

    /// <summary>
    /// 在 Unity 编辑器面板中每当修改数值时触发。
    /// 用于实现“所见即所得”，让你在拖动半径条时实时看到虚线圈变大变小。
    /// </summary>
    private void OnValidate()
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (col == null) col = GetComponent<CircleCollider2D>();

        if (col != null) col.radius = radius;

        if (lineRenderer != null)
        {
            DrawCircle();
        }
    }

    /// <summary>
    /// 利用极坐标三角函数（Sin / Cos）生成一个完美圆形的顶点。
    /// 并将其赋予 LineRenderer 以渲染出圆圈边缘。
    /// </summary>
    private void DrawCircle()
    {
        if (lineRenderer == null) return;

        // 保证圆圈跟随物体的本地坐标移动，而非钉死在世界坐标原点
        lineRenderer.useWorldSpace = false;

        // 决定圆圈边数的精度，60边形在视觉上已足够圆滑
        int segments = 60;
        lineRenderer.positionCount = segments + 1; 

        float angle = 0f;
        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
            
            angle += (360f / segments);
        }
    }

    private void Update()
    {
        // 动态呼吸灯视觉效果运算
        if (lineRenderer != null)
        {
            // 利用 Sin 函数随时间生成一个 0 到 1 之间往复波动的过渡值
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;

            // 通过平滑插值 (Lerp) 算出此刻的 Alpha (透明度)
            float currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, t);

            // 组合计算出的透明度并刷新线条颜色
            Color currentColor = baseColor;
            currentColor.a *= currentAlpha; 

            lineRenderer.startColor = currentColor;
            lineRenderer.endColor = currentColor;
        }
    }

    private void FixedUpdate()
    {
        // 遍历所有当前正在太空圈内的受波及物体
        foreach (var rb in spaceObjects.Keys)
        {
            if (rb == null) continue; // 容错处理：如果物体突然在圈内被销毁了

            // 1. 计算出指向黑洞中心的向心方向
            Vector2 pullDirection = (transform.position - rb.transform.position).normalized;

            // 2. 利用纯数学计算出与向心力垂直的“切线方向”，来制作环绕轨道
            // 顺时针垂直向量为 (y, -x)，逆时针为 (-y, x)
            Vector2 orbitDirection = isClockwise 
                ? new Vector2(pullDirection.y, -pullDirection.x) 
                : new Vector2(-pullDirection.y, pullDirection.x);

            // 施加向心的拉扯力 (防止物体离心飞出)
            rb.AddForce(pullDirection * pullForce, ForceMode2D.Force);

            // 施加侧面的环绕推力 (让物体转起来)
            rb.AddForce(orbitDirection * orbitForce, ForceMode2D.Force);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 防呆设计：不要把自己这个中心点吸进去
        if (collision.gameObject == gameObject) return;

        Rigidbody2D rb = collision.attachedRigidbody;

        // 条件过滤：必须是动态刚体 (Dynamic)，且尚未被记录在案。掉落的物体或玩家等。
        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic && !spaceObjects.ContainsKey(rb))
        {
            // 备份当前的真实世界物理规则
            spaceObjects[rb] = new OriginalState
            {
                gravityScale = rb.gravityScale,
                drag = rb.linearDamping
            };

            // 核心物理修改：强行剥夺重力，赋予太空阻力，制造完美失重体验
            rb.gravityScale = 0f;
            rb.linearDamping = spaceDrag;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Rigidbody2D rb = collision.attachedRigidbody;

        // 如果物体由于惯性成功逃脱了引力圈
        if (rb != null && spaceObjects.ContainsKey(rb))
        {
            // 查阅字典，将物体最初的重力和摩擦力原封不动地还给它
            OriginalState oldState = spaceObjects[rb];
            rb.gravityScale = oldState.gravityScale;
            rb.linearDamping = oldState.drag;

            // 解除追踪
            spaceObjects.Remove(rb);
        }
    }

    /// <summary>
    /// 编辑器专属：绘制一个辅助描边球，方便开发者在未选中该物体时也能看见大致范围。
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}