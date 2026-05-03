using System.Xml;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class Enemy : Entity
{
    public Enemy_IdleState idleState;
    public Enemy_MoveState moveState;

    [Header("战斗状态参数")]
    public float battleMoveSpeed = 3f;//战斗移动速度
    public float attackDistance = 2f;//攻击距离
    public float battleTimeDuration = 5f;//战斗时间持续时间
    public float minRetreatDistance = 1;//最小撤退距离
    public Vector2 retreatVelocity;//撤退速度

    [Header("反击参数(stunned state)")]
    public float stunnedDuration = 1;//眩晕持续时间
    public Vector2 stunnedVelocity = new Vector2(7, 7);//眩晕速度
    [SerializeField] protected bool canBeStunned;//是否可以被眩晕

    [Header("移动参数")]
    public float idleTime = 2f;//空闲时间
    public float moveSpeed = 1.4f;//移动速度
    [Range(0, 2)]
    public float moveAnimSpeedMultiplier = 1f;//移动动画速度倍率

    [Header("检测玩家参数")]
    [SerializeField] private LayerMask whatIsPlayer;//玩家图层
    [SerializeField] private Transform playerCheck;//玩家检测点
    [SerializeField] private float playerCheckDistance = 10f;//玩家检测距离
    public Transform player { get; private set; }//玩家引用


    

    protected virtual void HandlePlayerDeath()//玩家死亡后的行为逻辑
    {
        
    }

    public virtual void TryEnterBattleState(Transform player)//尝试进入战斗状态
    {
        

    }

    public Transform GetPlayerReference()//获取玩家引用
    {
        if (player == null)
            player = PlayerDetected().transform;

        return player;
    }

    public RaycastHit2D PlayerDetected()//检测玩家
    {
        RaycastHit2D hit =
            Physics2D.Raycast(playerCheck.position, Vector2.right * facingDir, playerCheckDistance, whatIsPlayer | whatIsGround);

        if (hit.collider == null || hit.collider.gameObject.layer != LayerMask.NameToLayer("Player"))
            return default;

        return hit;
    }

    

    private void OnEnable()//订阅玩家死亡事件
    {
        Player.OnPlayerDeath += HandlePlayerDeath;
    }

    private void OnDisable()//取消订阅玩家死亡事件
    {
        Player.OnPlayerDeath -= HandlePlayerDeath;
    }
}
