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
        yield return null; yield return null; 

        MovePlayerToSpawnPoint();

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
        yield return null; yield return null; 

        MovePlayerToSpawnPoint();

        yield return UIManager.Instance.FadeInRoutine();

        isTransitioning = false;
    }

    private IEnumerator TransitionToSavedCheckpoint(string sceneName)
    {
        isTransitioning = true;
        DOTween.KillAll();

        yield return UIManager.Instance.FadeOutRoutine();

        SceneManager.LoadScene(sceneName);
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