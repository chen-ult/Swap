using UnityEngine;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(TextMeshProUGUI))]
public class InteractiveLevelTitle : MonoBehaviour
{
    [Header("关卡标题设置")]
    public string levelTitle = "第一关：新的开始";
    public float entranceDelay = 0.5f; // 开场延迟几秒出现

    [Header("悬浮动画设置")]
    public float floatAmplitude = 25f; // UI画布下的浮动幅度（放大此数值以匹配左侧实体的视觉幅度）

    private TextMeshProUGUI textUI;
    private Vector3 originalScale;
    private float startY; // 记录初始高度基准
    private bool isFloating = false;

    private void Awake()
    {
        textUI = GetComponent<TextMeshProUGUI>();
        textUI.text = levelTitle;

        // 记录预设里排版好的缩放大小，并把当前大小捏成0，准备播放动画
        originalScale = transform.localScale;
        startY = transform.localPosition.y;
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

        // 3. 待机动画：切换为Update里基于全局真实时间的数学浮动，这样能与关卡内的UI元件完美同步跳动
        seq.AppendCallback(() => {
            isFloating = true;
        });
    }

    private void Update()
    {
        if (isFloating)
        {
            // 使用 Time.time 保持全局绝对同步，并直接使用面板上可调的 floatAmplitude
            float floatOffset = Mathf.Sin((Time.time) * Mathf.PI / 1.5f) * floatAmplitude;
            transform.localPosition = new Vector3(transform.localPosition.x, startY + floatOffset, transform.localPosition.z);
        }
    }

    private void OnDestroy()
    {
        // 销毁时清理所有相关的补间动画防止报错
        transform.DOKill();
        if (textUI != null) textUI.DOKill();
    }
}