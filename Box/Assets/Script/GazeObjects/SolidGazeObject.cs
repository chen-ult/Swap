using UnityEngine;

// 继承自刚才写的抽象基类 GazeObject
public class SolidGazeObject : GazeObject
{
    private Rigidbody2D rb2D;
    private MonoBehaviour[] customLogicScripts;

    private bool originalIsTrigger;
    private RigidbodyType2D originalBodyType;

    // 重写 Awake，在保留父类找 SpriteRenderer 的同时，查找专属的 Rigidbody
    protected override void Awake()
    {
        base.Awake(); // 必须调用父类的 Awake 执行画线准备

        rb2D = GetComponent<Rigidbody2D>();
        customLogicScripts = GetComponents<MonoBehaviour>();

        // 记录原始物理状态
        if (col2D != null) originalIsTrigger = col2D.isTrigger;
        if (rb2D != null) originalBodyType = rb2D.bodyType;
    }

    // 实现视线 100% 充满时的逻辑
    protected override void OnActivationComplete()
    {
        if (col2D != null) col2D.isTrigger = originalIsTrigger;
        if (rb2D != null) rb2D.bodyType = originalBodyType;

        foreach (var script in customLogicScripts)
        {
            if (script != this && script != null) script.enabled = true;
        }
    }

    // 实现刚开始虚化、或者充能耗尽时的逻辑
    protected override void OnDeactivatedComplete()
    {
        if (col2D != null) col2D.isTrigger = true;

        if (rb2D != null) 
        {
            rb2D.linearVelocity = Vector2.zero; // 或者旧版 Unity 使用 rb2D.velocity
            rb2D.bodyType = RigidbodyType2D.Static;
        }

        if (customLogicScripts != null)
        {
            foreach (var script in customLogicScripts)
            {
                if (script != this && script != null) script.enabled = false;
            }
        }
    }
}