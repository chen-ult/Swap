using UnityEngine;
using DG.Tweening;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class TrapRespawnable : MonoBehaviour
{
    [Header("陷阱重生设置")]
    [Tooltip("如果物体碰到尖刺或掉出地图被摧毁了，是否在场景里某个地点复活？")]
    public bool respawnOnTrapDeath = true;

    [Tooltip("被陷阱弄死后，会在这个指定的空物体位置复活！（如果不填，就在开始游戏时的出生位置复活！）")]
    public Transform respawnPoint;

    [Tooltip("掉刺里死掉到重新复活所需花费的时间")]
    public float respawnDelay = 1f;

    // 记录最初出生的老位置、旋转和缩放，以防没有填自定义复活点，并原样恢复它
    private Vector3 initialSpawnPos;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private Collider2D col;
    private Rigidbody2D rb;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>(); // 箱子一般会带刚体，如果是没刚体的其他解谜道具也能兼容

        initialSpawnPos = transform.position;
        initialRotation = transform.rotation;
        initialScale = transform.localScale;
    }

    /// <summary>
    /// 被尖刺等陷阱调用，触发掉落虚空或被扎破
    /// </summary>
    public void TriggerTrapDeath()
    {
        if (respawnOnTrapDeath)
        {
            // 只要死不死，速度全清零，防止掉虚空里的惯性
            if (rb != null) rb.linearVelocity = Vector2.zero;
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            // 没有复活恩赐，直接永久死亡毁灭
            Destroy(gameObject);
        }
    }

    private IEnumerator RespawnRoutine()
    {
        // 瞬间死亡特效效果：缩小！
        transform.DOKill();
        transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);

        // 关闭物理干涉避免死掉了还能挡人
        col.enabled = false;
        if (rb != null) rb.simulated = false;

        // 在死亡时间里等一会
        yield return new WaitForSeconds(respawnDelay);

        // 重新出生
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
        }
        else
        {
            transform.position = initialSpawnPos; // 没有特别位置指定，在哪出生的回哪去
            transform.rotation = initialRotation;
        }

        // 重新恢复它的刚体和物理控制
        if (rb != null) 
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        col.enabled = true;

        // 伴随弹出的动画复活，恢复它原本的Scale大小
        transform.DOScale(initialScale, 0.3f).SetEase(Ease.OutBack);
    }

    private void OnDestroy()
    {
        // 如果物体被直接从场景抹除，随时剥离身边的Tween免得抛错
        transform.DOKill();
    }
}