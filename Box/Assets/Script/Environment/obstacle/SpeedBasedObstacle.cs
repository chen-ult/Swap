using UnityEngine;

namespace Environment
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpeedBasedObstacle : MonoBehaviour
    {
        public enum Mode
        {
            GhostWhenFast, // ghost (passable, semi-transparent) when player speed > threshold
            GhostWhenSlow  // ghost when player speed < threshold
        }

        [Tooltip("选择行为模式：快速时虚化 或 慢速时虚化")]
        public Mode mode = Mode.GhostWhenFast;

        [Tooltip("速度阈值，默认 25")]
        public float speedThreshold = 25f;

        [Tooltip("半透明的 alpha 值（0-1）当为虚化状态时使用）")]
        [Range(0f, 1f)]
        public float ghostAlpha = 0.5f;

        [Tooltip("如果未指定，将尝试在场景中找到标签为 'Player' 的对象并读取其 Rigidbody2D")]
        public Rigidbody2D playerRigidbody;

        Collider2D col;
        SpriteRenderer sr;
        float solidAlpha = 1f;

        void Reset()
        {
            col = GetComponent<Collider2D>();
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        }

        void Awake()
        {
            col = GetComponent<Collider2D>();
            sr = GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                Debug.LogWarning("SpeedBasedObstacle requires a SpriteRenderer for visual feedback.");
            }
            if (playerRigidbody == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerRigidbody = player.GetComponent<Rigidbody2D>();
                }
            }
        }

        void Update()
        {
            if (playerRigidbody == null) return;

            float speed = playerRigidbody.linearVelocity.magnitude;
            bool shouldGhost;

            if (mode == Mode.GhostWhenFast)
            {
                shouldGhost = speed > speedThreshold;
            }
            else
            {
                shouldGhost = speed < speedThreshold;
            }

            ApplyState(shouldGhost);
        }

        void ApplyState(bool ghost)
        {
            if (col != null)
            {
                // 当虚化时，将 collider 设为 trigger 以允许穿过
                col.isTrigger = ghost;
            }

            if (sr != null)
            {
                Color c = sr.color;
                float targetAlpha = ghost ? ghostAlpha : solidAlpha;
                if (!Mathf.Approximately(c.a, targetAlpha))
                {
                    c.a = targetAlpha;
                    sr.color = c;
                }
            }
        }

        // 编辑器中调整时也要生效
        void OnValidate()
        {
            col = GetComponent<Collider2D>();
            sr = GetComponent<SpriteRenderer>();
            if (Application.isPlaying)
            {
                // 不在编辑模式中处理 playerRigidbody 自动查找
                return;
            }

            if (playerRigidbody == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerRigidbody = player.GetComponent<Rigidbody2D>();
                }
            }

            // 在编辑器里根据当前设置更新外观（如果找不到 player 则使用 ghost=false）
            bool ghost = false;
            if (playerRigidbody != null)
            {
                float speed = playerRigidbody.linearVelocity.magnitude;
                ghost = (mode == Mode.GhostWhenFast) ? (speed > speedThreshold) : (speed < speedThreshold);
            }
            ApplyState(ghost);
        }
    }
}
