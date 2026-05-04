using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DG.Tweening;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadNextLevel()
    {
        if (isTransitioning) return;

        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            StartCoroutine(TransitionToScene(nextSceneIndex));
        else
            Debug.Log("已经是最后一关了！");
    }

    public void LoadSpecificLevel(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionToSceneByName(sceneName));
    }

    public void RestartCurrentLevel()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionToScene(SceneManager.GetActiveScene().buildIndex));
    }

    public void RespawnAtCheckpoint()
    {
        if (isTransitioning) return;

        string savedScene = PlayerPrefs.GetString("CheckpointScene", "");

        if (!string.IsNullOrEmpty(savedScene))
            StartCoroutine(TransitionToSavedCheckpoint(savedScene));
        else
            RestartCurrentLevel();
    }

    // --- 核心协程逻辑（剥离了UI，直接呼叫UIManager） ---

    private IEnumerator TransitionToScene(int sceneIndex)
    {
        isTransitioning = true;

        // 跨场景终极保护：在老场景被拔掉前，强行杀掉此时所有正在全场运作的 DOTween
        DOTween.KillAll();

        // 1. 呼叫 UI 管理器：屏幕变黑
        yield return UIManager.Instance.FadeOutRoutine();

        // 2. 加载新场景
        SceneManager.LoadScene(sceneIndex);

        // 必须等待两帧，确保旧场景物体已被销毁，新场景物体已完全初始化
        yield return null; yield return null; 

        MovePlayerToSpawnPoint();
        SnapCinemachineCamera();

        // 3. 呼叫 UI 管理器：屏幕变亮
        yield return UIManager.Instance.FadeInRoutine();

        isTransitioning = false;
    }

    private IEnumerator TransitionToSceneByName(string sceneName)
    {
        isTransitioning = true;
        DOTween.KillAll();

        yield return UIManager.Instance.FadeOutRoutine();

        SceneManager.LoadScene(sceneName);

        // 必须等待两帧
        yield return null; yield return null; 

        MovePlayerToSpawnPoint();
        SnapCinemachineCamera();

        yield return UIManager.Instance.FadeInRoutine();

        isTransitioning = false;
    }

    private IEnumerator TransitionToSavedCheckpoint(string sceneName)
    {
        isTransitioning = true;
        DOTween.KillAll();

        yield return UIManager.Instance.FadeOutRoutine();

        SceneManager.LoadScene(sceneName);

        // 必须等待两帧，新场景和新玩家才算加载完毕
        yield return null; yield return null; 

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float posX = PlayerPrefs.GetFloat("CheckpointX");
            float posY = PlayerPrefs.GetFloat("CheckpointY");
            player.transform.position = new Vector2(posX, posY);

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        SnapCinemachineCamera();

        yield return UIManager.Instance.FadeInRoutine();

        isTransitioning = false;
    }

    private void MovePlayerToSpawnPoint()
    {
        GameObject spawnPoint = GameObject.Find("SpawnPoint"); 
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (spawnPoint != null && player != null)
        {
            player.transform.position = spawnPoint.transform.position;
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    private void SnapCinemachineCamera()
    {
        // 重置物理时间轴以防受子弹时间残留影响导致相机逻辑死机
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;

        var allBehaviors = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
        foreach (var b in allBehaviors)
        {
            string typeName = b.GetType().Name;
            if (typeName == "CinemachineVirtualCamera" || typeName == "CinemachineCamera")
            {
                // 1. 强制将新玩家设为跟随目标（防止它还追踪着上一局被销毁的老玩家尸体）
                var followProp = b.GetType().GetProperty("Follow");
                if (followProp != null)
                {
                    followProp.SetValue(b, p.transform);
                }

                // 2. 切断上一帧的记录，强制本帧发生跳变（而不是带有延迟去缓慢移动过去）
                var prop = b.GetType().GetProperty("PreviousStateIsValid");
                if (prop != null)
                {
                    prop.SetValue(b, false);
                }
            }
        }
    }

    public void CompleteLevel(string thisLevelName, string nextLevelName, int collectedStars)
    {
        // 游戏通关大结算记录逻辑

        // 1. 解锁下一关！把下一关的字符串标记为彻底打通
        if (!string.IsNullOrEmpty(nextLevelName))
        {
            PlayerPrefs.SetInt("Unlocked_" + nextLevelName, 1);
        }

        // 2. 对比这关历史上拿到的最多星星数并保存高分
        int currentHighScore = PlayerPrefs.GetInt("Stars_" + thisLevelName, 0);
        if (collectedStars > currentHighScore)
        {
            PlayerPrefs.SetInt("Stars_" + thisLevelName, collectedStars);
        }

        // 保存进磁盘
        PlayerPrefs.Save();

        // 播完或者存完直接跳转下一关
        if (!string.IsNullOrEmpty(nextLevelName))
        {
            LoadSpecificLevel(nextLevelName);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.deleteKey.wasPressedThisFrame) 
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("已在真实游戏中成功删除所有存档数据！");
        }
    }
}