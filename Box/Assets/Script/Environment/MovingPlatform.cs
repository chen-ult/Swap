using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MovingPlatform : MonoBehaviour
{
    [Header("移动设置")]
    [Tooltip("平台移动的目标点位 (可以设置多个。如A->B->C)")]
    public Transform[] waypoints;
    [Tooltip("移动速度")]
    public float speed = 2f;
    [Tooltip("到达每个点后的停顿时间")]
    public float waitTime = 0.5f;

    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;

    // 修改1：把初始化路点的逻辑从 Start 放到 Awake
    // 这是因为如果借助 SolidGazeObject 虚化机制，一开始这个脚本是被禁用的，Start不会执行。
    // Awake 无论脚本是否启用，只要挂载了就会在第一帧执行。
    private void Awake()
    {
        // 如果没有设置路点或者只有一个，就不移动了
        if (waypoints == null || waypoints.Length < 2)
        {
            Debug.LogWarning("移动平台没有设置足够的路径点！");
            enabled = false;
            return;
        }

        // 把初始位置作为起点（解绑父子关系以防路点随本身跟着一起动）
        foreach (Transform waypoint in waypoints)
        {
            waypoint.SetParent(null);
        }
    }

    private void Update()
    {
        // 计时器：停顿中
        if (waitTimer > 0)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        Transform targetWaypoint = waypoints[currentWaypointIndex];

        // 移动平滑算法，朝着目标路点移动
        transform.position = Vector2.MoveTowards(transform.position, targetWaypoint.position, speed * Time.deltaTime);

        // 如果距离目标点非常近了，代表到达该点
        if (Vector2.Distance(transform.position, targetWaypoint.position) < 0.05f)
        {
            // 停顿一会
            waitTimer = waitTime;
            
            // 切换到下一个点
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
            {
                currentWaypointIndex = 0; // 形成循环
            }
        }
    }

    // ==== 处理玩家跟随 ====
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 修改2：只有平台被激活时，才让玩家跟随（防止平台刚虚化消失时，玩家还贴着）
        if (enabled && collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 如果玩家当前确实是被平台绑定着，才解除
            if (collision.transform.parent == transform)
            {
                collision.transform.SetParent(null);
            }
        }
    }

    // 修改3：当平台被 SolidGazeObject 强制关闭(虚化)时，自动把站在上面的玩家丢落下来
    private void OnDisable()
    {
        // 找出所有子物体（即扒着平台的玩家），让他们解除父子关系
        foreach (Transform child in transform)
        {
            if (child.CompareTag("Player"))
            {
                child.SetParent(null);
            }
        }
    }
}