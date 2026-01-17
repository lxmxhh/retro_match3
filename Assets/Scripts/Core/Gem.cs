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

        /// <summary>是否为池化对象</summary>
        private bool isPooled = false;

        /// <summary>动画类型枚举</summary>
        public enum AnimationType
        {
            Linear,          // 线性
            EaseOutCubic,   // 三次方缓出
            EaseOutElastic, // 弹性缓出（适合交换）
            EaseOutBounce,  // 弹跳缓出（适合下落）
            EaseInOutBack   // 回退缓出（适合特殊效果）
        }

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
        /// <param name="animType">动画类型</param>
        public void MoveTo(Vector3 targetPos, float duration, AnimationType animType = AnimationType.EaseOutCubic)
        {
            if (isMoving)
            {
                StopAllCoroutines();
            }

            StartCoroutine(MoveCoroutine(targetPos, duration, animType));
        }

        /// <summary>
        /// 移动协程
        /// </summary>
        private IEnumerator MoveCoroutine(Vector3 targetPos, float duration, AnimationType animType)
        {
            isMoving = true;
            Vector3 startPos = transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // 根据动画类型应用不同的缓动函数
                float easedProgress = ApplyEasing(progress, animType);
                transform.position = Vector3.Lerp(startPos, targetPos, easedProgress);

                yield return null;
            }

            transform.position = targetPos;
            isMoving = false;
        }

        /// <summary>
        /// 带动画的销毁 - 增强版（缩放 + 旋转 + 淡出）
        /// </summary>
        /// <param name="useParticles">是否使用粒子效果（可选）</param>
        public void DestroyWithAnimation(bool useParticles = false)
        {
            if (isDestroying)
                return;

            StartCoroutine(EnhancedDestroyCoroutine(useParticles));
        }

        /// <summary>
        /// 增强版销毁协程 - 缩放 + 旋转 + 淡出效果
        /// </summary>
        private IEnumerator EnhancedDestroyCoroutine(bool useParticles)
        {
            isDestroying = true;
            float duration = 0.4f;
            float elapsed = 0f;
            Color originalColor = spriteRenderer.color;
            Vector3 originalScale = transform.localScale;
            Quaternion originalRotation = transform.rotation;

            // TODO: 后续添加粒子效果
            // if (useParticles)
            // {
            //     PlayParticleEffect();
            // }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // 淡出效果
                Color newColor = originalColor;
                newColor.a = Mathf.Lerp(1f, 0f, progress);
                spriteRenderer.color = newColor;

                // 缩放效果 - 先放大一点，然后缩小（punch效果）
                float scaleProgress = progress < 0.2f ?
                    Mathf.Lerp(1f, 1.2f, progress / 0.2f) :
                    Mathf.Lerp(1.2f, 0f, (progress - 0.2f) / 0.8f);
                transform.localScale = originalScale * scaleProgress;

                // 旋转效果 - 360度旋转
                float rotationAngle = Mathf.Lerp(0f, 360f, EaseOutCubic(progress));
                transform.rotation = originalRotation * Quaternion.Euler(0f, 0f, rotationAngle);

                yield return null;
            }

            // 如果是池化对象，归还到对象池
            // 否则直接销毁
            if (isPooled)
            {
                ReturnToPool();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 归还到对象池
        /// </summary>
        private void ReturnToPool()
        {
            isDestroying = false;

            // 使用GemPool归还
            var pool = RetroMatch2D.Managers.GemPool.Instance;
            if (pool != null)
            {
                pool.ReturnGem(this);
            }
            else
            {
                Debug.LogWarning("Gem: 无法找到GemPool实例，直接销毁");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 设置是否为池化对象
        /// </summary>
        public void SetPooled(bool pooled)
        {
            isPooled = pooled;
        }

        /// <summary>
        /// 获取是否为池化对象
        /// </summary>
        public bool IsPooled => isPooled;

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
        /// 应用缓动函数
        /// </summary>
        private float ApplyEasing(float t, AnimationType animType)
        {
            switch (animType)
            {
                case AnimationType.Linear:
                    return t;
                case AnimationType.EaseOutCubic:
                    return EaseOutCubic(t);
                case AnimationType.EaseOutElastic:
                    return EaseOutElastic(t);
                case AnimationType.EaseOutBounce:
                    return EaseOutBounce(t);
                case AnimationType.EaseInOutBack:
                    return EaseInOutBack(t);
                default:
                    return t;
            }
        }

        // ==================== 缓动函数库 ====================

        /// <summary>
        /// 三次方缓动函数（Out）- 平滑减速
        /// </summary>
        private float EaseOutCubic(float t)
        {
            float f = t - 1f;
            return f * f * f + 1f;
        }

        /// <summary>
        /// 弹性缓动函数（Out）- 适合交换动画
        /// 产生类似弹簧的弹性效果
        /// </summary>
        private float EaseOutElastic(float t)
        {
            if (t == 0f || t == 1f)
                return t;

            float p = 0.3f;
            float s = p / 4f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - s) * (2f * Mathf.PI) / p) + 1f;
        }

        /// <summary>
        /// 弹跳缓动函数（Out）- 适合下落动画
        /// 产生类似球落地弹跳的效果
        /// </summary>
        private float EaseOutBounce(float t)
        {
            if (t < 1f / 2.75f)
            {
                return 7.5625f * t * t;
            }
            else if (t < 2f / 2.75f)
            {
                t -= 1.5f / 2.75f;
                return 7.5625f * t * t + 0.75f;
            }
            else if (t < 2.5f / 2.75f)
            {
                t -= 2.25f / 2.75f;
                return 7.5625f * t * t + 0.9375f;
            }
            else
            {
                t -= 2.625f / 2.75f;
                return 7.5625f * t * t + 0.984375f;
            }
        }

        /// <summary>
        /// 回退缓动函数（InOut）- 适合特殊效果
        /// 先回退一点，再前进，产生预备动作的效果
        /// </summary>
        private float EaseInOutBack(float t)
        {
            float s = 1.70158f * 1.525f;
            t *= 2f;
            if (t < 1f)
            {
                return 0.5f * (t * t * ((s + 1f) * t - s));
            }
            t -= 2f;
            return 0.5f * (t * t * ((s + 1f) * t + s) + 2f);
        }
    }
}
