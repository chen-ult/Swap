using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; 

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("玩家血量 UI")]
    [Tooltip("装有所有心形图片的空父物体")]
    public Transform heartContainer;  // 改为引用父物体
    private Image[] heartImages;      // 变成私有，代码启动时自动去找，不用再手动手拖了！
    public Sprite fullHeartSprite;    
    public Sprite emptyHeartSprite;   

    [Header("过关星星 UI")]
    [Tooltip("装有3个星星图片的空父物体")]
    public Transform starContainer;
    private Image[] starImages;
    public Sprite fullStarSprite;
    public Sprite emptyStarSprite;
    private int currentStars = 0;

    [Header("过渡动画 UI")]
    public Image fadeImage;           
    public float fadeDuration = 0.5f; 

    private Entity_Stats playerStats;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 

            // 🌟 新增：自动获取父节点下的所有 Image 组件
            if (heartContainer != null)
            {
                // GetComponentsInChildren 会自动按顺序把底下所有的 Image 找出来存成数组
                heartImages = heartContainer.GetComponentsInChildren<Image>();
            }
            else
            {
                Debug.LogWarning("UIManager: 未指定 Heart Container！");
            }

            if (starContainer != null)
            {
                starImages = starContainer.GetComponentsInChildren<Image>();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 0);
            fadeImage.gameObject.SetActive(false);
        }

        FindAndSubscribeToPlayer();
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        FindAndSubscribeToPlayer();

        // 每次重新加载场景，归零收集的星星并清空UI
        currentStars = 0;
        UpdateStarUI();
    }

    private void UpdateStarUI()
    {
        if (starImages == null || starImages.Length == 0) return;

        for (int i = 0; i < starImages.Length; i++)
        {
            if (i < currentStars) starImages[i].sprite = fullStarSprite;
            else starImages[i].sprite = emptyStarSprite;
        }
    }

    /// <summary>
    /// 提供给收集品调用的公开接口，获取UI上星星的位置（世界坐标）
    /// 并同时登记星星已经被提前锁定准备飞过来了
    /// </summary>
    public Vector3 ClaimNextStarTargetPosition(out int targetIndex)
    {
        targetIndex = currentStars;

        // 避免超过3颗星越界报错
        if (starImages == null || starImages.Length == 0 || targetIndex >= starImages.Length)
        {
            return Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Mathf.Abs(Camera.main.transform.position.z)));
        }

        // 把目标 UI 的坐标从 Canvas 屏幕空间转化到 2D 世界空间
        Vector3 uiScreenPos = starImages[targetIndex].transform.position;
        Vector3 worldTarget = Camera.main.ScreenToWorldPoint(new Vector3(uiScreenPos.x, uiScreenPos.y, Mathf.Abs(Camera.main.transform.position.z)));

        // 登记它被收了
        currentStars++;
        return worldTarget;
    }

    /// <summary>
    /// 当世界中的那颗星星终于飞到了 UI 坐标时，调用它点亮该 UI 图标
    /// </summary>
    public void LightUpStar(int index)
    {
        if (starImages == null || index >= starImages.Length) return;

        starImages[index].sprite = fullStarSprite;

        // 为 UI 来一段“被点亮放入”的Q弹特效！
        starImages[index].transform.DOPunchScale(new Vector3(0.5f, 0.5f, 0), 0.5f, 5, 1f);
    }

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

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealthUI;
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

    public IEnumerator FadeOutRoutine()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
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
}