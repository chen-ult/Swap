using DG.Tweening;
using UnityEngine.InputSystem;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EndZone : MonoBehaviour
{
    [Header("视觉与交互设置")]
    [Tooltip("与 NextLevelDoor 一致的视觉效果：物体会做呼吸缩放")]
    public bool useDoorLikeEffect = true;

    [Tooltip("交互提示（例如 Press W 的 UI），放为 EndZone 的子对象或场景对象")]
    public GameObject interactPrompt;

    [Tooltip("提示相对于 EndZone 的本地偏移（世界单位）")]
    public Vector3 promptLocalOffset = new Vector3(0, 1.5f, 0);

    [Tooltip("按下 W 时的音效（可选）")]
    public AudioClip enterSound;

    [Header("结局UI设置")]
    [Tooltip("第一个结束图片 Panel（短暂显示）")]
    public GameObject endImagePanel;
    [Tooltip("谢谢游玩 Panel（显示总时间）")]
    public GameObject thanksPanel;
    [Tooltip("第一个结束图片显示时长（秒）")]
    public float firstImageDuration = 2f;

    private AudioSource audioSource;
    private bool isPlayerNear = false;
    private bool triggered = false;
    private Vector3 initialDoorScale;

    // optional renderer caching to set sorting layer to Ground
    private SpriteRenderer promptSpriteRenderer;
    private MeshRenderer promptMeshRenderer;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        initialDoorScale = transform.localScale;
        if (useDoorLikeEffect)
        {
            transform.DOScaleX(initialDoorScale.x * 1.05f, 0.7f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        if (interactPrompt != null)
        {
            // If interactPrompt refers to a Prefab asset (not in scene), instantiate a runtime instance
            // to avoid modifying prefab assets. Prefab assets have scene.rootCount == 0.
            if (interactPrompt.scene.rootCount == 0)
            {
                interactPrompt = Instantiate(interactPrompt);
            }

            // make it a child so local offset works consistently
            interactPrompt.transform.SetParent(transform, true);
            interactPrompt.transform.localPosition = promptLocalOffset;
            // set inactive initially
            interactPrompt.SetActive(false);

            // if it has a SpriteRenderer or MeshRenderer (world text), set sorting layer to Ground
            promptSpriteRenderer = interactPrompt.GetComponentInChildren<SpriteRenderer>();
            if (promptSpriteRenderer != null)
            {
                promptSpriteRenderer.sortingLayerName = "Ground";
                promptSpriteRenderer.sortingOrder = 100;
            }
            promptMeshRenderer = interactPrompt.GetComponentInChildren<MeshRenderer>();
            if (promptMeshRenderer != null)
            {
                promptMeshRenderer.sortingLayerName = "Ground";
                promptMeshRenderer.sortingOrder = 100;
            }
        }
    }

    private void Update()
    {
        if (isPlayerNear && !triggered)
        {
            bool isWPressed = Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame;
            bool isDpadUpPressed = Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame;
            if (isWPressed || isDpadUpPressed)
            {
                triggered = true;
                if (enterSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(enterSound);
                }

                float elapsed = 0f;
                if (GameTimer.Instance != null)
                {
                    elapsed = GameTimer.Instance.GetElapsedSeconds();
                    GameTimer.Instance.StopTimer();
                }

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowEndSequence(elapsed, firstImageDuration);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        isPlayerNear = true;

        if (interactPrompt != null)
        {
            // position relative to EndZone in case offset changed
            interactPrompt.transform.localPosition = promptLocalOffset;

            interactPrompt.SetActive(true);
            interactPrompt.transform.DOKill();
            interactPrompt.transform.localScale = Vector3.zero;
            interactPrompt.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
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
