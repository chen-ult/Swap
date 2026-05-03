using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class CameraFocusTrigger : MonoBehaviour
{
    [Header("摄像机聚焦设置")]
    [Tooltip("把场景里的一个空物体拖到这里，玩家进入后摄像机会平滑移动到这个空物体的中心点。")]
    public Transform focusTarget;

    [Tooltip("是否在移动焦点的同时改变摄像机的大小？")]
    public bool changeSize = false;

    [Tooltip("聚焦时的目标视野大小（如果勾选上方选项才生效）")]
    public float targetZoomSize = 5f;

    [Header("动画设置")]
    [Tooltip("平滑过渡过去花费的时间")]
    public float tweenDuration = 1.5f;
    public Ease easeType = Ease.InOutSine;

    [Header("离开设置")]
    [Tooltip("玩家离开触发区域后，是否恢复到当初进去前的位置和大小？")]
    public bool revertOnExit = true;

    private Camera mainCam;
    private float originalSize;

    // 我们用世界坐标来记录，因为 Focus Target 也是在世界坐标系摆放的
    private Vector3 originalPos;

    private Sequence currentSequence;

    // 防止玩家频繁进出时把半路上的坐标当成“原始坐标”导致偏移的终极防线
    private bool hasSavedOriginalState = false;

    // 一旦玩家死亡，锁死所有离开触发器后引起的摄像机回退
    private bool isPlayerDead = false;

    private void Start()
    {
        mainCam = Camera.main;
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnEnable()
    {
        Player.OnPlayerDeath += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        Player.OnPlayerDeath -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath()
    {
        isPlayerDead = true;
    }

    private void OnDestroy()
    {
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (mainCam == null) return;

        if (collision.CompareTag("Player") )
        {
            if (focusTarget == null)
            {
                Debug.LogWarning("触发器忘记绑定 Focus Target（焦点空物体）了！", gameObject);
                return;
            }

            // 打断正在进行的旧动画
            if (currentSequence != null && currentSequence.IsActive())
                currentSequence.Kill();

            // 【防偏移修复】：只有当之前没有存过原位置时（即全新的一次进入），才记录当前的摄像机状态！
            // 如果玩家跑出去一半又跑回来，这里直接跳过，保留最最开始那个正确的原位置！
            if (!hasSavedOriginalState)
            {
                originalSize = mainCam.orthographicSize;
                originalPos = mainCam.transform.position;
                hasSavedOriginalState = true;
            }

            currentSequence = DOTween.Sequence();

            // 构建目标坐标。绝配坑点：在 2D 游戏里，绝不能改变摄像机原来的 Z 轴深度（比如 -10），否则画面全消！
            Vector3 targetPos = new Vector3(focusTarget.position.x, focusTarget.position.y, originalPos.z);

            float finalSize = changeSize ? targetZoomSize : originalSize;

            // 让摄像机一起去目标点（世界坐标）和缩放
            currentSequence.Join(mainCam.DOOrthoSize(finalSize, tweenDuration).SetEase(easeType));
            currentSequence.Join(mainCam.transform.DOMove(targetPos, tweenDuration).SetEase(easeType));
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (mainCam == null) return;

        // 如果玩家已经阵亡（通常伴随着死亡时移除碰撞/销毁导致的触发退出），直接锁死放弃还原画面。
        if (isPlayerDead) return;

        if (revertOnExit && collision.CompareTag("Player") )
        {
            if (currentSequence != null && currentSequence.IsActive())
                currentSequence.Kill();

            currentSequence = DOTween.Sequence();

            // 平滑地退回当初记录的那个老位置！
            currentSequence.Join(mainCam.DOOrthoSize(originalSize, tweenDuration).SetEase(easeType));
            currentSequence.Join(mainCam.transform.DOMove(originalPos, tweenDuration).SetEase(easeType));

            // 当摄像机完完全全、毫厘不差地退回到原本的位置后，清空保护锁
            currentSequence.OnComplete(() => 
            {
                hasSavedOriginalState = false; 
            });
        }
    }
}