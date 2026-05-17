using UnityEngine;
using DG.Tweening;
using TMPro;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class GameTimeMomentumProvider : MonoBehaviour, IMomentumSwappable
{
    [Header("时间动量配置")]
    [Tooltip("生成速度的方向向量（Inspector可调）")]
    public Vector2 presetDirection = Vector2.right;

    [Header("视觉交互")]
    public Color normalColor = Color.white;
    public Color bulletTimeColor = Color.yellow;
    public Color selectedColor = Color.cyan;

    private Rigidbody2D rb;
    private TextMeshProUGUI textMesh;

    private bool isInBulletTime = false;
    private bool isCurrentlySelected = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        textMesh = GetComponent<TextMeshProUGUI>();
        if (textMesh != null)
        {
            normalColor = textMesh.color;
        }
    }

    private void Update()
    {
        // 持续基于游戏通关时间产出动量
        if (GameTimer.Instance != null && rb != null)
        {
            float timeValue = GameTimer.Instance.GetElapsedSeconds();
            rb.linearVelocity = presetDirection.normalized * timeValue;
        }
    }

    private void OnEnable()
    {
        MomentumSwapManager.OnBulletTimeToggled += HandleBulletTimeToggle;
    }

    private void OnDisable()
    {
        MomentumSwapManager.OnBulletTimeToggled -= HandleBulletTimeToggle;
        if(textMesh != null) textMesh.DOKill();
    }

    private void HandleBulletTimeToggle(bool active)
    {
        isInBulletTime = active;
        UpdateColor();
    }

    private void UpdateColor()
    {
        if (textMesh == null) return;
        if (isCurrentlySelected) textMesh.color = selectedColor;
        else if (isInBulletTime) textMesh.color = bulletTimeColor;
        else textMesh.color = normalColor;
    }

    // ====== IMomentumSwappable 接口实现 ======
    public Rigidbody2D MomentumRigidbody => rb;

    public void ApplyMomentum(Vector2 momentum)
    {
        // 游戏时间作为本源产出，忽略获得的动量（只出不进）
        FlashSuccess();
    }

    public void SetSelectedVisual(bool isSelected)
    {
        isCurrentlySelected = isSelected;
        UpdateColor();
    }

    public void FlashSuccess()
    {
        if (textMesh == null) return;
        textMesh.transform.DOKill();
        textMesh.transform.localScale = Vector3.one;
        textMesh.transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0), 0.3f, 5, 1f);
    }
}