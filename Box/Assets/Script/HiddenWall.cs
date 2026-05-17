using UnityEngine;

public class HiddenWall : MonoBehaviour
{
    [Header("墙体")]
    public SpriteRenderer wallSr;

    [Header("透明度控制")]
    public float fadeSpeed = 3f;
    public float targetAlpha = 0f; // 进入后透明度 0=全透 1=不透明

    private Color normalColor;
    private bool playerInside = false;

    void Start()
    {
        normalColor = wallSr.color;
    }

    void Update()
    {
        if (playerInside)
        {
            // 慢慢变透明
            Color c = wallSr.color;
            c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * fadeSpeed);
            wallSr.color = c;
        }
        else
        {
            // 慢慢恢复原状
            Color c = wallSr.color;
            c.a = Mathf.Lerp(c.a, normalColor.a, Time.deltaTime * fadeSpeed);
            wallSr.color = c;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")|| other.CompareTag("Box"))
        {
            playerInside = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")|| other.CompareTag("Box"))
        {
            playerInside = false;
        }
    }
}