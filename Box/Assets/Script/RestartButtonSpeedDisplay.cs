using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class RestartButtonSpeedDisplay : MonoBehaviour
{
    [Header("基础设置")]
    public float minShowSpeed = 0.5f;
    public float maxSpeed = 50f;
    public float arrowDistance = 25f;

    [Header("文字位置（这里改立刻生效）")]
    public Vector2 textOffset = new Vector2(0, 30);

    [Header("箭头自定义大小")]
    public float arrowWidth = 8f;     // 箭头粗细
    public float arrowMinHeight = 20f;
    public float arrowMaxHeight = 60f;

    [Header("挂载子物体")]
    public Image arrow;
    public TextMeshProUGUI speedText;

    private RectTransform arrowRect;
    private bool isShowing = false;

    private void Awake()
    {
        arrowRect = arrow.GetComponent<RectTransform>();
        arrow.gameObject.SetActive(false);
        speedText.gameObject.SetActive(false);

        arrow.transform.DOKill();
        speedText.transform.DOKill();
    }

    public void UpdateSpeedDisplay(Vector2 velocity)
    {
        float mag = velocity.magnitude;

        if (mag < minShowSpeed)
        {
            HideVisuals();
            return;
        }

        ShowVisuals(velocity, mag);
    }

    void ShowVisuals(Vector2 velocity, float mag)
    {
        if (isShowing) return;
        isShowing = true;

        arrow.gameObject.SetActive(true);
        speedText.gameObject.SetActive(true);

        Vector2 dir = velocity.normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 箭头位置 + 旋转
        arrowRect.anchoredPosition = dir * arrowDistance;
        arrowRect.localEulerAngles = new Vector3(0, 0, angle - 90);

        // 箭头大小：宽度你自己调 arrowWidth，不再锁死4
        float arrowHeight = Mathf.Lerp(arrowMinHeight, arrowMaxHeight, mag / maxSpeed);
        arrowRect.sizeDelta = new Vector2(arrowWidth, arrowHeight);

        // 文字赋值 + 初始位置用你设置的 textOffset
        speedText.text = mag.ToString("F1");
        speedText.rectTransform.anchoredPosition = textOffset;

        arrow.transform.DOKill();
        speedText.transform.DOKill();

        // 箭头弹出
        arrow.transform.localScale = Vector3.zero;
        arrow.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

        // TMP 弹出
        speedText.transform.localScale = Vector3.zero;
        speedText.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack)
            .OnComplete(() => speedText.transform.DOScale(1f, 0.2f).SetEase(Ease.InOutSine));

        // 🔥 关键：浮动直接用你设置的 textOffset，改面板立刻生效
        speedText.rectTransform.DOAnchorPosY(textOffset.y + 8f, 0.8f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    void HideVisuals()
    {
        if (!isShowing) return;
        isShowing = false;

        arrow.transform.DOKill();
        speedText.transform.DOKill();

        arrow.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack)
            .OnComplete(() => arrow.gameObject.SetActive(false));

        speedText.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack)
            .OnComplete(() => speedText.gameObject.SetActive(false));
    }
}