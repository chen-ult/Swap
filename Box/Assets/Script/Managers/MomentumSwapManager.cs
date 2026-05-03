using UnityEngine;
using System;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class MomentumSwapManager : MonoBehaviour
{
    #region 事件定义
    /// <summary>
    /// 子弹时间开启/关闭事件（参数：是否开启）
    /// </summary>
    public static event Action<bool> OnBulletTimeToggled;
    #endregion

    #region 可配置参数
    [Header("子弹时间设置")]
    [Tooltip("触发子弹时间的按键")]
    public Key timeSlowKey = Key.LeftShift;
    [Tooltip("子弹时间的时间缩放（0~1，越小越慢）")]
    public float slowMotionScale = 0.1f;

    [Header("交换/转移参数")]
    [Tooltip("速度交换/转移的倍率（1=原速，1.2=提升20%）")]
    public float swapMultiplier = 1.2f;
    [Tooltip("是否使用真实动量（动量=质量×速度），否则直接交换速度")]
    public bool useTrueMomentum = false;
    [Tooltip("速度上限（防止速度无限叠加）")]
    public float maxSpeedLimit = 80f;

    [Header("转移功能解锁")]
    [Tooltip("是否解锁“动量转移”功能（仅交换则关闭）")]
    public bool isTransferUnlocked = false;

    [Header("UI视觉配置")]
    [Tooltip("单向箭头（用于转移）")]
    public Sprite singleArrowSprite;
    [Tooltip("双向箭头（用于交换）")]
    public Sprite doubleArrowSprite;
    [Tooltip("箭头缩放比例")]
    public float arrowScale = 1f;
    #endregion

    #region 私有变量
    private bool isSlowingTime; // 是否处于子弹时间
    private Camera mainCam;     // 主相机

    // 选中的物体
    private IMomentumSwappable firstSelectedObj;
    private IMomentumSwappable secondSelectedObj;

    // UI相关
    private LineRenderer interactionLine; // 交互连线
    private SpriteRenderer arrowRenderer; // 箭头渲染
    private int currentActionType;        // 0=交换 1=左转移 2=右转移
    private IMomentumSwappable leftObj;   // 左侧物体（按X轴排序）
    private IMomentumSwappable rightObj;  // 右侧物体（按X轴排序）

    private float originalFixedDeltaTime; // 原始固定时间步长
    #endregion

    #region 生命周期
    private void Start()
    {
        // 初始化主相机
        mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("未找到主相机！请确保场景中有Tag为MainCamera的相机", this);
            enabled = false;
            return;
        }

        // 初始化时间（避免残留的时间缩放）
        Time.timeScale = 1f;
        originalFixedDeltaTime = Time.fixedDeltaTime;
        Time.fixedDeltaTime = originalFixedDeltaTime;

        // 初始化交互箭头
        InitInteractionArrow();

        // 初始化连线
        InitInteractionLine();
    }

    private void OnDestroy()
    {
        // 销毁时恢复时间缩放
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;

        // 销毁箭头物体
        if (arrowRenderer != null)
        {
            Destroy(arrowRenderer.gameObject);
        }
    }

    private void Update()
    {
        // 相机判空（防止运行时删除相机导致崩溃）
        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null) return;
        }

        // 处理子弹时间的按键
        HandleBulletTimeInput();

        // 子弹时间内的逻辑
        if (isSlowingTime)
        {
            // 已选中两个物体（转移模式）
            if (secondSelectedObj != null && isTransferUnlocked)
            {
                UpdateInteractionLineAndLogic();
                // 左键触发交换/转移
                if (Mouse.current?.leftButton.wasPressedThisFrame == true)
                {
                    ExecuteAction();
                }
            }
            // 未选中两个物体（选择物体）
            else if (Mouse.current?.leftButton.wasPressedThisFrame == true)
            {
                HandleMouseClick();
            }
        }
    }
    #endregion

    #region 初始化方法
    /// <summary>
    /// 初始化交互箭头
    /// </summary>
    private void InitInteractionArrow()
    {
        GameObject arrowObj = new GameObject("InteractionArrow");
        arrowObj.transform.SetParent(transform);
        arrowRenderer = arrowObj.AddComponent<SpriteRenderer>();
        arrowRenderer.sortingLayerName = "Ground";
        arrowRenderer.sortingOrder = 100; // 确保箭头在最上层
        arrowObj.SetActive(false);
    }

    /// <summary>
    /// 初始化交互连线
    /// </summary>
    private void InitInteractionLine()
    {
        interactionLine = GetComponent<LineRenderer>();
        if (interactionLine == null)
        {
            Debug.LogError("MomentumSwapManager缺少LineRenderer组件！", this);
            enabled = false;
            return;
        }

        interactionLine.useWorldSpace = true;
        interactionLine.positionCount = 0;
        interactionLine.numCapVertices = 8; // 让连线端点更圆润
    }
    #endregion

    #region 输入处理
    /// <summary>
    /// 处理子弹时间的按键输入
    /// </summary>
    private void HandleBulletTimeInput()
    {
        if (Keyboard.current == null) return;

        // 按下按键：开启子弹时间
        if (Keyboard.current[timeSlowKey].wasPressedThisFrame)
        {
            StartTimeSlow();
        }
        // 松开按键：关闭子弹时间
        else if (Keyboard.current[timeSlowKey].wasReleasedThisFrame)
        {
            StopTimeSlow();
        }
    }

    /// <summary>
    /// 处理鼠标点击（选择物体）
    /// </summary>
    private void HandleMouseClick()
    {
        Vector2 mouseWorldPos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        // 检测鼠标点击的物体（穿透检测，支持Trigger）
        RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null) continue;

            // 查找父级中实现了IMomentumSwappable的组件
            IMomentumSwappable clickedObj = hit.collider.GetComponentInParent<IMomentumSwappable>();
            if (clickedObj == null || clickedObj.MomentumRigidbody == null) continue;

            // 第一次选中
            if (firstSelectedObj == null)
            {
                firstSelectedObj = clickedObj;
                firstSelectedObj.SetSelectedVisual(true);
            }
            else
            {
                // 点击同一物体：取消选择
                if (firstSelectedObj == clickedObj)
                {
                    ClearSelection();
                    return;
                }

                // 未解锁转移：直接交换
                if (!isTransferUnlocked)
                {
                    SwapMomentum(firstSelectedObj, clickedObj);
                    ClearSelection();
                }
                // 已解锁转移：选中第二个物体，进入转移模式
                else
                {
                    secondSelectedObj = clickedObj;
                    secondSelectedObj.SetSelectedVisual(true);

                    // 按X轴排序左右物体
                    SortLeftRightObj(firstSelectedObj, secondSelectedObj);
                }
            }
            return; // 找到第一个可交互物体后退出
        }
    }
    #endregion

    #region 子弹时间控制
    /// <summary>
    /// 开启子弹时间
    /// </summary>
    private void StartTimeSlow()
    {
        isSlowingTime = true;
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = originalFixedDeltaTime * slowMotionScale;
        OnBulletTimeToggled?.Invoke(true);
    }

    /// <summary>
    /// 关闭子弹时间
    /// </summary>
    private void StopTimeSlow()
    {
        isSlowingTime = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;
        ClearSelection(); // 清空选中状态
        OnBulletTimeToggled?.Invoke(false);
    }
    #endregion

    #region 交互逻辑
    /// <summary>
    /// 按X轴排序左右物体
    /// </summary>
    private void SortLeftRightObj(IMomentumSwappable obj1, IMomentumSwappable obj2)
    {
        Component comp1 = obj1 as Component;
        Component comp2 = obj2 as Component;
        if (comp1 == null || comp2 == null) return;

        if (comp1.transform.position.x <= comp2.transform.position.x)
        {
            leftObj = obj1;
            rightObj = obj2;
        }
        else
        {
            leftObj = obj2;
            rightObj = obj1;
        }
    }

    /// <summary>
    /// 更新交互连线和箭头逻辑
    /// </summary>
    private void UpdateInteractionLineAndLogic()
    {
        Component leftComp = leftObj as Component;
        Component rightComp = rightObj as Component;
        if (leftComp == null || rightComp == null) return;

        // 更新连线
        interactionLine.positionCount = 2;
        interactionLine.SetPosition(0, leftComp.transform.position);
        interactionLine.SetPosition(1, rightComp.transform.position);

        // 计算鼠标位置相对连线的比例
        Vector2 mouseWorldPos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 posLeft = leftComp.transform.position;
        Vector2 posRight = rightComp.transform.position;
        Vector2 lineDir = posRight - posLeft;
        float sqrDist = lineDir.sqrMagnitude;

        // 物体距离过近：默认交换
        if (sqrDist < 0.64f)
        {
            currentActionType = 0;
        }
        else
        {
            // 计算鼠标在连线上的投影比例（0~1）
            Vector2 mouseDir = mouseWorldPos - posLeft;
            float mouseT = Vector2.Dot(mouseDir, lineDir) / sqrDist;

            // 根据比例判断操作类型
            if (mouseT < 0.35f) currentActionType = 1; // 左转移
            else if (mouseT > 0.65f) currentActionType = 2; // 右转移
            else currentActionType = 0; // 交换
        }

        // 更新箭头显示
        UpdateArrowVisual(posLeft, posRight, lineDir);
    }

    /// <summary>
    /// 更新箭头的视觉显示
    /// </summary>
    private void UpdateArrowVisual(Vector2 posLeft, Vector2 posRight, Vector2 lineDir)
    {
        if (arrowRenderer == null) return;

        arrowRenderer.gameObject.SetActive(true);
        // 箭头位置：连线中点
        arrowRenderer.transform.position = posLeft + lineDir * 0.5f;
        arrowRenderer.transform.localScale = Vector3.one * arrowScale;
        // 箭头旋转：匹配连线方向
        float angle = Mathf.Atan2(lineDir.y, lineDir.x) * Mathf.Rad2Deg;
        arrowRenderer.transform.rotation = Quaternion.Euler(0, 0, angle);

        // 根据操作类型切换箭头
        switch (currentActionType)
        {
            case 0: // 交换
                arrowRenderer.sprite = doubleArrowSprite;
                arrowRenderer.flipX = false;
                break;
            case 1: // 左转移（右→左）
                arrowRenderer.sprite = singleArrowSprite;
                arrowRenderer.flipX = true;
                break;
            case 2: // 右转移（左→右）
                arrowRenderer.sprite = singleArrowSprite;
                arrowRenderer.flipX = false;
                break;
        }
    }

    /// <summary>
    /// 执行交换/转移操作
    /// </summary>
    private void ExecuteAction()
    {
        switch (currentActionType)
        {
            case 0: SwapMomentum(leftObj, rightObj); break;
            case 1: TransferMomentum(rightObj, leftObj); break;
            case 2: TransferMomentum(leftObj, rightObj); break;
        }
        ClearSelection();
    }
    #endregion

    #region 核心功能：交换/转移动量
    /// <summary>
    /// 转移动量（从源物体到目标物体）
    /// </summary>
    /// <param name="source">源物体（失去速度）</param>
    /// <param name="target">目标物体（获得速度）</param>
    private void TransferMomentum(IMomentumSwappable source, IMomentumSwappable target)
    {
        if (source == null || target == null || source.MomentumRigidbody == null || target.MomentumRigidbody == null)
        {
            Debug.LogWarning("动量转移失败：源/目标物体为空或无刚体", this);
            return;
        }

        Vector2 targetVelocity;
        if (useTrueMomentum)
        {
            // 真实动量：动量=质量×速度
            Vector2 sourceMomentum = source.MomentumRigidbody.mass * source.MomentumRigidbody.linearVelocity;
            targetVelocity = target.MomentumRigidbody.linearVelocity + (sourceMomentum / target.MomentumRigidbody.mass) * swapMultiplier;
        }
        else
        {
            // 直接转移速度
            targetVelocity = target.MomentumRigidbody.linearVelocity + source.MomentumRigidbody.linearVelocity * swapMultiplier;
        }

        // 限制速度上限
        targetVelocity = Vector2.ClampMagnitude(targetVelocity, maxSpeedLimit);

        // 应用速度
        target.ApplyMomentum(targetVelocity);
        source.ApplyMomentum(Vector2.zero); // 源物体速度清零

        // 锁定目标物体输入（防止操作冲突）
        LockEntityInput(target, 0.15f);

        // 视觉反馈
        target.FlashSuccess();
        Debug.Log($"动量转移成功：{GetObjName(source)} → {GetObjName(target)}", this);
    }

    /// <summary>
    /// 交换两个物体的动量/速度
    /// </summary>
    private void SwapMomentum(IMomentumSwappable objA, IMomentumSwappable objB)
    {
        if (objA == null || objB == null || objA.MomentumRigidbody == null || objB.MomentumRigidbody == null)
        {
            Debug.LogWarning("动量交换失败：物体为空或无刚体", this);
            return;
        }

        Vector2 velocityA, velocityB;
        if (useTrueMomentum)
        {
            // 真实动量交换
            Vector2 momentumA = objA.MomentumRigidbody.mass * objA.MomentumRigidbody.linearVelocity;
            Vector2 momentumB = objB.MomentumRigidbody.mass * objB.MomentumRigidbody.linearVelocity;

            velocityA = Vector2.ClampMagnitude((momentumB / objA.MomentumRigidbody.mass) * swapMultiplier, maxSpeedLimit);
            velocityB = Vector2.ClampMagnitude((momentumA / objB.MomentumRigidbody.mass) * swapMultiplier, maxSpeedLimit);
        }
        else
        {
            // 直接交换速度
            velocityA = Vector2.ClampMagnitude(objB.MomentumRigidbody.linearVelocity * swapMultiplier, maxSpeedLimit);
            velocityB = Vector2.ClampMagnitude(objA.MomentumRigidbody.linearVelocity * swapMultiplier, maxSpeedLimit);
        }

        // 应用速度
        objA.ApplyMomentum(velocityA);
        objB.ApplyMomentum(velocityB);

        // 锁定两个物体输入
        LockEntityInput(objA, 0.15f);
        LockEntityInput(objB, 0.15f);

        // 视觉反馈
        objA.FlashSuccess();
        objB.FlashSuccess();
        Debug.Log($"动量交换成功：{GetObjName(objA)} ↔ {GetObjName(objB)}", this);
    }

    /// <summary>
    /// 锁定实体输入（防止操作冲突）
    /// </summary>
    private void LockEntityInput(IMomentumSwappable swappable, float duration)
    {
        Entity entity = (swappable as Component)?.GetComponent<Entity>();
        if (entity != null)
        {
            StartCoroutine(LockEntityInputCoroutine(entity, duration));
        }
    }

    private System.Collections.IEnumerator LockEntityInputCoroutine(Entity entity, float duration)
    {
        entity.isKnocked = true;
        yield return new WaitForSecondsRealtime(duration); // 不受时间缩放影响
        if (entity != null) entity.isKnocked = false;
    }
    #endregion

    #region 辅助方法
    /// <summary>
    /// 清空选中状态
    /// </summary>
    private void ClearSelection()
    {
        // 重置选中状态
        if (firstSelectedObj != null)
        {
            firstSelectedObj.SetSelectedVisual(false);
            firstSelectedObj = null;
        }
        if (secondSelectedObj != null)
        {
            secondSelectedObj.SetSelectedVisual(false);
            secondSelectedObj = null;
        }

        leftObj = null;
        rightObj = null;

        // 隐藏连线和箭头
        if (interactionLine != null) interactionLine.positionCount = 0;
        if (arrowRenderer != null) arrowRenderer.gameObject.SetActive(false);
    }

    /// <summary>
    /// 获取可交换物体的名称（辅助日志）
    /// </summary>
    private string GetObjName(IMomentumSwappable swappable)
    {
        return (swappable as Component)?.gameObject.name ?? "未知物体";
    }
    #endregion
}