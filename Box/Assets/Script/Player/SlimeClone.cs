using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class SlimeClone : MonoBehaviour
{
    [Header("分身设置")]
    public float bounceForceMultiplier = 1.5f;

    private float lifetime;
    private float currentTimer;
    private bool isCountingDown = false;
    private bool isDestroying = false;

    public Player playerSource;
    private GameObject textObj;
    private TextMesh timeText;
    private Vector3 originalScale;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // 平时：Dynamic + 高摩擦力 → 落地不滑、能被弹
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearDamping = 20f; // 高摩擦，落地停
        rb.angularDamping = 20f;
    }

    public void Init(Player player, float initialSpeed)
    {
        playerSource = player;
        lifetime = initialSpeed;
        currentTimer = lifetime;
        originalScale = transform.localScale;

        transform.localScale = Vector3.zero;
        transform.DOScale(originalScale, 0.4f).SetEase(Ease.OutBack);

        textObj = new GameObject("CloneTimerText");
        textObj.transform.SetParent(null);
        textObj.transform.position = transform.position + new Vector3(0, originalScale.y * 1.5f + 0.5f, 0);

        timeText = textObj.AddComponent<TextMesh>();
        timeText.anchor = TextAnchor.MiddleCenter;
        timeText.alignment = TextAlignment.Center;
        timeText.characterSize = 0.05f;
        timeText.fontSize = 60;
        timeText.color = Color.white;
        timeText.text = currentTimer.ToString("0.0");

        MeshRenderer meshRenderer = textObj.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = "Ground";
            meshRenderer.sortingOrder = 999;
        }
    }

    private void Update()
    {
        if (isDestroying) return;

        if (textObj != null)
        {
            textObj.transform.position = transform.position + new Vector3(0, originalScale.y * 1.5f + 0.5f, 0);
        }

        // 吸引时：给力、降低摩擦，允许被拉动
        if (playerSource != null && playerSource.IsAttracting)
        {
            rb.linearDamping = 5f; // 吸引时减小摩擦，能拉动
            float dist = Vector2.Distance(transform.position, playerSource.PlayerPos);
            if (dist <= playerSource.attractRadius)
            {
                Vector2 dir = ((Vector2)playerSource.PlayerPos - (Vector2)transform.position).normalized;
                rb.AddForce(dir * playerSource.attractSpeed * 0.4f);
                rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, playerSource.attractSpeed * 0.5f);
            }
        }
        else
        {
            // 不吸引：高摩擦、速度清零 → 落地不动、可被弹
            rb.linearDamping = 20f;
            rb.linearVelocity = Vector2.zero;
        }

        // 倒计时开始条件
        if (!isCountingDown && playerSource != null)
        {
            if (playerSource.moveInput.magnitude > 0.1f)
            {
                isCountingDown = true;
            }
        }

        // 倒计时
        if (isCountingDown)
        {
            currentTimer -= Time.deltaTime;
            if (timeText != null)
                timeText.text = Mathf.Max(currentTimer, 0).ToString("0.0");
            if (currentTimer <= 0)
            {
                isCountingDown = false;
                DestroyClone();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroying) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            // 吸引时：直接吸收，不反弹
            if (playerSource != null && playerSource.IsAttracting)
            {
                playerSource.AbsorbClone(this);
                return;
            }

            // 不吸引：玩家撞上去 → 玩家被反弹
            Entity entity = collision.gameObject.GetComponent<Entity>();
            Rigidbody2D pRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (entity != null && pRb != null)
            {
                Vector2 normal = collision.contacts[0].normal;
                Vector2 bounceDir = -normal.normalized;
                if (bounceDir.y > 0.1f) bounceDir = Vector2.up;
                entity.ApplyBounceKnockback(bounceDir, 13f * bounceForceMultiplier, 0.02f);

                transform.DOKill();
                DOTween.Sequence()
                    .Append(transform.DOScale(originalScale * 1.2f, 0.1f).SetEase(Ease.OutQuad))
                    .Append(transform.DOScale(originalScale, 0.1f).SetEase(Ease.InQuad));
            }
            return;
        }
    }

    public void DestroyClone()
    {
        if (isDestroying) return;
        isDestroying = true;
        GetComponent<Collider2D>().enabled = false;
        if (textObj != null) Destroy(textObj);

        transform.DOKill();
        DOTween.Sequence()
            .SetLink(gameObject)
            .Append(transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                if (playerSource != null && playerSource.gameObject != null)
                {
                    playerSource.RestoreFromSplit();
                }
                Destroy(gameObject);
            });
    }

    private void OnDestroy()
    {
        transform.DOKill();
        if (textObj != null) Destroy(textObj);
    }
}