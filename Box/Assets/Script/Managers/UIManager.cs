using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    #region 单例
    public static UIManager Instance { get; private set; }
    #endregion

    #region 公开序列化字段（Inspector面板）
    [Header("玩家血量 UI")]
    [Tooltip("装有所有心形图片的空父物体")]
    public Transform heartContainer;
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;



    [Header("过渡动画 UI")]
    public Image fadeImage;
    public float fadeDuration = 0.5f;

    [Header("结束面板")]
    public GameObject endPanel;
    public TextMeshProUGUI endTimeText;

    [Header("结束序列UI")]
    public GameObject endImagePanel;
    public GameObject thanksPanel;

    [Header("暂停面板")]
    public GameObject pausePanel;
    public Button pauseResumeButton;
    public Button pauseQuitButton;
    public Button pauseRestartButton;
    public float pausePanelAnimDuration = 0.35f;

    [Header("屏幕菜单按钮")]
    public Button screenPauseButton;

    [Header("开始面板")]
    public GameObject startPanel;
    public TextMeshProUGUI startTitleText;
    public Image startTitleImage;
    public Button startStartButton;
    public Button startQuitButton;
    public float startPanelAnimDuration = 0.45f;
    public RawImage startBackgroundRaw;
    public float startBackgroundScrollDuration = 8f;
    public float startBackgroundBobScale = 1.025f;
    public float startBackgroundBobPeriod = 4f;

    [Header("Start Title Image Effects")]
    public float startTitleImagePopDuration = 0.35f;
    public float startTitleImageBobScale = 1.05f;
    public float startTitleImageBobPeriod = 3f;
    public float startTitleImageRotateAmount = 6f;
    public float startTitleImageRotatePeriod = 4f;

    [Header("结束面板文本")]
    public TextMeshProUGUI thanksTimeText;
    public TextMeshProUGUI thanksTitleText;
    public Button quitButton;
    public Button restartButton;

    [Header("End Sequence Config")]
    public float typingDuration = 1.2f;
    public float timeCountDuration = 1.0f;
    public Color titleGradientColorA = Color.cyan;
    public Color titleGradientColorB = Color.magenta;
    public Vector3 titleFinalPunch = new Vector3(0.2f, 0.2f, 0);
    public Vector3 timeFinalPunch = new Vector3(0.25f, 0.25f, 0);
    public Vector3 charPunch = new Vector3(0.06f, 0.06f, 0);
    public Vector3 buttonPopScale = default(Vector3);
    public float buttonPopDuration = 0.35f;

    [Header("UI 音效")]
    public AudioClip buttonClickSound;
    [Range(0f, 1f)] public float clickVolume = 1f;

    [Header("End Sequence Idle Effects")]
    public float titleIdleScale = 1.03f;
    public float titleIdlePeriod = 2f;
    public float titleBobAmount = 6f;
    public float timeBobAmount = 6f;
    public Color timeHighlightColor = Color.yellow;
    public float timeHighlightPeriod = 1.2f;

    [Header("跟随相机按钮")]
    public FollowCameraPauseButton followCameraButton;

    [Header("星星UI（图片×数量）")]
    public Image starIcon;       // 星星图标
    public TextMeshProUGUI starText; // 数量文本
    public int totalStarCount = 100000;   // 总数量（自己定）


    #endregion

    #region 私有变量
    private Image[] heartImages;

    private AudioSource uiAudio;

    private DG.Tweening.Core.TweenerCore<float, float, DG.Tweening.Plugins.Options.FloatOptions> titleGradientTweenRef;
    private DG.Tweening.Tween titleIdleTweenRef;
    private DG.Tweening.Tween titleBobTweenRef;
    private DG.Tweening.Tween timeHighlightTweenRef;
    private DG.Tweening.Tween timeBobTweenRef;
    private DG.Tweening.Tween timeCountTweenRef;

    private Entity_Stats playerStats;
    private Color restartButtonOrigColor = Color.white;
    private Color thanksTimeOrigColor = Color.white;
    

    public bool isPaused { get; private set; } = false;
    private bool isStartMenuVisible = false;
    public bool IsStartMenuVisible => isStartMenuVisible;

    private Coroutine endSequenceCoroutine;

    private DG.Tweening.Tween startBgScrollTween;
    private DG.Tweening.Tween startBgBobTween;
    private DG.Tweening.Tween titleImagePopTween;
    private DG.Tweening.Tween titleImageBobTween;
    private DG.Tweening.Tween titleImageRotateTween;
    private Color startTitleImageOrigColor;
    private Vector3 startTitleImageOrigScale;

    private float _tempSpeedFromTime = 0f;
    private bool _hasClickedTime = false;
    private float _finalElapsedTime = 0f;

    //star
    private int currentStarUI = 0; // UI显示计数


    #endregion

    #region 生命周期
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (heartContainer != null)
            {
                heartImages = heartContainer.GetComponentsInChildren<Image>();
            }
            else
            {
                Debug.LogWarning("UIManager: 未指定 Heart Container！");
            }

          


            

            uiAudio = GetComponent<AudioSource>();
            if (uiAudio == null)
            {
                uiAudio = gameObject.AddComponent<AudioSource>();
                uiAudio.playOnAwake = false;
            }

            if (startStartButton != null)
            {
                startStartButton.onClick.RemoveListener(OnStartButtonPressed);
                startStartButton.onClick.AddListener(OnStartButtonPressed);
                startStartButton.gameObject.SetActive(false);
            }
            if (startQuitButton != null)
            {
                startQuitButton.onClick.RemoveListener(OnQuitButtonPressed);
                startQuitButton.onClick.AddListener(OnQuitButtonPressed);
                startQuitButton.gameObject.SetActive(false);
            }

            if (startTitleImage != null)
            {
                startTitleImage.gameObject.SetActive(false);
                startTitleImageOrigColor = startTitleImage.color;
                startTitleImageOrigScale = startTitleImage.transform.localScale;
            }
            if (startTitleText != null)
            {
                startTitleText.gameObject.SetActive(false);
            }

            if (screenPauseButton != null)
            {
                screenPauseButton.onClick.RemoveListener(OnScreenPauseButtonClicked);
                screenPauseButton.onClick.AddListener(OnScreenPauseButtonClicked);
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        InitStarUI();
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 0);
            fadeImage.gameObject.SetActive(false);
        }

        FindAndSubscribeToPlayer();
        FindSceneButtons();

        try
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name == "level_0" && startPanel != null)
            {
                // 新增：判断 LevelManager 的重启标记，仅非重启时等待过渡后显示开始菜单
                if (LevelManager.Instance != null)
                {
                    if (LevelManager.Instance.IsTransitioning && !LevelManager.Instance.isRestartingToLevel0)
                    {
                        StartCoroutine(WaitForTransitionThenShowStart());
                    }
                    else if (!LevelManager.Instance.isRestartingToLevel0)
                    {
                        ShowStartMenu();
                    }
                }
                else if (!LevelManager.Instance?.isRestartingToLevel0 ?? true)
                {
                    // 无 LevelManager 时的兜底（首次进入）
                    ShowStartMenu();
                }
            }
        }
        catch { }

        if (followCameraButton != null)
        {
            followCameraButton.gameObject.SetActive(true);
            followCameraButton.transform.localScale = Vector3.zero;
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        try
        {
            DOTween.KillAll();
            if (titleGradientTweenRef != null) DOTween.Kill(titleGradientTweenRef);
            if (titleIdleTweenRef != null) DOTween.Kill(titleIdleTweenRef);
            if (titleBobTweenRef != null) DOTween.Kill(titleBobTweenRef);
            if (timeHighlightTweenRef != null) DOTween.Kill(timeHighlightTweenRef);
            if (timeBobTweenRef != null) DOTween.Kill(timeBobTweenRef);
            if (timeCountTweenRef != null) DOTween.Kill(timeCountTweenRef);

            if (thanksTitleText != null) DOTween.Kill(thanksTitleText);
            if (thanksTimeText != null) DOTween.Kill(thanksTimeText);
            if (endImagePanel != null) DOTween.Kill(endImagePanel);
            if (thanksPanel != null) DOTween.Kill(thanksPanel);
        }
        catch { }
    }

    private void Update()
    {
        if (endImagePanel != null && endImagePanel.activeSelf)
            return;

        bool escPressed = false;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
            escPressed = kb.escapeKey.wasPressedThisFrame;

        if (escPressed)
        {
            if (!isPaused) ShowPauseMenu();
            else HidePauseMenu();
        }

        try
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && thanksPanel != null && thanksPanel.activeSelf)
            {
                bool shiftDown = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
                if (restartButton != null && restartButton.image != null)
                {
                    restartButton.image.color = shiftDown ? timeHighlightColor : restartButtonOrigColor;
                }
                if (thanksTimeText != null)
                {
                    thanksTimeText.color = shiftDown ? timeHighlightColor : thanksTimeOrigColor;
                }
            }
        }
        catch { }
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealthUI;
        }
    }
    #endregion

    #region 场景加载与初始化
    private IEnumerator WaitForTransitionThenShowStart()
    {
        while (LevelManager.Instance != null && LevelManager.Instance.IsTransitioning)
            yield return null;
        ShowStartMenu();
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (endSequenceCoroutine != null)
        {
            StopCoroutine(endSequenceCoroutine);
            endSequenceCoroutine = null;
        }

        FindAndSubscribeToPlayer();
        FindSceneButtons();



        if (endImagePanel != null) endImagePanel.SetActive(false);
        if (thanksPanel != null) thanksPanel.SetActive(false);

        if (thanksTitleText != null) DOTween.Kill(thanksTitleText);
        if (thanksTimeText != null) DOTween.Kill(thanksTimeText);
        if (endImagePanel != null) DOTween.Kill(endImagePanel);
        if (thanksPanel != null) DOTween.Kill(thanksPanel);
    }

    private void FindSceneButtons()
    {
        try
        {
            FollowCameraPauseButton[] followBtns = Resources.FindObjectsOfTypeAll<FollowCameraPauseButton>();
            foreach (var btn in followBtns)
            {
                if (btn.gameObject.scene.isLoaded)
                {
                    followCameraButton = btn;
                    break;
                }
            }

            GameObject screenBtnObj = GameObject.Find("ScreenPauseButton");
            try
            {
                if (screenBtnObj == null) screenBtnObj = GameObject.FindWithTag("PauseButton");
            }
            catch { }

            if (screenBtnObj != null)
            {
                screenPauseButton = screenBtnObj.GetComponent<Button>();
            }

            if (screenPauseButton != null)
            {
                screenPauseButton.onClick.RemoveListener(OnScreenPauseButtonClicked);
                screenPauseButton.onClick.AddListener(OnScreenPauseButtonClicked);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("FindSceneButtons 出错: " + e.Message);
        }
    }
    #endregion

    #region 玩家与血量UI
    public void FindAndSubscribeToPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerStats = player.GetComponent<Entity_Stats>();

            if (playerStats != null)
            {
                playerStats.OnHealthChanged -= UpdateHealthUI;
                playerStats.OnHealthChanged += UpdateHealthUI;
                UpdateHealthUI(playerStats.currentHealth, playerStats.maxHealth);
            }
        }
    }

    private void UpdateHealthUI(int currentHealth, int maxHealth)
    {
        if (heartImages == null || heartImages.Length == 0) return;

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (i < currentHealth) heartImages[i].sprite = fullHeartSprite;
            else heartImages[i].sprite = emptyHeartSprite;

            if (i < maxHealth) heartImages[i].enabled = true;
            else heartImages[i].enabled = false;
        }
    }
    #endregion

    #region 星星UI
    void InitStarUI()
    {
        currentStarUI = StarSaveManager.Instance.GetStarCount();
        // 只显示：×数量
        starText.text = $"×{currentStarUI}";
    }

    // 收集星星
    public void CollectStar()
    {
        currentStarUI++;
        starText.text = $"×{currentStarUI}";

        // 动画
        starIcon.transform.DOKill();
        starIcon.transform.DOPunchScale(new Vector3(0.4f, 0.4f, 0), 0.4f, 5, 1f);
    }

    // 重置UI
    public void ResetStarUI()
    {
        currentStarUI = 0;
        starText.text = $"×0";
    }

    // 飞星目标位置
    public static Vector3 ClaimNextStarTargetPosition(out int targetIndex)
    {
        targetIndex = 0;
        return Camera.main.ScreenToWorldPoint(
            new Vector3(
                UIManager.Instance.starIcon.transform.position.x,
                UIManager.Instance.starIcon.transform.position.y,
                Mathf.Abs(Camera.main.transform.position.z)
            )
        );
    }
    #endregion

    #region 淡入淡出动画
    public IEnumerator FadeOutRoutine()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.transform.SetAsLastSibling();
            yield return fadeImage.DOFade(1f, fadeDuration).WaitForCompletion();
        }
    }

    public IEnumerator FadeInRoutine()
    {
        if (fadeImage != null)
        {
            yield return fadeImage.DOFade(0f, fadeDuration).WaitForCompletion();
            fadeImage.gameObject.SetActive(false);
        }
    }
    #endregion

    #region 结束面板
    public void ShowEndPanel(float elapsedSeconds)
    {
        if (endPanel == null) return;
        endPanel.SetActive(true);
        if (endTimeText != null)
        {
            endTimeText.text = string.Format("Time: {0:F2}s", elapsedSeconds);
        }
    }

    public void ShowEndSequence(float elapsedSeconds, float firstImageDuration = 2f)
    {
        // 👇 关键：把结束时的定格时间存起来
        _finalElapsedTime = elapsedSeconds;

        if (endImagePanel == null && thanksPanel == null)
            return;

        if (endSequenceCoroutine != null)
        {
            StopCoroutine(endSequenceCoroutine);
        }
        endSequenceCoroutine = StartCoroutine(ShowEndSequenceRoutine(elapsedSeconds, firstImageDuration));
    }

    private IEnumerator ShowEndSequenceRoutine(float elapsedSeconds, float firstImageDuration)
    {
        var title = Object.FindAnyObjectByType<InteractiveLevelTitle>();
        if (title != null)
        {
            title.PlayExitAndDestroy(0.1f);
        }

        if (endImagePanel != null)
        {
            var rt = endImagePanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
            }
            endImagePanel.SetActive(true);
            endImagePanel.transform.SetAsLastSibling();
            var t = endImagePanel.transform;
            t.localScale = Vector3.zero;
            t.DOKill();
            t.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        yield return new WaitForSeconds(firstImageDuration);

        if (endImagePanel != null)
        {
            endImagePanel.SetActive(false);
        }

        if (thanksPanel != null)
        {
            var rt2 = thanksPanel.GetComponent<RectTransform>();
            if (rt2 != null)
            {
                rt2.anchorMin = Vector2.zero;
                rt2.anchorMax = Vector2.one;
                rt2.anchoredPosition = Vector2.zero;
                rt2.sizeDelta = Vector2.zero;
            }

            if (thanksTimeText != null)
                thanksTimeText.text = string.Format("Total Time: {0:F2}s", elapsedSeconds);

            thanksPanel.SetActive(true);
            thanksPanel.transform.SetAsLastSibling();

            if (thanksTitleText != null)
            {
                string full = thanksTitleText.text;
                thanksTitleText.text = "";
                var g = new VertexGradient(titleGradientColorA, titleGradientColorA, titleGradientColorB, titleGradientColorB);
                thanksTitleText.colorGradient = g;
                thanksTitleText.DOKill();
                StartCoroutine(TypewriteText(thanksTitleText, full, typingDuration));

                if (thanksTitleText != null && thanksTitleText.gameObject.activeInHierarchy)
                {
                    titleGradientTweenRef = DG.Tweening.DOVirtual.Float(0f, 1f, typingDuration * 1.25f, v =>
                    {
                        if (thanksTitleText == null) return;
                        Color c1 = Color.Lerp(titleGradientColorA, titleGradientColorB, v);
                        Color c2 = Color.Lerp(titleGradientColorB, titleGradientColorA, v);
                        thanksTitleText.colorGradient = new VertexGradient(c1, c1, c2, c2);
                    }).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetTarget(thanksTitleText) as DG.Tweening.Core.TweenerCore<float, float, DG.Tweening.Plugins.Options.FloatOptions>;
                }
                DOVirtual.DelayedCall(typingDuration, () => StartTitleIdle());
            }

            if (thanksTimeText != null)
            {
                thanksTimeText.text = "Total Time: 0.00s";
                float current = 0f;
                if (thanksTimeText != null && thanksTimeText.gameObject.activeInHierarchy)
                {
                    timeCountTweenRef = DOVirtual.Float(0f, elapsedSeconds, timeCountDuration, v =>
                    {
                        if (thanksTimeText == null) return;
                        current = v;
                        thanksTimeText.text = string.Format("Total Time: {0:F2}s", current);
                    }).OnComplete(() =>
                    {
                        if (thanksTimeText != null) thanksTimeText.transform.DOPunchScale(timeFinalPunch, 0.45f, 8, 1f);
                        StartTimeIdle();
                    }).SetTarget(thanksTimeText);
                }

                try
                {
                    thanksTimeOrigColor = thanksTimeText.color;

                    if (restartButton != null && restartButton.image != null)
                    {
                        restartButtonOrigColor = restartButton.image.color;
                    }

                    var timeBtn = thanksTimeText.GetComponent<UnityEngine.UI.Button>();
                    if (timeBtn == null)
                        timeBtn = thanksTimeText.gameObject.AddComponent<UnityEngine.UI.Button>();
                    timeBtn.onClick.RemoveListener(OnThanksTimeClicked);
                    timeBtn.onClick.AddListener(OnThanksTimeClicked);
                }
                catch { }
            }

            if (quitButton != null)
            {
                quitButton.gameObject.SetActive(false);
                quitButton.onClick.RemoveListener(OnQuitButtonPressed);
                quitButton.onClick.AddListener(OnQuitButtonPressed);
            }
            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(false);
                restartButton.onClick.RemoveListener(OnRestartButtonPressed);
                restartButton.onClick.AddListener(OnRestartButtonPressed);
            }

            if (quitButton != null)
            {
                yield return new WaitForSeconds(0.15f);
                quitButton.gameObject.SetActive(true);
                var btnT = quitButton.transform;
                btnT.localScale = Vector3.zero;
                btnT.DOKill();
                btnT.DOScale(buttonPopScale == Vector3.zero ? Vector3.one : buttonPopScale, buttonPopDuration).SetEase(Ease.OutBack);
            }
            if (restartButton != null)
            {
                yield return new WaitForSeconds(0.12f);
                restartButton.gameObject.SetActive(true);
                var btnT2 = restartButton.transform;
                btnT2.localScale = Vector3.zero;
                btnT2.DOKill();
                btnT2.DOScale(buttonPopScale == Vector3.zero ? Vector3.one : buttonPopScale, buttonPopDuration).SetEase(Ease.OutBack);
            }
        }
        yield break;
    }
    #endregion

    #region 暂停菜单
    public void ShowPauseMenu()
    {
        if (pausePanel == null) return;
        PlayButtonSound();

        if (followCameraButton != null)
        {
            followCameraButton.ShowButton();
        }

        pausePanel.SetActive(true);
        pausePanel.transform.SetAsLastSibling();
        var rt = pausePanel.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        var t = pausePanel.transform;
        t.localScale = Vector3.zero;
        t.DOKill();
        t.DOScale(Vector3.one, pausePanelAnimDuration).SetEase(Ease.OutBack).SetUpdate(true);

        if (pauseResumeButton != null)
        {
            pauseResumeButton.gameObject.SetActive(false);
            pauseResumeButton.onClick.RemoveListener(HidePauseMenu);
            pauseResumeButton.onClick.AddListener(HidePauseMenu);
        }
        if (pauseQuitButton != null)
        {
            pauseQuitButton.gameObject.SetActive(false);
            pauseQuitButton.onClick.RemoveListener(OnQuitButtonPressed);
            pauseQuitButton.onClick.AddListener(OnQuitButtonPressed);
        }
        if (pauseRestartButton != null)
        {
            pauseRestartButton.gameObject.SetActive(false);
            pauseRestartButton.onClick.RemoveListener(OnPauseRestartButtonPressed);
            pauseRestartButton.onClick.AddListener(OnPauseRestartButtonPressed);

            bool hasCheckpoint = !string.IsNullOrEmpty(PlayerPrefs.GetString("CheckpointScene", ""));
            pauseRestartButton.interactable = hasCheckpoint;
        }

        DOVirtual.DelayedCall(pausePanelAnimDuration * 0.5f, () =>
        {
            if (pauseResumeButton != null)
            {
                pauseResumeButton.gameObject.SetActive(true);
                var b = pauseResumeButton.transform;
                b.localScale = Vector3.zero;
                b.DOKill();
                b.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
            }
            if (pauseRestartButton != null)
            {
                DOVirtual.DelayedCall(0.08f, () =>
                {
                    pauseRestartButton.gameObject.SetActive(true);
                    var b3 = pauseRestartButton.transform;
                    b3.localScale = Vector3.zero;
                    b3.DOKill();
                    b3.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
                }).SetUpdate(true);
            }
            if (pauseQuitButton != null)
            {
                DOVirtual.DelayedCall(0.08f, () =>
                {
                    pauseQuitButton.gameObject.SetActive(true);
                    var b2 = pauseQuitButton.transform;
                    b2.localScale = Vector3.zero;
                    b2.DOKill();
                    b2.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
                }).SetUpdate(true);
            }
        }).SetUpdate(true);

        isPaused = true;
    }

    public void HidePauseMenu()
    {
        if (pausePanel == null) return;
        PlayButtonSound();

        Time.timeScale = 1f;

        var t = pausePanel.transform;
        t.DOKill();
        t.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
        {
            pausePanel.SetActive(false);
        });

        if (isPaused && followCameraButton != null)
        {
            followCameraButton.HideButton();
        }

        if (pauseResumeButton != null) pauseResumeButton.gameObject.SetActive(false);
        if (pauseQuitButton != null) pauseQuitButton.gameObject.SetActive(false);
        if (pauseRestartButton != null) pauseRestartButton.gameObject.SetActive(false);
        isPaused = false;
    }
    #endregion

    #region 开始菜单
    public void ShowStartMenu()
    {
        if (startPanel == null) return;
        PlayButtonSound();

        startPanel.SetActive(true);
        startPanel.transform.SetAsLastSibling();
        var rt = startPanel.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        var t = startPanel.transform;
        t.DOKill();
        t.localScale = Vector3.one;
        StartBackgroundEffects();

        if (startTitleImage != null)
        {
            startTitleImage.gameObject.SetActive(true);
            if (startTitleText != null) startTitleText.gameObject.SetActive(false);
            startTitleImage.transform.DOKill();
            startTitleImage.color = startTitleImageOrigColor;
            startTitleImage.transform.localScale = Vector3.zero;

            titleImagePopTween?.Kill();
            titleImagePopTween = startTitleImage.transform.DOScale(startTitleImageOrigScale, startTitleImagePopDuration).SetEase(Ease.OutBack).SetUpdate(false);
            titleImagePopTween.OnComplete(() =>
            {
                titleImageBobTween?.Kill();
                titleImageBobTween = startTitleImage.transform.DOScale(startTitleImageOrigScale * startTitleImageBobScale, startTitleImageBobPeriod * 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(false);

                titleImageRotateTween?.Kill();
                titleImageRotateTween = startTitleImage.transform.DOLocalRotate(new Vector3(0, 0, startTitleImageRotateAmount), startTitleImageRotatePeriod * 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetRelative(true).SetUpdate(false);
            });
        }
        else if (startTitleText != null)
        {
            startTitleText.gameObject.SetActive(true);
        }

        if (startStartButton != null)
        {
            startStartButton.gameObject.SetActive(true);
            startStartButton.transform.localScale = Vector3.one;
        }
        if (startQuitButton != null)
        {
            startQuitButton.gameObject.SetActive(true);
            startQuitButton.transform.localScale = Vector3.one;
        }

        isStartMenuVisible = true;
    }

    public void HideStartMenu()
    {
        if (startPanel == null) return;
        PlayButtonSound();

        var t = startPanel.transform;
        t.DOKill();
        t.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).SetUpdate(false).OnComplete(() =>
        {
            startPanel.SetActive(false);
            if (startStartButton != null) startStartButton.gameObject.SetActive(false);
            if (startQuitButton != null) startQuitButton.gameObject.SetActive(false);
            isStartMenuVisible = false;
            StopBackgroundEffects();
            StopTitleImageEffects();
        });
    }
    #endregion

    #region 按钮点击事件
    private void OnScreenPauseButtonClicked()
    {
        if (isPaused)
        {
            HidePauseMenu();
        }
        else
        {
            ShowPauseMenu();
        }
    }

    private void OnStartButtonPressed()
    {
        PlayButtonSound();
        if (startStartButton != null)
        {
            var b = startStartButton.transform;
            b.DOKill();
            b.DOPunchScale(new Vector3(0.12f, 0.12f, 0), 0.18f, 8, 1f);
        }
        HideStartMenu();
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.StartTimer();
        }
    }

    private void OnQuitButtonPressed()
    {
        PlayButtonSound();
        if (quitButton != null)
        {
            var t = quitButton.transform;
            t.DOKill();
            t.DOPunchScale(new Vector3(0.12f, 0.12f, 0), 0.2f, 8, 1f);
        }
        DOVirtual.DelayedCall(0.18f, () =>
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        });
    }

    // ======================
    // 重启按钮点击（核心逻辑）
    // ======================
    private void OnRestartButtonPressed()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        bool shiftDown = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;

        // ==========================================
        // 按住 Shift → 速度存储模式
        // ==========================================
        if (shiftDown)
        {
            // 必须先点时间才生效
            if (!_hasClickedTime) return;

            // 构造速度
            Vector2 finalVel = Vector2.up * _tempSpeedFromTime;

            // 存入LevelManager
            if (LevelManager.Instance != null)
                LevelManager.Instance.storedRestartVelocity = finalVel;

            // 显示箭头UI
            if (restartButton != null)
            {
                var display = restartButton.GetComponent<RestartButtonSpeedDisplay>();
                if (display != null)
                    display.UpdateSpeedDisplay(finalVel);
            }

            // 清空状态
            _hasClickedTime = false;
            _tempSpeedFromTime = 0f;
            return;
        }

        // ==========================================
        // 没按 Shift → 正常重启
        // ==========================================
        PlayButtonSound();
        if (restartButton != null)
        {
            restartButton.transform.DOKill();
            
        }

        DOVirtual.DelayedCall(0.18f, () =>
        {
            
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.SetRestartingToLevel0(true);
                LevelManager.Instance.LoadSpecificLevel("level_0");
            }
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("level_0");
        });
    }


    private void OnPauseRestartButtonPressed()
    {
        PlayButtonSound();

        if (pauseRestartButton != null)
        {
            var t = pauseRestartButton.transform;
            t.DOKill();
            t.DOPunchScale(new Vector3(0.12f, 0.12f, 0), 0.2f, 8, 1f);
        }

        HidePauseMenu();

        DOVirtual.DelayedCall(0.2f, () =>
        {
            isPaused = false;
            Time.timeScale = 1f;

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.RespawnAtCheckpoint();
            }
        }).SetUpdate(true);
    }

    // ======================
    // 总时间点击（存速度）
    // ======================
    private void OnThanksTimeClicked()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        bool shiftDown = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
        if (!shiftDown) return;

        // 只记录速度，不应用
        _tempSpeedFromTime = _finalElapsedTime;
        _hasClickedTime = true;
    }
    #endregion

    #region 辅助功能
    private void PlayButtonSound()
    {
        if (uiAudio != null && buttonClickSound != null)
            uiAudio.PlayOneShot(buttonClickSound, clickVolume);
    }

    // UIManager.cs 中新增方法
    public void SkipStartMenu()
    {
        isStartMenuVisible = false;
        if (startPanel != null)
        {
            startPanel.SetActive(false);
            StopBackgroundEffects();
            StopTitleImageEffects();
        }
        // 同时启动游戏计时器（如果需要）
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.StartTimer();
        }
    }

    private void AssignGameTimeToRestart()
    {
        try
        {
            // 1. 获取总游戏时间（作为速度大小）
            float totalTime = 0f;
            if (GameTimer.Instance != null)
                totalTime = GameTimer.Instance.GetElapsedSeconds();

            // 2. 设置固定方向（你可以自己改：up/right/left/down）
            Vector2 direction = Vector2.up;
            Vector2 finalVelocity = direction * totalTime;

            // 3. 把【时间作为速度】存入 LevelManager（重启用）
            if (LevelManager.Instance != null)
                LevelManager.Instance.storedRestartVelocity = finalVelocity;

            // 4. 只更新 UI 显示（圆环+箭头+速度数值）
            if (restartButton != null)
            {
                restartButton.GetComponent<RestartButtonSpeedDisplay>().UpdateSpeedDisplay(finalVelocity);
            }

            // 5. 按钮闪烁反馈（保留）
            if (restartButton != null && restartButton.image != null)
            {
                restartButton.image.color = timeHighlightColor;
                DOVirtual.DelayedCall(0.35f, () =>
                {
                    if (restartButton != null && restartButton.image != null)
                        restartButton.image.color = restartButtonOrigColor;
                });
            }
        }
        catch { }
    }




    private IEnumerator TypewriteText(TextMeshProUGUI textComp, string full, float duration)
    {
        if (textComp == null) yield break;
        textComp.text = "";
        int total = full.Length;
        if (total == 0) yield break;
        float interval = Mathf.Max(0.01f, duration / total);
        for (int i = 0; i < total; i++)
        {
            textComp.text += full[i];
            textComp.transform.DOKill();
            textComp.transform.DOPunchScale(charPunch, 0.25f, 1, 0.5f);
            yield return new WaitForSeconds(interval);
        }
        textComp.transform.DOPunchScale(titleFinalPunch, 0.35f, 6, 1f);
        StartTitleIdle();
    }

    private void StartTitleIdle()
    {
        if (thanksTitleText == null) return;
        thanksTitleText.transform.DOKill();
        float baseScaleX = thanksTitleText.transform.localScale.x;
        Vector3 upScale = new Vector3(baseScaleX * titleIdleScale, baseScaleX * titleIdleScale, 1f);
        titleIdleTweenRef = thanksTitleText.transform.DOScale(upScale, titleIdlePeriod * 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetTarget(thanksTitleText);

        var rt = thanksTitleText.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.DOKill();
            titleBobTweenRef = rt.DOLocalMoveY(rt.localPosition.y + titleBobAmount, titleIdlePeriod * 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetTarget(thanksTitleText);
        }
    }

    private void StartTimeIdle()
    {
        if (thanksTimeText == null) return;
        var originalColor = thanksTimeText.color;
        if (thanksTimeText.gameObject.activeInHierarchy)
        {
            timeHighlightTweenRef = DOVirtual.Float(0f, 1f, timeHighlightPeriod, v =>
            {
                if (thanksTimeText == null) return;
                thanksTimeText.color = Color.Lerp(originalColor, timeHighlightColor, v);
            }).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetTarget(thanksTimeText);
        }
        var rt = thanksTimeText.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.DOKill();
            timeBobTweenRef = rt.DOLocalMoveY(rt.localPosition.y + timeBobAmount, timeHighlightPeriod * 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetTarget(thanksTimeText);
        }
    }

    private void StartBackgroundEffects()
    {
        if (startBackgroundRaw == null) return;
        var uv = startBackgroundRaw.uvRect;
        uv.x = 0f;
        startBackgroundRaw.uvRect = uv;

        startBgScrollTween?.Kill();
        startBgScrollTween = DG.Tweening.DOVirtual.Float(0f, 1f, startBackgroundScrollDuration, v =>
        {
            if (startBackgroundRaw == null) return;
            var r = startBackgroundRaw.uvRect;
            r.x = v;
            startBackgroundRaw.uvRect = r;
        }).SetEase(DG.Tweening.Ease.Linear).SetLoops(-1, DG.Tweening.LoopType.Restart).SetUpdate(true);

        startBgBobTween?.Kill();
        var t = startBackgroundRaw.transform;
        t.localScale = Vector3.one;
        startBgBobTween = t.DOScale(new Vector3(startBackgroundBobScale, startBackgroundBobScale, 1f), startBackgroundBobPeriod * 0.5f)
            .SetEase(DG.Tweening.Ease.InOutSine).SetLoops(-1, DG.Tweening.LoopType.Yoyo).SetUpdate(true);
    }

    private void StopBackgroundEffects()
    {
        if (startBgScrollTween != null) { startBgScrollTween.Kill(); startBgScrollTween = null; }
        if (startBgBobTween != null) { startBgBobTween.Kill(); startBgBobTween = null; }
        if (startBackgroundRaw != null)
        {
            var uv = startBackgroundRaw.uvRect;
            uv.x = 0f;
            startBackgroundRaw.uvRect = uv;
            startBackgroundRaw.transform.localScale = Vector3.one;
        }
    }

    private void StopTitleImageEffects()
    {
        if (titleImagePopTween != null) { titleImagePopTween.Kill(); titleImagePopTween = null; }
        if (titleImageBobTween != null) { titleImageBobTween.Kill(); titleImageBobTween = null; }
        if (titleImageRotateTween != null) { titleImageRotateTween.Kill(); titleImageRotateTween = null; }
        if (startTitleImage != null)
        {
            startTitleImage.transform.localScale = startTitleImageOrigScale;
            startTitleImage.transform.localRotation = Quaternion.identity;
            startTitleImage.color = startTitleImageOrigColor;
            startTitleImage.gameObject.SetActive(false);
        }
    }
    #endregion
}