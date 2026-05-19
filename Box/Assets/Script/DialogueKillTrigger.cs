using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class DialogueKillTrigger : MonoBehaviour
{
    [Header("对话内容")]
    [TextArea]
    public List<string> dialogueLines = new List<string>
    {
        "你终于来了...",
        "这条路不该属于你。",
        "你已经触犯了禁忌。",
        "现在——你必须死。"
    };

    [Header("UI引用（和NextLevelDoor一致）")]
    public GameObject dialoguePanel;
    public GameObject interactPrompt;
    public GameObject endImage;

    [Header("参数")]
    public float killDelay = 2f;

    private int _currentLine = 0;
    private bool _isPlayerNear = false;
    private bool _dialogueActive = false;
    private bool _dialogueFinished = false;

    private GameObject _player;
    private Entity_Stats _playerStats;
    private TextMeshProUGUI _dialogueText;
    private InputActionMap _playerActionMap;

    private void Awake()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        _playerStats = _player?.GetComponent<Entity_Stats>();

        if (dialoguePanel != null)
            _dialogueText = dialoguePanel.GetComponentInChildren<TextMeshProUGUI>();

        dialoguePanel?.SetActive(false);
        interactPrompt?.SetActive(false);
        endImage?.SetActive(false);

        if (_player != null)
        {
            var pi = _player.GetComponent<PlayerInput>();
            if (pi != null)
                _playerActionMap = pi.actions.FindActionMap("Player");
        }
    }

    private void Update()
    {
        // 对话中：只响应 W
        if (_dialogueActive && !_dialogueFinished)
        {
            bool w = Keyboard.current?.wKey.wasPressedThisFrame ?? false;
            bool up = Gamepad.current?.dpad.up.wasPressedThisFrame ?? false;
            if (w || up)
                TryNextLine();
            return;
        }

        // 没开始对话：范围内显示提示、按W开始
        if (_isPlayerNear && !_dialogueFinished)
        {
            interactPrompt?.SetActive(true);
            bool w = Keyboard.current?.wKey.wasPressedThisFrame ?? false;
            bool up = Gamepad.current?.dpad.up.wasPressedThisFrame ?? false;
            if (w || up)
                StartDialogue();
        }
        else
        {
            // 未开始对话才隐藏；对话开始后不再隐藏
            if (!_dialogueActive)
                interactPrompt?.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !_dialogueFinished)
        {
            _isPlayerNear = true;
            interactPrompt?.SetActive(true);
            interactPrompt.transform.DOKill();
            interactPrompt.transform.localScale = Vector3.zero;
            interactPrompt.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _isPlayerNear = false;
            // 对话已开始：不隐藏提示
            if (!_dialogueActive)
            {
                interactPrompt?.transform.DOKill();
                interactPrompt.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
                    .OnComplete(() => interactPrompt.SetActive(false));
            }
        }
    }

    void StartDialogue()
    {
        _dialogueActive = true;
        // 开始对话后：提示**一直保留**，不再隐藏

        // 锁玩家输入
        if (_playerActionMap != null)
            _playerActionMap.Disable();

        dialoguePanel?.SetActive(true);
        _currentLine = 0;
        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        if (_currentLine >= dialogueLines.Count) return;
        if (_dialogueText != null)
            _dialogueText.text = dialogueLines[_currentLine];
    }

    void TryNextLine()
    {
        _currentLine++;
        if (_currentLine < dialogueLines.Count)
        {
            ShowCurrentLine();
            return;
        }
        // 最后一句翻完：隐藏对话+提示，切图片
        if (_currentLine == dialogueLines.Count)
        {
            dialoguePanel?.SetActive(false);
            interactPrompt?.SetActive(false); // 只在最后结束时隐藏
            endImage?.SetActive(true);
            _dialogueFinished = true;
            StartCoroutine(KillPlayerAfterDelay());
        }
    }

    IEnumerator KillPlayerAfterDelay()
    {
        yield return new WaitForSeconds(killDelay);

        _playerStats?.TakeDamage(_playerStats.maxHealth);

        endImage?.SetActive(false);
        if (_playerActionMap != null)
            _playerActionMap.Enable();

        _dialogueActive = false;
    }

    private void OnDestroy()
    {
        interactPrompt?.transform.DOKill();
        if (_playerActionMap != null)
            _playerActionMap.Enable();
    }
}