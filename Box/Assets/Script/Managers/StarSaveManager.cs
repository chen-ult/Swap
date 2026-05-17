using UnityEngine;
using UnityEngine.InputSystem;

public class StarSaveManager : MonoBehaviour
{
    public static StarSaveManager Instance { get; private set; }
    private const string STAR_COUNT = "StarTotalCount";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int GetStarCount()
    {
        return PlayerPrefs.GetInt(STAR_COUNT, 0);
    }

    public void AddStar()
    {
        PlayerPrefs.SetInt(STAR_COUNT, GetStarCount() + 1);
        PlayerPrefs.Save();
    }

    // 🔥 R键：清空数量+清空所有星星存档
    public void ResetStars()
    {
        // 1. 清空总数量
        PlayerPrefs.SetInt(STAR_COUNT, 0);

        // 2. 清空所有星星的拾取记录（关键！）
        var stars = Object.FindObjectsByType<StarPickup>(FindObjectsInactive.Include);
        foreach (var star in stars)
        {
            PlayerPrefs.DeleteKey(star.starID);
        }

        PlayerPrefs.Save();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetStars();

            if (UIManager.Instance != null)
                UIManager.Instance.ResetStarUI();

            // 🔥 重新加载当前场景，让星星重新生成
            LevelManager.Instance.RestartCurrentLevel();

            Debug.Log("✅ R键：星星数量+拾取记录全部重置，场景重载");
        }
    }
}