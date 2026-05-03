using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class CollectibleStar : MonoBehaviour
{
    [Header("奖励设置")]
    [Tooltip("吃掉这颗星星能得到多少分/几颗星")]
    public int starValue = 1;

    [Tooltip("如果你有粒子特效预制体，可以拖放到这里。吃掉时会自动生成！")]
    public GameObject collectEffect; 

    [Header("待机动画设置 (DOTween)")]
    [Tooltip("上下浮动的幅度")]
    public float floatHeight = 0.25f;
    [Tooltip("浮动一个来回花费的时间")]
    public float floatDuration = 1.5f;

    private bool isCollected = false;

    private void Start()
    {
        // 自动将碰撞体设为触发器，绝对不会阻挡玩家走路
        GetComponent<Collider2D>().isTrigger = true;

        // 1. 无限上下浮动的待机动画
        transform.DOMoveY(transform.position.y + floatHeight, floatDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        // 2. Q弹呼吸缩放：让星星像果冻一样微微拉伸和拍扁（去掉惹人厌的3D翻转）
        transform.DOScale(new Vector3(1.1f, 0.9f, 1f), floatDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 如果已经被吃了哪怕一毫秒，也不允许被重复吃引发多次加分
        if (isCollected) return;

        // 只有玩家能吃（箱子碰到了不算）
        if (collision.CompareTag("Player"))
        {
            CollectStar();
        }
    }

    private void CollectStar()
    {
        isCollected = true;

        // 暴力打断刚才的待机无限动画
        transform.DOKill();

        // 收集表现动画第一步：接触瞬间稍微发光变大一下，像果冻弹起
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(new Vector3(1.5f, 1.5f, 1.5f), 0.1f)); 

        // ----- 第二步：飞向我们屏幕左上角真正的收集栏 UI (由 UIManager 管理分配位置) ------
        int targetSlotIndex = -1;
        Vector3 targetUIWorldPos = transform.position; // 默认防报错

        if (UIManager.Instance != null)
        {
            // 向管理者报告“我被吃了”，索取我这个排号的空槽真实在世界上的对应物理坐标在哪！
            targetUIWorldPos = UIManager.Instance.ClaimNextStarTargetPosition(out targetSlotIndex);
        }

        // 把这颗星星直接使用极快、带抛物线的弧形动作，射入那个 UI 位置里去！
        seq.Append(transform.DOMove(targetUIWorldPos, 0.6f).SetEase(Ease.InCubic));
        seq.Join(transform.DOScale(Vector3.zero, 0.4f).SetDelay(0.2f)); // 在拉过去的尾声被吸成小点点点亮它

        seq.OnComplete(() =>
        {
            // 如果你配了亮晶晶的粒子特效，这会儿在那个目标位置炸出来！
            if (collectEffect != null)
            {
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            }

            // 告诉 UI 真正的图可以被点亮了，并且产生UI颤动
            if (UIManager.Instance != null && targetSlotIndex != -1)
            {
                UIManager.Instance.LightUpStar(targetSlotIndex);
            }

            // 彻底销毁这个被吃掉的世界星星
            Destroy(gameObject);
        });
    }

    private void OnDestroy()
    {
        // 安全保护：如果没被吃过而是因为重开游戏被销毁，杀掉残留动画防止爆红字
        transform.DOKill();
    }
}