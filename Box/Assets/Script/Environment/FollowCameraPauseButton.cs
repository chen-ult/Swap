using UnityEngine;
using DG.Tweening;

[DefaultExecutionOrder(1000)] // 确保此类的 LateUpdate 在 Cinemachine 相机真正移动完毕后才执行
public class FollowCameraPauseButton : MonoBehaviour
{
    [Header("摄像头跟随")]
    public Vector3 worldOffset = new Vector3(0, 2, 1);
    public float smoothSpeed = 10f;

    [Header("弹出/隐藏动画")]
    public float showDuration = 0.25f;
    public Ease showEase = Ease.OutBack;
    public float hideDuration = 0.2f;
    public Ease hideEase = Ease.InBack;

    [Header("延迟显示（和重启按钮同步）")]
    public float showDelay = 0.08f;

    private Vector3 originalScale;
    private Tween currentTween;
    private Camera mainCamera;

    private void Awake()
    {
        originalScale = transform.localScale;
        transform.localScale = Vector3.zero;
    }

    private void Start()
    {
        FindMainCamera();
    }

    public void FindMainCamera()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            mainCamera = FindAnyObjectByType<Camera>();
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            FindMainCamera();
            return;
        }

        // ✅ 钉死跟随，无延迟、无Lerp
        FollowCameraImmediate();
    }

    // ✅ 绝对固定在相机前方，不会飘！
    void FollowCameraImmediate()
    {
        Vector3 targetPos = mainCamera.transform.position + mainCamera.transform.TransformVector(worldOffset);

        // 直接强制同步 Transform，抛弃 rb.position 的物理帧延迟。只要关闭了刚体的 interpolation，这样强设就能绝对锁定视觉。
        transform.SetPositionAndRotation(targetPos, mainCamera.transform.rotation);

        // 如果有刚体，顺便把刚体位置拉过来（防止内部不同步），但主要靠 Transform 控制视觉
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = targetPos;
            rb.rotation = mainCamera.transform.eulerAngles.z;
        }
    }

    public void ShowButton()
    {
        KillTween();

        DOVirtual.DelayedCall(showDelay, () =>
        {
            transform.localScale = Vector3.zero;
            currentTween = transform.DOScale(originalScale, showDuration)
              .SetEase(showEase)
              .SetUpdate(true)
              // 新增：显示后触发箭头更新
              .OnComplete(() => {
                  var checkpoint = GetComponent<ClickableCheckpointSprite>();
                  if (checkpoint != null) checkpoint.UpdateArrowVisibility();
              });
        }).SetUpdate(true);
    }

    public void HideButton()
    {
        KillTween();
        currentTween = transform.DOScale(Vector3.zero, 0) // 修复：原代码隐藏时用了 0 时长，改为配置的 hideDuration
          .SetEase(hideEase)
          .SetUpdate(true)
          // 新增：隐藏后触发箭头更新
          .OnComplete(() => {
              var checkpoint = GetComponent<ClickableCheckpointSprite>();
              if (checkpoint != null) checkpoint.HideArrow();
          });
    }

    void KillTween()
    {
        if (currentTween != null) currentTween.Kill();
        DOTween.Kill(transform);
    }

    // 新增：对外暴露显示状态（可选）
    public bool IsVisible => transform.localScale.magnitude > 0.1f;
}