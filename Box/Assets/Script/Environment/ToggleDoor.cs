using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class ToggleDoor : MonoBehaviour
{
    [Header("状态图片配置")]
    [Tooltip("关门（阻挡）状态的图片")]
    public Sprite closedSprite;
    
    [Tooltip("开门（可通过）状态的图片")]
    public Sprite openSprite;

    private SpriteRenderer sr;
    private Collider2D col;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    /// <summary>
    /// 控制机关门状态的方法
    /// </summary>
    /// <param name="isOpen">true = 开门放行, false = 关门阻挡</param>
    public void SetDoorState(bool isOpen)
    {
        // 门打开时，关闭碰撞体（玩家可以通过）；门关上时，开启碰撞体
        col.enabled = !isOpen;
        
        // 切换对应的美术图片
        if (isOpen && openSprite != null)
            sr.sprite = openSprite;
        else if (!isOpen && closedSprite != null)
            sr.sprite = closedSprite;
    }
}