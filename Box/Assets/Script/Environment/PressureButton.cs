using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PressureButton : MonoBehaviour
{
    [Header("按钮美术配置")]
    public Sprite unpressedSprite; // 未被踩下的图片
    public Sprite pressedSprite;   // 被踩下时的图片

    [Header("行为设置")]
    [Tooltip("如果勾选，按钮一旦被踩下就会永久保持激活状态，哪怕箱子或玩家离开了也不会弹起关门！")]
    public bool stayPressed = false;

    [Header("机关连接")]
    [Tooltip("把场景里你想开启的门（带有ToggleDoor脚本的物体）拖到这个数组里")]
    public ToggleDoor[] linkedDoors;

    private SpriteRenderer sr;
    
    // 记录目前有几个物体压在上面，防止玩家和箱子都在上面时，玩家一走门就关了
    private int objectsOnButton = 0; 

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (unpressedSprite != null) sr.sprite = unpressedSprite;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 可以根据需要限制只有实体（比如玩家、箱子）才能触发，忽略虚影、子弹等
        // if (!collision.CompareTag("Player") && !collision.CompareTag("Box")) return;

        objectsOnButton++;

        // 只要刚从 0 变成 1，说明按钮被踩下了
        if (objectsOnButton == 1)
        {
            ActivateMechanism(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // 【核心修改】：如果设置为永久压下，那么即便物体离开了，也直接忽略还原逻辑，让大门和按钮永远保持激活！
        if (stayPressed) return;

        objectsOnButton--;

        // 防止计算错误掉到0以下，当确认为0时，说明上面没东西了，松开按钮
        if (objectsOnButton <= 0)
        {
            objectsOnButton = 0;
            ActivateMechanism(false);
        }
    }

    private void ActivateMechanism(bool isPressed)
    {
        // 切换按钮自己的图片
        if (isPressed && pressedSprite != null)
            sr.sprite = pressedSprite;
        else if (!isPressed && unpressedSprite != null)
            sr.sprite = unpressedSprite;

        // 让所有连接到的门都跟着打开或关闭
        foreach (ToggleDoor door in linkedDoors)
        {
            if (door != null)
            {
                door.SetDoorState(isPressed);
            }
        }
    }
}