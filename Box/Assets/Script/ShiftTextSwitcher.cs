using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.InputSystem;

[RequireComponent(typeof(TMP_Text))]
public class ShiftTextShake : MonoBehaviour
{
    [Header("文字内容")]
    [TextArea] public string normalText = "普通文字";
    [TextArea] public string shiftText = "愤怒文字！";

    [Header("颜色")]
    public Color normalColor = Color.white;
    public Color shiftColor = Color.red;

    [Header("抖动参数")]
    public float shakeDuration = 0.2f;
    public float shakeStrength = 5f;
    public int shakeVibrato = 10;

    private TMP_Text _text;
    private Tweener _shakeTween;
    private bool _isShaking = false;
    private Vector2 _originalAnchoredPos; // 存原始位置

    void Awake()
    {
        _text = GetComponent<TMP_Text>();
        // 记录初始位置
        _originalAnchoredPos = _text.rectTransform.anchoredPosition;
    }

    void Start()
    {
        ResetText();
    }

    void Update()
    {
        bool shiftDown = Keyboard.current != null &&
                          (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

        if (shiftDown && !_isShaking)
        {
            EnterAngry();
        }
        else if (!shiftDown && _isShaking)
        {
            ExitAngry();
        }
    }

    void EnterAngry()
    {
        _isShaking = true;
        _text.text = shiftText;
        _text.color = shiftColor;

        _shakeTween = _text.rectTransform
            .DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, 90f, true)
            .SetLoops(-1, LoopType.Restart)
            .SetEase(Ease.Linear)
            .SetUpdate(true);
    }

    void ExitAngry()
    {
        _isShaking = false;
        _shakeTween?.Kill();
        ResetText();
    }

    void ResetText()
    {
        _text.text = normalText;
        _text.color = normalColor;
        // 恢复原始位置，不是硬设0,0
        _text.rectTransform.anchoredPosition = _originalAnchoredPos;
    }

    void OnDestroy()
    {
        _shakeTween?.Kill();
    }
}