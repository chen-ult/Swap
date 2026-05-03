using UnityEngine;
using UnityEngine.InputSystem;

// 继承刚才写的父类 EntityGaze
public class PlayerGaze : EntityGaze
{
    private PlayerInputSet input;
    private Camera mainCamera;

    protected override void Awake()
    {
        base.Awake(); // 调用父类的 Awake 来初始化 LineRenderer
        input = new PlayerInputSet();
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (input != null) input.Enable();
    }

    private void OnDisable()
    {
        if (input != null) input.Disable();
    }

    protected override void Update()
    {
        // 1. 处理玩家专属的按键：开关射线
        if (input.Player.CloseLine.WasPerformedThisFrame() || (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame))
        {
            isClosedLine = !isClosedLine;
        }

        // 2. 执行父类的核心射线逻辑（画线和判定）
        base.Update();
    }

    // 3. 实现父类的抽象要求：告诉父类，玩家的视线目标=鼠标位置
    protected override Vector2 GetGazeTargetPosition()
    {
        if (Mouse.current == null) return headPoint.position;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        return mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, -mainCamera.transform.position.z));
    }
}