using System;
using UnityEngine;

public class Entity_Stats : MonoBehaviour
{
    private Entity entity;

    [Header("血量系统 (几点血就是几颗心)")]
    public int maxHealth = 1; // 默认3颗心
    public int currentHealth;

    // 当血量发生变化时触发的事件（非常适合用来通知UI血条更新）
    public event Action<int, int> OnHealthChanged; 
    
    // 是否死亡的标志
    public bool isDead { get; private set; }

    protected virtual void Awake()
    {
        entity = GetComponent<Entity>();
    }

    protected virtual void Start()
    {
        // 初始化血量
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// 受到伤害的方法
    /// </summary>
    /// <param name="damage">承受的伤害值</param>
    public virtual void TakeDamage(int damage)
    {
        // 如果实体已死亡，或者处于无敌状态，不受伤害
        if (isDead || (entity != null && entity.IsInvulnerable))
            return;

        currentHealth -= damage;
        
        // 确保血量不会掉到0以下
        if (currentHealth < 0) 
            currentHealth = 0;

        // 触发血量更新事件
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"{gameObject.name} 受到了 {damage} 点伤害！当前血量：{currentHealth}/{maxHealth}");

        // 如果这是玩家，播放受伤音效（由 Player 持有音源和音效引用）
        var player = entity as Player;
        if (player != null && player.audioSource != null && player.sfx_PlayerHurt != null)
        {
            player.audioSource.PlayOneShot(player.sfx_PlayerHurt, player.sfxVolume);
        }

        // 检测是否死亡
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 回复血量的方法
    /// </summary>
    public virtual void Heal(int healAmount)
    {
        if (isDead) return;

        currentHealth += healAmount;
        
        // 确保不会超过最大血量
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// 死亡逻辑
    /// </summary>
    protected virtual void Die()
    {
        isDead = true;

        if (entity != null)
        {
            // 调用实体的死亡逻辑（对于玩家来说，它就会执行回到存档点等操作）
            entity.EntityDeath();
        }
        else
        {
            // 如果没有挂载 Entity，仅作基础护底销毁
            Destroy(gameObject); 
        }
    }
}
