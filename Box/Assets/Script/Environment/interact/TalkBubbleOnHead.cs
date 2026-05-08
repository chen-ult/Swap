using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class BubblePopAnim : MonoBehaviour
{
    [Header("👇 把你自己做的气泡拖这里")]
    public GameObject talkBubble;

    [Header("动画参数")]
    public float popTime = 0.3f;
    public float bounceScale = 2f;

    private RectTransform _bubbleRect;

    void Awake()
    {
        // 自动设置触发器
        GetComponent<Collider2D>().isTrigger = true;

        // 初始隐藏气泡
        if (talkBubble != null)
        {
            talkBubble.SetActive(false);
            _bubbleRect = talkBubble.GetComponent<RectTransform>();
        }
    }

    // 玩家靠近 → 弹出
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            ShowBubble();
    }

    // 玩家离开 → 收回
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            HideBubble();
    }

    void ShowBubble()
    {
        if (talkBubble == null || _bubbleRect == null) return;

        talkBubble.SetActive(true);
        _bubbleRect.localScale = Vector3.zero;

        // 弹出动画 + 轻轻弹跳
        _bubbleRect.DOScale(Vector3.one, popTime)
                   .SetEase(Ease.OutBack)
                   .OnComplete(() =>
                   {
                       _bubbleRect.DOScale(Vector3.one * bounceScale, 0.2f)
                                  .SetLoops(2, LoopType.Yoyo)
                                  .SetEase(Ease.InOutSine);
                   });
    }

    void HideBubble()
    {
        if (talkBubble == null || _bubbleRect == null) return;

        _bubbleRect.DOScale(Vector3.zero, popTime * 0.8f)
                   .SetEase(Ease.InBack)
                   .OnComplete(() => talkBubble.SetActive(false));
    }
}