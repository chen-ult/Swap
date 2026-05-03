using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpikeTrap : MonoBehaviour
{
    [Header("陷阱设置")]
    [Tooltip("造成的伤害值（玩家只有1滴血，填1或者999都可以秒杀）")]
    public int damageAmount = 1;

    // 如果你的尖刺是实体的（玩家踩在上面不掉下去），用这个触发
    private void OnCollisionEnter2D(Collision2D collision)
    {
        DealDamage(collision.gameObject);
    }

    // 如果你的尖刺是触发器（勾选了 Is Trigger），用这个触发
    private void OnTriggerEnter2D(Collider2D collider)
    {
        DealDamage(collider.gameObject);
    }

    private void DealDamage(GameObject target)
    {
        // ------------- 摧毁与可以重生物体的逻辑 -------------
        TrapRespawnable respawnable = target.GetComponent<TrapRespawnable>();
        if (respawnable != null)
        {
            respawnable.TriggerTrapDeath();
            return; // 处理完刷新直接退出，不走下面的扣血逻辑
        }

        // 尝试获取碰到的物体身上的血量系统
        Entity_Stats stats = target.GetComponent<Entity_Stats>();
        
        // 如果碰到的东西有血量系统，且没有死，就扣血
        if (stats != null && !stats.isDead)
        {
            stats.TakeDamage(damageAmount);
        }
    }
}