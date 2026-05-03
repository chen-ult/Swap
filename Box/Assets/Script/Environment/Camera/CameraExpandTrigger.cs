using UnityEngine;

using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class CameraExpandTrigger : MonoBehaviour
{
    public enum ExpandDirection { All, Down, Up, Left, Right }

    [Header("视野扩展设置")]
    [Tooltip("是否改变摄像机大小？如果只想移动位置，取消勾选")]
    public bool changeSize = true;

    [Tooltip("目标视野大小（如果勾选改变大小才生效）")]
    public float targetZoomSize = 8f;

    [Tooltip("动画平滑过渡的时间（秒）")]
    public float tweenDuration = 1.5f;

    [Tooltip("围绕视野大小改变的自动偏移方向（仅在改变大小时起效，如果选All则为自身中心不变）")]
    public ExpandDirection expandDirection = ExpandDirection.All;

    [Header("手动位置偏移")]
    [Tooltip("相对于进入时的摄像机位置额外移动多少？(X为左右，Y为上下) 可以单纯只填这个来实现摄像机位置偏移")]
    public Vector2 manualOffset = Vector2.zero;

    public Ease easeType = Ease.InOutSine;

    [Header("离开设置")]
    [Tooltip("离开触发区域后，是否恢复原状？")]
    public bool revertOnExit = true;

    private Camera mainCam;
    private float originalSize;
    private Vector3 originalLocalPos;
    
    // 保存当前的动画序列，防止频繁进出触发器导致动画冲突
    private Sequence currentSequence;

    // 防止玩家频繁进出导致记录错误的半路坐标偏移防线
    private bool hasSavedOriginalState = false;

    private void Start()
    {
        mainCam = Camera.main;
        // 把触发器勾选为 IsTrigger
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")|| collision.CompareTag("Box"))
        {
            // 打断正在进行的旧动画
            if (currentSequence != null && currentSequence.IsActive())
                currentSequence.Kill();

            // 只有当之前没有存过原位置时（全新的一次进入），才记录当前的初始状态
            if (!hasSavedOriginalState)
            {
                originalSize = mainCam.orthographicSize;
                originalLocalPos = mainCam.transform.localPosition;
                hasSavedOriginalState = true;
            }

            currentSequence = DOTween.Sequence();

            // 目标大小，如果不想改变大小就维持原样
            float finalTargetSize = changeSize ? targetZoomSize : originalSize;

            // 计算视野增加的差值
            float sizeDiff = finalTargetSize - originalSize;

            // 屏幕宽高比，用于计算横向偏移补偿
            float aspect = mainCam.aspect; 

            Vector3 targetPos = originalLocalPos;

            // 核心推导：基于缩放方向的自动偏移补偿
            if (changeSize)
            {
                switch (expandDirection)
                {
                    case ExpandDirection.All: break;
                    case ExpandDirection.Down: targetPos.y -= sizeDiff; break;
                    case ExpandDirection.Up: targetPos.y += sizeDiff; break;
                    case ExpandDirection.Left: targetPos.x -= (sizeDiff * aspect); break;
                    case ExpandDirection.Right: targetPos.x += (sizeDiff * aspect); break;
                }
            }

            // 叠加你想要的手动位置偏移
            targetPos.x += manualOffset.x;
            targetPos.y += manualOffset.y;

            // 同步执行放大视野 + 偏移位置动画
            currentSequence.Join(mainCam.DOOrthoSize(finalTargetSize, tweenDuration).SetEase(easeType));
            currentSequence.Join(mainCam.transform.DOLocalMove(targetPos, tweenDuration).SetEase(easeType));
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (revertOnExit && collision.CompareTag("Player"))
        {
            if (currentSequence != null && currentSequence.IsActive())
                currentSequence.Kill();

            currentSequence = DOTween.Sequence();
            
            // 恢复初始大小和初始位置
            currentSequence.Join(mainCam.DOOrthoSize(originalSize, tweenDuration).SetEase(easeType));
            currentSequence.Join(mainCam.transform.DOLocalMove(originalLocalPos, tweenDuration).SetEase(easeType));

            currentSequence.OnComplete(() => 
            {
                hasSavedOriginalState = false;
            });
        }
    }

    private void OnDestroy()
    {
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }
    }
}