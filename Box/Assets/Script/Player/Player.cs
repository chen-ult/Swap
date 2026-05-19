using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class Player : Entity
{
    public static event Action OnPlayerDeath;

    public PlayerInputSet input;

    [Header("Audio / SFX")]
    public AudioSource audioSource;
    [Tooltip("专用脚步声源，确保停止移动时停止脚步声，音效独立")]
    public AudioSource footstepSource;
    public AudioClip sfx_PlayerJump;
    public AudioClip sfx_PlayerLand;
    public AudioClip sfx_PlayerHurt;
    public AudioClip sfx_Split;
    public AudioClip sfx_SlimeCloneSpawn;
    public AudioClip sfx_SmallBounce;
    public AudioClip sfx_Footstep;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Footstep")]
    [SerializeField] private float stepInterval = 0.35f;
    public float StepInterval => stepInterval;

    #region State
    public Player_IdleState idleState { get; private set; }
    public Player_MoveState moveState { get; private set; }
    public Player_JumpState jumpState { get; private set; }
    public Player_FallState fallState { get; private set; }
    public Player_FallEndState fallendState { get; private set; }
    public Player_DeadState deadState { get; private set; }
    #endregion

    #region 移动参数
    [Header("------------移动参数------------")]
    public float movespeed;
    public float jumpforce = 5;
    public Vector2 wallJumpForce;

    [Range(0f, 1f)]
    public float inAirMoveMultiplier = .7f;
    [Range(0f, 1f)]
    public float wallSlideMultiplier = .3f;

    [Header("冲刺")]
    public float dashDuration = .25f;
    public float dashSpeed = 20;

    public Vector2 moveInput { get; private set; }
    #endregion

    private Collider2D col;
    public LayerMask obstacleLayer;

    [Space(10)]
    [Header("分裂与粘液克隆")]
    public float splitSpeedThreshold = 15f;
    public bool isSplit = false;
    public float splitBounceForce = 12f;

    [Tooltip("分裂出来的粘液克隆预制体")]
    public GameObject slimeClonePrefab;

    [Tooltip("分裂后玩家的缩放比例，建议0.7")]
    public float splitScaleMultiplier = 0.7f;

    private Vector3 originalScale;

    // ====================== 【吸引圈设置 · 只有圈动】 ======================
    [Header("SlimeClone 吸引设置")]
    public float attractSpeed = 10f;
    public float attractRadius = 4f;
    public Color attractLineColor = Color.cyan;
    public float attractLineWidth = 0.12f;

    [Header("虚线材质（拖入你做好的材质）")]
    public Material dashedCircleMaterial;

    [Header("动态效果（圈的，不是玩家）")]
    public float breathScale = 0.15f;
    public float breathSpeed = 2f;
    public float rotateSpeed = 15f;
    public float fadeDuration = 0.2f;
    public float dashDensity = 8f;

    private bool isAttracting;
    public bool IsAttracting => isAttracting;
    public Vector3 PlayerPos => transform.position;

    private LineRenderer attractCircleLine;
    private Transform circleTrans;   // 圈的独立Transform！！！
    private readonly int circlePoints = 180;
    private Tween circleBreathTween;
    private Tween circleRotateTween;
    private Tween fadeTween;
    private bool wasAttracting = false;
    // ======================================================================

    public Vector3 GetOriginalScale()
    {
        return originalScale;
    }

    private bool isPassingThroughPlatform = false;

    protected override void Awake()
    {
        base.Awake();

        input = new PlayerInputSet();
        originalScale = transform.localScale;
        col = GetComponent<Collider2D>();

        idleState = new Player_IdleState(this, stateMachine, "idle");
        moveState = new Player_MoveState(this, stateMachine, "move");
        jumpState = new Player_JumpState(this, stateMachine, "jumpFall");
        fallState = new Player_FallState(this, stateMachine, "jumpFall");
        fallendState = new Player_FallEndState(this, stateMachine, "fallend");
        deadState = new Player_DeadState(this, stateMachine, "dead");

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        if (footstepSource == null)
        {
            footstepSource = transform.Find("FootstepSource")?.GetComponent<AudioSource>();
            if (footstepSource == null)
            {
                footstepSource = gameObject.AddComponent<AudioSource>();
                footstepSource.playOnAwake = false;
                footstepSource.loop = false;
                footstepSource.spatialBlend = audioSource != null ? audioSource.spatialBlend : 0f;
            }
        }

        // ====================== 虚线圈初始化：独立物体！！ ======================
        GameObject lineObj = new GameObject("AttractCircle");
        lineObj.transform.SetParent(transform, false);
        lineObj.transform.localPosition = Vector3.zero;
        lineObj.transform.localScale = Vector3.one;
        circleTrans = lineObj.transform;  // 存圈的transform

        attractCircleLine = lineObj.AddComponent<LineRenderer>();
        attractCircleLine.enabled = false;
        attractCircleLine.positionCount = circlePoints + 1;
        attractCircleLine.loop = true;
        attractCircleLine.useWorldSpace = true;

        attractCircleLine.startWidth = attractLineWidth;
        attractCircleLine.endWidth = attractLineWidth;
        attractCircleLine.startColor = attractLineColor;
        attractCircleLine.endColor = attractLineColor;
        attractCircleLine.material = dashedCircleMaterial;
        attractCircleLine.textureMode = LineTextureMode.Tile;
        attractCircleLine.material.mainTextureScale = new Vector2(dashDensity, 1);
        attractCircleLine.material.color = new Color(attractLineColor.r, attractLineColor.g, attractLineColor.b, 0);

        attractCircleLine.sortingLayerName = "Ground";
        attractCircleLine.sortingOrder = 100;

        // 呼吸动画（独立，不进序列）
        circleBreathTween = DOTween.To(() => circleTrans.localScale,
            s => circleTrans.localScale = s,
            Vector3.one * (1 + breathScale),
            breathSpeed)
            .SetTarget(circleTrans)
            .SetLink(gameObject)
            .SetLoops(-1, LoopType.Yoyo)
            .SetAutoKill(false)
            .Pause();

        // 旋转动画（独立，不进序列，保留虚线滚动）
        circleRotateTween = DOTween.To(() => circleTrans.rotation.eulerAngles.z,
            r => circleTrans.rotation = Quaternion.Euler(0, 0, r),
            360,
            rotateSpeed)
            .SetTarget(circleTrans)
            .SetLink(gameObject)
            .SetSpeedBased()
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Incremental)
            .SetAutoKill(false)
            .Pause();
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    protected override void Update()
    {
        base.Update();
        DrawAttractCircle();

        // 只在按下/松开Q时执行一次动画，不每帧生成
        if (isAttracting != wasAttracting)
        {
            wasAttracting = isAttracting;

            if (isAttracting)
            {
                attractCircleLine.enabled = true;
                circleBreathTween.Play();
                circleRotateTween.Play();

                fadeTween?.Kill();
                fadeTween = attractCircleLine.material.DOFade(1, fadeDuration);
            }
            else
            {
                circleBreathTween.Pause();
                circleRotateTween.Pause();

                fadeTween?.Kill();
                fadeTween = attractCircleLine.material.DOFade(0, fadeDuration).OnComplete(() =>
                {
                    attractCircleLine.enabled = false;
                });
            }
        }

        if (moveInput.y < -0.5f && groundDetected && !isPassingThroughPlatform)
        {
            RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
            if (hit.collider != null && hit.collider.GetComponent<PlatformEffector2D>() != null)
            {
                StartCoroutine(PassThroughOneWayPlatform(hit.collider));
            }
        }
    }

    void DrawAttractCircle()
    {
        for (int i = 0; i <= circlePoints; i++)
        {
            float angle = Mathf.Deg2Rad * (360f / circlePoints * i);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * attractRadius;
            attractCircleLine.SetPosition(i, transform.position + (Vector3)offset);
        }
    }

    private IEnumerator PassThroughOneWayPlatform(Collider2D platformCollider)
    {
        isPassingThroughPlatform = true;
        Collider2D[] playerColliders = GetComponentsInChildren<Collider2D>();

        foreach (var pCol in playerColliders)
        {
            Physics2D.IgnoreCollision(pCol, platformCollider, true);
        }

        yield return new WaitForSeconds(0.35f);

        if (platformCollider != null)
        {
            foreach (var pCol in playerColliders)
            {
                if (pCol != null)
                    Physics2D.IgnoreCollision(pCol, platformCollider, false);
            }
        }

        isPassingThroughPlatform = false;
    }

    public override void EntityDeath()
    {
        base.EntityDeath();
        OnPlayerDeath?.Invoke();
        stateMachine.ChangeState(deadState);
        StartCoroutine(WaitDeathAnimation());
    }

    private IEnumerator WaitDeathAnimation()
    {
        yield return new WaitForSeconds(1.0f);

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RespawnAtCheckpoint();
        }
        else
        {
            Debug.LogError("未找到 LevelManager，无法复活！");
        }
    }

    private void OnEnable()
    {
        input.Enable();

        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        input.Player.Attract.performed += _ => isAttracting = true;
        input.Player.Attract.canceled += _ => isAttracting = false;
    }

    private void OnDisable()
    {
        input.Player.Attract.performed -= _ => isAttracting = true;
        input.Player.Attract.canceled -= _ => isAttracting = false;
        input.Disable();
    }

    protected virtual void OnDestroy()
    {
        fadeTween?.Kill();
        circleBreathTween?.Kill();
        circleRotateTween?.Kill();
        circleTrans?.DOKill();
        transform.DOKill();
    }

    public void PlayFootstep()
    {
        if (footstepSource == null || sfx_Footstep == null) return;
        if (footstepSource.isPlaying) return;

        float oldPitch = footstepSource.pitch;
        float rp = UnityEngine.Random.Range(0.97f, 1.03f);
        footstepSource.pitch = rp;
        footstepSource.PlayOneShot(sfx_Footstep, sfxVolume);
        footstepSource.pitch = oldPitch;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int layerMask = 1 << collision.gameObject.layer;
        if ((layerMask & whatIsGround.value) != 0)
        {
            float impactSpeed = collision.relativeVelocity.magnitude;

            if (impactSpeed >= splitSpeedThreshold && !isSplit)
            {
                TriggerSplit(impactSpeed, collision.contacts[0].normal);
            }
        }
    }

    private void TriggerSplit(float speed, Vector2 hitNormal)
    {
        isSplit = true;

        transform.DOKill();
        transform.DOScale(originalScale * splitScaleMultiplier, 0.2f).SetEase(Ease.OutBack);

        Vector2 bounceDir = hitNormal;
        if (Mathf.Abs(bounceDir.y) < 0.1f) bounceDir.y = 1f;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(bounceDir.normalized * splitBounceForce, ForceMode2D.Impulse);

        if (audioSource != null)
        {
            if (sfx_Split != null) audioSource.PlayOneShot(sfx_Split, sfxVolume);
            if (sfx_SmallBounce != null) audioSource.PlayOneShot(sfx_SmallBounce, sfxVolume);
        }

        CreateClone(speed);
    }

    private void CreateClone(float speed)
    {
        if (slimeClonePrefab == null)
        {
            Debug.LogWarning("未设置 SlimeClone 预制体！");
            return;
        }

        Vector3 spawnPos = transform.position - new Vector3(0, originalScale.y * (1f - splitScaleMultiplier) * 0.5f, 0);
        GameObject cloneObj = Instantiate(slimeClonePrefab, spawnPos, Quaternion.identity);

        SlimeClone cloneScript = cloneObj.GetComponent<SlimeClone>();
        cloneScript.Init(this, speed);

        if (audioSource != null && sfx_SlimeCloneSpawn != null)
            audioSource.PlayOneShot(sfx_SlimeCloneSpawn, sfxVolume);
    }

    public void AbsorbClone(SlimeClone clone)
    {
        if (!isSplit || clone == null) return;

        RestoreFromSplit();
        Destroy(clone.gameObject);
    }

    public void RestoreFromSplit()
    {
        isSplit = false;
        transform.DOKill();
        transform.DOScale(originalScale, 0.3f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                bool isStuck = CheckPlayerStuck();
                if (isStuck)
                {
                    stats.TakeDamage(1);
                }
            });
    }

    bool CheckPlayerStuck()
    {
        Vector2 center = col.bounds.center;
        Vector2 size = col.bounds.size;
        Collider2D hit = Physics2D.OverlapBox(center, size, 0, obstacleLayer);
        return hit != null;
    }
}