using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SwappableObject))]
[RequireComponent(typeof(Collider2D))]
public class MomentumElevator : MonoBehaviour
{
    [Header("触发要求")]
    [Tooltip("需要接收多大的速度才能启动电梯？")]
    public float requiredSpeed = 15f;

    [Header("路径设置")]
    [Tooltip("电梯开往的终点位置（请在这里绑定一个空物体做坐标标记）")]
    public Transform endPoint;

    [Header("运动表现 (DOTween)")]
    [Tooltip("单程移动花费的时间")]
    public float moveDuration = 2f;
    [Tooltip("极其平滑的电梯加减速曲线")]
    public Ease moveEase = Ease.InOutQuad;

    [Tooltip("是否移动完后会自动退回原点？（如果不勾选，则到了对岸后，需要再次被注入速度才会开回来）")]
    public bool autoReturn = false;
    [Tooltip("到达后停留多久再自动退回（仅勾选自动退回时有效）")]
    public float returnDelay = 1f;

    [Header("阻挡反弹设置")]
    [Tooltip("电梯碰到了哪些层级的物体会被判定为阻挡？(推荐勾选 Ground, Obstacle等。注意避开 Player/Box )")]
    public LayerMask obstacleLayer;
    [Tooltip("如果被障碍物阻挡，电梯原地停留多少秒后自动退回？")]
    public float obstaclePauseTime = 1f;

    [Header("悬浮要求文字")]
    [Tooltip("也可以绑定一个特定的空物体来吸附文字位置！")]
    public Transform customTextPosition;
    public Vector3 textOffset = new Vector3(0, 1.5f, 0);
    public Color textColor = Color.white;
    public Color rejectColor = Color.red;

    private Rigidbody2D rb;
    private Vector3 startPos;
    private Vector3 targetPos;

    private bool isMoving = false;
    private bool isAtEnd = false;
    private bool isReturningFromObstacle = false;

    private GameObject textObj;
    private TextMesh requireText;
    private Sequence textBounceSeq;
    private Tweener elevatorTweener;
    private Tweener holderTweener;

    private Transform passengerHolder;
    private Rigidbody2D passengerHolderRb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        startPos = transform.position;

        // 创建乘客托盘（解决父子层级报错）
        passengerHolder = new GameObject("PassengerHolder_" + gameObject.name).transform;
        passengerHolder.SetParent(null);
        passengerHolder.position = transform.position;

        passengerHolderRb = passengerHolder.gameObject.AddComponent<Rigidbody2D>();
        passengerHolderRb.bodyType = RigidbodyType2D.Kinematic;
        passengerHolderRb.interpolation = RigidbodyInterpolation2D.Interpolate;

        InitializeText();
    }

    private void Start()
    {
        if (endPoint != null)
        {
            targetPos = endPoint.position;
            endPoint.SetParent(null);
        }
        else
        {
            Debug.LogError("动量电梯没有绑定终点 endpoint 坐标！", gameObject);
            enabled = false;
        }
    }

    private void InitializeText()
    {
        textObj = new GameObject("ElevatorRequirement_" + gameObject.name);
        textObj.transform.SetParent(transform);

        // 修复：初始化文字缩放
        textObj.transform.localScale = Vector3.one;

        if (customTextPosition != null)
            textObj.transform.position = customTextPosition.position;
        else
            textObj.transform.localPosition = textOffset;

        requireText = textObj.AddComponent<TextMesh>();
        requireText.anchor = TextAnchor.MiddleCenter;
        requireText.alignment = TextAlignment.Center;
        requireText.characterSize = 0.05f;
        requireText.fontSize = 80;
        requireText.color = textColor;
        requireText.text = "≥ " + requiredSpeed.ToString("F0");

        MeshRenderer meshRenderer = textObj.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = "Ground";
            meshRenderer.sortingOrder = 80;
        }

        textObj.transform.DOLocalMoveY(textObj.transform.localPosition.y + 0.15f, 1f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void Update()
    {
        // 防文字翻转
        if (textObj != null)
        {
            float parentSignX = Mathf.Sign(transform.lossyScale.x);
            Vector3 currentScale = textObj.transform.localScale;
            textObj.transform.localScale = new Vector3(Mathf.Abs(currentScale.x) * parentSignX, currentScale.y, currentScale.z);
        }

        if (isMoving)
        {
            if (rb.linearVelocity.magnitude > 0.1f)
                rb.linearVelocity = Vector2.zero;
            return;
        }

        float currentSpeed = rb.linearVelocity.magnitude;

        if (currentSpeed > 0.5f)
        {
            if (currentSpeed >= requiredSpeed)
            {
                rb.linearVelocity = Vector2.zero;
                TriggerElevator();
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
                ShowRejectFeedback();
            }
        }
    }

    private void TriggerElevator()
    {
        isMoving = true;

        Vector3 destination;
        if (isReturningFromObstacle)
            destination = isAtEnd ? targetPos : startPos;
        else
            destination = isAtEnd ? startPos : targetPos;

        // 打断旧动画
        if (elevatorTweener != null && elevatorTweener.IsActive()) elevatorTweener.Kill();
        if (holderTweener != null && holderTweener.IsActive()) holderTweener.Kill();

        // 启动反馈动画
        if (textBounceSeq != null && textBounceSeq.IsActive()) textBounceSeq.Kill();
        textBounceSeq = DOTween.Sequence();
        textBounceSeq.Append(DOTween.To(() => requireText.color, x => requireText.color = x, Color.green, 0.1f));
        textBounceSeq.Append(textObj.transform.DOScale(new Vector3(1.3f, 0.7f, 1f), 0.1f));
        textBounceSeq.Append(textObj.transform.DOScale(Vector3.one, 0.2f));
        textBounceSeq.Append(DOTween.To(() => requireText.color, x => requireText.color = x, textColor, 0.5f));

        // 双刚体同步移动（FixedUpdate物理帧，防抖）
        if (passengerHolderRb != null)
            holderTweener = passengerHolderRb.DOMove(destination, moveDuration).SetEase(moveEase).SetUpdate(UpdateType.Fixed);

        elevatorTweener = rb.DOMove(destination, moveDuration).SetEase(moveEase).SetUpdate(UpdateType.Fixed);
        elevatorTweener.OnComplete(() =>
        {
            isMoving = false;

            // 障碍物退回完成
            if (isReturningFromObstacle)
            {
                isReturningFromObstacle = false;
                return;
            }

            // 更新位置状态
            isAtEnd = Vector3.Distance(transform.position, targetPos) < 0.05f;

            // 自动返回逻辑（修复：只调用一次）
            if (isAtEnd && autoReturn)
            {
                DOVirtual.DelayedCall(returnDelay, () =>
                {
                    if (this != null && gameObject.activeInHierarchy)
                    {
                        TriggerElevator();
                    }
                }).SetLink(gameObject);
            }
        });
    }

    private void ShowRejectFeedback()
    {
        if (textBounceSeq != null && textBounceSeq.IsActive()) return;

        textBounceSeq = DOTween.Sequence();
        textBounceSeq.Append(DOTween.To(() => requireText.color, x => requireText.color = x, rejectColor, 0.05f));
        textBounceSeq.Join(textObj.transform.DOShakePosition(0.5f, new Vector3(0.2f, 0, 0), 12, 90));
        textBounceSeq.Append(DOTween.To(() => requireText.color, x => requireText.color = x, textColor, 0.3f));
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!gameObject.activeInHierarchy || !collision.gameObject.activeInHierarchy) return;

        // 障碍物碰撞检测
        if ((obstacleLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            if (isMoving && !isReturningFromObstacle)
            {
                HandleObstacleCollision();
            }
        }

        // 玩家/箱子 上电梯
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Box"))
        {
            if (passengerHolder != null)
                collision.transform.SetParent(passengerHolder);
        }
    }

    private void HandleObstacleCollision()
    {
        // 紧急刹车
        if (elevatorTweener != null && elevatorTweener.IsActive()) elevatorTweener.Kill();
        if (holderTweener != null && holderTweener.IsActive()) holderTweener.Kill();

        // 碰撞反馈
        if (textBounceSeq != null && textBounceSeq.IsActive()) textBounceSeq.Kill();
        textBounceSeq = DOTween.Sequence();
        textBounceSeq.Append(DOTween.To(() => requireText.color, x => requireText.color = x, rejectColor, 0.1f));
        textBounceSeq.Join(textObj.transform.DOShakePosition(0.5f, new Vector3(0.1f, 0.1f, 0), 20, 90));
        textBounceSeq.Append(DOTween.To(() => requireText.color, x => requireText.color = x, textColor, 0.3f));

        // 延迟返回
        DOVirtual.DelayedCall(obstaclePauseTime, () =>
        {
            if (this == null || !gameObject.activeInHierarchy) return;

            isReturningFromObstacle = true;
            TriggerElevator();

        }).SetLink(gameObject);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!gameObject.activeInHierarchy || !collision.gameObject.activeInHierarchy) return;

        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Box"))
        {
            if (passengerHolder != null && collision.transform.parent == passengerHolder)
            {
                collision.transform.SetParent(null);
            }
        }
    }

    private void OnDisable()
    {
        // 安全卸载乘客
        if (passengerHolder != null)
        {
            for (int i = passengerHolder.childCount - 1; i >= 0; i--)
            {
                Transform child = passengerHolder.GetChild(i);
                if (child.CompareTag("Player") || child.CompareTag("Box"))
                {
                    child.SetParent(null);
                }
            }
        }
    }

    private void OnDestroy()
    {
        // 清理DOTween
        if (textBounceSeq != null) textBounceSeq.Kill();
        if (textObj != null) textObj.transform.DOKill();
        if (passengerHolder != null) passengerHolder.DOKill();
        transform.DOKill();

        // 销毁托盘
        if (passengerHolder != null)
            Destroy(passengerHolder.gameObject);
    }
}