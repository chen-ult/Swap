using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class Player : Entity
{
    public static event Action OnPlayerDeath;//��������¼�

    public PlayerInputSet input;

    [Header("Audio / SFX")]
    public AudioSource audioSource;
    [Tooltip("ר�ýŲ���Դ��������ֹͣ�ƶ�ʱ����ֹͣ�Ų�����Ӱ��������Ч")]
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


    #region �ƶ�����
    [Header("------------�ƶ�����------------")]
    public float movespeed;//�ƶ��ٶ�
    public float jumpforce = 5;//��Ծ��
    public Vector2 wallJumpForce;//ǽ����Ծ��

    [Range(0f, 1f)]
    public float inAirMoveMultiplier = .7f;//�����ƶ�����
    [Range(0f, 1f)]
    public float wallSlideMultiplier = .3f;//ǽ�ڻ��б���

    [Header("��̲���")]
    public float dashDuration = .25f;//��̳���ʱ��
    public float dashSpeed = 20;//����ٶ�

    public Vector2 moveInput { get; private set; }//�ƶ�����
    #endregion

    private Collider2D col;
    public LayerMask obstacleLayer; // ���ڼ������Ƿ񱻿���ǽ��������

    [Space(10)]
    [Header("ʷ��ķ���ѻ���")]
    public float splitSpeedThreshold = 15f; // ײ��ǽ��������Ҫ�ﵽ����ٶȲŻᴥ������
    public bool isSplit = false; // �Ƿ��Ѿ����ڷ���״̬
    public float splitBounceForce = 12f; // ���Ѻ����ϵ�����

    [Tooltip("���ѷ�����Ԥ���壨������Ŀ�����ú�һ������SlimeClone�ű���Ԥ���岢����˲�λ��")]
    public GameObject slimeClonePrefab;

    [Tooltip("���Ѻ��������ű�����0.5����һ�룬���̫С���Ե�Ϊ0.7���ң�")]
    public float splitScaleMultiplier = 0.7f;

    private Vector3 originalScale; // ���ԭʼ��С

    // �������޸������ṩ���ⲿ�����Ŷ�ȡ������׼��С�ķ�������ֹ��ή�����ŵ���һֱ�м���
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

        // prepare audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
        // prepare dedicated footstep source
        if (footstepSource == null)
        {
            // try find child named FootstepSource first
            footstepSource = transform.Find("FootstepSource")?.GetComponent<AudioSource>();
            if (footstepSource == null)
            {
                footstepSource = gameObject.AddComponent<AudioSource>();
                footstepSource.playOnAwake = false;
                footstepSource.loop = false;
                footstepSource.spatialBlend = audioSource != null ? audioSource.spatialBlend : 0f;
            }
        }

    }

    protected override void Start()
    {
        base.Start();

        stateMachine.Initialize(idleState);
    }



    protected override void Update()
    {
        base.Update();

        // ����ƽ̨�����߼� (�� S ���ڵ���)
        if (moveInput.y < -0.5f && groundDetected && !isPassingThroughPlatform)
        {
            RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
            if (hit.collider != null && hit.collider.GetComponent<PlatformEffector2D>() != null)
            {
                StartCoroutine(PassThroughOneWayPlatform(hit.collider));
            }
        }
    }

    private IEnumerator PassThroughOneWayPlatform(Collider2D platformCollider)
    {
        isPassingThroughPlatform = true;
        Collider2D[] playerColliders = GetComponentsInChildren<Collider2D>();
        
        // ������ײ������ҵ���ȥ
        foreach (var pCol in playerColliders)
        {
            Physics2D.IgnoreCollision(pCol, platformCollider, true);
        }
        
        yield return new WaitForSeconds(0.35f); // 0.35��ͨ���㹻����һ��ƽ̨
        
        // �ָ���ײ
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

    public override void EntityDeath()//ʵ����������д
    {
        base.EntityDeath();

        OnPlayerDeath?.Invoke();
        stateMachine.ChangeState(deadState);
        StartCoroutine(WaitDeathAnimation());
    }

    private IEnumerator WaitDeathAnimation()
    {
        // �ȴ�1�루�����������������ʵ�ʳ����޸����ʱ�䣩
        yield return new WaitForSeconds(1.0f);

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RespawnAtCheckpoint();
        }
        else
        {
            Debug.LogError("û���ҵ� LevelManager.Instance���޷�������ң���ȷ���ڴ˳������Ѿ������� LevelManager��");
        }
    }


    private void OnEnable()
    {
        input.Enable();

        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero;
    }

    private void OnDisable()
    {
        input.Disable();
    }

    protected virtual void OnDestroy()
    {
        transform.DOKill();
    }

    /// <summary>
    /// Play a footstep sound. Intended to be called from MoveState distance logic or from animation events.
    /// </summary>
    public void PlayFootstep()
    {
        if (footstepSource == null || sfx_Footstep == null) return;
        if (footstepSource.isPlaying) return;
        // small random pitch variation for natural feel
        float oldPitch = footstepSource.pitch;
        float rp = UnityEngine.Random.Range(0.97f, 1.03f);
        footstepSource.pitch = rp;
        footstepSource.PlayOneShot(sfx_Footstep, sfxVolume);
        footstepSource.pitch = oldPitch;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ֻ�������������ǽ�ڲż��� (����ײ���Layer��whatIsGround��)
        int layerMask = 1 << collision.gameObject.layer;
        if ((layerMask & whatIsGround.value) != 0)
        {
            // ��ȡ��ײ˲��˫��������ٶȴ�С
            float impactSpeed = collision.relativeVelocity.magnitude;

            // �����������ֵ����Ŀǰ��û�з���
            if (impactSpeed >= splitSpeedThreshold && !isSplit)
            {
                TriggerSplit(impactSpeed, collision.contacts[0].normal);
            }
        }
    }

    private void TriggerSplit(float speed, Vector2 hitNormal)
    {
        isSplit = true;

        // 1. ��С�Ķ���
        transform.DOKill();
        transform.DOScale(originalScale * splitScaleMultiplier, 0.2f).SetEase(Ease.OutBack);

        // 2. ������÷���ĵ������� (���߼򵥵������Ϸ���)
        Vector2 bounceDir = hitNormal;
        if (Mathf.Abs(bounceDir.y) < 0.1f) bounceDir.y = 1f; // ȷ����΢�и����ϵ�������

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(bounceDir.normalized * splitBounceForce, ForceMode2D.Impulse);

        // play split and small bounce sounds
        if (audioSource != null)
        {
            if (sfx_Split != null) audioSource.PlayOneShot(sfx_Split, sfxVolume);
            if (sfx_SmallBounce != null) audioSource.PlayOneShot(sfx_SmallBounce, sfxVolume);
        }

        // 3. ��ԭ������һ���޷��ƶ��ķ���
        CreateClone(speed);
    }

    private void CreateClone(float speed)
    {
        if (slimeClonePrefab == null)
        {
            Debug.LogWarning("δ���� SlimeClone Ԥ���壡���� Player ��������� Slime Clone Prefab��");
            return;
        }

        Vector3 spawnPos = transform.position - new Vector3(0, originalScale.y * (1f - splitScaleMultiplier) * 0.5f, 0); // ���ڽŵ�

        // ʵ��������Ԥ���壬��ȫʹ��Ԥ�����Դ��ĳߴ�
        GameObject cloneObj = Instantiate(slimeClonePrefab, spawnPos, Quaternion.identity);

        SlimeClone cloneScript = cloneObj.GetComponent<SlimeClone>();
        if (cloneScript == null) cloneScript = cloneObj.AddComponent<SlimeClone>();

        cloneScript.Init(this, speed);

        // play slime clone spawn sound
        if (audioSource != null && sfx_SlimeCloneSpawn != null)
            audioSource.PlayOneShot(sfx_SlimeCloneSpawn, sfxVolume);
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

    // ����Ƿ����ϰ�����
    bool CheckPlayerStuck()
    {
        Vector2 center = col.bounds.center;
        Vector2 size = col.bounds.size;

        // ��⵱ǰ�����Ƿ����ϰ���㣨ֻ���ǽ�����棬������ҡ����ߣ�
        Collider2D hit = Physics2D.OverlapBox(center, size, 0, obstacleLayer);

        // ����ײ�ص� = ����ס
        return hit != null;
    }


}
