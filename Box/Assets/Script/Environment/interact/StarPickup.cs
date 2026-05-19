using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class StarPickup : MonoBehaviour
{
    [Header("奖励设置")]
    public int starValue = 1;

    [Header("收集特效")]
    public GameObject collectEffect;

    [Header("待机动画")]
    public float floatHeight = 0.25f;
    public float floatDuration = 1.5f;

    [Header("存档ID（每个星星唯一）")]
    public string starID;

    private bool isCollected = false;

    void Start()
    {
        // 读存档：已捡则隐藏，没捡则显示
        if (PlayerPrefs.GetInt(starID, 0) == 1)
        {
            gameObject.SetActive(false);
            return;
        }

        // 动画（不变）
        GetComponent<Collider2D>().isTrigger = true;
        transform.DOMoveY(transform.position.y + floatHeight, floatDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
        transform.DOScale(new Vector3(1.1f, 0.9f, 1f), floatDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected || collision.CompareTag("Player") == false) return;
        CollectStar();
    }

    void CollectStar()
    {
        isCollected = true;
        transform.DOKill();

        // 标记已捡（永久）
        PlayerPrefs.SetInt(starID, 1);
        PlayerPrefs.Save();

        // 飞行动画（不变）
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(1.5f, 0.1f));
        Vector3 uiPos = UIManager.ClaimNextStarTargetPosition(out _);
        seq.Append(transform.DOMove(uiPos, 0.6f).SetEase(Ease.InCubic));
        seq.Join(transform.DOScale(0, 0.4f).SetDelay(0.2f));

        seq.OnComplete(() =>
        {
            StarSaveManager.Instance.AddStar();
            UIManager.Instance.CollectStar();
            if (collectEffect != null) Instantiate(collectEffect, transform.position, Quaternion.identity);
            gameObject.SetActive(false); // 改为隐藏而不是销毁，保证 R 键重置时能通过 FindObjectsInactive 找到它
        });
    }

    void OnDestroy()
    {
        transform.DOKill();
    }
}