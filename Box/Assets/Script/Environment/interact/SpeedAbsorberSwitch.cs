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

    [Header("吸收过滤")]
    [Tooltip("只吸收这些标签的物体，留空=吸收所有刚体")]
    public string[] allowedTags;

    [Header("目标障碍物设置")]
    [Tooltip("关联的多个障碍物对象（支持多个门或墙）")]
    public GameObject[] targetObstacles;

    [Tooltip("障碍物虚化时替换的图片")]
    public Sprite ghostSprite;

    [Tooltip("障碍物虚化时的透明度")]
    [Range(0f, 1f)]
    public float ghostAlpha = 0.5f;

    // 音效只留吸收
    [Header("音效")]
    public AudioClip absorbSound;
    [Range(0f, 1f)] public float soundVolume = 0.9f;
    private AudioSource audioSource;

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

    void Awake()
    {
        // 自动加音源
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void OnDestroy()
    {
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
        switchRenderer = GetComponent<SpriteRenderer>();
        if (switchRenderer != null)
        {
            originalSwitchSprite = switchRenderer.sprite;
        }

        switchText = CreateTextDisplay(transform, "SwitchText", switchTextOffset);

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
        textObj.transform.SetParent(null);
        textObj.transform.position = target.position + offset;

        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = 0.05f;
        tm.fontSize = 80;
        tm.color = textColor;
        tm.gameObject.SetActive(false);

        MeshRenderer mr = textObj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingLayerName = "Ground";
            mr.sortingOrder = 100;
        }

        return tm;
    }

    // 检查标签是否允许吸收
    private bool IsTagAllowed(string tag)
    {
        if (allowedTags == null || allowedTags.Length == 0)
            return true;

        foreach (string t in allowedTags)
        {
            if (t == tag) return true;
        }
        return false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 只要有刚体、标签允许，就吸收
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb == null) return;
        if (!IsTagAllowed(other.tag)) return;

        float speed = rb.linearVelocity.magnitude;
        if (speed <= 0.5f) return;

        float previousTimer = timer;
        timer += speed * speedToTimeMultiplier;
        timer = Mathf.Min(timer, maxTimeLimit);

        if (previousTimer >= maxTimeLimit - 0.05f)
        {
            PlayRejectAnimation();
        }
        else
        {
            PlayAbsorbAnimation();
            // 只在成功吸收时播放音效
            if (absorbSound != null)
                audioSource.PlayOneShot(absorbSound, soundVolume);
        }

        // 把物体速度清零
        rb.linearVelocity = Vector2.zero;

        if (!isGhosted)
        {
            SetGhostState(true);
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
                SetGhostState(false);
            }
        }
    }

    void LateUpdate()
    {
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
            switchText.transform.DOKill(true);
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

        if (switchRenderer != null && switchActiveSprite != null)
        {
            switchRenderer.sprite = ghost ? switchActiveSprite : originalSwitchSprite;
        }

        if (switchText != null) switchText.gameObject.SetActive(ghost);

        if (obstacleStates != null)
        {
            for (int i = 0; i < obstacleStates.Length; i++)
            {
                if (targetObstacles[i] == null) continue;

                ObstacleState state = obstacleStates[i];

                if (state.col != null)
                {
                    state.col.isTrigger = ghost;
                }

                if (state.sr != null)
                {
                    state.sr.sprite = ghost && ghostSprite != null ? ghostSprite : state.originalSprite;

                    Color c = state.sr.color;
                    c.a = ghost ? ghostAlpha : 1f;
                    state.sr.color = c;
                }
            }
        }

        if (obstacleGroupText != null)
        {
            obstacleGroupText.gameObject.SetActive(ghost);
        }

        if (!ghost)
        {
            if (switchText != null) switchText.transform.localScale = Vector3.one;
            if (obstacleGroupText != null) obstacleGroupText.transform.localScale = Vector3.one;
        }
    }

    private void UpdateTexts(float t)
    {
        string timeStr = t.ToString("F1") + "s";

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

        int currentTickSecond = Mathf.CeilToInt(t);
        if (currentTickSecond != lastTickSecond && t > 0)
        {
            lastTickSecond = currentTickSecond;

            if (switchText != null && !DOTween.IsTweening(switchText.transform))
                switchText.transform.DOPunchScale(new Vector3(0.15f, -0.1f, 0), 0.2f, 1, 0.5f);

            if (obstacleGroupText != null && !DOTween.IsTweening(obstacleGroupText.transform))
                obstacleGroupText.transform.DOPunchScale(new Vector3(0.15f, -0.1f, 0), 0.2f, 1, 0.5f);
        }
    }
}