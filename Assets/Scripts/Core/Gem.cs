using UnityEngine;
using System.Collections;

namespace RetroMatch2D.Core
{
    /// <summary>
    /// 宝石类 - 表示游戏中的单个宝石对象
    /// </summary>
    public class Gem : MonoBehaviour
    {
        /// <summary>宝石类型</summary>
        private GemType gemType;
        
        /// <summary>网格X坐标</summary>
        private int x;
        
        /// <summary>网格Y坐标</summary>
        private int y;
        
        /// <summary>SpriteRenderer组件引用</summary>
        private SpriteRenderer spriteRenderer;
        
        /// <summary>是否正在移动</summary>
        private bool isMoving = false;
        
        /// <summary>是否正在销毁</summary>
        private bool isDestroying = false;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError("Gem必须拥有SpriteRenderer组件！");
            }
        }

        /// <summary>
        /// 初始化宝石
        /// </summary>
        /// <param name="type">宝石类型</param>
        /// <param name="gridX">网格X坐标</param>
        /// <param name="gridY">网格Y坐标</param>
        public void Initialize(GemType type, int gridX, int gridY)
        {
            gemType = type;
            x = gridX;
            y = gridY;
            
            gameObject.name = $"Gem_{type}_{gridX}_{gridY}";
            
            // 根据宝石类型设置颜色（可根据需要调整）
            SetGemColor(type);
            
            Debug.Log($"宝石已初始化: {type} 位置: ({gridX}, {gridY})");
        }

        /// <summary>
        /// 设置网格坐标
        /// </summary>
        /// <param name="newX">新的X坐标</param>
        /// <param name="newY">新的Y坐标</param>
        public void SetPosition(int newX, int newY)
        {
            x = newX;
            y = newY;
            gameObject.name = $"Gem_{gemType}_{x}_{y}";
        }

        /// <summary>
        /// 设置网格位置（Vector2Int重载）
        /// </summary>
        /// <param name="position">新的网格位置</param>
        public void SetGridPosition(Vector2Int position)
        {
            SetPosition(position.x, position.y);
        }

        /// <summary>
        /// 移动到目标世界位置
        /// </summary>
        /// <param name="targetPos">目标世界坐标</param>
        /// <param name="duration">移动时间（秒）</param>
        public void MoveTo(Vector3 targetPos, float duration)
        {
            if (isMoving)
            {
                StopCoroutine(MoveCoroutine(targetPos, duration));
            }
            
            StartCoroutine(MoveCoroutine(targetPos, duration));
        }

        /// <summary>
        /// 移动协程
        /// </summary>
        private IEnumerator MoveCoroutine(Vector3 targetPos, float duration)
        {
            isMoving = true;
            Vector3 startPos = transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                
                // 使用平滑缓动
                transform.position = Vector3.Lerp(startPos, targetPos, EaseOutCubic(progress));
                
                yield return null;
            }

            transform.position = targetPos;
            isMoving = false;
        }

        /// <summary>
        /// 带动画的销毁 - 淡出效果
        /// </summary>
        public void DestroyWithAnimation()
        {
            if (isDestroying)
                return;

            StartCoroutine(DestroyCoroutine());
        }

        /// <summary>
        /// 销毁协程 - 淡出效果
        /// </summary>
        private IEnumerator DestroyCoroutine()
        {
            isDestroying = true;
            float duration = 0.3f;
            float elapsed = 0f;
            Color originalColor = spriteRenderer.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                Color newColor = originalColor;
                newColor.a = Mathf.Lerp(1f, 0f, progress);
                spriteRenderer.color = newColor;
                
                // 缩放效果
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.5f, progress);
                
                yield return null;
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// 获取宝石类型
        /// </summary>
        public GemType GetGemType()
        {
            return gemType;
        }

        /// <summary>
        /// 获取网格X坐标
        /// </summary>
        public int GetGridX()
        {
            return x;
        }

        /// <summary>
        /// 获取网格Y坐标
        /// </summary>
        public int GetGridY()
        {
            return y;
        }

        /// <summary>
        /// 是否正在移动
        /// </summary>
        public bool IsMoving()
        {
            return isMoving;
        }

        /// <summary>
        /// 获取宝石的网格位置（Vector2Int格式）
        /// </summary>
        public Vector2Int GridPosition
        {
            get { return new Vector2Int(x, y); }
        }

        /// <summary>
        /// 获取宝石类型（属性访问器）
        /// </summary>
        public GemType Type
        {
            get { return gemType; }
        }

        /// <summary>
        /// 根据宝石类型设置颜色
        /// </summary>
        private void SetGemColor(GemType type)
        {
            if (spriteRenderer == null)
                return;

            Color color = Color.white;
            switch (type)
            {
                case GemType.Red:
                    color = new Color(1f, 0.2f, 0.2f);
                    break;
                case GemType.Blue:
                    color = new Color(0.2f, 0.5f, 1f);
                    break;
                case GemType.Green:
                    color = new Color(0.2f, 1f, 0.2f);
                    break;
                case GemType.Yellow:
                    color = new Color(1f, 1f, 0.2f);
                    break;
                case GemType.Purple:
                    color = new Color(0.8f, 0.2f, 1f);
                    break;
                case GemType.Orange:
                    color = new Color(1f, 0.6f, 0.2f);
                    break;
            }
            
            spriteRenderer.color = color;
        }

        /// <summary>
        /// 三次方缓动函数（Out）- 动画曲线
        /// </summary>
        private float EaseOutCubic(float t)
        {
            float f = t - 1f;
            return f * f * f + 1f;
        }
    }
}
