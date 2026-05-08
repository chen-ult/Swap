using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SwappableObject))]
public class MomentumPortal : MonoBehaviour
{
    [Header("传送链接")]
    public MomentumPortal linkedPortal;

    [Header("传送设置")]
    public float transitionDuration = 0.25f;
    public bool redirectVelocity = true;
    public Vector2 ejectDirection = Vector2.up;
    public float velocityMultiplier = 1.0f;
    public float inertiaLockTime = 0.3f;

    [Header("动画控制")]
    public string idleAnimTrigger = "Idle";
    public string eatAnimTrigger = "Eat";
    public string spitAnimTrigger = "Spit";

    [Header("悬浮文字设置")]
    public Vector3 textOffset = new Vector3(0, 1.2f, 0);
    public Color textColor = Color.white;
    public float minShowSpeed = 0.5f;

    [Header("存储速度设置")]
    public float maxStoredSpeed = 50f;
    [HideInInspector] public Vector2 storedVelocity = Vector2.zero;

    public float ringRadius = 1.5f;
    private LineRenderer ringLine;
    private LineRenderer arrowLine;

    // ====================== 【新增：传送门音效】 ======================
    [Header("传送门音效")]
    public AudioClip portalInSound;    // 吸入音效
    public AudioClip portalOutSound;   // 吐出音效
    [Range(0, 1)] public float soundVolume = 0.8f;
    private AudioSource audioSource;
    // ================================================================

    private HashSet<Rigidbody2D> cooldownObjects = new HashSet<Rigidbody2D>();
    private Animator anim;
    private Rigidbody2D parentRb;

    private TextMesh speedText;
    private GameObject textObj;
    private float lastSpeed = -1f;
    private Sequence textBounceSeq;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        parentRb = GetComponent<Rigidbody2D>();
        if (parentRb == null)
        {
            parentRb = gameObject.AddComponent<Rigidbody2D>();
        }
        parentRb.bodyType = RigidbodyType2D.Kinematic;

        // ====================== 【新增：自动添加 AudioSource】 ======================
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = 1f;
        // ==========================================================================

        textObj = new GameObject("SpeedDisplay_" + gameObject.name);
        textObj.transform.SetParent(null);
        textObj.transform.position = transform.position + textOffset;

        speedText = textObj.AddComponent<TextMesh>();
        speedText.anchor = TextAnchor.MiddleCenter;
        speedText.alignment = TextAlignment.Center;
        speedText.characterSize = 0.05f;
        speedText.fontSize = 80;
        speedText.color = textColor;
        speedText.text = "0";
        speedText.gameObject.SetActive(false);

        MeshRenderer meshRenderer = textObj.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = "Ground";
            meshRenderer.sortingOrder = 80;
        }

        GameObject ringObj = new GameObject("VelocityRing_" + gameObject.name);
        ringObj.transform.SetParent(transform);
        ringObj.transform.localPosition = Vector3.zero;

        ringLine = ringObj.AddComponent<LineRenderer>();
        ringLine.material = new Material(Shader.Find("Sprites/Default"));
        ringLine.startColor = new Color(1f, 1f, 1f, 1f);
        ringLine.endColor = new Color(1f, 1f, 1f, 1f);
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

        GameObject arrowObj = new GameObject("VelocityArrow_" + gameObject.name);
        arrowObj.transform.SetParent(ringObj.transform);
        arrowObj.transform.localPosition = Vector3.zero;

        arrowLine = arrowObj.AddComponent<LineRenderer>();
        arrowLine.material = new Material(Shader.Find("Sprites/Default"));
        arrowLine.startColor = new Color(1f, 1f, 1f, 1f);
        arrowLine.endColor = new Color(1f, 1f, 1f, 0f);
        arrowLine.startWidth = 0.5f;
        arrowLine.endWidth = 0f;
        arrowLine.useWorldSpace = false;
        arrowLine.positionCount = 2;
        arrowLine.sortingLayerName = "Ground";
        arrowLine.sortingOrder = 75;
        arrowLine.enabled = false;
    }

    private void OnDestroy()
    {
        if (textBounceSeq != null && textBounceSeq.IsActive()) textBounceSeq.Kill();
        if (textObj != null)
        {
            textObj.transform.DOKill();
            Destroy(textObj);
        }
        if (ringLine != null && ringLine.gameObject != null) Destroy(ringLine.gameObject);
    }

    private void Start()
    {
        if (anim != null && !string.IsNullOrEmpty(idleAnimTrigger))
        {
            anim.SetTrigger(idleAnimTrigger);
        }
    }

    private void Update()
    {
        bool hitMaxLimitThisFrame = false;

        if (parentRb != null && parentRb.linearVelocity.magnitude > 0.05f)
        {
            Vector2 incomingVel = parentRb.linearVelocity;
            storedVelocity += incomingVel;

            if (storedVelocity.magnitude >= maxStoredSpeed)
            {
                hitMaxLimitThisFrame = true;
                storedVelocity = Vector2.ClampMagnitude(storedVelocity, maxStoredSpeed);
            }
            parentRb.linearVelocity = Vector2.zero;
        }

        if (parentRb != null && speedText != null)
        {
            if (speedText.gameObject.activeSelf)
            {
                float floatOffsetY = Mathf.Sin(Time.time * 3f) * 0.15f;
                textObj.transform.position = transform.position + textOffset + new Vector3(0, floatOffsetY, 0);
                if (textBounceSeq == null || !textBounceSeq.IsActive())
                {
                    textObj.transform.rotation = Quaternion.identity;
                }
            }

            float currentSpeed = storedVelocity.magnitude;

            if (ringLine != null && arrowLine != null)
            {
                if (currentSpeed >= minShowSpeed)
                {
                    ringLine.enabled = true;
                    arrowLine.enabled = true;

                    Vector2 dir = storedVelocity.normalized;
                    Vector3 arrowStart = (Vector3)(dir * ringRadius);
                    float arrowLength = 0.5f + (currentSpeed / maxStoredSpeed) * 2f;
                    Vector3 arrowEnd = arrowStart + (Vector3)(dir * arrowLength);

                    arrowLine.SetPosition(0, arrowStart);
                    arrowLine.SetPosition(1, arrowEnd);

                    ringLine.startColor = new Color(1f, 1f, 1f, 1f);
                    ringLine.endColor = new Color(1f, 1f, 1f, 1f);
                }
                else
                {
                    ringLine.enabled = false;
                    arrowLine.enabled = false;
                }
            }

            if (currentSpeed < minShowSpeed)
            {
                if (speedText.gameObject.activeSelf)
                    speedText.gameObject.SetActive(false);
                lastSpeed = currentSpeed;
            }
            else
            {
                if (!speedText.gameObject.activeSelf)
                    speedText.gameObject.SetActive(true);

                bool speedChanged = Mathf.Abs(currentSpeed - lastSpeed) > 0.1f;
                if (speedChanged || hitMaxLimitThisFrame)
                {
                    if (currentSpeed >= maxStoredSpeed)
                    {
                        speedText.text = maxStoredSpeed.ToString();
                        speedText.color = Color.red;
                    }
                    else
                    {
                        speedText.text = currentSpeed.ToString("F1");
                        speedText.color = textColor;
                    }

                    if (hitMaxLimitThisFrame)
                    {
                        if (textBounceSeq == null || !textBounceSeq.IsActive())
                        {
                            textBounceSeq = DOTween.Sequence();
                            textBounceSeq.Append(textObj.transform.DOScale(new Vector3(1.5f, 1.5f, 1f), 0.1f));
                            textBounceSeq.Join(textObj.transform.DOPunchRotation(new Vector3(0, 0, 30f), 0.3f, 10, 1f));
                            textBounceSeq.Append(textObj.transform.DOScale(Vector3.one, 0.2f));
                        }
                    }
                    else if (speedChanged && currentSpeed - lastSpeed > 5f)
                    {
                        if (textBounceSeq == null || !textBounceSeq.IsActive())
                        {
                            textBounceSeq = DOTween.Sequence();
                            textBounceSeq.Append(textObj.transform.DOScale(new Vector3(1.3f, 0.7f, 1f), 0.1f));
                            textBounceSeq.Append(textObj.transform.DOScale(new Vector3(0.8f, 1.2f, 1f), 0.1f));
                            textBounceSeq.Append(textObj.transform.DOScale(Vector3.one, 0.1f));
                        }
                    }

                    if (speedChanged)
                    {
                        lastSpeed = currentSpeed;
                    }
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (linkedPortal == null) return;

        Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
        if (rb == null) rb = collision.GetComponentInParent<Rigidbody2D>();
        if (rb == null) return;

        if (cooldownObjects.Contains(rb)) return;

        if (collision.CompareTag("Player") || collision.CompareTag("Box") || rb.CompareTag("Player") || rb.CompareTag("Box"))
        {
            StartCoroutine(TeleportRoutine(rb.gameObject, rb));
        }
    }

    private IEnumerator TeleportRoutine(GameObject obj, Rigidbody2D rb)
    {
        cooldownObjects.Add(rb);
        Vector2 incomingVelocity = rb.linearVelocity;
        Vector3 portalOriginalScale = obj.transform.localScale;
        Player player = obj.GetComponent<Player>();

        if (incomingVelocity.magnitude < 1f)
        {
            incomingVelocity = -transform.up * 5f;
        }

        rb.simulated = false;
        Entity entity = obj.GetComponent<Entity>();
        if (entity != null) entity.isKnocked = true;

        // ====================== 【播放：吸入音效】 ======================
        PlayPortalInSound();
        // ==============================================================

        if (anim != null && !string.IsNullOrEmpty(eatAnimTrigger))
        {
            anim.SetTrigger(eatAnimTrigger);
        }

        obj.transform.DOKill();
        Sequence eatSeq = DOTween.Sequence();
        eatSeq.Append(obj.transform.DOMove(transform.position, transitionDuration).SetEase(Ease.InBack));
        eatSeq.Join(obj.transform.DOScale(Vector3.zero, transitionDuration).SetEase(Ease.InBack));
        eatSeq.OnComplete(() => {
            if (anim != null && !string.IsNullOrEmpty(idleAnimTrigger))
                anim.SetTrigger(idleAnimTrigger);
        });

        yield return eatSeq.WaitForCompletion();

        if (obj == null || rb == null)
        {
            cooldownObjects.Remove(rb);
            yield break;
        }

        obj.transform.position = linkedPortal.transform.position;
        linkedPortal.AddExitCooldown(rb, transitionDuration + inertiaLockTime + 0.2f);

        // ====================== 【播放：吐出音效】 ======================
        linkedPortal.PlayPortalOutSound();
        // ==============================================================

        if (linkedPortal.anim != null && !string.IsNullOrEmpty(linkedPortal.spitAnimTrigger))
        {
            linkedPortal.anim.SetTrigger(linkedPortal.spitAnimTrigger);
        }

        Sequence spitSeq = DOTween.Sequence();
        spitSeq.Append(obj.transform.DOScale(portalOriginalScale, transitionDuration).SetEase(Ease.OutBack));
        spitSeq.OnComplete(() => {
            if (linkedPortal.anim != null && !string.IsNullOrEmpty(linkedPortal.idleAnimTrigger))
                linkedPortal.anim.SetTrigger(linkedPortal.idleAnimTrigger);
        });

        yield return spitSeq.WaitForCompletion();

        if (obj == null || rb == null)
        {
            cooldownObjects.Remove(rb);
            yield break;
        }

        rb.simulated = true;
        Vector2 outVelocity = incomingVelocity;
        Vector2 baseDirection = incomingVelocity.normalized;
        if (baseDirection == Vector2.zero) baseDirection = linkedPortal.transform.up;

        if (linkedPortal.redirectVelocity)
        {
            baseDirection = linkedPortal.transform.TransformDirection(linkedPortal.ejectDirection.normalized);
            outVelocity = baseDirection * incomingVelocity.magnitude;
        }

        Vector2 bonusVelocity = Vector2.zero;
        if (linkedPortal != null && linkedPortal.storedVelocity.magnitude > 0.1f)
        {
            bonusVelocity = linkedPortal.storedVelocity;
            linkedPortal.storedVelocity = Vector2.zero;
        }

        rb.linearVelocity = outVelocity * linkedPortal.velocityMultiplier + bonusVelocity;

        if (entity != null)
        {
            if (inertiaLockTime > 0)
            {
                yield return new WaitForSeconds(inertiaLockTime);
            }
            entity.isKnocked = false;
        }

        yield return new WaitForSeconds(0.1f);
        cooldownObjects.Remove(rb);
    }

    // ====================== 【音效播放方法】 ======================
    void PlayPortalInSound()
    {
        if (audioSource != null && portalInSound != null)
        {
            audioSource.PlayOneShot(portalInSound, soundVolume);
        }
    }

    void PlayPortalOutSound()
    {
        if (audioSource != null && portalOutSound != null)
        {
            audioSource.PlayOneShot(portalOutSound, soundVolume);
        }
    }
    // ============================================================

    public void AddExitCooldown(Rigidbody2D rb, float lockTime)
    {
        cooldownObjects.Add(rb);
        StartCoroutine(RemoveCooldownRoutine(rb, lockTime));
    }

    private IEnumerator RemoveCooldownRoutine(Rigidbody2D rb, float lockTime)
    {
        yield return new WaitForSeconds(lockTime);
        if (rb != null && cooldownObjects.Contains(rb))
        {
            cooldownObjects.Remove(rb);
        }
        else
        {
            cooldownObjects.RemoveWhere(item => item == null);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector2 worldEjectDir = transform.TransformDirection(ejectDirection.normalized);
        Gizmos.DrawRay(transform.position, worldEjectDir * 2f);

        if (linkedPortal != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, linkedPortal.transform.position);
        }
    }
}