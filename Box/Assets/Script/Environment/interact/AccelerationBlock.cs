using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AccelerationBlock : MonoBehaviour
{
    [Header("加速设置")]
    [Tooltip("加速方向是否使用方块自身的Transform.right（红轴）")]
    public bool useTransformRight = true;

    [Tooltip("自定义加速方向（当 useTransformRight 为 false 时生效）")]
    public Vector2 customDirection = Vector2.right;

    [Tooltip("加速力的大小")]
    public float accelerationForce = 20f;

    [Tooltip("加速时，暂时锁定玩家输入的持续时间（避免速度被立刻重置）")]
    public float boostDuration = 0.2f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ApplyAcceleration(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 兼容非触发器模式时的碰撞
        ApplyAcceleration(collision.gameObject);
    }

    private void ApplyAcceleration(GameObject obj)
    {
        Vector2 dir = useTransformRight ? (Vector2)transform.right : customDirection.normalized;

        Entity entity = obj.GetComponent<Entity>();
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();

        if (entity != null)
        {
            // 对于玩家等Entity，需要使用 ApplyBoost 将 isKnocked 设为 true，
            // 否则在 Update 中的 SetVelocity 会在下一帧立刻将速度重置。
            entity.ApplyBoost(dir, accelerationForce, boostDuration);
        }
        else if (rb != null)
        {
            // 对普通刚体施加瞬间加速
            rb.AddForce(dir * accelerationForce, ForceMode2D.Impulse);
        }
    }
}
