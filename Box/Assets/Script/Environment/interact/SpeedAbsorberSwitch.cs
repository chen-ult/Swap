using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class SpeedAbsorberSwitch : MonoBehaviour
{
    [Header("机关自身设置")]
    [Tooltip("机关吸收速度激活后替换的图片（可选）")]
    public Sprite switchActiveSprite;
    private Sprite originalSwitchSprite;
    private SpriteRenderer switchRenderer;

    [Header("目标障碍物设置")]
    [Tooltip("关联的多个障碍物对象（支持多个门或墙）")]
    public GameObject[] targetObstacles;

    [Tooltip("障碍物虚化时替换的图片")]
    public Sprite ghostSprite;

    [Tooltip("障碍物虚化时的透明度")]
    [Range(0f, 1f)]
    public float ghostAlpha = 0.5f;

    // 用于存储每个障碍物组件状态的内部结构
    private struct ObstacleState
    {
        public Collider2D col;
        public SpriteRenderer sr;
        public Sprite originalSprite;
    }
    private ObstacleState[] obstacleStates;

    [Header("倒计时显示设置")]
    [Tooltip("机关上方文本位置偏移")]
    public Vector3 switchTextOffset = new Vector3(0, 1.2f, 0);
    [Tooltip("障碍物上方文本位置偏移")]
    public Vector3 obstacleTextOffset = new Vector3(0, 1.2f, 0);
    public Color textColor = Color.yellow;
    [Tooltip("时间快结束时的警告颜色（剩下不到3秒时）")]
    public Color warningColor = Color.red;

    [Header("时间与吸收设置")]
    [Tooltip("吸收速度转化时间的倍率(默认1速度=1秒)")]
    public float speedToTimeMultiplier = 1f;
    [Tooltip("可吸收叠加的时间上限（秒）")]
    public float maxTimeLimit = 60f;

    private TextMesh switchText;
    private TextMesh obstacleGroupText;

    private float timer = 0f;
    private bool isGhosted = false;
    private int lastTickSecond = -1;

    private Transform firstObstacleTransform;

    void OnDestroy()
    {
        // 游戏物体销毁时，手动清理掉生成的UI避免残留在场景，并停止所有Tween动画
        if (switchText != null) 
        {
            switchText.transform.DOKill();
            Destroy(switchText.gameObject);
        }
        if (obstacleGroupText != null) 
        {
            obstacleGroupText.transform.DOKill();
            Destroy(obstacleGroupText.gameObject);
        }
    }

    void Start()
    {
        // 记录机关自身的图片组件
        switchRenderer = GetComponent<SpriteRenderer>();
        if (switchRenderer != null)
        {
            originalSwitchSprite = switchRenderer.sprite;
        }

        // 创建物体自身的文本显示
        switchText = CreateTextDisplay(transform, "SwitchText", switchTextOffset);

        // 初始化所有目标障碍物
        if (targetObstacles != null && targetObstacles.Length > 0)
        {
            obstacleStates = new ObstacleState[targetObstacles.Length];
            for (int i = 0; i < targetObstacles.Length; i++)
            {
                GameObject obs = targetObstacles[i];
                if (obs == null) continue;

                ObstacleState state = new ObstacleState();
                state.col = obs.GetComponent<Collider2D>();
                state.sr = obs.GetComponent<SpriteRenderer>();
                if (state.sr != null)
                {
                    state.originalSprite = state.sr.sprite;
                }

                obstacleStates[i] = state;
            }

            // 只在第一个有效的障碍物上方生成一个倒计时文本
            for (int i = 0; i < targetObstacles.Length; i++)
            {
                if (targetObstacles[i] != null)
                {
                    firstObstacleTransform = targetObstacles[i].transform;
                    obstacleGroupText = CreateTextDisplay(firstObstacleTransform, "ObstacleGroupText", obstacleTextOffset);
                    break;
                }
            }
        }
    }

    private TextMesh CreateTextDisplay(Transform target, string name, Vector3 offset)
    {
        GameObject textObj = new GameObject(name);
        // 先设为空父级，避免受任何父物体缩放导致文字变得巨大或看不见
        textObj.transform.SetParent(null); 
        textObj.transform.position = target.position + offset;

        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = 0.05f;
        tm.fontSize = 80;
        tm.color = textColor;
        tm.gameObject.SetActive(false); // 默认隐藏

        MeshRenderer mr = textObj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingLayerName = "Ground"; // 改成了和游戏内其他文字一样的渲染层 Ground
            mr.sortingOrder = 100;
        }

        return tm;
    }

    // 利用触发器检测玩家（此物体需要勾选 Collider2D 的 isTrigger）
    // 或者您也可以改成 OnCollisionEnter2D
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                float speed = playerRb.linearVelocity.magnitude;

                // 如果带有足够的速度
                if (speed > 0.5f)
                {
                    // 记录上一次的时间
                    float previousTimer = timer;

                    // 吸收速度转为倒计时时间（可叠加）
                    timer += speed * speedToTimeMultiplier;

                    // 增加设定的时间上限
                    timer = Mathf.Min(timer, maxTimeLimit);

                    // 如果撞击前就已经时间满了（或极度接近满），再塞入速度则触发拒绝动画，反之播放吸收动画
                    if (previousTimer >= maxTimeLimit - 0.05f)
                    {
                        PlayRejectAnimation();
                    }
                    else
                    {
                        PlayAbsorbAnimation();
                    }

                    // 将玩家速度清零吸干
                    playerRb.linearVelocity = Vector2.zero;

                    // 开启虚化状态
                    if (!isGhosted)
                    {
                        SetGhostState(true);
                    }
                }
            }
        }
    }

    void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            UpdateTexts(timer);

            if (timer <= 0)
            {
                timer = 0;
                SetGhostState(false); // 时间结束，恢复实体
            }
        }
    }

    void LateUpdate()
    {
        // 保证所有文字不被旋转，还能手动无视缩放去跟随物体的位置
        if (switchText != null && switchText.gameObject.activeSelf)
        {
            switchText.transform.position = transform.position + switchTextOffset;
            switchText.transform.rotation = Quaternion.identity;
        }

        if (obstacleGroupText != null && obstacleGroupText.gameObject.activeSelf && firstObstacleTransform != null)
        {
            obstacleGroupText.transform.position = firstObstacleTransform.position + obstacleTextOffset;
            obstacleGroupText.transform.rotation = Quaternion.identity;
        }
    }

    private void PlayAbsorbAnimation()
    {
        if (switchText != null)
        {
            switchText.transform.DOKill(true); // 保证清除上一个动画
            // 来一个猛烈的Q弹缩放效果（变到1.4倍后弹回去）
            switchText.transform.DOPunchScale(new Vector3(0.4f, 0.4f, 0), 0.4f, 5, 1f); 
        }

        if (obstacleGroupText != null)
        {
            obstacleGroupText.transform.DOKill(true);
            obstacleGroupText.transform.DOPunchScale(new Vector3(0.4f, 0.4f, 0), 0.4f, 5, 1f);
        }
    }

    private void PlayRejectAnimation()
    {
        if (switchText != null)
        {
            switchText.transform.DOKill(true);
            // 拒绝时的剧烈缩放抖动（像受到无效冲击一样抽搐发抖）
            switchText.transform.DOShakeScale(0.3f, new Vector3(0.4f, 0.4f, 0), 20, 90f, true);
        }

        if (obstacleGroupText != null)
        {
            obstacleGroupText.transform.DOKill(true);
            obstacleGroupText.transform.DOShakeScale(0.3f, new Vector3(0.4f, 0.4f, 0), 20, 90f, true);
        }
    }

    private void SetGhostState(bool ghost)
    {
        isGhosted = ghost;

        // 更换机关自身的图片
        if (switchRenderer != null && switchActiveSprite != null)
        {
            switchRenderer.sprite = ghost ? switchActiveSprite : originalSwitchSprite;
        }
        
        // 显示/隐藏机关自身的文本
        if (switchText != null) switchText.gameObject.SetActive(ghost);

        // 统一处理所有障碍物的虚化/恢复
        if (obstacleStates != null)
        {
            for (int i = 0; i < obstacleStates.Length; i++)
            {
                if (targetObstacles[i] == null) continue;
                
                ObstacleState state = obstacleStates[i];

                // 1. 碰撞体开启/关闭
                if (state.col != null)
                {
                    state.col.isTrigger = ghost; 
                }

                // 2. 更换图片和透明度
                if (state.sr != null)
                {
                    state.sr.sprite = ghost && ghostSprite != null ? ghostSprite : state.originalSprite;

                    Color c = state.sr.color;
                    c.a = ghost ? ghostAlpha : 1f;
                    state.sr.color = c;
                }
            }
        }

        // 显示/隐藏唯一的障碍物文本
        if (obstacleGroupText != null)
        {
            obstacleGroupText.gameObject.SetActive(ghost);
        }
        
        // 恢复默认的缩放值，防止最后卡在Punch效果动画中
        if (!ghost)
        {
            if (switchText != null) switchText.transform.localScale = Vector3.one;
            if (obstacleGroupText != null) obstacleGroupText.transform.localScale = Vector3.one;
        }
    }

    private void UpdateTexts(float t)
    {
        string timeStr = t.ToString("F1") + "s";

        // 最后3秒变成警告颜色，达到上限（或者极其接近上限）时也变成警告颜色
        Color currentColor = textColor;
        if (t <= 3f || Mathf.Abs(t - maxTimeLimit) < 0.1f)
        {
            currentColor = warningColor;
        }

        if (switchText != null) 
        {
            switchText.text = timeStr;
            switchText.color = currentColor;
        }

        if (obstacleGroupText != null)
        {
            obstacleGroupText.text = timeStr;
            obstacleGroupText.color = currentColor;
        }

        // 每经过一整个自然秒数，文字会有一个微小的果冻弹跳效果（像是在倒数滴答跳动）
        int currentTickSecond = Mathf.CeilToInt(t);
        if (currentTickSecond != lastTickSecond && t > 0)
        {
            lastTickSecond = currentTickSecond;

            // 滴答跳动动画
            if (switchText != null && !DOTween.IsTweening(switchText.transform)) 
                switchText.transform.DOPunchScale(new Vector3(0.15f, -0.1f, 0), 0.2f, 1, 0.5f);

            if (obstacleGroupText != null && !DOTween.IsTweening(obstacleGroupText.transform)) 
                obstacleGroupText.transform.DOPunchScale(new Vector3(0.15f, -0.1f, 0), 0.2f, 1, 0.5f);
        }
    }
}