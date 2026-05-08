using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class EndLetter : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color originalColor;
    [Tooltip("闪烁为白色的时长")]
    public float flashDuration = 0.5f;
    [Tooltip("闪烁回到原色的时长")]
    public float returnDuration = 0.3f;

    private bool lit = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (lit) return;
        if (other.CompareTag("Player"))
        {
            LightUp();
        }
    }

    public void LightUp()
    {
        if (sr == null) return;
        lit = true;
        // 先变成纯白
        sr.DOColor(Color.white, flashDuration).SetEase(Ease.OutFlash);
        // 然后回到原色（延时）
        sr.DOColor(originalColor, returnDuration).SetDelay(flashDuration).SetEase(Ease.InOutSine);
    }
}
