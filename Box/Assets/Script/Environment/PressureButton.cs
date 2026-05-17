using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
public class PressureButton : MonoBehaviour
{
    [Header("按钮美术配置")]
    public Sprite unpressedSprite;
    public Sprite pressedSprite;

    [Header("音效")]
    public AudioClip pressSound;
    [Range(0f, 1f)] public float soundVolume = 1f;

    [Header("行为设置")]
    [Tooltip("如果勾选，按钮一旦被踩下就会永久保持激活状态，哪怕箱子或玩家离开了也不会弹起关门！")]
    public bool stayPressed = false;

    [Header("机关连接")]
    public ToggleDoor[] linkedDoors;

    private SpriteRenderer sr;
    private AudioSource audioSource;
    private int objectsOnButton = 0;
    private bool isPermanentlyActivated = false;

    [SerializeField] private string buttonSaveID;
    private const string SAVE_KEY_PREFIX = "PressureButton_";

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (string.IsNullOrEmpty(buttonSaveID))
        {
            buttonSaveID = System.Guid.NewGuid().ToString();
        }
    }

    private void Start()
    {
        // 把读取存档放到 Start，确保门已初始化
        LoadButtonState();

        if (unpressedSprite != null && !isPermanentlyActivated)
            sr.sprite = unpressedSprite;
    }

    private void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetAllButtons();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isPermanentlyActivated) return;

        objectsOnButton++;

        if (objectsOnButton == 1)
        {
            PlayPressSound();
            ActivateMechanism(true);

            if (stayPressed)
            {
                isPermanentlyActivated = true;
                SaveButtonState();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (stayPressed || isPermanentlyActivated) return;

        objectsOnButton--;

        if (objectsOnButton <= 0)
        {
            objectsOnButton = 0;
            ActivateMechanism(false);
        }
    }

    private void ActivateMechanism(bool isPressed)
    {
        // 切换图片
        if (isPressed && pressedSprite != null)
            sr.sprite = pressedSprite;
        else if (!isPressed && unpressedSprite != null)
            sr.sprite = unpressedSprite;

        // 安全控制门（防空引用）
        if (linkedDoors == null) return;
        foreach (ToggleDoor door in linkedDoors)
        {
            if (door != null)
                door.SetDoorState(isPressed);
        }
    }

    #region 存档 & 重置
    private void SaveButtonState()
    {
        PlayerPrefs.SetInt(SAVE_KEY_PREFIX + buttonSaveID, isPermanentlyActivated ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadButtonState()
    {
        if (!stayPressed) return;

        isPermanentlyActivated = PlayerPrefs.GetInt(SAVE_KEY_PREFIX + buttonSaveID, 0) == 1;

        if (isPermanentlyActivated)
        {
            ActivateMechanism(true);
        }
    }

    public void ResetButton()
    {
        objectsOnButton = 0;
        isPermanentlyActivated = false;
        ActivateMechanism(false);
        SaveButtonState();
    }

    private void ResetAllButtons()
    {
        PressureButton[] allButtons = Object.FindObjectsByType<PressureButton>();
        foreach (var btn in allButtons)
        {
            btn.ResetButton();
        }
        Debug.Log("已重置所有按钮");
    }
    #endregion

    private void PlayPressSound()
    {
        if (pressSound == null || audioSource == null) return;
        audioSource.PlayOneShot(pressSound, soundVolume);
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(buttonSaveID))
            buttonSaveID = System.Guid.NewGuid().ToString();
    }
}