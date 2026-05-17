using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class PreviousLevelDoor : MonoBehaviour
{
    [Header("关卡分支设置")]
    [Tooltip("如果不填，则默认加载上一关。如果填了，则跳转到指定名称的场景。")]
    public string targetSceneName;

    [Header("互动提示")]
    [Tooltip("靠近门时弹出的提示文字，例如上方子物体里的 TextMeshPro / Sprite")]
    public GameObject interactPrompt;

    [Header("音效设置")]
    [Tooltip("进入门（返回上一关）时的音效")]
    public AudioClip enterSound;

    private AudioSource audioSource;
    private bool isPlayerNear = false;
    private Vector3 initialDoorScale;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }
    }

    private void Start()
    {
        initialDoorScale = transform.localScale;

        transform.DOScaleX(initialDoorScale.x * 1.05f, 0.7f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void Update()
    {
        if (isPlayerNear)
        {
            bool isWPressed = Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame;
            bool isDpadUpPressed = Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame;

            if (isWPressed || isDpadUpPressed)
            {
                if (enterSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(enterSound);
                }
                EnterPreviousLevel();
            }
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = true;

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

    private void EnterPreviousLevel()
    {
        isPlayerNear = false;
        if (interactPrompt != null) interactPrompt.SetActive(false);

        if (!string.IsNullOrEmpty(targetSceneName))
        {
            Debug.Log($"[PreviousLevelDoor] 返回指定关卡: {targetSceneName}");
            LevelManager.Instance.LoadSpecificLevel(targetSceneName, true); // 此处明确标记这属于退回关卡
        }
        else
        {
            Debug.Log("[PreviousLevelDoor] 返回上一关卡");
            LevelManager.Instance.LoadPreviousLevel();
        }
    }
}