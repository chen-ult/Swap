using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpringBlock : MonoBehaviour
{
    [Header("弹跳设置")]
    [Tooltip("是否自动根据碰撞面反弹？（勾选后，踩上面往上弹，顶下面往下弹，撞侧面往回弹）")]
    public bool autoOppositeDirection = true;

    [Tooltip("固定弹跳的方向（如果不勾选自动反弹，则使用此固定方向）")]
    public Vector2 fixedBounceDirection = Vector2.up;

    [Tooltip("弹力大小")]
    public float bounceForce = 15f;

    [Tooltip("弹起后的失控保护时间（秒），防止起跳瞬间移速被玩家覆盖")]
    public float bounceLockDuration = 0.15f;

    [Header("动画(可选)")]
    [Tooltip("如果有Animator，可以在弹起时播放动画")]
    public Animator anim;
    [Tooltip("弹起动画的Trigger名称")]
    public string bounceTriggerName = "Bounce";

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 尝试获取碰撞对象上的 Entity 组件 (玩家或敌人)
        Entity entity = collision.gameObject.GetComponent<Entity>();

        // 尝试获取普通刚体 (比如场景里可以被弹飞的普通箱子)
        Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();

        // 核心改动：获取反弹方向
        Vector2 dir = Vector2.up;
        if (autoOppositeDirection)
        {
            // Unity物理引擎的小坑：在 OnCollisionEnter2D 里，如果脚本挂在弹簧上，
            // 拿到接触点的法线 (normal) 其实是指向弹簧内部的！这会导致弹力把玩家死死按在地板上！
            // 所以我们需要给法线加一个负号 (-)，让它反过来指向玩家（向外弹）。
            dir = -collision.GetContact(0).normal;

            // 为了手感更好，强制把它规整为绝对的上下左右四个方向之一（不会因为撞到拐角而斜着乱飞）
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                dir = new Vector2(Mathf.Sign(dir.x), 0);
            }
            else
            {
                dir = new Vector2(0, Mathf.Sign(dir.y));
            }
        }
        else
        {
            dir = fixedBounceDirection.normalized;
        }

        if (entity != null)
        {
            // 如果是带有 Entity 的角色，使用我们之前写好的保护机制
            entity.ApplyBounceKnockback(dir, bounceForce, bounceLockDuration);
            PlayBounceAnimation();
        }
        else if (rb != null)
        {
            // 如果只是个普通刚体，直接粗暴地赋予速度和力
            Vector2 currentVel = rb.linearVelocity;
            
            // 清除施力方向上的旧速度，防止力叠加或抵消
            if (Mathf.Abs(dir.x) > 0.1f) currentVel.x = 0;
            if (Mathf.Abs(dir.y) > 0.1f) currentVel.y = 0;
            
            rb.linearVelocity = currentVel;
            rb.AddForce(dir * bounceForce, ForceMode2D.Impulse);
            
            PlayBounceAnimation();
        }
    }

    private void PlayBounceAnimation()
    {
        if (anim != null && !string.IsNullOrEmpty(bounceTriggerName))
        {
            anim.SetTrigger(bounceTriggerName);
        }
    }
}