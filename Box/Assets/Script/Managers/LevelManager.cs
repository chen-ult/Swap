using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DG.Tweening;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private bool isTransitioning = false;
    public bool IsTransitioning => isTransitioning;

    private bool isReturningFromNextLevel = false;

    public Vector2 storedCheckpointVelocity;
    public Vector2 storedRestartVelocity;
    public bool isRestartingToLevel0 = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (GameTimer.Instance == null)
            {
                GameObject gt = new GameObject("GameTimer");
                gt.AddComponent<GameTimer>();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetRestartingToLevel0(bool value)
    {
        isRestartingToLevel0 = value;
    }

    public void LoadNextLevel()
    {
        if (isTransitioning) return;
        isReturningFromNextLevel = false;
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            StartCoroutine(TransitionToScene(nextSceneIndex));
    }

    public void LoadPreviousLevel()
    {
        if (isTransitioning) return;
        isReturningFromNextLevel = true;
        int prevSceneIndex = SceneManager.GetActiveScene().buildIndex - 1;
        if (prevSceneIndex >= 0)
            StartCoroutine(TransitionToScene(prevSceneIndex));
    }

    public void LoadSpecificLevel(string sceneName, bool isReturning = false)
    {
        if (isTransitioning) return;
        isReturningFromNextLevel = isReturning;
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

    private IEnumerator TransitionToScene(int sceneIndex)
    {
        isTransitioning = true;
        DOTween.KillAll();

        yield return UIManager.Instance.FadeOutRoutine();
        SceneManager.LoadScene(sceneIndex);
        yield return null; yield return null;

        MovePlayerToSpawnPoint();
        SnapCinemachineCamera();
        CheckAndDestroyCollectedStars(); // 👈 新加：销毁已捡星星

        if (SceneManager.GetActiveScene().name == "level_0")
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowStartMenu();
        }

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
        CheckAndDestroyCollectedStars(); // 👈 新加：销毁已捡星星

        try
        {
            if (SceneManager.GetActiveScene().name == "level_0")
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.WakeUp();
                        rb.linearVelocity = storedRestartVelocity;
                        storedRestartVelocity = Vector2.zero;
                    }
                }
            }
        }
        catch { }

        SnapCinemachineCamera();

        if (SceneManager.GetActiveScene().name == "level_0" && !isRestartingToLevel0)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowStartMenu();
        }

        isRestartingToLevel0 = false;
        yield return UIManager.Instance.FadeInRoutine();
        isTransitioning = false;
    }

    private IEnumerator TransitionToSavedCheckpoint(string sceneName)
    {
        isTransitioning = true;
        DOTween.KillAll();

        yield return UIManager.Instance.FadeOutRoutine();
        SceneManager.LoadScene(sceneName);
        yield return null;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float posX = PlayerPrefs.GetFloat("CheckpointX");
            float posY = PlayerPrefs.GetFloat("CheckpointY");
            player.transform.position = new Vector2(posX, posY);

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.linearVelocity = storedCheckpointVelocity;
            }
        }

        SnapCinemachineCamera();
        CheckAndDestroyCollectedStars(); // 👈 新加：销毁已捡星星

        yield return UIManager.Instance.FadeInRoutine();
        FindAnyObjectByType<ClickableCheckpointSprite>()?.OnPlayerRespawnedAtCheckpoint();
        isTransitioning = false;
    }

    private void MovePlayerToSpawnPoint()
    {
        GameObject spawnPoint = null;

        if (isReturningFromNextLevel)
        {
            var nextDoor = Object.FindAnyObjectByType<NextLevelDoor>();
            if (nextDoor != null) spawnPoint = nextDoor.gameObject;
        }

        if (spawnPoint == null)
            spawnPoint = GameObject.Find("SpawnPoint");

        if (spawnPoint == null)
        {
            var prevDoor = Object.FindAnyObjectByType<PreviousLevelDoor>();
            if (prevDoor != null) spawnPoint = prevDoor.gameObject;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (spawnPoint != null && player != null)
        {
            player.transform.position = spawnPoint.transform.position;
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        isReturningFromNextLevel = false;
    }

    private void SnapCinemachineCamera()
    {
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
                var followProp = b.GetType().GetProperty("Follow");
                if (followProp != null)
                {
                    followProp.SetValue(b, p.transform);
                }

                var prop = b.GetType().GetProperty("PreviousStateIsValid");
                if (prop != null)
                {
                    prop.SetValue(b, false);
                }
            }
        }
    }

    // ✅ 关键：场景加载时自动销毁已捡星星
    private void CheckAndDestroyCollectedStars()
    {
        var stars = Object.FindObjectsByType<StarPickup>(FindObjectsInactive.Include);
        foreach (var star in stars)
        {
            if (PlayerPrefs.GetInt(star.starID, 0) == 1)
            {
                Destroy(star.gameObject);
            }
        }
    }

    public void CompleteLevel(string thisLevelName, string nextLevelName, int collectedStars)
    {
        if (!string.IsNullOrEmpty(nextLevelName))
        {
            PlayerPrefs.SetInt("Unlocked_" + nextLevelName, 1);
        }

        int currentHighScore = PlayerPrefs.GetInt("Stars_" + thisLevelName, 0);
        if (collectedStars > currentHighScore)
        {
            PlayerPrefs.SetInt("Stars_" + thisLevelName, collectedStars);
        }

        PlayerPrefs.Save();

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
            Debug.Log("已删除所有存档数据！");
        }

        // R键：重置星星 → 清空星星记录 + 重载场景（星星重新出现）
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug.Log("R键：重置星星并重载场景");
            LevelManager.Instance.RestartCurrentLevel();
        }
    }
}