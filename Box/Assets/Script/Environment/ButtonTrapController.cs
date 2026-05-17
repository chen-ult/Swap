using UnityEngine;

public class ButtonTrapController : MonoBehaviour
{
    [Header("要控制的尖刺")]
    public GameObject[] targetSpikes; // 拖入所有要控制的尖刺

    [Header("按钮效果")]
    public Sprite pressedSprite; // 按下后的按钮图片（可选）
    private SpriteRenderer _sr;
    private Sprite _originalSprite;

    private bool _isActivated = false;

    void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null)
        {
            _originalSprite = _sr.sprite;
        }
    }

    // 玩家碰到按钮就触发
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")||other.CompareTag("Box") && !_isActivated)
        {
            PressButton();
        }
    }

    // 按下按钮：尖刺消失
    void PressButton()
    {
        _isActivated = true;

        // 切换按钮图片
        if (_sr != null && pressedSprite != null)
        {
            _sr.sprite = pressedSprite;
        }

        // 关闭所有尖刺（包含碰撞+显示）
        foreach (GameObject spike in targetSpikes)
        {
            if (spike != null)
            {
                spike.SetActive(false);
            }
        }
    }

    // 可选：重置按钮（死亡后恢复）
    public void ResetButton()
    {
        _isActivated = false;
        if (_sr != null) _sr.sprite = _originalSprite;

        foreach (GameObject spike in targetSpikes)
        {
            if (spike != null)
            {
                spike.SetActive(true);
            }
        }
    }
}