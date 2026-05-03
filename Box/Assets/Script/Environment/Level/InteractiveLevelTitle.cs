using UnityEngine;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(TextMeshProUGUI))]
public class InteractiveLevelTitle : MonoBehaviour
{
    [Header("关卡标题设置")]
    public string levelTitle = "第一关：新的开始";
    public float entranceDelay = 0.5f; // 开场延迟几秒出现

    private TextMeshProUGUI textUI;
    private Vector3 originalScale;

    private void Awake()
    {
        textUI = GetComponent<TextMeshProUGUI>();
        textUI.text = levelTitle;

        // 记录预设里排版好的缩放大小，并把当前大小捏成0，准备播放动画
        originalScale = transform.localScale;
        transform.localScale = Vector3.zero;

        // 确保文字一开始是完全透明的
        Color c = textUI.color;
        c.a = 0;
        textUI.color = c;
    }

    private void Start()
    {
        PlayCuteEntrance();
    }

    private void PlayCuteEntrance()
    {
        Sequence seq = DOTween.Sequence();

        // 延迟一小会儿再出现
        seq.AppendInterval(entranceDelay);

        // 1. Q弹变大出现 (OutBack中的参数2.5f带来了极致的果冻回弹感！)
        seq.Append(transform.DOScale(originalScale, 0.8f).SetEase(Ease.OutBack, 2.5f));

        // 伴随着透明度的快速淡入
        seq.Join(textUI.DOFade(1f, 0.5f));

        // 2. 刚刚长到最大时，顺势来一个轻微的左摇右摆 (像可爱地跟你打招呼)
        seq.Append(transform.DOPunchRotation(new Vector3(0, 0, 12f), 0.6f, 6, 0.5f));

        // 3. 待机动画：进入一个无限循环的呼吸漂浮效果，让挂在那里的UI看起来肉嘟嘟活生生的
        seq.AppendCallback(() => {
            // 持续的轻微上下呼吸浮动（幅度10像素，1.5秒一次）
            transform.DOLocalMoveY(transform.localPosition.y + 10f, 1.5f)
                     .SetLoops(-1, LoopType.Yoyo)
                     .SetEase(Ease.InOutSine);
        });
    }

    private void OnDestroy()
    {
        // 销毁时清理所有相关的补间动画防止报错
        transform.DOKill();
        if (textUI != null) textUI.DOKill();
    }
}