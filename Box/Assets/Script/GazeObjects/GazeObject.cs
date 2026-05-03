using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public abstract class GazeObject : MonoBehaviour
{
    [Header("虚化时的透明度")]
    [Range(0f, 1f)]
    public float etherealAlpha = 0.3f;

    [Header("描边与激活设置")]
    public float fillDuration = 1.5f;          // 射线照满所需的时间（秒）
    public float remainDuration = 1.0f;        // 移开视线后，保持激活的时间（秒）
    public float fadeDuration = 1.0f;          // 描边逐渐褪去所需的时间（秒）
    public Color outlineColor = Color.cyan;    
    public float outlineWidth = 0.05f;         

    protected SpriteRenderer spriteRenderer;
    protected Collider2D col2D; // 子类提取轮廓需要用到

    private bool isBeingGazed = false;     
    protected bool isFullyActivated = false; 
    private float fillProgress = 0f;       

    private float remainTimer = 0f;        

    // 绘制描边相关的变量
    private LineRenderer outlineRenderer;
    private List<Vector2> localOutlinePoints = new List<Vector2>();
    private float[] segmentDistances;
    private float totalOutlineLength;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col2D = GetComponent<Collider2D>();

        SetupOutlineRenderer();
        GenerateOutlinePath();
    }

    protected virtual void Start()
    {
        ForceDeactivate();
    }

    protected virtual void Update()
    {
        if (isBeingGazed)
        {
            if (!isFullyActivated)
            {
                fillProgress += Time.deltaTime / fillDuration;
                if (fillProgress >= 1f)
                {
                    fillProgress = 1f;
                    CompleteActivation();
                }
            }
        }
        else if (fillProgress > 0f)
        {
            if (remainTimer > 0f)
            {
                remainTimer -= Time.deltaTime;
            }
            else
            {
                fillProgress -= Time.deltaTime / fadeDuration;
                if (fillProgress <= 0f)
                {
                    fillProgress = 0f;
                    isFullyActivated = false;
                    ForceDeactivate();
                    if (outlineRenderer != null) outlineRenderer.positionCount = 0;
                }
            }
        }

        if (fillProgress > 0f)
        {
            UpdateOutlineVisual();
        }
    }

    public void ActivateViaGaze()
    {
        isBeingGazed = true;
        remainTimer = remainDuration;
    }

    public void DeactivateViaGaze()
    {
        isBeingGazed = false;
        if (isFullyActivated) remainTimer = remainDuration;
        else remainTimer = 0f;
    }

    private void CompleteActivation()
    {
        isFullyActivated = true;
        remainTimer = remainDuration; 

        // 统一的视觉：恢复不透明
        Color c = spriteRenderer.color;
        c.a = 1f;
        spriteRenderer.color = c;

        // ⭐ 调用子类各自的专属激活逻辑
        OnActivationComplete();
    }

    private void ForceDeactivate()
    {
        // 统一的视觉：变成虚化透明版
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = etherealAlpha;
            spriteRenderer.color = c;
        }

        // ⭐ 调用子类各自的专属虚化逻辑
        OnDeactivatedComplete();
    }

    // 🌟 抽象方法：强制所有继承这个类的子类必须实现具体的激活/失效行为
    protected abstract void OnActivationComplete();
    protected abstract void OnDeactivatedComplete();

    #region 描边核心视觉逻辑 (完全保留之前代码)
    // 注意：这里的内容和原来一模一样，不用修改
    private void SetupOutlineRenderer()
    {
        GameObject outlineObj = new GameObject("OutlineRenderer");
        outlineObj.transform.SetParent(transform);
        outlineObj.transform.localPosition = Vector3.zero;
        
        outlineRenderer = outlineObj.AddComponent<LineRenderer>();
        outlineRenderer.useWorldSpace = true; 
        outlineRenderer.startWidth = outlineWidth;
        outlineRenderer.endWidth = outlineWidth;
        outlineRenderer.material = new Material(Shader.Find("Sprites/Default")); 
        outlineRenderer.startColor = outlineColor;
        outlineRenderer.endColor = outlineColor;
        outlineRenderer.positionCount = 0;
        outlineRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        outlineRenderer.sortingOrder = spriteRenderer.sortingOrder + 1; 
    }

    private void GenerateOutlinePath()
    {
        localOutlinePoints.Clear();

        if (col2D is BoxCollider2D box)
        {
            Vector2 size = box.size / 2f;
            Vector2 offset = box.offset;
            localOutlinePoints.Add(offset + new Vector2(0, -size.y));
            localOutlinePoints.Add(offset + new Vector2(size.x, -size.y));
            localOutlinePoints.Add(offset + new Vector2(size.x, size.y));
            localOutlinePoints.Add(offset + new Vector2(-size.x, size.y));
            localOutlinePoints.Add(offset + new Vector2(-size.x, -size.y));
            localOutlinePoints.Add(offset + new Vector2(0, -size.y));
        }
        else if (col2D is CircleCollider2D circle)
        {
            int segments = 36;
            float radius = circle.radius;
            Vector2 offset = circle.offset;
            for (int i = 0; i <= segments; i++)
            {
                float angle = -Mathf.PI / 2f + (i * Mathf.PI * 2f / segments);
                localOutlinePoints.Add(offset + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }
        else if (col2D is PolygonCollider2D poly)
        {
            Vector2[] points = poly.points;
            if (points.Length < 3) return;

            int lowestIndex = 0;
            for (int i = 1; i < points.Length; i++)
            {
                if (points[i].y < points[lowestIndex].y)
                    lowestIndex = i;
            }

            for (int i = 0; i <= points.Length; i++)
            {
                int index = (lowestIndex + i) % points.Length;
                localOutlinePoints.Add(points[index]);
            }
        }

        totalOutlineLength = 0f;
        segmentDistances = new float[localOutlinePoints.Count - 1];
        
        for (int i = 0; i < localOutlinePoints.Count - 1; i++)
        {
            segmentDistances[i] = Vector2.Distance(localOutlinePoints[i], localOutlinePoints[i + 1]);
            totalOutlineLength += segmentDistances[i];
        }
    }

    private void UpdateOutlineVisual()
    {
        if (localOutlinePoints.Count == 0 || totalOutlineLength == 0) return;

        float targetDrawLength = fillProgress * totalOutlineLength;
        float currentLength = 0f;

        List<Vector3> renderPoints = new List<Vector3>();

        for (int i = 0; i < localOutlinePoints.Count - 1; i++)
        {
            Vector3 worldPos = transform.TransformPoint(localOutlinePoints[i]);
            renderPoints.Add(worldPos);

            if (currentLength + segmentDistances[i] >= targetDrawLength)
            {
                float remainder = targetDrawLength - currentLength;
                float t = remainder / segmentDistances[i];

                Vector3 nextWorldPos = transform.TransformPoint(localOutlinePoints[i + 1]);
                Vector3 interpolatedPos = Vector3.Lerp(worldPos, nextWorldPos, t);
                renderPoints.Add(interpolatedPos);
                break; 
            }
            else
            {
                currentLength += segmentDistances[i];
            }
        }

        outlineRenderer.positionCount = renderPoints.Count;
        outlineRenderer.SetPositions(renderPoints.ToArray());
    }
    #endregion
}