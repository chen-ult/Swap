using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public abstract class EntityGaze : MonoBehaviour
{
    [Header("基础射线设置")]
    public Transform headPoint; // 射线起点
    public float gazeDistance;  // 视线距离
    public LayerMask hitLayers; // 射线能够击中的层级

    protected LineRenderer lineRenderer;
    protected GazeObject currentlyGazedObject;
    private GazeObject newlyHitGazeObject = null;

    protected bool isClosedLine = false;

    protected virtual void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        // 初始化白线的视觉参数
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.white;
    }

    protected virtual void Update()
    {
        if (isClosedLine || headPoint == null)
        {
            if (lineRenderer.enabled)
            {
                lineRenderer.enabled = false;
                HandleGazeStateChange(null);
            }
            return;
        }

        lineRenderer.enabled = true;

        // 1. 获取目标位置（由各个子类自己实现）
        Vector2 targetPos = GetGazeTargetPosition();
        Vector2 gazeStartPos = headPoint.position;

        Vector2 direction = (targetPos - gazeStartPos).normalized;
        gazeDistance = (targetPos - gazeStartPos).magnitude; // 根据目标动态调整长度

        // 2. 发射物理射线
        RaycastHit2D hit = Physics2D.Raycast(gazeStartPos, direction, gazeDistance, hitLayers);

        // 3. 画线并检测物体
        DrawLine(gazeStartPos, direction, hit);

        // 4. 更新注视物体的状态逻辑
        HandleGazeStateChange(newlyHitGazeObject);
    }

    // 抽象方法：强制子类去实现它们该看向哪里
    protected abstract Vector2 GetGazeTargetPosition();

    private void DrawLine(Vector2 gazeStartPos, Vector2 direction, RaycastHit2D hit)
    {
        lineRenderer.SetPosition(0, gazeStartPos);

        if (hit.collider != null)
        {
            lineRenderer.SetPosition(1, hit.point);
            newlyHitGazeObject = hit.collider.GetComponent<GazeObject>();
        }
        else
        {
            lineRenderer.SetPosition(1, gazeStartPos + direction * gazeDistance);
            newlyHitGazeObject = null; // 离开物体置空
        }
    }

    protected void HandleGazeStateChange(GazeObject newObject)
    {
        if (newObject != currentlyGazedObject)
        {
            if (currentlyGazedObject != null) currentlyGazedObject.DeactivateViaGaze();
            if (newObject != null) newObject.ActivateViaGaze();
            currentlyGazedObject = newObject;
        }
    }
}