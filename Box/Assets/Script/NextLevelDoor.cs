using UnityEngine;


using UnityEngine.InputSystem;
using DG.Tweening;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class NextLevelDoor : MonoBehaviour
{
    [Header("关卡分支设置")]
    [Tooltip("如果不填，则默认加载下一关（顺序加载）。如果填了，则跳转到指定名称的场景。")]
    public string targetSceneName;

    [Header("锁与钥匙设置")]
    [Tooltip("是否需要钥匙才能进入？如果不需要，玩家碰到门直接通关")]
    public bool requiresKey = false;

    [Tooltip("锁的图片/物体（拖入场景里贴在门上的锁），解锁时会播放震动掉落动画")]
    public Transform lockIcon;

    [Header("互动提示")]
    [Tooltip("靠近门时弹出的提示文字，例如上方子物体里的 TextMeshPro / Sprite")]
    public GameObject interactPrompt;

    [Header("音效设置")]
    [Tooltip("门解锁时的音效")]
    public AudioClip unlockSound;
    [Tooltip("无法开启（拒绝）时的音效")]
    public AudioClip rejectSound;
    [Tooltip("进入门时的音效")]
    public AudioClip enterSound;

    private AudioSource audioSource;
    private bool isLocked;
    private bool isPlayerNear = false; // 玩家是否在门前
    private Vector3 initialLockScale;
    private Vector3 initialDoorScale;

    private void Awake()
    {
        // 初始化AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // 初始化时，如果需要钥匙，就默认为上锁状态
        isLocked = requiresKey;

        // 一开始隐藏提示
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }
    }

    private void Start()
    {
        initialDoorScale = transform.localScale;

        // 门本身的Q弹呼吸待机动画
        // 只拉伸 X 轴（宽度），保持 Y 轴（高度）完全不变。
        // 这样可以避免由于门图片的中心点(Pivot)在居中位置，导致缩放时门的底部悬空或钻入地下的问题。
        transform.DOScaleX(initialDoorScale.x * 1.05f, 0.7f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        if (isLocked && lockIcon != null)
        {
            initialLockScale = lockIcon.localScale;

            // 锁的Q弹呼吸待机动画 (横向拉伸，纵向压扁，循环播放)
            lockIcon.DOScale(new Vector3(initialLockScale.x * 1.15f, initialLockScale.y * 0.85f, initialLockScale.z), 0.7f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    private void Update()
    {
        // 如果玩家在门范围内，且当前没上锁，按下 W 或 上方向键 进入
        if (isPlayerNear && !isLocked)
        {
            bool isWPressed = Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame;
            bool isDpadUpPressed = Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame;
            
            if (isWPressed || isDpadUpPressed)
            {
                if (enterSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(enterSound);
                }
                EnterNextLevel();
            }
        }
    }

    private void OnDestroy()
    {
        // 销毁时清理Tween避免报错
        transform.DOKill();
        if (lockIcon != null) lockIcon.DOKill();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = true;

            if (isLocked)
            {
                // 如果还锁着，玩家碰到门时给个拒绝的震动提示和音效
                if (rejectSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(rejectSound);
                }

                if (lockIcon != null && !DOTween.IsTweening(lockIcon))
                {
                    lockIcon.DOShakePosition(0.2f, 0.3f, 20, 90f);
                }
                return;
            }

            // 解锁状态下靠近，显示提示动画
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(true);
                interactPrompt.transform.DOKill();
                interactPrompt.transform.localScale = Vector3.zero;
                interactPrompt.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = false;

            // 离开范围关闭提示
            if (interactPrompt != null && interactPrompt.activeSelf)
            {
                interactPrompt.transform.DOKill();
                interactPrompt.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    interactPrompt.SetActive(false);
                });
            }
        }
    }

    private void EnterNextLevel()
    {
        // 一旦进入就可以阻止重复触发
        isPlayerNear = false;
        if (interactPrompt != null) interactPrompt.SetActive(false);

        // 如果填写了目标场景名称，就走分支逻辑
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            Debug.Log($"[NextLevelDoor] 进入指定关卡: {targetSceneName}");
            LevelManager.Instance.LoadSpecificLevel(targetSceneName);
        }
        // 否则按照老逻辑按顺序加载
        else
        {
            Debug.Log("[NextLevelDoor] 进入下一关卡");
            LevelManager.Instance.LoadNextLevel();
        }
    }

    /// <summary>
    /// 被钥匙吃掉时调用，解锁该门
    /// </summary>
    public void UnlockDoor()
    {
        if (!isLocked) 
        {
            Debug.LogWarning("[NextLevelDoor] 尝试解锁已解锁的门");
            return;
        }
        isLocked = false;
        Debug.Log("[NextLevelDoor] 门已解锁");

        if (unlockSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(unlockSound);
        }

        if (lockIcon != null)
        {
            // 打断待机的Q弹动画，并恢复默认大小作为掉落基准
            lockIcon.DOKill();
            lockIcon.localScale = initialLockScale;

            Sequence unlockSeq = DOTween.Sequence();

            // 1. 锁左右剧烈震动一下 (模拟解锁)
            unlockSeq.Append(lockIcon.DOShakePosition(0.4f, strength: new Vector3(0.4f, 0, 0), vibrato: 30));

            // 2. 震完以后锁往下掉落
            unlockSeq.Append(lockIcon.DOMoveY(lockIcon.position.y - 2f, 0.5f).SetEase(Ease.InCubic));

            // 3. 同时锁带有缩小的效果
            unlockSeq.Join(lockIcon.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack));

            // 4. 彻底隐藏毁掉这个锁
            unlockSeq.OnComplete(() =>
            {
                lockIcon.gameObject.SetActive(false);
            });
        }
    }
}