using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(1001)] // 确保在 FollowCameraPauseButton(1000) 之后执行，防止文本位置相对主体产生一帧的延迟抖动
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer), typeof(FollowCameraPauseButton))]
public class ClickableCheckpointSprite : MonoBehaviour, IMomentumSwappable
{
    [Header("存档点基础设置")]
    public float disabledAlpha = 0.5f;
    public float bulletTimeAlpha = 0.4f;

    [Header("速度存储可视化")]
    public Vector3 speedTextOffset = new Vector3(0, 1.2f, 0);
    public Color textColor = Color.white;
    public float minShowSpeed = 0.5f;

    [Header("LineRenderer 圆圈箭头设置（和传送门同款）")]
    public float ringRadius = 0.6f;
    public Color ringColor = Color.white;
    public Color arrowColor = Color.white;

    [Header("Swappable 视觉效果")]
    public Color bulletTimeHintColor = Color.yellow;
    public Color selectedColor = Color.cyan;
    public Color successColor = Color.green;

    private bool hasCheckpoint;
    private bool isInBulletTime;
    private bool canClickToRespawn = true;
    private bool hasStoredVelocity;
    private Vector2 storedVelocity;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private TextMesh speedText;
    private GameObject textObj;
    private float originalAlpha;
    private Color originalColor;

    private FollowCameraPauseButton followButton;
    private bool isButtonVisible;

    // ====================== 传送门同款 LineRenderer ======================
    private LineRenderer ringLine;
    private LineRenderer arrowLine;
    // ====================================================================

    private static ClickableCheckpointSprite instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        followButton = GetComponent<FollowCameraPauseButton>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (sr != null)
        {
            originalColor = sr.color;
            originalAlpha = sr.color.a;
        }

        if (rb != null)
        {
            // 关闭插值，防止与 Transform 强制修改（如随相机移动）冲突而导致剧烈抖动
            rb.interpolation = RigidbodyInterpolation2D.None;
        }

        CreateRingAndArrowLines();
    }

    private void Start()
    {
        CheckCheckpointState();
        RecreateVisualElements();
        UpdateArrowVisibility();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void LateUpdate()
    {
        if (this == null || !gameObject.activeSelf) return;

        UpdateShiftState();

        if (textObj != null) UpdateSpeedText();
        UpdateDirectionArrow();
        ResetVisual();
        UpdateArrowVisibility();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RecreateVisualElements();
        CheckCheckpointState();
        UpdateArrowVisibility();
    }

    private void RecreateVisualElements()
    {
        if (textObj != null) Destroy(textObj);
        textObj = null;

        InitSpeedText();
    }

    private void UpdateShiftState()
    {
        if (Keyboard.current == null) return;

        bool shiftDown = Keyboard.current.leftShiftKey.isPressed;

        if (shiftDown && !isInBulletTime)
        {
            isInBulletTime = true;
            canClickToRespawn = false;
        }
        else if (!shiftDown && isInBulletTime)
        {
            isInBulletTime = false;
            canClickToRespawn = true;
        }

        SetBulletTimeVisual(isInBulletTime);
    }

    private void OnMouseDown()
    {
        if (!hasCheckpoint) return;
        if (!canClickToRespawn) return;

        if (UIManager.Instance != null)
            UIManager.Instance.HidePauseMenu();

        if (LevelManager.Instance != null)
            LevelManager.Instance.RespawnAtCheckpoint();

        if (followButton != null)
            followButton.HideButton();
    }

    // ====================== 完全保留你原来的方法名 ======================
    public void HideArrow()
    {
        if (ringLine != null) ringLine.enabled = false;
        if (arrowLine != null) arrowLine.enabled = false;
        DOTween.Kill(transform);
    }

    public void ShowArrow()
    {
        UpdateArrowVisibility();
    }

    public void UpdateArrowVisibility()
    {
        if (ringLine == null || arrowLine == null) return;

        // 关键：从按钮获取真实显示状态
        isButtonVisible = followButton != null && followButton.IsVisible;
        bool shouldShow = isButtonVisible && hasStoredVelocity && storedVelocity.magnitude >= minShowSpeed;

        // 🔥 终极修复：强制把子物体的 localScale 恢复为 1，不受父物体影响
        if (ringLine.transform.localScale != Vector3.one)
            ringLine.transform.localScale = Vector3.one;

        if (arrowLine.transform.localScale != Vector3.one)
            arrowLine.transform.localScale = Vector3.one;

        ringLine.enabled = shouldShow;
        arrowLine.enabled = shouldShow;

        if (shouldShow)
        {
            UpdateDirectionArrow();
        }
    }
    // ====================================================================

    public void OnPlayerRespawnedAtCheckpoint()
    {
        HideArrow();
        ClearStoredVelocity();
        UpdateArrowVisibility();
    }

    private void ApplyVelocityAfterRespawn()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            ClearStoredVelocity();
            return;
        }

        Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
        if (prb == null)
        {
            ClearStoredVelocity();
            return;
        }
        prb.linearVelocity = Vector2.zero;
        prb.WakeUp();
        prb.linearVelocity = storedVelocity;
    }

    private void ClearStoredVelocity()
    {
        storedVelocity = Vector2.zero;
        hasStoredVelocity = false;
        if (LevelManager.Instance != null)
            LevelManager.Instance.storedCheckpointVelocity = Vector2.zero;

        HideArrow();
    }

    public void ApplyMomentum(Vector2 momentum)
    {
        if (momentum.magnitude < 0.1f)
        {
            HideArrow();
            return;
        }

        if (LevelManager.Instance != null)
            LevelManager.Instance.storedCheckpointVelocity = momentum;

        storedVelocity = momentum;
        hasStoredVelocity = true;
        FlashSuccess();
        UpdateArrowVisibility();
    }

    private void InitSpeedText()
    {
        textObj = new GameObject("SpeedDisplay");
        speedText = textObj.AddComponent<TextMesh>();
        speedText.anchor = TextAnchor.MiddleCenter;
        speedText.alignment = TextAlignment.Center;
        speedText.characterSize = 0.05f;
        speedText.fontSize = 80;
        speedText.color = textColor;
        MeshRenderer textRenderer = textObj.GetComponent<MeshRenderer>();
        if (textRenderer != null)
        {
            textRenderer.sortingLayerName = "Ground";
            textRenderer.sortingOrder = 100;
        }
        textObj.SetActive(false);
    }

    private void CreateRingAndArrowLines()
    {
        // 圆圈
        GameObject ringObj = new GameObject("Ring");
        ringObj.transform.SetParent(transform);
        ringObj.transform.localPosition = Vector3.zero;
        ringLine = ringObj.AddComponent<LineRenderer>();
        ringLine.material = new Material(Shader.Find("Sprites/Default"));
        ringLine.startColor = ringColor;
        ringLine.endColor = ringColor;
        ringLine.startWidth = 0.1f;
        ringLine.endWidth = 0.1f;
        ringLine.useWorldSpace = false;
        ringLine.sortingLayerName = "Ground";
        ringLine.sortingOrder = 74;

        int segments = 40;
        ringLine.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            ringLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * ringRadius, Mathf.Sin(angle) * ringRadius, 0));
        }
        ringLine.enabled = false;

        // 箭头
        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(ringObj.transform);
        arrowObj.transform.localPosition = Vector3.zero;
        arrowLine = arrowObj.AddComponent<LineRenderer>();
        arrowLine.material = new Material(Shader.Find("Sprites/Default"));
        arrowLine.startColor = arrowColor;
        arrowLine.endColor = new Color(arrowColor.r, arrowColor.g, arrowColor.b, 0);
        arrowLine.startWidth = 0.4f;
        arrowLine.endWidth = 0f;
        arrowLine.useWorldSpace = false;
        arrowLine.positionCount = 2;
        arrowLine.sortingLayerName = "Ground";
        arrowLine.sortingOrder = 75;
        arrowLine.enabled = false;
    }

    private void UpdateDirectionArrow()
    {
        if (!hasStoredVelocity || ringLine == null || arrowLine == null) return;

        float mag = storedVelocity.magnitude;
        if (mag < minShowSpeed)
        {
            HideArrow();
            return;
        }

        Vector2 dir = storedVelocity.normalized;
        Vector3 start = dir * ringRadius;
        float arrowLength = 0.6f;
        Vector3 end = start + (Vector3)dir * arrowLength;

        arrowLine.SetPosition(0, start);
        arrowLine.SetPosition(1, end);
    }

    private void UpdateSpeedText()
    {
        if (!hasStoredVelocity || speedText == null)
        {
            textObj.SetActive(false);
            return;
        }

        textObj.transform.position = transform.position + speedTextOffset;
        textObj.transform.rotation = Quaternion.identity;
        float mag = storedVelocity.magnitude;

        if (mag < minShowSpeed)
            textObj.SetActive(false);
        else
        {
            textObj.SetActive(true);
            speedText.text = mag.ToString("F1");
        }
    }

    private void SetBulletTimeVisual(bool active)
    {
        if (sr == null) return;
        Color c = active ? bulletTimeHintColor : originalColor;
        c.a = active ? bulletTimeAlpha : originalAlpha;
        sr.color = c;
    }

    private void ResetVisual()
    {
        if (sr == null || isInBulletTime) return;
        Color c = originalColor;
        c.a = hasCheckpoint ? originalAlpha : disabledAlpha;
        sr.color = c;
    }

    public void CheckCheckpointState()
    {
        hasCheckpoint = !string.IsNullOrEmpty(PlayerPrefs.GetString("CheckpointScene", ""));
    }

    public void FlashSuccess()
    {
        StartCoroutine(Flash());
    }

    private System.Collections.IEnumerator Flash()
    {
        Color org = sr.color;
        sr.color = successColor;
        yield return new WaitForSecondsRealtime(0.2f);
        sr.color = org;
    }

    public Rigidbody2D MomentumRigidbody => rb;
    public void SetSelectedVisual(bool selected) { }
}