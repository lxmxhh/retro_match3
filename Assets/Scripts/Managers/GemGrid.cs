using UnityEngine;
using RetroMatch2D.Core;

namespace RetroMatch2D.Managers
{
    /// <summary>
    /// 宝石网格管理器 - 单例模式
    /// 负责管理整个游戏棋盘的宝石网格
    /// </summary>
    public class GemGrid : MonoBehaviour
    {
        private static GemGrid instance;
        
        /// <summary>
        /// 单例访问器
        /// </summary>
        public static GemGrid Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<GemGrid>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("GemGrid");
                        instance = go.AddComponent<GemGrid>();
                    }
                }
                return instance;
            }
        }

        [Header("棋盘设置")]
        [SerializeField] private int width = 8;
        [SerializeField] private int height = 8;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector3 gridOffset = Vector3.zero;
        
        /// <summary>二维宝石数组</summary>
        private Gem[,] gems;
        
        /// <summary>Board组件引用</summary>
        private Board board;

        private void Awake()
        {
            // 单例模式实现
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 初始化网格
            InitializeGrid();
        }

        /// <summary>
        /// 初始化网格系统
        /// </summary>
        private void InitializeGrid()
        {
            gems = new Gem[width, height];
            
            // 创建或获取Board组件
            board = GetComponent<Board>();
            if (board == null)
            {
                board = gameObject.AddComponent<Board>();
            }
            
            Debug.Log($"GemGrid已初始化: {width}x{height}");
        }

        /// <summary>
        /// 获取指定位置的宝石
        /// </summary>
        public Gem GetGem(int x, int y)
        {
            // 从Board组件读取宝石数据
            if (board != null)
            {
                return board.GetGem(x, y);
            }

            // 如果Board不存在，使用本地数组（兼容性处理）
            if (IsValidPosition(x, y))
            {
                return gems[x, y];
            }
            return null;
        }

        /// <summary>
        /// 设置指定位置的宝石
        /// </summary>
        public void SetGem(int x, int y, Gem gem)
        {
            if (IsValidPosition(x, y))
            {
                gems[x, y] = gem;
                if (gem != null)
                {
                    gem.SetPosition(x, y);
                }
            }
        }

        /// <summary>
        /// 获取相邻宝石（根据方向）
        /// </summary>
        public Gem GetAdjacentGem(int x, int y, Vector2Int direction)
        {
            int newX = x + direction.x;
            int newY = y + direction.y;
            return GetGem(newX, newY);
        }

        /// <summary>
        /// 检查坐标是否有效
        /// </summary>
        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        /// <summary>
        /// 将网格坐标转换为世界坐标
        /// </summary>
        public Vector3 GridToWorldPosition(int x, int y)
        {
            return new Vector3(x * cellSize, y * cellSize, 0) + gridOffset;
        }

        /// <summary>
        /// 将世界坐标转换为网格坐标
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            Vector3 localPos = worldPos - gridOffset;
            int x = Mathf.RoundToInt(localPos.x / cellSize);
            int y = Mathf.RoundToInt(localPos.y / cellSize);
            return new Vector2Int(x, y);
        }

        /// <summary>
        /// 清空整个网格
        /// </summary>
        public void ClearGrid()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (gems[x, y] != null)
                    {
                        Destroy(gems[x, y].gameObject);
                        gems[x, y] = null;
                    }
                }
            }
        }

        /// <summary>
        /// 获取Board引用
        /// </summary>
        public Board GetBoard()
        {
            return board;
        }

        /// <summary>
        /// 属性访问器
        /// </summary>
        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public Vector3 GridOffset => gridOffset;
    }
}
