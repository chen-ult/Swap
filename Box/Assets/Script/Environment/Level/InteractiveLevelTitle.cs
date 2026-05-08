using UnityEngine;
using System.Collections;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(TextMeshProUGUI))]
public class InteractiveLevelTitle : MonoBehaviour
{
    [Header("关卡标题设置")]
    public string levelTitle = "第一关：新的开始";
    public float entranceDelay = 0.5f;

    [Header("悬浮动画设置")]
    public float floatAmplitude = 25f;

    private TextMeshProUGUI textUI;
    private Vector3 originalScale;
    private float startY;
    private bool isFloating = false;

    private void Awake()
    {
        textUI = GetComponent<TextMeshProUGUI>();
        textUI.text = levelTitle;

        originalScale = transform.localScale;
        startY = transform.localPosition.y;
        transform.localScale = Vector3.zero;

        Color c = textUI.color;
        c.a = 0;
        textUI.color = c;
    }

    private void Start()
    {
        // 【终极修复】强制等 2 帧，让 UIManager 完全初始化
        StartCoroutine(WaitForUIManagerFullReady());
    }

    private IEnumerator WaitForUIManagerFullReady()
    {
        // 1. 先等 UIManager 实例化出来
        while (UIManager.Instance == null)
        {
            yield return null;
        }

        // 2. 等一整帧，确保 StartMenu 已经完成显示
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // 3. 只要开始菜单还在显示，就一直等
        while (UIManager.Instance.IsStartMenuVisible)
        {
            yield return null;
        }

        // 4. 等关卡加载完成
        while (LevelManager.Instance != null && LevelManager.Instance.IsTransitioning)
        {
            yield return null;
        }

        // 全部安全了，才显示标题
        PlayCuteEntrance();
    }

    private void PlayCuteEntrance()
    {
        Sequence seq = DOTween.Sequence();

        seq.AppendInterval(entranceDelay);
        seq.Append(transform.DOScale(originalScale, 0.8f).SetEase(Ease.OutBack, 2.5f));
        seq.Join(textUI.DOFade(1f, 0.5f));
        seq.Append(transform.DOPunchRotation(new Vector3(0, 0, 12f), 0.6f, 6, 0.5f));
        seq.AppendCallback(() => {
            isFloating = true;
        });
    }

    private void Update()
    {
        if (isFloating)
        {
            float floatOffset = Mathf.Sin(Time.time * Mathf.PI / 1.5f) * floatAmplitude;
            transform.localPosition = new Vector3(transform.localPosition.x, startY + floatOffset, transform.localPosition.z);
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
        if (textUI != null) textUI.DOKill();
    }

    public void PlayExitAndDestroy(float delay = 0f)
    {
        isFloating = false;

        Sequence seq = DOTween.Sequence();
        if (delay > 0f) seq.AppendInterval(delay);

        seq.Append(transform.DOScale(Vector3.zero, 0.6f).SetEase(Ease.InBack));
        if (textUI != null)
        {
            seq.Join(textUI.DOFade(0f, 0.45f));
        }

        seq.OnComplete(() => {
            if (textUI != null) textUI.DOKill();
            transform.DOKill();
            Destroy(gameObject);
        });
    }
}