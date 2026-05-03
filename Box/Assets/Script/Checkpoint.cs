using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    private Animator anim;
    private bool isActivated = false;

    // 全局静态委托：用来告诉场景里所有的 Checkpoint 有新的大哥被激活了
    public static event Action<Checkpoint> OnAnyCheckpointActivated;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();

        // 订阅全局静态事件
        OnAnyCheckpointActivated += HandleNewCheckpointActivated;
    }

    private void Start()
    {
        // 放在 Start 里校验，保证 Awake 的静态事件已绑定完毕
        CheckIfIsCurrentSavedPoint();
    }

    private void OnDestroy()
    {
        // 组件销毁时必须取消订阅静态事件，防止内存泄漏
        OnAnyCheckpointActivated -= HandleNewCheckpointActivated;
    }

    private void CheckIfIsCurrentSavedPoint()
    {
        float savedX = PlayerPrefs.GetFloat("CheckpointX", 0);
        float savedY = PlayerPrefs.GetFloat("CheckpointY", 0);
        string savedScene = PlayerPrefs.GetString("CheckpointScene", "");

        // 如果这个存盘点就是本地记录上的最新存盘点
        if (savedScene == SceneManager.GetActiveScene().name &&
            Mathf.Abs(savedX - transform.position.x) < 0.1f &&
            Mathf.Abs(savedY - transform.position.y) < 0.1f)
        {
            isActivated = true;
            if (anim != null) anim.SetBool("isActivated", true);
            
            // 可选：刚进场景查出自己是老大时，也可以广播一下，确保别的被重置
            // OnAnyCheckpointActivated?.Invoke(this);
        }
        else
        {
            // 确保不是老大的点处于关闭状态
            isActivated = false;
            if (anim != null) anim.SetBool("isActivated", false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 如果玩家碰到，且自己还没被激活，才执行
        if (!isActivated && collision.CompareTag("Player"))
        {
            ActivateCheckpoint();
        }
    }

    private void ActivateCheckpoint()
    {
        isActivated = true;
        
        Debug.Log($"玩家激活了新的存档点：{gameObject.name}");

        // 1. 保存自身的坐标
        PlayerPrefs.SetString("CheckpointScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.SetFloat("CheckpointX", transform.position.x);
        PlayerPrefs.SetFloat("CheckpointY", transform.position.y);
        PlayerPrefs.Save(); 

        // 2. 播放自己的点亮动画
        if (anim != null)
        {
            anim.SetBool("isActivated", true);
        }

        // 3. 广播给全世界：“我亮了，其他所有点都给我关上！” (传入自己作为参数)
        OnAnyCheckpointActivated?.Invoke(this);
    }

    /// <summary>
    /// 其他 Checkpoint 触发激活时，这里会自动执行
    /// </summary>
    /// <param name="newlyActivatedPoint">刚刚被点亮的那个存档点对象</param>
    private void HandleNewCheckpointActivated(Checkpoint newlyActivatedPoint)
    {
        // 如果点亮的这个对象不是我（自己），说明我的老大哥地位被顶替了，我得熄灭
        if (newlyActivatedPoint != this)
        {
            isActivated = false; // 内部重置为未激活
            
            if (anim != null)
            {
                anim.SetBool("isActivated", false); // 播放熄灭的动画
            }
        }
    }
}