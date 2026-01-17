using UnityEngine;
using System.Collections.Generic;
using RetroMatch2D.Core;

namespace RetroMatch2D.Managers
{
    /// <summary>
    /// 宝石对象池系统
    /// 复用宝石GameObject，避免频繁创建和销毁导致的性能问题
    /// </summary>
    public class GemPool : MonoBehaviour
    {
        #region Singleton Pattern

        private static GemPool _instance;

        public static GemPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GemPool>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GemPool");
                        _instance = go.AddComponent<GemPool>();
                    }
                }
                return _instance;
            }
        }

        #endregion

        [Header("对象池配置")]
        [SerializeField]
        [Tooltip("初始预生成的宝石数量")]
        private int initialPoolSize = 64; // 8x8棋盘的初始大小

        [SerializeField]
        [Tooltip("对象池扩展时每次增加的数量")]
        private int expandSize = 16;

        [SerializeField]
        [Tooltip("对象池最大容量（0表示无限制）")]
        private int maxPoolSize = 128;

        // 可用的宝石对象队列（已停用）
        private Queue<Gem> availableGems = new Queue<Gem>();

        // 所有创建过的宝石对象（用于追踪和清理）
        private List<Gem> allGems = new List<Gem>();

        // 当前正在使用中的宝石数量
        private int activeGemsCount = 0;

        // Sprite配置（从GameManager传递）
        private Dictionary<GemType, Sprite> gemSprites = new Dictionary<GemType, Sprite>();

        // 统计信息
        private int totalCreatedCount = 0; // 总共创建的宝石数
        private int totalReuseCount = 0;   // 总共复用的次数
        private int peakActiveCount = 0;   // 峰值同时使用数量

        #region 初始化

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 初始化对象池
        /// </summary>
        public void Initialize(Dictionary<GemType, Sprite> sprites)
        {
            gemSprites = sprites;

            // 预生成初始数量的宝石
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewGem();
            }

            Debug.Log($"GemPool: 初始化完成，预生成 {initialPoolSize} 个宝石对象");
        }

        #endregion

        #region 对象池核心方法

        /// <summary>
        /// 从对象池获取一个宝石
        /// </summary>
        public Gem GetGem(GemType type, int x, int y, Transform parent)
        {
            Gem gem;

            // 如果池中有可用对象，复用
            if (availableGems.Count > 0)
            {
                gem = availableGems.Dequeue();
                totalReuseCount++;
            }
            else
            {
                // 检查是否达到最大容量
                if (maxPoolSize > 0 && allGems.Count >= maxPoolSize)
                {
                    Debug.LogWarning($"GemPool: 已达到最大容量 {maxPoolSize}，强制复用最旧的对象");
                    // 这里可以实现更复杂的策略，比如销毁最旧的宝石
                    gem = CreateNewGem();
                }
                else
                {
                    // 创建新对象
                    gem = CreateNewGem();
                }
            }

            // 配置宝石
            gem.gameObject.SetActive(true);
            gem.transform.SetParent(parent);
            gem.Initialize(type, x, y);

            // 设置Sprite
            SpriteRenderer spriteRenderer = gem.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && gemSprites.ContainsKey(type))
            {
                spriteRenderer.sprite = gemSprites[type];
            }

            // 更新统计
            activeGemsCount++;
            if (activeGemsCount > peakActiveCount)
            {
                peakActiveCount = activeGemsCount;
            }

            return gem;
        }

        /// <summary>
        /// 将宝石归还到对象池
        /// </summary>
        public void ReturnGem(Gem gem)
        {
            if (gem == null)
            {
                Debug.LogWarning("GemPool: 尝试归还空的宝石对象");
                return;
            }

            // 停止所有协程（避免动画继续执行）
            gem.StopAllCoroutines();

            // 重置宝石状态
            gem.gameObject.SetActive(false);
            gem.transform.SetParent(transform); // 移到池对象下

            // 重置Transform
            gem.transform.localPosition = Vector3.zero;
            gem.transform.localRotation = Quaternion.identity;
            gem.transform.localScale = Vector3.one;

            // 重置SpriteRenderer
            SpriteRenderer spriteRenderer = gem.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }

            // 归还到池中
            availableGems.Enqueue(gem);
            activeGemsCount--;
        }

        /// <summary>
        /// 创建新的宝石对象
        /// </summary>
        private Gem CreateNewGem()
        {
            GameObject gemObj = new GameObject("PooledGem");
            gemObj.transform.SetParent(transform);
            gemObj.SetActive(false);

            // 添加组件
            SpriteRenderer spriteRenderer = gemObj.AddComponent<SpriteRenderer>();
            Gem gem = gemObj.AddComponent<Gem>();

            // 标记为池化对象
            gem.SetPooled(true);

            // 记录
            allGems.Add(gem);
            totalCreatedCount++;

            return gem;
        }

        #endregion

        #region 扩展和清理

        /// <summary>
        /// 扩展对象池
        /// </summary>
        public void Expand(int count = -1)
        {
            int expandCount = count > 0 ? count : expandSize;

            for (int i = 0; i < expandCount; i++)
            {
                CreateNewGem();
            }

            Debug.Log($"GemPool: 扩展对象池 +{expandCount}，当前总数: {allGems.Count}");
        }

        /// <summary>
        /// 清空对象池（慎用）
        /// </summary>
        public void Clear()
        {
            // 销毁所有宝石对象
            foreach (var gem in allGems)
            {
                if (gem != null && gem.gameObject != null)
                {
                    Destroy(gem.gameObject);
                }
            }

            allGems.Clear();
            availableGems.Clear();
            activeGemsCount = 0;
            totalCreatedCount = 0;
            totalReuseCount = 0;
            peakActiveCount = 0;

            Debug.Log("GemPool: 对象池已清空");
        }

        #endregion

        #region 统计和调试

        /// <summary>
        /// 获取对象池统计信息
        /// </summary>
        public string GetStatistics()
        {
            float reuseRate = totalCreatedCount > 0
                ? (float)totalReuseCount / (totalCreatedCount + totalReuseCount) * 100f
                : 0f;

            return $"GemPool Statistics:\n" +
                   $"  Total Created: {totalCreatedCount}\n" +
                   $"  Total Reused: {totalReuseCount}\n" +
                   $"  Reuse Rate: {reuseRate:F1}%\n" +
                   $"  Available: {availableGems.Count}\n" +
                   $"  Active: {activeGemsCount}\n" +
                   $"  Peak Active: {peakActiveCount}\n" +
                   $"  Pool Capacity: {allGems.Count}";
        }

        /// <summary>
        /// 打印统计信息到控制台
        /// </summary>
        public void LogStatistics()
        {
            Debug.Log(GetStatistics());
        }

        /// <summary>
        /// 获取当前活跃宝石数量
        /// </summary>
        public int GetActiveCount() => activeGemsCount;

        /// <summary>
        /// 获取可用宝石数量
        /// </summary>
        public int GetAvailableCount() => availableGems.Count;

        /// <summary>
        /// 获取对象池总容量
        /// </summary>
        public int GetTotalCapacity() => allGems.Count;

        #endregion

        #region Unity生命周期

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        // 编辑器下显示统计信息
        private void OnGUI()
        {
            if (Application.isPlaying && _instance == this)
            {
                // 可以在这里绘制性能监控UI
                // GUILayout.Label(GetStatistics());
            }
        }

        #endregion
    }
}
