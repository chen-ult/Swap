using UnityEngine;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }

    private float startTime;
    private bool running = false;

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
            return;
        }
    }

    private void Start()
    {
        StartTimer();
    }

    public void StartTimer()
    {
        startTime = Time.unscaledTime;
        running = true;
    }

    public void StopTimer()
    {
        running = false;
    }

    public float GetElapsedSeconds()
    {
        if (!running) return Time.unscaledTime - startTime;
        return Time.unscaledTime - startTime;
    }
}
