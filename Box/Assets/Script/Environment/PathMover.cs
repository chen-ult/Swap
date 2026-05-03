using UnityEngine;
using DG.Tweening;

public class PathMover : MonoBehaviour
{
    [Header("路径设置")]
    [Tooltip("把场景里作为路径点的空物体拖入这里。物体会依次经过这些点。")]
    public Transform[] waypoints;

    [Tooltip("走完一整圈(或一趟)需要多少秒？")]
    public float duration = 5f;

    [Header("动画设置")]
    [Tooltip("路径类型：\nLinear = 直线、直角拐弯\nCatmullRom = 平滑的曲线拐弯")]
    public PathType pathType = PathType.Linear;

    [Tooltip("循环方式：\nYoyo = 走到终点后原路倒退回来\nRestart = 瞬间闪回起点重新走")]
    public LoopType loopType = LoopType.Yoyo;

    [Tooltip("运动的节奏：Linear 代表全程匀速（最适合平台游戏）")]
    public Ease easeType = Ease.Linear;

    [Tooltip("是否闭合路径？（如果是，终点会自动连回起点变成一个圈）")]
    public bool isClosedPath = false;

    private void Start()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning("未设置路径点！", gameObject);
            return;
        }

        // DOTween 的 DOPath 需要一个 Vector3 数组，所以我们把 Transform 转成 Vector3
        Vector3[] positions = new Vector3[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
        {
            positions[i] = waypoints[i].position;
        }

        // 开启路径移动 (持续时间为 duration)
        // SetOptions 决定是否收尾相连
        // SetLoops(-1) 代表无限循环
        transform.DOPath(positions, duration, pathType)
                 .SetOptions(isClosedPath)
                 .SetLoops(-1, loopType)
                 .SetEase(easeType);
    }

    // --- 以下代码仅仅为了在 Unity 编辑器里直观看到路径的辅助线 ---
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }

        // 如果是闭合路径，把首尾连起来
        if (isClosedPath && waypoints[0] != null && waypoints[waypoints.Length - 1] != null)
        {
            Gizmos.DrawLine(waypoints[waypoints.Length - 1].position, waypoints[0].position);
        }
    }
}