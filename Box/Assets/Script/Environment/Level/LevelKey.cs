using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class LevelKey : MonoBehaviour
{
    [Header("绑定的门")]
    [Tooltip("捡起该钥匙后会解锁的门")]
    public NextLevelDoor targetDoor;

    [Header("钥匙被吃掉时的效果")]
    public float collectDuration = 0.3f;

    [Header("待机动画设置")]
    public float floatHeight = 0.2f;   // 悬浮高度
    public float floatDuration = 1f;   // 悬浮周期时长

    private bool isCollected = false;
    private Vector3 initialScale;

    private void Start()
    {
        initialScale = transform.localScale;

        // 1. 上下悬浮动画
        transform.DOMoveY(transform.position.y + floatHeight, floatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // 2. Q弹呼吸/缩放变形动画 (横向稍微拉伸，纵向稍微压扁，交替循环)
        transform.DOScale(new Vector3(initialScale.x * 1.1f, initialScale.y * 0.9f, initialScale.z), floatDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        if (collision.CompareTag("Player") || collision.GetComponent<Entity>() != null)
        {
            isCollected = true;

            // 通知门解锁
            if (targetDoor != null)
            {
                targetDoor.UnlockDoor();
            }

            // 吃掉时先清除所有待机悬浮动画
            transform.DOKill();

            // 恢复正常的比例为基准，然后做缩小消失动画
            transform.localScale = initialScale;
            transform.DOScale(Vector3.zero, collectDuration).SetEase(Ease.InBack).OnComplete(() =>
            {
                Destroy(gameObject);
            });
        }
    }
}
