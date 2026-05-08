using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class SpeedBreakableObstacle : MonoBehaviour
{
    [Header("撞击要求")]
    public float requiredSpeed = 15f;

    [Header("悬浮文字要求提示")]
    public Transform customTextPosition;
    public Vector3 textOffset = new Vector3(0, 1.2f, 0);
    public bool enableFloatingAnim = true;
    public Color textColor = Color.white;
    public Color rejectColor = Color.red;

    [Header("破碎效果(DOTween版)")]
    public Vector2 popOffset = new Vector2(0f, 2f);
    public float popDuration = 0.3f;
    public float fallDuration = 0.8f;
    public float fallDistance = 15f;

    [Header("音效")]
    public AudioClip breakSound;        // 破碎音效
    [Range(0f, 1f)] public float volume = 1f;
    private AudioSource audioSource;

    private Collider2D col;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool isBroken = false;

    private GameObject textObj;
    private TextMesh speedText;
    private Sequence textBounceSeq;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        // 自动添加 AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // --- 动态创建要求文字 ---
        textObj = new GameObject("RequiredSpeedDisplay");
        textObj.transform.SetParent(transform);

        if (customTextPosition != null)
        {
            textObj.transform.position = customTextPosition.position;
        }
        else
        {
            textObj.transform.localPosition = textOffset;
        }

        speedText = textObj.AddComponent<TextMesh>();
        speedText.anchor = TextAnchor.MiddleCenter;
        speedText.alignment = TextAlignment.Center;
        speedText.characterSize = 0.05f;
        speedText.fontSize = 80;
        speedText.color = textColor;
        speedText.text = "≥ " + requiredSpeed.ToString("F0");

        MeshRenderer meshRenderer = textObj.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = "Ground";
            meshRenderer.sortingOrder = 100;
        }

        if (enableFloatingAnim)
        {
            float startLocalY = textObj.transform.localPosition.y;
            textObj.transform.DOLocalMoveY(startLocalY + 0.15f, 1f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (textBounceSeq != null && textBounceSeq.IsActive())
            textBounceSeq.Kill();

        if (textObj != null)
            textObj.transform.DOKill();

        transform.DOKill();
    }

    private void Update()
    {
        if (textObj != null)
        {
            float parentSignX = Mathf.Sign(transform.lossyScale.x);
            Vector3 currentScale = textObj.transform.localScale;
            textObj.transform.localScale = new Vector3(Mathf.Abs(currentScale.x) * parentSignX, currentScale.y, currentScale.z);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isBroken) return;

        Rigidbody2D hitterRb = collision.gameObject.GetComponent<Rigidbody2D>();

        if (hitterRb != null)
        {
            float hitSpeed = collision.relativeVelocity.magnitude;

            if (hitSpeed >= requiredSpeed)
            {
                BreakObstacle(hitterRb);
            }
            else
            {
                ShowRejectFeedback();
            }
        }
    }

    private void ShowRejectFeedback()
    {
        if (textBounceSeq != null && textBounceSeq.IsActive()) return;

        textBounceSeq = DOTween.Sequence();
        textBounceSeq.Append(DOTween.To(() => speedText.color, x => speedText.color = x, rejectColor, 0.05f));
        textBounceSeq.Join(textObj.transform.DOShakePosition(0.5f, new Vector3(0.2f, 0, 0), 12, 90));
        textBounceSeq.Append(DOTween.To(() => speedText.color, x => speedText.color = x, textColor, 0.3f));
    }

    private void BreakObstacle(Rigidbody2D hitter)
    {
        isBroken = true;

        // ========== 播放破碎音效 ==========
        if (breakSound != null)
            audioSource.PlayOneShot(breakSound, volume);

        col.isTrigger = true;
        if (rb != null) rb.simulated = false;

        Sequence popSeq = DOTween.Sequence();
        Vector3 startPos = transform.position;
        Vector3 popPeakPos = startPos + (Vector3)popOffset;
        Vector3 fallEndPos = popPeakPos + new Vector3(0, -fallDistance, 0);

        popSeq.Append(transform.DOMove(popPeakPos, popDuration).SetEase(Ease.OutQuad));
        if (textObj != null)
            popSeq.Join(textObj.transform.DOScale(new Vector3(1.5f, 0.2f, 1f), popDuration));

        popSeq.Append(transform.DOMove(fallEndPos, fallDuration).SetEase(Ease.InQuad));

        if (speedText != null)
        {
            popSeq.Join(DOTween.ToAlpha(() => speedText.color, x => speedText.color = x, 0f, fallDuration));
        }

        popSeq.OnComplete(() =>
        {
            if (gameObject != null)
                Destroy(gameObject);
        });
    }
}