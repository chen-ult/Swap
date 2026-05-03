using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class Player : Entity
{
    public static event Action OnPlayerDeath;//玩家死亡事件

    public PlayerInputSet input;

    #region State
    public Player_IdleState idleState { get; private set; }
    public Player_MoveState moveState { get; private set; }
    public Player_JumpState jumpState { get; private set; }
    public Player_FallState fallState { get; private set; }
    public Player_FallEndState fallendState { get; private set; }
    public Player_DeadState deadState { get; private set; }
    #endregion


    #region 移动参数
    [Header("------------移动参数------------")]
    public float movespeed;//移动速度
    public float jumpforce = 5;//跳跃力
    public Vector2 wallJumpForce;//墙壁跳跃力

    [Range(0f, 1f)]
    public float inAirMoveMultiplier = .7f;//空中移动倍率
    [Range(0f, 1f)]
    public float wallSlideMultiplier = .3f;//墙壁滑行倍率

    [Header("冲刺参数")]
    public float dashDuration = .25f;//冲刺持续时间
    public float dashSpeed = 20;//冲刺速度

    public Vector2 moveInput { get; private set; }//移动输入
    #endregion

    private Collider2D col;
    public LayerMask obstacleLayer; // 用于检测玩家是否被卡在墙里或地面里

    [Space(10)]
    [Header("史莱姆分裂机制")]
    public float splitSpeedThreshold = 15f; // 撞击墙面或地面需要达到多大速度才会触发分裂
    public bool isSplit = false; // 是否已经处于分裂状态
    public float splitBounceForce = 12f; // 分裂后往上弹的力

    [Tooltip("分裂分身的预制体（请在项目里配置好一个包含SlimeClone脚本的预制体并拖入此槽位）")]
    public GameObject slimeClonePrefab;

    [Tooltip("分裂后的体积缩放比例（0.5代表一半，如果太小可以调为0.7左右）")]
    public float splitScaleMultiplier = 0.7f;

    private Vector3 originalScale; // 玩家原始大小

    // 【新增修复】：提供给外部传送门读取真正标准大小的方法，防止它萎缩钻门导致一直残疾。
    public Vector3 GetOriginalScale()
    {
        return originalScale;
    }

    private bool isPassingThroughPlatform = false;

    protected override void Awake()
    {
        base.Awake();

        input = new PlayerInputSet();
        originalScale = transform.localScale;
        col = GetComponent<Collider2D>();



        idleState = new Player_IdleState(this, stateMachine, "idle");
        moveState = new Player_MoveState(this, stateMachine, "move");
        jumpState = new Player_JumpState(this, stateMachine, "jumpFall");
        fallState = new Player_FallState(this, stateMachine, "jumpFall");
        fallendState = new Player_FallEndState(this, stateMachine, "fallend");
        deadState = new Player_DeadState(this, stateMachine, "dead");

    }

    protected override void Start()
    {
        base.Start();

        stateMachine.Initialize(idleState);
    }



    protected override void Update()
    {
        base.Update();

        // 单向平台下落逻辑 (按 S 且在地面)
        if (moveInput.y < -0.5f && groundDetected && !isPassingThroughPlatform)
        {
            RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
            if (hit.collider != null && hit.collider.GetComponent<PlatformEffector2D>() != null)
            {
                StartCoroutine(PassThroughOneWayPlatform(hit.collider));
            }
        }
    }

    private IEnumerator PassThroughOneWayPlatform(Collider2D platformCollider)
    {
        isPassingThroughPlatform = true;
        Collider2D[] playerColliders = GetComponentsInChildren<Collider2D>();
        
        // 忽略碰撞，让玩家掉下去
        foreach (var pCol in playerColliders)
        {
            Physics2D.IgnoreCollision(pCol, platformCollider, true);
        }
        
        yield return new WaitForSeconds(0.35f); // 0.35秒通常足够穿过一层平台
        
        // 恢复碰撞
        if (platformCollider != null)
        {
            foreach (var pCol in playerColliders)
            {
                if (pCol != null)
                    Physics2D.IgnoreCollision(pCol, platformCollider, false);
            }
        }
        
        isPassingThroughPlatform = false;
    }

    public override void EntityDeath()//实体死亡，重写
    {
        base.EntityDeath();

        OnPlayerDeath?.Invoke();
        stateMachine.ChangeState(deadState);
        StartCoroutine(WaitDeathAnimation());
    }

    private IEnumerator WaitDeathAnimation()
    {
        // 等待1秒（请根据你死亡动画的实际长度修改这个时间）
        yield return new WaitForSeconds(1.0f);

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RespawnAtCheckpoint();
        }
        else
        {
            Debug.LogError("没有找到 LevelManager.Instance，无法复活玩家！请确保在此场景中已经布置了 LevelManager。");
        }
    }


    private void OnEnable()
    {
        input.Enable();

        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero;
    }

    private void OnDisable()
    {
        input.Disable();
    }

    protected virtual void OnDestroy()
    {
        transform.DOKill();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 只有碰到地面或者墙壁才计算 (即碰撞物的Layer在whatIsGround中)
        int layerMask = 1 << collision.gameObject.layer;
        if ((layerMask & whatIsGround.value) != 0)
        {
            // 获取碰撞瞬间双方的相对速度大小
            float impactSpeed = collision.relativeVelocity.magnitude;

            // 如果到达了阈值，且目前还没有分裂
            if (impactSpeed >= splitSpeedThreshold && !isSplit)
            {
                TriggerSplit(impactSpeed, collision.contacts[0].normal);
            }
        }
    }

    private void TriggerSplit(float speed, Vector2 hitNormal)
    {
        isSplit = true;

        // 1. 变小的动画
        transform.DOKill();
        transform.DOScale(originalScale * splitScaleMultiplier, 0.2f).SetEase(Ease.OutBack);

        // 2. 将玩家用反向的弹力弹起 (或者简单地向正上方弹)
        Vector2 bounceDir = hitNormal;
        if (Mathf.Abs(bounceDir.y) < 0.1f) bounceDir.y = 1f; // 确保稍微有个向上的抛物线

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(bounceDir.normalized * splitBounceForce, ForceMode2D.Impulse);

        // 3. 在原地生成一个无法移动的分身
        CreateClone(speed);
    }

    private void CreateClone(float speed)
    {
        if (slimeClonePrefab == null)
        {
            Debug.LogWarning("未分配 SlimeClone 预制体！请在 Player 面板中设置 Slime Clone Prefab。");
            return;
        }

        Vector3 spawnPos = transform.position - new Vector3(0, originalScale.y * (1f - splitScaleMultiplier) * 0.5f, 0); // 放在脚底

        // 实例化分身预制体，完全使用预制体自带的尺寸
        GameObject cloneObj = Instantiate(slimeClonePrefab, spawnPos, Quaternion.identity);

        SlimeClone cloneScript = cloneObj.GetComponent<SlimeClone>();
        if (cloneScript == null) cloneScript = cloneObj.AddComponent<SlimeClone>();

        cloneScript.Init(this, speed);
    }

    public void RestoreFromSplit()
    {
        isSplit = false;
        transform.DOKill();
        transform.DOScale(originalScale, 0.3f)
        .SetEase(Ease.OutBack)
        .OnComplete(() =>
        {
            bool isStuck = CheckPlayerStuck();
            if (isStuck)
            {
                stats.TakeDamage(1);
            }
        });
    }

    // 检测是否卡在障碍物里
    bool CheckPlayerStuck()
    {
        Vector2 center = col.bounds.center;
        Vector2 size = col.bounds.size;

        // 检测当前区域是否有障碍物层（只检测墙、地面，忽略玩家、道具）
        Collider2D hit = Physics2D.OverlapBox(center, size, 0, obstacleLayer);

        // 有碰撞重叠 = 被卡住
        return hit != null;
    }


}
