using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SwappableObject))] // 使用SwappableObject允许它参与子弹时间的交互
public class MomentumPortal : MonoBehaviour
{
    [Header("传送链接")]
    [Tooltip("另一扇传送门的引用。把场景里的另一个传送门拖到这里互相关联！")]
    public MomentumPortal linkedPortal;

    [Header("传送设置")]
    [Tooltip("吃进去和吐出来需要花费的动画视觉时间")]
    public float transitionDuration = 0.25f;

    [Tooltip("是否强制重定向吐出物体的速度方向？\n勾选：物体会顺着下方设定的【喷出方向】笔直喷出\n不勾选：保留物体进去时的抛物线和角度原样穿梭")]
    public bool redirectVelocity = true;

    [Tooltip("自定义喷出方向(基于门的本地坐标系)。\n默认(0,1)是门的正上方。\n如果你想让它往右吐，填(1,0)。往左下吐，填(-1,-1)")]
    public Vector2 ejectDirection = Vector2.up;

    [Tooltip("传送出来的速度保留倍数（如果你填2，那就成了加速喷射门！）")]
    public float velocityMultiplier = 1.0f;

    [Tooltip("【动作游戏必做】吐出后强制锁定玩家操作的时间。防止一出来就被按键截断了速度，实现高空抛射惯性飞越手感！")]
    public float inertiaLockTime = 0.3f;

    [Header("动画控制 (可选)")]
    [Tooltip("传送门平时待机时的Trigger名字（游戏开始以及传送结束后会自动调用）")]
    public string idleAnimTrigger = "Idle";
    [Tooltip("如果你给传送门做了Animator动画，请在这里填入吃进去时的Trigger名字")]
    public string eatAnimTrigger = "Eat";
    [Tooltip("请在这里填入吐出来时的Trigger名字")]
    public string spitAnimTrigger = "Spit";

    [Header("悬浮文字设置")]
    public Vector3 textOffset = new Vector3(0, 1.2f, 0); // 文字在物体头上的偏移量
    public Color textColor = Color.white;
    public float minShowSpeed = 0.5f; // 速度太低就不显示了，保持画面整洁

    // 避免无限循环传送的黑名单冷却池 (极其重要)
    private HashSet<Rigidbody2D> cooldownObjects = new HashSet<Rigidbody2D>();
    private Animator anim;
    private Rigidbody2D parentRb;

    private TextMesh speedText;
    private GameObject textObj;
    private float lastSpeed = -1f;
    private Sequence textBounceSeq;

    // 【修改】：不仅存速度数值，专门存下带有方向的动能矢量 Vector2
    [Header("存储速度设置")]
    [Tooltip("传送门存储速度的上限")]
    public float maxStoredSpeed = 50f;
    [HideInInspector] public Vector2 storedVelocity = Vector2.zero;

    public float ringRadius = 1.5f; // 指示圆圈的半径
    private LineRenderer ringLine;  // 外围半透明圆圈
    private LineRenderer arrowLine; // 圆圈上的箭头

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        // 为传送门添加一个运动学刚体，这是配合 SwappableObject 运作必须的底层组件
        parentRb = GetComponent<Rigidbody2D>();
        if (parentRb == null)
        {
            parentRb = gameObject.AddComponent<Rigidbody2D>();
        }
        parentRb.bodyType = RigidbodyType2D.Kinematic; // 传送门自己是不受重力和碰撞位移的！

        // 动态创建悬浮的3D文字（完全脱离父子关系防缩放扭曲）
        textObj = new GameObject("SpeedDisplay_" + gameObject.name);
        textObj.transform.SetParent(null); 
        textObj.transform.position = transform.position + textOffset;

        speedText = textObj.AddComponent<TextMesh>();
        speedText.anchor = TextAnchor.MiddleCenter;
        speedText.alignment = TextAlignment.Center;

        speedText.characterSize = 0.05f; 
        speedText.fontSize = 80;
        speedText.color = textColor;
        speedText.text = "0";
        speedText.gameObject.SetActive(false); 

        MeshRenderer meshRenderer = textObj.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = "Ground";
            meshRenderer.sortingOrder = 80; 
        }

        // 动态创建用于指示方向的【外围圆圈】
        GameObject ringObj = new GameObject("VelocityRing_" + gameObject.name);
        ringObj.transform.SetParent(transform);
        ringObj.transform.localPosition = Vector3.zero;

        ringLine = ringObj.AddComponent<LineRenderer>();
        ringLine.material = new Material(Shader.Find("Sprites/Default"));
        ringLine.startColor = new Color(1f, 1f, 1f, 1f); // 纯白色圆圈，与箭头底部一致
        ringLine.endColor = new Color(1f, 1f, 1f, 1f);
        ringLine.startWidth = 0.1f;
        ringLine.endWidth = 0.1f;
        ringLine.useWorldSpace = false; // 使用本地坐标
        ringLine.sortingLayerName = "Ground";
        ringLine.sortingOrder = 74;

        // 用数学方法画一个完美的360度圈
        int segments = 40;
        ringLine.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            ringLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * ringRadius, Mathf.Sin(angle) * ringRadius, 0));
        }
        ringLine.enabled = false;

        // 动态创建圆圈上的【箭头】
        GameObject arrowObj = new GameObject("VelocityArrow_" + gameObject.name);
        arrowObj.transform.SetParent(ringObj.transform);
        arrowObj.transform.localPosition = Vector3.zero;

        arrowLine = arrowObj.AddComponent<LineRenderer>();
        arrowLine.material = new Material(Shader.Find("Sprites/Default"));
        arrowLine.startColor = new Color(1f, 1f, 1f, 1f); // 亮白极光
        arrowLine.endColor = new Color(1f, 1f, 1f, 0f);   // 白色透明散去
        arrowLine.startWidth = 0.5f; // 粗底部，贴在圈上
        arrowLine.endWidth = 0f;     // 尖锐矛头
        arrowLine.useWorldSpace = false;
        arrowLine.positionCount = 2;
        arrowLine.sortingLayerName = "Ground";
        arrowLine.sortingOrder = 75;
        arrowLine.enabled = false;
    }

    private void OnDestroy()
    {
        if (textBounceSeq != null && textBounceSeq.IsActive()) textBounceSeq.Kill();
        if (textObj != null)
        {
            textObj.transform.DOKill();
            Destroy(textObj);
        }
        if (ringLine != null && ringLine.gameObject != null) Destroy(ringLine.gameObject);
    }

    private void Start()
    {
        // 初始状态强制设置为Idle待机状态
        if (anim != null && !string.IsNullOrEmpty(idleAnimTrigger))
        {
            anim.SetTrigger(idleAnimTrigger);
        }
    }

    private void Update()
    {
        bool hitMaxLimitThisFrame = false;

        // ------------- 将施加在门刚体上的速度【完整向量】吸收进入核心，保留方向动能 -------------
        if (parentRb != null && parentRb.linearVelocity.magnitude > 0.05f)
        {
            Vector2 incomingVel = parentRb.linearVelocity;
            storedVelocity += incomingVel; // 直接叠加方向矢量

            if (storedVelocity.magnitude >= maxStoredSpeed)
            {
                hitMaxLimitThisFrame = true; // 达到了存储上限
                storedVelocity = Vector2.ClampMagnitude(storedVelocity, maxStoredSpeed);
            }
            parentRb.linearVelocity = Vector2.zero; // 瞬间剥夺物理权限回归死物
        }

        // ------------- 悬浮数字显示被注进去的动能状态 -------------
        if (parentRb != null && speedText != null)
        {
            if (speedText.gameObject.activeSelf)
            {
                float floatOffsetY = Mathf.Sin(Time.time * 3f) * 0.15f;
                textObj.transform.position = transform.position + textOffset + new Vector3(0, floatOffsetY, 0);
                // 只有在没有进行晃动特效时才重置旋转
                if (textBounceSeq == null || !textBounceSeq.IsActive())
                {
                    textObj.transform.rotation = Quaternion.identity;
                }
            }

            // 读取存进门里的速度响量的实际大小
            float currentSpeed = storedVelocity.magnitude;

            // 控制视觉指示外圈与箭头，让玩家肉眼看到力量从圈上指向哪！
            if (ringLine != null && arrowLine != null)
            {
                if (currentSpeed >= minShowSpeed)
                {
                    ringLine.enabled = true;
                    arrowLine.enabled = true;

                    Vector2 dir = storedVelocity.normalized;
                    // 箭头的起点固定坐在圆圈的高级边缘上
                    Vector3 arrowStart = (Vector3)(dir * ringRadius);
                    // 根据速度大小，决定箭头的拔出长度（0.5 到 2.5 之间）
                    float arrowLength = 0.5f + (currentSpeed / maxStoredSpeed) * 2f; 
                    Vector3 arrowEnd = arrowStart + (Vector3)(dir * arrowLength);

                    arrowLine.SetPosition(0, arrowStart);
                    arrowLine.SetPosition(1, arrowEnd);

                    // 保持圆圈颜色与箭头底部完全一致（不改变透明度）
                    ringLine.startColor = new Color(1f, 1f, 1f, 1f);
                    ringLine.endColor = new Color(1f, 1f, 1f, 1f);
                }
                else
                {
                    ringLine.enabled = false;
                    arrowLine.enabled = false;
                }
            }

            if (currentSpeed < minShowSpeed)
            {
                if (speedText.gameObject.activeSelf) 
                    speedText.gameObject.SetActive(false);
                lastSpeed = currentSpeed;
            }
            else
            {
                if (!speedText.gameObject.activeSelf)
                    speedText.gameObject.SetActive(true);

                bool speedChanged = Mathf.Abs(currentSpeed - lastSpeed) > 0.1f;
                // 如果速度有变更，或者这帧试图灌入速度但因为上限溢出了，都会刷新文字并播放特效
                if (speedChanged || hitMaxLimitThisFrame)
                {
                    if (currentSpeed >= maxStoredSpeed)
                    {
                        speedText.text = maxStoredSpeed.ToString();
                        speedText.color = Color.red; 
                    }
                    else
                    {
                        speedText.text = currentSpeed.ToString("F1"); // 显示目前门里存的速度
                        speedText.color = textColor;
                    }

                    if (hitMaxLimitThisFrame)
                    {
                        // 达到上限的拒绝/溢出特效：放大并左右震动一下
                        if (textBounceSeq == null || !textBounceSeq.IsActive())
                        {
                            textBounceSeq = DOTween.Sequence();
                            textBounceSeq.Append(textObj.transform.DOScale(new Vector3(1.5f, 1.5f, 1f), 0.1f));
                            textBounceSeq.Join(textObj.transform.DOPunchRotation(new Vector3(0, 0, 30f), 0.3f, 10, 1f));
                            textBounceSeq.Append(textObj.transform.DOScale(Vector3.one, 0.2f));
                        }
                    }
                    else if (speedChanged && currentSpeed - lastSpeed > 5f)
                    {
                        // 被玩家瞬间注入巨大的动能时，产生果冻膨胀特效！
                        if (textBounceSeq == null || !textBounceSeq.IsActive())
                        {
                            textBounceSeq = DOTween.Sequence();
                            textBounceSeq.Append(textObj.transform.DOScale(new Vector3(1.3f, 0.7f, 1f), 0.1f));
                            textBounceSeq.Append(textObj.transform.DOScale(new Vector3(0.8f, 1.2f, 1f), 0.1f));
                            textBounceSeq.Append(textObj.transform.DOScale(Vector3.one, 0.1f));
                        }
                    }

                    if (speedChanged)
                    {
                        lastSpeed = currentSpeed;
                    }
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (linkedPortal == null) return;

        // 【防卡死死循环修复】：玩家身上通常有多个碰撞体（本体+脚底板探测圈等）。
        // 如果只防碰撞器，脚底板还是会引发传送！所以必须唯一锁定他们的肉身(Rigidbody2D)！
        Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
        if (rb == null) rb = collision.GetComponentInParent<Rigidbody2D>();
        if (rb == null) return;

        // 如果这个唯一物理肉身还在被我们记录的穿梭黑名单冷却里，拒绝吸入
        if (cooldownObjects.Contains(rb)) return;

        // 只检测箱子和玩家 (兼容检测父物体标签)
        if (collision.CompareTag("Player") || collision.CompareTag("Box") || rb.CompareTag("Player") || rb.CompareTag("Box"))
        {
            StartCoroutine(TeleportRoutine(rb.gameObject, rb));
        }
    }

    private IEnumerator TeleportRoutine(GameObject obj, Rigidbody2D rb)
    {
        // 1. 虚空锁定：立刻给入口大门挂上该刚体的冷却，防止无限死循环传送
        cooldownObjects.Add(rb);

        // 2. 捕获进入黑洞前那一瞬间的终极速度和缩放数据！
        Vector2 incomingVelocity = rb.linearVelocity;

        // 【关键修复】：如果进入门的是玩家，不应该简单粗暴地记录它当前的 Scale，
        // 防止玩家正好在处于“被压缩/变小状态”时钻入传送门，导致它永远吐出来就是个残疾形态。
        // 所以，我们需要动态判断，如果是玩家并处于 isSplit 状态，如果它现在是分裂残疾状态，就如实保存它现在的分裂体积。
        // （如果之前逻辑干预了它的大小获取它原本大小会让它强行恢复满血）
        Vector3 portalOriginalScale = obj.transform.localScale;
        Player player = obj.GetComponent<Player>();
        // 在这里不再强行把 portalOriginalScale 改成 player.GetOriginalScale() 了，
        // 这样只要他处于分身状态钻门，门依然认为他的本质大小就是被砸扁的小尺寸。

        // 如果玩家慢慢吞吞走进去，为了影视表现，给他一个5f的保底滑入速度
        if (incomingVelocity.magnitude < 1f) 
        {
            incomingVelocity = -transform.up * 5f; 
        }

        // 3. 剥夺天理引擎权限：将物体脱离重力和碰撞，任由我们揉捏揉扁
        rb.simulated = false;

        // 如果是玩家，开启硬直锁。否则引擎一转交回主角，他的状态机立马就会把速度清空！
        Entity entity = obj.GetComponent<Entity>();
        if (entity != null) entity.isKnocked = true; 

        // 4. 【吃进去】坍缩视觉特效
        if (anim != null && !string.IsNullOrEmpty(eatAnimTrigger))
        {
            anim.SetTrigger(eatAnimTrigger);
        }

        // 防冲突：杀掉由于被门夹或者其他意外引发的乱七八糟旧动画
        obj.transform.DOKill();

        Sequence eatSeq = DOTween.Sequence();
        eatSeq.Append(obj.transform.DOMove(transform.position, transitionDuration).SetEase(Ease.InBack));
        eatSeq.Join(obj.transform.DOScale(Vector3.zero, transitionDuration).SetEase(Ease.InBack)); // 缩成一个极小的奇点

        // 吃完以后，吸入点的门自己平滑回到Idle状态
        eatSeq.OnComplete(() => {
            if (anim != null && !string.IsNullOrEmpty(idleAnimTrigger))
                anim.SetTrigger(idleAnimTrigger);
        });

        yield return eatSeq.WaitForCompletion();

        // [安全校验]：如果在吃进入黑洞的这段时间，物体被意外销毁或关卡重置了，立刻终止执行！并清理入口冷却池！
        if (obj == null || rb == null)
        {
            cooldownObjects.Remove(rb);
            yield break;
        }

        // ---------- 宇宙重置：瞬间来到另一个星球点 ----------
        obj.transform.position = linkedPortal.transform.position;
        // [安全调整]：在抵达出口的这一刻，再给出口门挂上防误触冷却！避免在虫洞里穿梭太久导致出口门护盾提前过期。
        linkedPortal.AddExitCooldown(rb, transitionDuration + inertiaLockTime + 0.2f);

        // 5. 【吐出来】从奇点爆发出原本的大小
        if (linkedPortal.anim != null && !string.IsNullOrEmpty(linkedPortal.spitAnimTrigger))
        {
            linkedPortal.anim.SetTrigger(linkedPortal.spitAnimTrigger);
        }

        Sequence spitSeq = DOTween.Sequence();
        spitSeq.Append(obj.transform.DOScale(portalOriginalScale, transitionDuration).SetEase(Ease.OutBack));

        // 吐完以后，喷射点的门也各自回到Idle待机状态
        spitSeq.OnComplete(() => {
            if (linkedPortal.anim != null && !string.IsNullOrEmpty(linkedPortal.idleAnimTrigger))
                linkedPortal.anim.SetTrigger(linkedPortal.idleAnimTrigger);
        });

        yield return spitSeq.WaitForCompletion();

        // [安全校验]：如果在吐出来的过程中发生了意外（如被地刺扎破销毁），停止重置物理的操作，直接终止！
        if (obj == null || rb == null)
        {
            cooldownObjects.Remove(rb);
            yield break;
        }

        // 6. 交还物理：回到真实世界并准备接受引力和碰撞
        rb.simulated = true;

        // ---------- 重新定义物理法则 ----------
        Vector2 outVelocity = incomingVelocity;

        // 我们需要拿一个基础方向来作为参考，以防有些速度直接是0
        Vector2 baseDirection = incomingVelocity.normalized;
        if (baseDirection == Vector2.zero) baseDirection = linkedPortal.transform.up;

        if (linkedPortal.redirectVelocity)
        {
            // 如果勾选了重定向，统一改为门的强制喷射方向
            baseDirection = linkedPortal.transform.TransformDirection(linkedPortal.ejectDirection.normalized);
            outVelocity = baseDirection * incomingVelocity.magnitude;
        }

        // ================= 【核心绝杀】：将传送门里存好的带方向的动量真实释放赋予物体！=================
        Vector2 bonusVelocity = Vector2.zero;
        if (linkedPortal != null && linkedPortal.storedVelocity.magnitude > 0.1f)
        {
            // 这次直接使用另一个门肚子里吸附的真实物理方向，砸哪往哪弹！
            bonusVelocity = linkedPortal.storedVelocity;

            // 速度一旦释放给物体，储蓄罐马上彻底清空！
            linkedPortal.storedVelocity = Vector2.zero; 
        }

        // 最终速度 = (原本的入口速度 * 加速板倍数) + (这扇门存储的外来充能速度)
        rb.linearVelocity = outVelocity * linkedPortal.velocityMultiplier + bonusVelocity;

        // 7. 【手感核心】保留刚才进入前积累的庞大惯性！
        // 在这零点几秒内，完全不给主角操控按键制动的权利，让他硬生生享受超高速被发射出去的快感！
        if (entity != null)
        {
            if (inertiaLockTime > 0)
            {
                yield return new WaitForSeconds(inertiaLockTime);
            }
            entity.isKnocked = false; // 大飞跃落地，终于交还操作！
        }

        // 解开入口门的冷却
        yield return new WaitForSeconds(0.1f);
        cooldownObjects.Remove(rb);
    }

    /// <summary>
    /// 被连通的传送口调用这个指令，用于在一定时间里拒绝该物体，防止刚吐出就吃回去
    /// </summary>
    public void AddExitCooldown(Rigidbody2D rb, float lockTime)
    {
        cooldownObjects.Add(rb);
        StartCoroutine(RemoveCooldownRoutine(rb, lockTime));
    }

    private IEnumerator RemoveCooldownRoutine(Rigidbody2D rb, float lockTime)
    {
        yield return new WaitForSeconds(lockTime);
        if (rb != null && cooldownObjects.Contains(rb))
        {
            cooldownObjects.Remove(rb);
        }
        else
        {
            // 如果rb被销毁，利用哈希集合的特性强行容错剔除
            cooldownObjects.RemoveWhere(item => item == null);
        }
    }

    // ===== Scene 便捷开发功能：画线辅助 =====
    private void OnDrawGizmos()
    {
        // 在编辑器里画一条绿水晶的射击线，指出大门会往哪个方向“喷”东西
        Gizmos.color = Color.green;
        Vector2 worldEjectDir = transform.TransformDirection(ejectDirection.normalized);
        Gizmos.DrawRay(transform.position, worldEjectDir * 2f);

        // 连一条青线，让你一目了然这个传送门连着谁
        if (linkedPortal != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, linkedPortal.transform.position);
        }
    }
}